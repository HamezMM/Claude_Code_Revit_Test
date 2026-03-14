// GridBuilderAddin | Revit 2024 | net48
using GridBuilderAddin.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GridBuilderAddin.UI
{
    // ── Preview model types for vertical elevation diagram ────────────────────

    /// <summary>A single horizontal line in the level schematic preview.</summary>
    public class LevelPreviewLine
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public bool IsExisting { get; set; }
    }

    /// <summary>A text label in the level schematic preview.</summary>
    public class LevelPreviewLabel
    {
        public string Text { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsVertical { get; set; }
        public bool IsExisting { get; set; }
    }

    // ── LevelBuilderViewModel ─────────────────────────────────────────────────

    /// <summary>
    /// MVVM ViewModel for <see cref="LevelBuilderWindow"/>.
    /// Allows the user to view, rename, re-elevate, add, and delete levels.
    /// Existing Revit levels are shown in red and cannot be deleted.
    /// No Revit API references — all values are stored and returned in millimetres.
    /// </summary>
    public class LevelBuilderViewModel : INotifyPropertyChanged
    {
        // ── Level collection ──────────────────────────────────────────────────

        /// <summary>Observable collection of all level rows shown in the UI.</summary>
        public ObservableCollection<LevelRow> Levels { get; } = new ObservableCollection<LevelRow>();

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>Human-readable validation message shown above footer buttons.</summary>
        public string ValidationMessage { get; private set; } = string.Empty;

        /// <summary><c>true</c> when all levels are valid and "Next" may proceed.</summary>
        public bool IsValid { get; private set; }

        // ── Summary ───────────────────────────────────────────────────────────

        /// <summary>Total building height from lowest to highest level, formatted in mm.</summary>
        public string TotalHeightText { get; private set; } = string.Empty;

        // ── Preview ───────────────────────────────────────────────────────────

        /// <summary>Horizontal lines to draw on the elevation schematic preview.</summary>
        public ObservableCollection<LevelPreviewLine>  PreviewLines  { get; } = new ObservableCollection<LevelPreviewLine>();

        /// <summary>Text labels to draw on the elevation schematic preview.</summary>
        public ObservableCollection<LevelPreviewLabel> PreviewLabels { get; } = new ObservableCollection<LevelPreviewLabel>();

        // ── Close action ──────────────────────────────────────────────────────

        /// <summary>Set when the window closes; read by <c>GridBuilderCmd</c> to drive the flow.</summary>
        public WindowCloseAction CloseAction { get; private set; } = WindowCloseAction.Cancelled;

        /// <summary>Raised when the ViewModel requests the window to close.</summary>
        public event EventHandler? RequestClose;

        // ── Commands ──────────────────────────────────────────────────────────

        /// <summary>Adds a new blank level above the current highest level.</summary>
        public ICommand AddLevelCommand { get; }

        /// <summary>Removes the specified non-existing level row.</summary>
        public ICommand DeleteLevelCommand { get; }

        /// <summary>Proceeds to the Structure Builder step.</summary>
        public ICommand NextCommand { get; }

        /// <summary>Returns to the Grid Builder step.</summary>
        public ICommand BackCommand { get; }

        /// <summary>Cancels the entire Building Builder flow.</summary>
        public ICommand CancelCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the ViewModel with the levels currently in the Revit model.
        /// </summary>
        /// <param name="existingLevels">
        /// Level rows fetched from the Revit document by <c>LevelBuilderService.FetchExistingLevels</c>.
        /// These are displayed first and shown in red.
        /// </param>
        public LevelBuilderViewModel(IEnumerable<LevelRow> existingLevels)
        {
            AddLevelCommand    = new RelayCommand(OnAddLevel);
            DeleteLevelCommand = new RelayCommand<LevelRow>(OnDeleteLevel, r => r != null && !r.IsExisting);
            NextCommand        = new RelayCommand(OnNext, () => IsValid);
            BackCommand        = new RelayCommand(OnBack);
            CancelCommand      = new RelayCommand(OnCancel);

            // Load existing levels and subscribe to their changes
            foreach (var row in existingLevels ?? Enumerable.Empty<LevelRow>())
            {
                row.PropertyChanged += Row_PropertyChanged;
                Levels.Add(row);
            }

            // If no levels exist (empty project), seed a default Level 1 at elevation 0
            if (!Levels.Any())
            {
                var seed = new LevelRow("Level 1", 0.0);
                seed.PropertyChanged += Row_PropertyChanged;
                Levels.Add(seed);
            }

            Revalidate();
            Debug.WriteLine($"[LevelBuilder] ViewModel initialised with {Levels.Count} level(s).");
        }

        // ── Command handlers ──────────────────────────────────────────────────

        private void OnAddLevel()
        {
            // Place new level above the current highest elevation
            double newElevation = Levels.Count > 0
                ? Levels.Max(r => r.IsValidElevation ? r.ElevationMm : 0.0)
                  + GridBuilderConstants.DefaultFloorToFloorMm
                : 0.0;

            int newIndex = Levels.Count + 1;
            var row = new LevelRow($"Level {newIndex}", newElevation);
            row.PropertyChanged += Row_PropertyChanged;
            Levels.Add(row);
            Revalidate();
            Debug.WriteLine($"[LevelBuilder] Added new level at {newElevation:F0} mm.");
        }

        private void OnDeleteLevel(LevelRow row)
        {
            if (row == null || row.IsExisting) return;
            row.PropertyChanged -= Row_PropertyChanged;
            Levels.Remove(row);
            Revalidate();
            Debug.WriteLine($"[LevelBuilder] Deleted level \"{row.Name}\".");
        }

        private void OnNext()
        {
            CloseAction = WindowCloseAction.Confirmed;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnBack()
        {
            CloseAction = WindowCloseAction.GoBack;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancel()
        {
            CloseAction = WindowCloseAction.Cancelled;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        // ── Public data accessor ──────────────────────────────────────────────

        /// <summary>Returns the current level list as a plain <see cref="List{T}"/>.</summary>
        public List<LevelRow> GetLevels() => Levels.ToList();

        // ── Validation & preview ──────────────────────────────────────────────

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e) => Revalidate();

        private void Revalidate()
        {
            var msg   = ComputeValidationMessage();
            var valid = string.IsNullOrEmpty(msg);

            ValidationMessage = msg;
            IsValid            = valid;

            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(IsValid));
            CommandManager.InvalidateRequerySuggested();

            UpdateTotalHeight();
            if (valid) UpdatePreview();
        }

        private string ComputeValidationMessage()
        {
            if (!Levels.Any())
                return "At least one level is required.";

            if (Levels.Any(r => !r.IsValid))
                return "One or more levels have invalid names or elevations.";

            // Duplicate names
            var names = Levels.Select(r => r.Name.Trim().ToLowerInvariant()).ToList();
            if (names.Count != names.Distinct().Count())
                return "Level names must be unique.";

            // Duplicate elevations
            var validElevations = Levels.Where(r => r.IsValidElevation).Select(r => r.ElevationMm).ToList();
            if (validElevations.Count != validElevations.Distinct().Count())
                return "Level elevations must be unique.";

            return string.Empty;
        }

        private void UpdateTotalHeight()
        {
            var valid = Levels.Where(r => r.IsValidElevation).ToList();
            if (valid.Count < 2)
            {
                TotalHeightText = string.Empty;
                OnPropertyChanged(nameof(TotalHeightText));
                return;
            }

            double totalMm = valid.Max(r => r.ElevationMm) - valid.Min(r => r.ElevationMm);
            TotalHeightText = $"Total Height: {totalMm:N0} mm";
            OnPropertyChanged(nameof(TotalHeightText));
        }

        // ── Elevation schematic preview ───────────────────────────────────────

        private const double CanvasWidth   = 340.0;
        private const double CanvasHeight  = 220.0;
        private const double MarginTop     = 20.0;
        private const double MarginBottom  = 20.0;
        private const double MarginLeft    = 100.0;  // room for level name labels
        private const double MarginRight   = 90.0;   // room for elevation labels + total annotation
        private const double LineXStart    = MarginLeft;
        private const double LineXEnd      = CanvasWidth - MarginRight;

        private void UpdatePreview()
        {
            PreviewLines.Clear();
            PreviewLabels.Clear();

            var sortedLevels = Levels
                .Where(r => r.IsValidElevation)
                .OrderBy(r => r.ElevationMm)
                .ToList();

            if (sortedLevels.Count < 1) return;

            double minElev  = sortedLevels.First().ElevationMm;
            double maxElev  = sortedLevels.Last().ElevationMm;
            double range    = maxElev - minElev;
            double drawH    = CanvasHeight - MarginTop - MarginBottom;

            // scale: pixels per mm (avoid divide-by-zero when all on same elevation)
            double scale = range > 0 ? drawH / range : 1.0;

            foreach (var row in sortedLevels)
            {
                // In canvas coords: Y=0 is top; higher elevation = smaller canvas Y
                double canvasY = MarginTop + (maxElev - row.ElevationMm) * scale;

                PreviewLines.Add(new LevelPreviewLine
                {
                    X1         = LineXStart,
                    Y1         = canvasY,
                    X2         = LineXEnd,
                    Y2         = canvasY,
                    IsExisting = row.IsExisting
                });

                // Name label on the left
                PreviewLabels.Add(new LevelPreviewLabel
                {
                    Text       = row.Name,
                    X          = 2,
                    Y          = canvasY - 8,
                    IsExisting = row.IsExisting
                });

                // Elevation label on the right
                PreviewLabels.Add(new LevelPreviewLabel
                {
                    Text       = $"{row.ElevationMm:N0} mm",
                    X          = LineXEnd + 4,
                    Y          = canvasY - 8,
                    IsExisting = row.IsExisting
                });
            }

            // Overall height annotation — rotated label between first and last level
            if (sortedLevels.Count >= 2 && range > 0)
            {
                double topY    = MarginTop;
                double botY    = CanvasHeight - MarginBottom;
                double midY    = (topY + botY) / 2.0;

                PreviewLabels.Add(new LevelPreviewLabel
                {
                    Text       = $"{range:N0} mm",
                    X          = CanvasWidth - MarginRight + 48,
                    Y          = midY,
                    IsVertical = true,
                    IsExisting = false
                });
            }

            Debug.WriteLine($"[LevelBuilder] Preview updated: {sortedLevels.Count} level(s).");
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
