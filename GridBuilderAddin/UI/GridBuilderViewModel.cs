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
    // ── Supporting view-model types ─────────────────────────────────────────

    /// <summary>
    /// Represents a single configurable spacing interval in the X or Y override list.
    /// Raises <see cref="INotifyPropertyChanged"/> so the parent ViewModel can
    /// revalidate and refresh the preview on every keystroke.
    /// </summary>
    public class SpacingIntervalRow : INotifyPropertyChanged
    {
        private string _spacingText;

        /// <summary>Human-readable label shown to the left of the text box, e.g. "1 → 2" or "A → B".</summary>
        public string Label { get; }

        /// <summary>
        /// Text currently entered in the spacing text box. Must parse to a positive double
        /// for the row to be considered valid. Triggers <see cref="IsValid"/> recalculation.
        /// </summary>
        public string SpacingText
        {
            get => _spacingText;
            set
            {
                _spacingText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
            }
        }

        /// <summary><c>true</c> when <see cref="SpacingText"/> represents a positive finite number.</summary>
        public bool IsValid =>
            double.TryParse(SpacingText, out var v) && v > 0 && !double.IsInfinity(v);

        /// <summary>
        /// Parses <see cref="SpacingText"/> and returns the spacing in millimetres.
        /// Callers should check <see cref="IsValid"/> first.
        /// </summary>
        public double ValueMm => double.TryParse(SpacingText, out var v) ? v : 0;

        /// <summary>Initialises the row with the given label and pre-fills the spacing text.</summary>
        public SpacingIntervalRow(string label, double defaultSpacingMm)
        {
            Label = label;
            _spacingText = defaultSpacingMm.ToString("0.##");
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Data for a single line drawn on the live preview <see cref="System.Windows.Controls.Canvas"/>.
    /// Coordinates are in canvas pixels.
    /// </summary>
    public class PreviewLineModel
    {
        /// <summary>Start X in canvas pixels.</summary>
        public double X1 { get; set; }
        /// <summary>Start Y in canvas pixels.</summary>
        public double Y1 { get; set; }
        /// <summary>End X in canvas pixels.</summary>
        public double X2 { get; set; }
        /// <summary>End Y in canvas pixels.</summary>
        public double Y2 { get; set; }
        /// <summary><c>true</c> for X-axis (vertical) grid lines; <c>false</c> for Y-axis (horizontal).</summary>
        public bool IsXGrid { get; set; }
    }

    /// <summary>
    /// Data for a single text label drawn on the live preview canvas.
    /// </summary>
    public class PreviewLabelModel
    {
        /// <summary>Label text (e.g. "1", "A", "AA").</summary>
        public string Text { get; set; } = string.Empty;
        /// <summary>Canvas.Left position in pixels.</summary>
        public double X { get; set; }
        /// <summary>Canvas.Top position in pixels.</summary>
        public double Y { get; set; }
    }

    // ── Main ViewModel ──────────────────────────────────────────────────────

    /// <summary>
    /// MVVM ViewModel for <see cref="GridBuilderWindow"/>.
    /// Implements <see cref="INotifyPropertyChanged"/> and dynamically builds
    /// spacing interval row collections as the user changes grid counts.
    /// No Revit API references — all values are in millimetres.
    /// </summary>
    public class GridBuilderViewModel : INotifyPropertyChanged
    {
        // ── Raw text inputs (validated in ViewModel) ────────────────────────

        private string _xCountText = GridBuilderConstants.DefaultXCount.ToString();
        private string _yCountText = GridBuilderConstants.DefaultYCount.ToString();
        private string _defaultSpacingText = GridBuilderConstants.DefaultSpacingMm.ToString("0.##");

        /// <summary>Text value bound to the X-axis grid count text box.</summary>
        public string XCountText
        {
            get => _xCountText;
            set
            {
                _xCountText = value;
                OnPropertyChanged();
                RebuildXRowsIfCountChanged();
                Revalidate();
            }
        }

        /// <summary>Text value bound to the Y-axis grid count text box.</summary>
        public string YCountText
        {
            get => _yCountText;
            set
            {
                _yCountText = value;
                OnPropertyChanged();
                RebuildYRowsIfCountChanged();
                Revalidate();
            }
        }

        /// <summary>Text value bound to the default spacing text box (millimetres).</summary>
        public string DefaultSpacingText
        {
            get => _defaultSpacingText;
            set
            {
                _defaultSpacingText = value;
                OnPropertyChanged();
                Revalidate();
            }
        }

        // ── Spacing interval row collections ────────────────────────────────

        /// <summary>
        /// Observable collection of X-axis spacing override rows.
        /// Contains <c>XCount − 1</c> rows; each represents one grid interval.
        /// </summary>
        public ObservableCollection<SpacingIntervalRow> XSpacingRows { get; } = new ObservableCollection<SpacingIntervalRow>();

        /// <summary>
        /// Observable collection of Y-axis spacing override rows.
        /// Contains <c>YCount − 1</c> rows; each represents one grid interval.
        /// </summary>
        public ObservableCollection<SpacingIntervalRow> YSpacingRows { get; } = new ObservableCollection<SpacingIntervalRow>();

        // ── Validation ───────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable validation message shown above the footer buttons.
        /// Empty string when the configuration is valid.
        /// </summary>
        public string ValidationMessage { get; private set; } = string.Empty;

        /// <summary><c>true</c> when all inputs are valid and "Create Grids" may be enabled.</summary>
        public bool IsValid { get; private set; }

        // ── Preview ──────────────────────────────────────────────────────────

        /// <summary>Lines to render on the live schematic preview canvas.</summary>
        public ObservableCollection<PreviewLineModel> PreviewLines { get; } = new ObservableCollection<PreviewLineModel>();

        /// <summary>Labels to render on the live schematic preview canvas.</summary>
        public ObservableCollection<PreviewLabelModel> PreviewLabels { get; } = new ObservableCollection<PreviewLabelModel>();

        // ── Commands ─────────────────────────────────────────────────────────

        /// <summary>Bound to the "Create Grids" button; enabled only when <see cref="IsValid"/> is <c>true</c>.</summary>
        public ICommand CreateGridsCommand { get; }

        /// <summary>Bound to the "Cancel" button.</summary>
        public ICommand CancelCommand { get; }

        // ── Dialog result ────────────────────────────────────────────────────

        /// <summary>Set to <c>true</c> when the user confirms; <c>false</c> on cancel.</summary>
        public bool? DialogResult { get; private set; }

        /// <summary>Raised when the ViewModel requests the window to close.</summary>
        public event EventHandler? RequestClose;

        // ── Constructor ──────────────────────────────────────────────────────

        /// <summary>Initialises the ViewModel with default values and builds the initial row collections.</summary>
        public GridBuilderViewModel()
        {
            CreateGridsCommand = new RelayCommand(OnCreateGrids, () => IsValid);
            CancelCommand      = new RelayCommand(OnCancel);

            // Build initial rows using default counts
            RebuildXRows(GridBuilderConstants.DefaultXCount);
            RebuildYRows(GridBuilderConstants.DefaultYCount);

            Revalidate();

            Debug.WriteLine("[GridBuilder] ViewModel initialised with defaults.");
        }

        // ── Command handlers ─────────────────────────────────────────────────

        private void OnCreateGrids()
        {
            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        // ── Build GridConfig ─────────────────────────────────────────────────

        /// <summary>
        /// Constructs a <see cref="GridConfig"/> from the current ViewModel state.
        /// Call only after confirming <see cref="IsValid"/> is <c>true</c>.
        /// </summary>
        public GridConfig BuildConfig()
        {
            int xCount   = int.Parse(XCountText);
            int yCount   = int.Parse(YCountText);
            double defSp = double.Parse(DefaultSpacingText);

            return new GridConfig
            {
                XCount          = xCount,
                YCount          = yCount,
                DefaultSpacingMm = defSp,
                XSpacingsMm     = XSpacingRows.Select(r => r.ValueMm).ToList(),
                YSpacingsMm     = YSpacingRows.Select(r => r.ValueMm).ToList()
            };
        }

        // ── Row rebuild helpers ──────────────────────────────────────────────

        private int _currentXCount;
        private int _currentYCount;

        private void RebuildXRowsIfCountChanged()
        {
            if (!int.TryParse(XCountText, out var count) || count < GridBuilderConstants.MinGridCount)
                return;

            if (count != _currentXCount)
                RebuildXRows(count);
        }

        private void RebuildYRowsIfCountChanged()
        {
            if (!int.TryParse(YCountText, out var count) || count < GridBuilderConstants.MinGridCount)
                return;

            if (count != _currentYCount)
                RebuildYRows(count);
        }

        private void RebuildXRows(int xCount)
        {
            // Unsubscribe from old rows
            foreach (var row in XSpacingRows)
                row.PropertyChanged -= SpacingRow_PropertyChanged;

            XSpacingRows.Clear();

            double defSp = TryParseDefaultSpacing();

            // Interval labels: "1 → 2", "2 → 3", ...
            for (int i = 0; i < xCount - 1; i++)
            {
                var label = $"{i + 1} \u2192 {i + 2}";
                var row   = new SpacingIntervalRow(label, defSp);
                row.PropertyChanged += SpacingRow_PropertyChanged;
                XSpacingRows.Add(row);
            }

            _currentXCount = xCount;
            Debug.WriteLine($"[GridBuilder] Rebuilt {XSpacingRows.Count} X spacing rows for XCount={xCount}.");
        }

        private void RebuildYRows(int yCount)
        {
            foreach (var row in YSpacingRows)
                row.PropertyChanged -= SpacingRow_PropertyChanged;

            YSpacingRows.Clear();

            double defSp = TryParseDefaultSpacing();

            // Interval labels: "A → B", "B → C", ...
            for (int i = 0; i < yCount - 1; i++)
            {
                var label = $"{GetAlphaLabel(i)} \u2192 {GetAlphaLabel(i + 1)}";
                var row   = new SpacingIntervalRow(label, defSp);
                row.PropertyChanged += SpacingRow_PropertyChanged;
                YSpacingRows.Add(row);
            }

            _currentYCount = yCount;
            Debug.WriteLine($"[GridBuilder] Rebuilt {YSpacingRows.Count} Y spacing rows for YCount={yCount}.");
        }

        private void SpacingRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Revalidate();
        }

        // ── Validation & preview ─────────────────────────────────────────────

        private void Revalidate()
        {
            var msg  = ComputeValidationMessage();
            var valid = string.IsNullOrEmpty(msg);

            ValidationMessage = msg;
            IsValid            = valid;

            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(IsValid));
            CommandManager.InvalidateRequerySuggested();

            if (valid)
                UpdatePreview();
        }

        private string ComputeValidationMessage()
        {
            if (!int.TryParse(XCountText, out var xCount) || xCount < GridBuilderConstants.MinGridCount)
                return $"Number of X Grids must be an integer ≥ {GridBuilderConstants.MinGridCount}.";

            if (!int.TryParse(YCountText, out var yCount) || yCount < GridBuilderConstants.MinGridCount)
                return $"Number of Y Grids must be an integer ≥ {GridBuilderConstants.MinGridCount}.";

            if (!double.TryParse(DefaultSpacingText, out var defSp) || defSp <= 0)
                return "Default Spacing must be a positive number.";

            if (XSpacingRows.Any(r => !r.IsValid))
                return "One or more X spacing values are invalid. All spacings must be positive numbers.";

            if (YSpacingRows.Any(r => !r.IsValid))
                return "One or more Y spacing values are invalid. All spacings must be positive numbers.";

            return string.Empty;
        }

        private double TryParseDefaultSpacing()
        {
            return double.TryParse(DefaultSpacingText, out var v) && v > 0
                ? v
                : GridBuilderConstants.DefaultSpacingMm;
        }

        // ── Live preview ─────────────────────────────────────────────────────

        private void UpdatePreview()
        {
            PreviewLines.Clear();
            PreviewLabels.Clear();

            var xSpacings = XSpacingRows.Select(r => r.ValueMm).ToList();
            var ySpacings = YSpacingRows.Select(r => r.ValueMm).ToList();

            double totalX = xSpacings.Sum();   // world mm
            double totalY = ySpacings.Sum();   // world mm (represented positive; actual Y is negative)

            double cw     = GridBuilderConstants.PreviewCanvasWidth;
            double ch     = GridBuilderConstants.PreviewCanvasHeight;
            double margin = GridBuilderConstants.PreviewMarginPx;
            double extPx  = GridBuilderConstants.PreviewExtentPx;

            double drawW = cw - 2 * margin;
            double drawH = ch - 2 * margin;

            // Scale: fit the grid into the drawable area, preserving aspect ratio
            double scaleX = totalX > 0 ? drawW / totalX : 1.0;
            double scaleY = totalY > 0 ? drawH / totalY : 1.0;
            double scale  = Math.Min(scaleX, scaleY);

            // Centre the grid in the canvas
            double gridPxW = totalX * scale;
            double gridPxH = totalY * scale;
            double offX    = margin + (drawW - gridPxW) / 2.0;
            double offY    = margin + (drawH - gridPxH) / 2.0;

            // Accumulate X positions (canvas pixels)
            var xPositions = new List<(double cx, string label)>();
            double cumX = 0;
            int xCount  = XSpacingRows.Count + 1;
            xPositions.Add((offX, "1"));
            for (int i = 0; i < xSpacings.Count; i++)
            {
                cumX += xSpacings[i];
                xPositions.Add((offX + cumX * scale, (i + 2).ToString()));
            }

            // Accumulate Y positions (canvas pixels; Y grids go downward in canvas)
            var yPositions = new List<(double cy, string label)>();
            double cumY = 0;
            int yCount  = YSpacingRows.Count + 1;
            yPositions.Add((offY, GetAlphaLabel(0)));
            for (int i = 0; i < ySpacings.Count; i++)
            {
                cumY += ySpacings[i];
                yPositions.Add((offY + cumY * scale, GetAlphaLabel(i + 1)));
            }

            double lineTop = offY - extPx;
            double lineBot = offY + gridPxH + extPx;
            double lineLeft  = offX - extPx;
            double lineRight = offX + gridPxW + extPx;

            // Draw vertical X grid lines
            foreach (var (cx, label) in xPositions)
            {
                PreviewLines.Add(new PreviewLineModel { X1 = cx, Y1 = lineTop, X2 = cx, Y2 = lineBot, IsXGrid = true });
                PreviewLabels.Add(new PreviewLabelModel { Text = label, X = cx + 2, Y = lineTop - 14 });
            }

            // Draw horizontal Y grid lines
            foreach (var (cy, label) in yPositions)
            {
                PreviewLines.Add(new PreviewLineModel { X1 = lineLeft, Y1 = cy, X2 = lineRight, Y2 = cy, IsXGrid = false });
                PreviewLabels.Add(new PreviewLabelModel { Text = label, X = lineLeft - 18, Y = cy - 7 });
            }

            Debug.WriteLine($"[GridBuilder] Preview updated: {xCount} X grids, {yCount} Y grids.");
        }

        // ── Alpha label helper ───────────────────────────────────────────────

        /// <summary>
        /// Converts a zero-based index to an alphabetical grid label.
        /// 0→"A", 1→"B", …, 25→"Z", 26→"AA", 27→"AB", …, 701→"ZZ", 702→"AAA", etc.
        /// </summary>
        public static string GetAlphaLabel(int zeroBasedIndex)
        {
            var result = string.Empty;
            var n      = zeroBasedIndex + 1; // 1-based
            while (n > 0)
            {
                n--;
                result = (char)('A' + n % 26) + result;
                n     /= 26;
            }
            return result;
        }

        // ── INotifyPropertyChanged ───────────────────────────────────────────

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises <see cref="PropertyChanged"/> for the given property name.</summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ── RelayCommand ────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal <see cref="ICommand"/> implementation that delegates to supplied delegates.
    /// Hooks into <see cref="CommandManager.RequerySuggested"/> for automatic CanExecute refresh.
    /// </summary>
    internal class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        /// <summary>Creates a new relay command.</summary>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc/>
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        /// <inheritdoc/>
        public void Execute(object? parameter) => _execute();

        /// <inheritdoc/>
        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
