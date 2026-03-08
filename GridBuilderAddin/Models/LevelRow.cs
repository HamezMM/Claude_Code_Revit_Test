// GridBuilderAddin | Revit 2024 | net48
// Pure C# model — no Revit API dependency.
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GridBuilderAddin.Models
{
    /// <summary>
    /// Represents a single level row in the Level Builder UI.
    /// Implements <see cref="INotifyPropertyChanged"/> so it can be bound directly
    /// in the ItemsControl without a wrapper ViewModel.
    /// </summary>
    public class LevelRow : INotifyPropertyChanged
    {
        // ── Backing fields ────────────────────────────────────────────────────
        private string _name          = string.Empty;
        private string _elevationText = string.Empty;
        private double _elevationMm   = 0.0;

        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>
        /// Revit internal element ID for an existing level; <c>-1</c> for newly added levels
        /// that have not yet been written to the model.
        /// </summary>
        public long RevitId { get; }

        /// <summary>
        /// <c>true</c> when this level already exists in the Revit model.
        /// Shown in red in the UI to indicate the user should not delete it.
        /// </summary>
        public bool IsExisting { get; }

        // ── Editable fields ───────────────────────────────────────────────────

        /// <summary>Level name — editable for both existing and new levels.</summary>
        public string Name
        {
            get => _name;
            set { _name = value ?? string.Empty; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); }
        }

        /// <summary>
        /// Elevation input text in millimetres (can be negative for levels below origin).
        /// Setting this updates <see cref="ElevationMm"/> when the text is parseable.
        /// </summary>
        public string ElevationText
        {
            get => _elevationText;
            set
            {
                _elevationText = value ?? string.Empty;
                if (double.TryParse(_elevationText, out var mm))
                    _elevationMm = mm;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ElevationMm));
                OnPropertyChanged(nameof(IsValidElevation));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        /// <summary>Parsed elevation in millimetres. Only reliable when <see cref="IsValidElevation"/> is <c>true</c>.</summary>
        public double ElevationMm => _elevationMm;

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary><c>true</c> when <see cref="ElevationText"/> parses to a finite number.</summary>
        public bool IsValidElevation =>
            double.TryParse(_elevationText, out var v) && !double.IsInfinity(v) && !double.IsNaN(v);

        /// <summary><c>true</c> when both the name is non-empty and the elevation is valid.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(_name) && IsValidElevation;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates a <see cref="LevelRow"/> with the given name and elevation.
        /// </summary>
        /// <param name="name">Level name.</param>
        /// <param name="elevationMm">Elevation in millimetres.</param>
        /// <param name="isExisting"><c>true</c> for levels already in the Revit model.</param>
        /// <param name="revitId">Revit element ID; pass <c>-1</c> for new levels.</param>
        public LevelRow(string name, double elevationMm, bool isExisting = false, long revitId = -1)
        {
            _name          = name ?? throw new ArgumentNullException(nameof(name));
            _elevationMm   = elevationMm;
            _elevationText = elevationMm.ToString("0.##");
            IsExisting     = isExisting;
            RevitId        = revitId;
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
