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
    // ── Unit mode enum ───────────────────────────────────────────────────────

    /// <summary>
    /// Controls whether spacing values are entered in millimetres or in
    /// feet + decimal inches (imperial).
    /// </summary>
    public enum GridUnitMode
    {
        /// <summary>All spacing values in millimetres.</summary>
        Millimeters,
        /// <summary>All spacing values as whole feet + decimal inches (0–11.99).</summary>
        FeetAndInches
    }

    // ── SpacingIntervalRow ────────────────────────────────────────────────────

    /// <summary>
    /// Represents a single configurable spacing interval in the X or Y override list.
    /// Supports both millimetre and feet-and-inches input; the active mode is driven
    /// by the parent ViewModel via the <see cref="UnitMode"/> property.
    /// </summary>
    public class SpacingIntervalRow : INotifyPropertyChanged
    {
        // ── Backing fields ────────────────────────────────────────────────────
        private string       _spacingText      = string.Empty;
        private string       _feetText         = string.Empty;
        private string       _inchesText       = string.Empty;
        private GridUnitMode _unitMode         = GridUnitMode.Millimeters;
        private bool         _isManualOverride = false;

        // ── Label ─────────────────────────────────────────────────────────────

        /// <summary>Human-readable label shown to the left of the input fields, e.g. "1 → 2" or "A → B".</summary>
        public string Label { get; }

        // ── Manual override flag ──────────────────────────────────────────────

        /// <summary>
        /// <c>true</c> when the user has manually edited this row's spacing, overriding the
        /// typical default. Set to <c>false</c> when the row is initialised or reset via
        /// <see cref="ResetToDefault"/>. The Refresh command skips rows where this is <c>true</c>.
        /// </summary>
        public bool IsManualOverride
        {
            get => _isManualOverride;
            private set { _isManualOverride = value; OnPropertyChanged(); }
        }

        // ── Unit mode ─────────────────────────────────────────────────────────

        /// <summary>
        /// Current unit mode for this row. Set by the parent ViewModel when the user
        /// switches units. Triggers <see cref="IsValid"/> and helper bool recalculations.
        /// </summary>
        public GridUnitMode UnitMode
        {
            get => _unitMode;
            set
            {
                _unitMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMmMode));
                OnPropertyChanged(nameof(IsFtInMode));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        /// <summary><c>true</c> when <see cref="UnitMode"/> is <see cref="GridUnitMode.Millimeters"/>.</summary>
        public bool IsMmMode   => _unitMode == GridUnitMode.Millimeters;

        /// <summary><c>true</c> when <see cref="UnitMode"/> is <see cref="GridUnitMode.FeetAndInches"/>.</summary>
        public bool IsFtInMode => _unitMode == GridUnitMode.FeetAndInches;

        // ── Millimetre field ──────────────────────────────────────────────────

        /// <summary>
        /// Text value for the millimetre input field. Setting this via the property setter
        /// (i.e. from WPF binding / user input) marks the row as <see cref="IsManualOverride"/>.
        /// </summary>
        public string SpacingText
        {
            get => _spacingText;
            set { _spacingText = value; _isManualOverride = true; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); }
        }

        // ── Feet and Inches fields ────────────────────────────────────────────

        /// <summary>
        /// Whole-feet part of the spacing in <see cref="GridUnitMode.FeetAndInches"/> mode.
        /// Must parse to a non-negative integer. Setting raises <see cref="IsManualOverride"/>.
        /// </summary>
        public string FeetText
        {
            get => _feetText;
            set { _feetText = value; _isManualOverride = true; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); }
        }

        /// <summary>
        /// Decimal-inches part of the spacing in <see cref="GridUnitMode.FeetAndInches"/> mode.
        /// Must parse to a <see cref="double"/> in the range [0, 12). Setting raises <see cref="IsManualOverride"/>.
        /// </summary>
        public string InchesText
        {
            get => _inchesText;
            set { _inchesText = value; _isManualOverride = true; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); }
        }

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        /// <c>true</c> when the active mode's input fields contain a valid, positive spacing.
        /// </summary>
        public bool IsValid => _unitMode == GridUnitMode.Millimeters ? IsValidMm : IsValidFtIn;

        private bool IsValidMm =>
            double.TryParse(_spacingText, out var v) && v > 0 && !double.IsInfinity(v);

        private bool IsValidFtIn =>
            int.TryParse(_feetText, out var f) && f >= 0
            && double.TryParse(_inchesText, out var i) && i >= 0 && i < 12
            && (f > 0 || i > 0);   // total spacing must be positive

        // ── Computed value ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the spacing value in millimetres from whichever field set is active.
        /// Returns 0 if the inputs are invalid. Callers should check <see cref="IsValid"/> first.
        /// </summary>
        public double ValueMm
        {
            get
            {
                if (_unitMode == GridUnitMode.Millimeters)
                    return double.TryParse(_spacingText, out var v) ? v : 0.0;

                int.TryParse(_feetText,      out var f);
                double.TryParse(_inchesText, out var i);
                return f * 304.8 + i * 25.4;
            }
        }

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the row with the given label and pre-fills both input sets
        /// from the supplied default spacing in millimetres. <see cref="IsManualOverride"/>
        /// is <c>false</c> after construction.
        /// </summary>
        /// <param name="label">Human-readable interval label (e.g. "1 → 2"). Must not be null.</param>
        /// <param name="defaultSpacingMm">Initial spacing value in millimetres.</param>
        /// <param name="unitMode">Active unit mode at construction time.</param>
        public SpacingIntervalRow(string label, double defaultSpacingMm, GridUnitMode unitMode = GridUnitMode.Millimeters)
        {
            Label    = label ?? throw new ArgumentNullException(nameof(label));
            _unitMode = unitMode;
            SetFromMm(defaultSpacingMm);
            // _isManualOverride stays false — SetFromMm writes to backing fields directly
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Populates both the mm and ft-in backing fields from a value in millimetres.
        /// Does <b>not</b> set <see cref="IsManualOverride"/>. Called on construction and
        /// whenever the unit mode changes.
        /// </summary>
        public void SetFromMm(double mm)
        {
            _spacingText = mm.ToString("0.##");

            var totalInches = mm / 25.4;
            var feet        = (int)(totalInches / 12);
            var inches      = Math.Round(totalInches % 12, 2);

            _feetText   = feet.ToString();
            _inchesText = inches.ToString("0.##");
        }

        /// <summary>
        /// Resets this row to the given typical spacing value, clearing
        /// <see cref="IsManualOverride"/>. Called by the Refresh command on non-overridden rows.
        /// </summary>
        public void ResetToDefault(double mm)
        {
            _isManualOverride = false;
            SetFromMm(mm);
            OnPropertyChanged(nameof(SpacingText));
            OnPropertyChanged(nameof(FeetText));
            OnPropertyChanged(nameof(InchesText));
            OnPropertyChanged(nameof(IsManualOverride));
            OnPropertyChanged(nameof(IsValid));
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Preview model types ───────────────────────────────────────────────────

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
        /// <summary>Label text (e.g. "1", "A", "32,000 mm").</summary>
        public string Text { get; set; } = string.Empty;
        /// <summary>Canvas.Left position in pixels.</summary>
        public double X { get; set; }
        /// <summary>Canvas.Top position in pixels.</summary>
        public double Y { get; set; }
        /// <summary>
        /// When <c>true</c>, the XAML DataTemplate applies a 90° clockwise rotation so
        /// the label reads top-to-bottom. Used for the Y-axis overall dimension annotation
        /// which appears to the right of the grid where horizontal space is limited.
        /// </summary>
        public bool IsVertical { get; set; }
    }

    // ── Main ViewModel ────────────────────────────────────────────────────────

    /// <summary>
    /// MVVM ViewModel for <see cref="GridBuilderWindow"/>.
    /// Implements <see cref="INotifyPropertyChanged"/> and dynamically builds
    /// spacing interval row collections as the user changes grid counts or unit mode.
    /// No Revit API references — all values are stored and returned in millimetres.
    /// </summary>
    public class GridBuilderViewModel : INotifyPropertyChanged
    {
        // ── Unit mode ─────────────────────────────────────────────────────────

        private GridUnitMode _unitMode = GridUnitMode.Millimeters;

        /// <summary>
        /// Active unit mode. Changing this converts all existing spacing values and
        /// propagates the new mode to all spacing interval rows.
        /// </summary>
        public GridUnitMode UnitMode
        {
            get => _unitMode;
            set
            {
                if (_unitMode == value) return;
                ConvertAllRowsOnModeChange();
                _unitMode = value;
                PropagateUnitModeToRows();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMmMode));
                OnPropertyChanged(nameof(IsFtInMode));
                OnPropertyChanged(nameof(SpacingUnitLabel));
                OnPropertyChanged(nameof(DefaultSpacingLabel));
                Revalidate();
            }
        }

        /// <summary>
        /// <c>true</c> when <see cref="UnitMode"/> is <see cref="GridUnitMode.Millimeters"/>.
        /// Two-way bindable; setting <c>true</c> sets <see cref="UnitMode"/> to Millimeters.
        /// </summary>
        public bool IsMmMode
        {
            get => _unitMode == GridUnitMode.Millimeters;
            set { if (value) UnitMode = GridUnitMode.Millimeters; }
        }

        /// <summary>
        /// <c>true</c> when <see cref="UnitMode"/> is <see cref="GridUnitMode.FeetAndInches"/>.
        /// Two-way bindable; setting <c>true</c> sets <see cref="UnitMode"/> to FeetAndInches.
        /// </summary>
        public bool IsFtInMode
        {
            get => _unitMode == GridUnitMode.FeetAndInches;
            set { if (value) UnitMode = GridUnitMode.FeetAndInches; }
        }

        /// <summary>Unit suffix shown in spacing override section headings.</summary>
        public string SpacingUnitLabel =>
            _unitMode == GridUnitMode.Millimeters ? "mm per interval" : "ft-in per interval";

        /// <summary>Dynamic heading for the X spacing overrides card, including total building width.</summary>
        public string XSpacingHeading
        {
            get
            {
                var heading = $"X SPACING OVERRIDES  ({SpacingUnitLabel})";
                if (XSpacingRows.Count > 0 && XSpacingRows.All(r => r.IsValid))
                    heading += $"  —  Total: {FormatDimension(XSpacingRows.Sum(r => r.ValueMm))}";
                return heading;
            }
        }

        /// <summary>Dynamic heading for the Y spacing overrides card, including total building depth.</summary>
        public string YSpacingHeading
        {
            get
            {
                var heading = $"Y SPACING OVERRIDES  ({SpacingUnitLabel})";
                if (YSpacingRows.Count > 0 && YSpacingRows.All(r => r.IsValid))
                    heading += $"  —  Total: {FormatDimension(YSpacingRows.Sum(r => r.ValueMm))}";
                return heading;
            }
        }

        /// <summary>Label shown next to the default spacing field — kept for backward compatibility.</summary>
        public string DefaultSpacingLabel =>
            _unitMode == GridUnitMode.Millimeters ? "Default Spacing (mm)" : "Default Spacing";

        // ── Raw text inputs ───────────────────────────────────────────────────

        private string _xCountText          = GridBuilderConstants.DefaultXCount.ToString();
        private string _yCountText          = GridBuilderConstants.DefaultYCount.ToString();

        // X typical spacing
        private string _xDefaultSpacingText = GridBuilderConstants.DefaultSpacingMm.ToString("0.##");
        private string _xDefaultFeetText    = GridBuilderConstants.DefaultFeet.ToString();
        private string _xDefaultInchesText  = GridBuilderConstants.DefaultInches.ToString("0.##");

        // Y typical spacing (independent of X)
        private string _yDefaultSpacingText = GridBuilderConstants.DefaultSpacingMm.ToString("0.##");
        private string _yDefaultFeetText    = GridBuilderConstants.DefaultFeet.ToString();
        private string _yDefaultInchesText  = GridBuilderConstants.DefaultInches.ToString("0.##");

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

        // ── X typical spacing properties ──────────────────────────────────────

        /// <summary>Text value for the X typical spacing in millimetre mode.</summary>
        public string XDefaultSpacingText
        {
            get => _xDefaultSpacingText;
            set { _xDefaultSpacingText = value; OnPropertyChanged(); Revalidate(); }
        }

        /// <summary>Whole-feet part of the X typical spacing in feet-and-inches mode.</summary>
        public string XDefaultFeetText
        {
            get => _xDefaultFeetText;
            set { _xDefaultFeetText = value; OnPropertyChanged(); Revalidate(); }
        }

        /// <summary>Decimal-inches part of the X typical spacing (0–11.99) in feet-and-inches mode.</summary>
        public string XDefaultInchesText
        {
            get => _xDefaultInchesText;
            set { _xDefaultInchesText = value; OnPropertyChanged(); Revalidate(); }
        }

        // ── Y typical spacing properties ──────────────────────────────────────

        /// <summary>Text value for the Y typical spacing in millimetre mode.</summary>
        public string YDefaultSpacingText
        {
            get => _yDefaultSpacingText;
            set { _yDefaultSpacingText = value; OnPropertyChanged(); Revalidate(); }
        }

        /// <summary>Whole-feet part of the Y typical spacing in feet-and-inches mode.</summary>
        public string YDefaultFeetText
        {
            get => _yDefaultFeetText;
            set { _yDefaultFeetText = value; OnPropertyChanged(); Revalidate(); }
        }

        /// <summary>Decimal-inches part of the Y typical spacing (0–11.99) in feet-and-inches mode.</summary>
        public string YDefaultInchesText
        {
            get => _yDefaultInchesText;
            set { _yDefaultInchesText = value; OnPropertyChanged(); Revalidate(); }
        }

        // ── Spacing interval row collections ──────────────────────────────────

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

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable validation message shown above the footer buttons.
        /// Empty string when the configuration is valid.
        /// </summary>
        public string ValidationMessage { get; private set; } = string.Empty;

        /// <summary><c>true</c> when all inputs are valid and "Create Grids" may be enabled.</summary>
        public bool IsValid { get; private set; }

        // ── Preview ───────────────────────────────────────────────────────────

        /// <summary>Lines to render on the live schematic preview canvas.</summary>
        public ObservableCollection<PreviewLineModel>  PreviewLines     { get; } = new ObservableCollection<PreviewLineModel>();

        /// <summary>Grid line labels to render on the live schematic preview canvas.</summary>
        public ObservableCollection<PreviewLabelModel> PreviewLabels    { get; } = new ObservableCollection<PreviewLabelModel>();

        /// <summary>Overall dimension annotations rendered on the live schematic preview canvas.</summary>
        public ObservableCollection<PreviewLabelModel> PreviewDimLabels { get; } = new ObservableCollection<PreviewLabelModel>();

        // ── Building Builder mode ─────────────────────────────────────────────

        private bool _isBuildingBuilderEnabled = false;

        /// <summary>
        /// When <c>true</c> the tool enters multi-step Building Builder mode (Grid → Level → Structure).
        /// The footer primary button changes from "Create Grids" to "Next →" and a step indicator appears.
        /// </summary>
        public bool IsBuildingBuilderEnabled
        {
            get => _isBuildingBuilderEnabled;
            set
            {
                if (_isBuildingBuilderEnabled == value) return;
                _isBuildingBuilderEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PrimaryButtonText));
                OnPropertyChanged(nameof(StepIndicatorText));
                OnPropertyChanged(nameof(ShowStepIndicator));
            }
        }

        /// <summary>Text for the primary footer button — changes based on Building Builder mode.</summary>
        public string PrimaryButtonText => _isBuildingBuilderEnabled ? "Next →" : "Create Grids";

        /// <summary>Step indicator text shown when Building Builder mode is active.</summary>
        public string StepIndicatorText => "Step 1 of 3  —  Grid Builder";

        /// <summary><c>true</c> when the step indicator should be visible.</summary>
        public bool ShowStepIndicator => _isBuildingBuilderEnabled;

        // ── Commands ──────────────────────────────────────────────────────────

        /// <summary>Bound to the primary footer button; enabled only when <see cref="IsValid"/> is <c>true</c>.</summary>
        public ICommand CreateGridsCommand { get; }

        /// <summary>Bound to the "Cancel" button.</summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Re-applies the X typical spacing to all X intervals that have not been manually overridden.
        /// Intervals where <see cref="SpacingIntervalRow.IsManualOverride"/> is <c>true</c> are skipped.
        /// </summary>
        public ICommand RefreshXDefaultCommand { get; }

        /// <summary>
        /// Re-applies the Y typical spacing to all Y intervals that have not been manually overridden.
        /// Intervals where <see cref="SpacingIntervalRow.IsManualOverride"/> is <c>true</c> are skipped.
        /// </summary>
        public ICommand RefreshYDefaultCommand { get; }

        // ── Dialog result ─────────────────────────────────────────────────────

        /// <summary>Set to <c>true</c> when the user confirms; <c>false</c> on cancel.</summary>
        public bool? DialogResult { get; private set; }

        /// <summary>Raised when the ViewModel requests the window to close.</summary>
        public event EventHandler? RequestClose;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>Initialises the ViewModel with default values and builds the initial row collections.</summary>
        public GridBuilderViewModel()
        {
            CreateGridsCommand     = new RelayCommand(OnCreateGrids, () => IsValid);
            CancelCommand          = new RelayCommand(OnCancel);
            RefreshXDefaultCommand = new RelayCommand(OnRefreshXDefault);
            RefreshYDefaultCommand = new RelayCommand(OnRefreshYDefault);

            RebuildXRows(GridBuilderConstants.DefaultXCount);
            RebuildYRows(GridBuilderConstants.DefaultYCount);
            Revalidate();

            Debug.WriteLine("[GridBuilder] ViewModel initialised with defaults.");
        }

        // ── Command handlers ──────────────────────────────────────────────────

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

        private void OnRefreshXDefault()
        {
            var mm = TryParseXDefaultSpacingMm();
            foreach (var row in XSpacingRows)
                if (!row.IsManualOverride)
                    row.ResetToDefault(mm);
            Revalidate();
        }

        private void OnRefreshYDefault()
        {
            var mm = TryParseYDefaultSpacingMm();
            foreach (var row in YSpacingRows)
                if (!row.IsManualOverride)
                    row.ResetToDefault(mm);
            Revalidate();
        }

        // ── Build GridConfig ──────────────────────────────────────────────────

        /// <summary>
        /// Constructs a <see cref="GridConfig"/> from the current ViewModel state.
        /// All spacing values in the returned config are always in millimetres,
        /// regardless of which unit mode is active. Call only when <see cref="IsValid"/> is <c>true</c>.
        /// </summary>
        public GridConfig BuildConfig()
        {
            return new GridConfig
            {
                XCount           = int.Parse(XCountText),
                YCount           = int.Parse(YCountText),
                DefaultSpacingMm = TryParseXDefaultSpacingMm(),
                XSpacingsMm      = XSpacingRows.Select(r => r.ValueMm).ToList(),
                YSpacingsMm      = YSpacingRows.Select(r => r.ValueMm).ToList()
            };
        }

        // ── Row rebuild helpers ───────────────────────────────────────────────

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
            foreach (var row in XSpacingRows)
                row.PropertyChanged -= SpacingRow_PropertyChanged;
            XSpacingRows.Clear();

            var defMm = TryParseXDefaultSpacingMm();
            for (int i = 0; i < xCount - 1; i++)
            {
                var row = new SpacingIntervalRow($"{i + 1} \u2192 {i + 2}", defMm, _unitMode);
                row.PropertyChanged += SpacingRow_PropertyChanged;
                XSpacingRows.Add(row);
            }

            _currentXCount = xCount;
            Debug.WriteLine($"[GridBuilder] Rebuilt {XSpacingRows.Count} X rows for XCount={xCount}.");
        }

        private void RebuildYRows(int yCount)
        {
            foreach (var row in YSpacingRows)
                row.PropertyChanged -= SpacingRow_PropertyChanged;
            YSpacingRows.Clear();

            var defMm = TryParseYDefaultSpacingMm();
            for (int i = 0; i < yCount - 1; i++)
            {
                var row = new SpacingIntervalRow($"{GetAlphaLabel(i)} \u2192 {GetAlphaLabel(i + 1)}", defMm, _unitMode);
                row.PropertyChanged += SpacingRow_PropertyChanged;
                YSpacingRows.Add(row);
            }

            _currentYCount = yCount;
            Debug.WriteLine($"[GridBuilder] Rebuilt {YSpacingRows.Count} Y rows for YCount={yCount}.");
        }

        private void SpacingRow_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
            Revalidate();

        // ── Unit mode switching ───────────────────────────────────────────────

        /// <summary>
        /// Captures the current mm values from every row and both default spacing fields,
        /// then writes converted values back via <see cref="SpacingIntervalRow.SetFromMm"/>
        /// so both backing-field sets are populated before the mode flag flips.
        /// </summary>
        private void ConvertAllRowsOnModeChange()
        {
            var xMmValues  = XSpacingRows.Select(r => r.ValueMm).ToList();
            var yMmValues  = YSpacingRows.Select(r => r.ValueMm).ToList();
            var xDefaultMm = TryParseXDefaultSpacingMm();
            var yDefaultMm = TryParseYDefaultSpacingMm();

            for (int i = 0; i < XSpacingRows.Count; i++) XSpacingRows[i].SetFromMm(xMmValues[i]);
            for (int i = 0; i < YSpacingRows.Count; i++) YSpacingRows[i].SetFromMm(yMmValues[i]);

            // Sync X default display fields for both modes
            _xDefaultSpacingText = xDefaultMm.ToString("0.##");
            var xTotalIn         = xDefaultMm / 25.4;
            _xDefaultFeetText    = ((int)(xTotalIn / 12)).ToString();
            _xDefaultInchesText  = Math.Round(xTotalIn % 12, 2).ToString("0.##");

            // Sync Y default display fields for both modes
            _yDefaultSpacingText = yDefaultMm.ToString("0.##");
            var yTotalIn         = yDefaultMm / 25.4;
            _yDefaultFeetText    = ((int)(yTotalIn / 12)).ToString();
            _yDefaultInchesText  = Math.Round(yTotalIn % 12, 2).ToString("0.##");

            OnPropertyChanged(nameof(XDefaultSpacingText));
            OnPropertyChanged(nameof(XDefaultFeetText));
            OnPropertyChanged(nameof(XDefaultInchesText));
            OnPropertyChanged(nameof(YDefaultSpacingText));
            OnPropertyChanged(nameof(YDefaultFeetText));
            OnPropertyChanged(nameof(YDefaultInchesText));
        }

        /// <summary>Pushes the current <see cref="_unitMode"/> to every spacing row.</summary>
        private void PropagateUnitModeToRows()
        {
            foreach (var row in XSpacingRows) row.UnitMode = _unitMode;
            foreach (var row in YSpacingRows) row.UnitMode = _unitMode;
        }

        // ── Validation & preview ──────────────────────────────────────────────

        private void Revalidate()
        {
            var msg   = ComputeValidationMessage();
            var valid = string.IsNullOrEmpty(msg);

            ValidationMessage = msg;
            IsValid            = valid;

            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(XSpacingHeading));
            OnPropertyChanged(nameof(YSpacingHeading));
            CommandManager.InvalidateRequerySuggested();

            if (valid) UpdatePreview();
        }

        private string ComputeValidationMessage()
        {
            if (!int.TryParse(XCountText, out var xCount) || xCount < GridBuilderConstants.MinGridCount)
                return $"Number of X Grids must be an integer \u2265 {GridBuilderConstants.MinGridCount}.";

            if (!int.TryParse(YCountText, out var yCount) || yCount < GridBuilderConstants.MinGridCount)
                return $"Number of Y Grids must be an integer \u2265 {GridBuilderConstants.MinGridCount}.";

            if (_unitMode == GridUnitMode.Millimeters)
            {
                if (!double.TryParse(XDefaultSpacingText, out var xSp) || xSp <= 0)
                    return "Default X Spacing must be a positive number.";
                if (!double.TryParse(YDefaultSpacingText, out var ySp) || ySp <= 0)
                    return "Default Y Spacing must be a positive number.";
            }
            else
            {
                if (!int.TryParse(XDefaultFeetText, out var xdf) || xdf < 0
                    || !double.TryParse(XDefaultInchesText, out var xdi) || xdi < 0 || xdi >= 12
                    || (xdf == 0 && xdi <= 0))
                    return "Default X Spacing must be a positive ft-in value (inches: 0 \u2013 11.99).";
                if (!int.TryParse(YDefaultFeetText, out var ydf) || ydf < 0
                    || !double.TryParse(YDefaultInchesText, out var ydi) || ydi < 0 || ydi >= 12
                    || (ydf == 0 && ydi <= 0))
                    return "Default Y Spacing must be a positive ft-in value (inches: 0 \u2013 11.99).";
            }

            if (XSpacingRows.Any(r => !r.IsValid))
                return "One or more X spacing values are invalid.";

            if (YSpacingRows.Any(r => !r.IsValid))
                return "One or more Y spacing values are invalid.";

            return string.Empty;
        }

        private double TryParseXDefaultSpacingMm()
        {
            if (_unitMode == GridUnitMode.Millimeters)
            {
                return double.TryParse(_xDefaultSpacingText, out var v) && v > 0
                    ? v
                    : GridBuilderConstants.DefaultSpacingMm;
            }

            int.TryParse(_xDefaultFeetText,    out var f);
            double.TryParse(_xDefaultInchesText, out var i);
            var computed = f * 304.8 + i * 25.4;
            return computed > 0 ? computed : GridBuilderConstants.DefaultSpacingMm;
        }

        private double TryParseYDefaultSpacingMm()
        {
            if (_unitMode == GridUnitMode.Millimeters)
            {
                return double.TryParse(_yDefaultSpacingText, out var v) && v > 0
                    ? v
                    : GridBuilderConstants.DefaultSpacingMm;
            }

            int.TryParse(_yDefaultFeetText,    out var f);
            double.TryParse(_yDefaultInchesText, out var i);
            var computed = f * 304.8 + i * 25.4;
            return computed > 0 ? computed : GridBuilderConstants.DefaultSpacingMm;
        }

        // ── Live preview ──────────────────────────────────────────────────────

        private void UpdatePreview()
        {
            PreviewLines.Clear();
            PreviewLabels.Clear();
            PreviewDimLabels.Clear();

            var xSpacings = XSpacingRows.Select(r => r.ValueMm).ToList();
            var ySpacings = YSpacingRows.Select(r => r.ValueMm).ToList();

            double totalX = xSpacings.Sum();
            double totalY = ySpacings.Sum();

            double cw     = GridBuilderConstants.PreviewCanvasWidth;
            double ch     = GridBuilderConstants.PreviewCanvasHeight;
            double margin = GridBuilderConstants.PreviewMarginPx;
            double extPx  = GridBuilderConstants.PreviewExtentPx;

            double drawW  = cw - 2 * margin;
            double drawH  = ch - 2 * margin;

            double scaleX = totalX > 0 ? drawW / totalX : 1.0;
            double scaleY = totalY > 0 ? drawH / totalY : 1.0;
            double scale  = Math.Min(scaleX, scaleY);

            double gridPxW = totalX * scale;
            double gridPxH = totalY * scale;
            double offX    = margin + (drawW - gridPxW) / 2.0;
            double offY    = margin + (drawH - gridPxH) / 2.0;

            // X grid line positions (vertical)
            var xPositions = new List<(double cx, string label)> { (offX, "1") };
            double cumX    = 0;
            for (int i = 0; i < xSpacings.Count; i++)
            {
                cumX += xSpacings[i];
                xPositions.Add((offX + cumX * scale, (i + 2).ToString()));
            }

            // Y grid line positions (horizontal)
            var yPositions = new List<(double cy, string label)> { (offY, GetAlphaLabel(0)) };
            double cumY    = 0;
            for (int i = 0; i < ySpacings.Count; i++)
            {
                cumY += ySpacings[i];
                yPositions.Add((offY + cumY * scale, GetAlphaLabel(i + 1)));
            }

            double lineTop   = offY - extPx;
            double lineBot   = offY + gridPxH + extPx;
            double lineLeft  = offX - extPx;
            double lineRight = offX + gridPxW + extPx;

            foreach (var (cx, label) in xPositions)
            {
                PreviewLines.Add(new PreviewLineModel { X1 = cx, Y1 = lineTop, X2 = cx, Y2 = lineBot, IsXGrid = true });
                PreviewLabels.Add(new PreviewLabelModel { Text = label, X = cx + 2, Y = lineTop - 14 });
            }

            foreach (var (cy, label) in yPositions)
            {
                PreviewLines.Add(new PreviewLineModel { X1 = lineLeft, Y1 = cy, X2 = lineRight, Y2 = cy, IsXGrid = false });
                PreviewLabels.Add(new PreviewLabelModel { Text = label, X = lineLeft - 18, Y = cy - 7 });
            }

            // ── Overall dimension annotations ─────────────────────────────────
            if (totalX > 0)
            {
                var xMidPx = (xPositions[0].cx + xPositions[xPositions.Count - 1].cx) / 2.0;
                PreviewDimLabels.Add(new PreviewLabelModel
                {
                    Text = FormatDimension(totalX),
                    X    = xMidPx - 28,
                    Y    = lineBot + 5
                });
            }

            if (totalY > 0)
            {
                var yMidPx = (yPositions[0].cy + yPositions[yPositions.Count - 1].cy) / 2.0;
                PreviewDimLabels.Add(new PreviewLabelModel
                {
                    Text       = FormatDimension(totalY),
                    X          = lineRight + 4,
                    Y          = yMidPx,
                    IsVertical = true   // rotated 90° so text reads downward without clipping
                });
            }

            Debug.WriteLine($"[GridBuilder] Preview updated: {xPositions.Count} X, {yPositions.Count} Y.");
        }

        private string FormatDimension(double mm)
        {
            if (_unitMode == GridUnitMode.Millimeters)
                return $"{mm:N0} mm";

            var totalIn = mm / 25.4;
            var ft      = (int)(totalIn / 12);
            var inch    = Math.Round(totalIn % 12, 2);
            return inch > 0
                ? $"{ft} ft {inch:0.##} in"
                : $"{ft} ft";
        }

        // ── Alpha label helper ────────────────────────────────────────────────

        /// <summary>
        /// Converts a zero-based index to an alphabetical grid label.
        /// 0→"A", 25→"Z", 26→"AA", 701→"ZZ", 702→"AAA", etc.
        /// </summary>
        public static string GetAlphaLabel(int zeroBasedIndex)
        {
            var result = string.Empty;
            var n      = zeroBasedIndex + 1;
            while (n > 0)
            {
                n--;
                result = (char)('A' + n % 26) + result;
                n     /= 26;
            }
            return result;
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises <see cref="PropertyChanged"/> for the given property name.</summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ── WindowCloseAction ─────────────────────────────────────────────────────

    /// <summary>
    /// Signals how a Building Builder dialog window was closed.
    /// Used by <see cref="GridBuilderAddin.Commands.GridBuilderCmd"/> to drive the multi-step flow.
    /// </summary>
    public enum WindowCloseAction
    {
        /// <summary>User confirmed and wants to proceed to the next step.</summary>
        Confirmed,
        /// <summary>User cancelled; abort the entire Building Builder flow.</summary>
        Cancelled,
        /// <summary>User wants to return to the previous step.</summary>
        GoBack
    }

    // ── RelayCommand ──────────────────────────────────────────────────────────

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

    // ── RelayCommand<T> ───────────────────────────────────────────────────────

    /// <summary>
    /// Generic <see cref="ICommand"/> implementation that passes a typed parameter to the execute delegate.
    /// </summary>
    internal class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        /// <summary>Creates a new parameterised relay command.</summary>
        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc/>
        public bool CanExecute(object? parameter) =>
            parameter is T t ? (_canExecute?.Invoke(t) ?? true) : false;

        /// <inheritdoc/>
        public void Execute(object? parameter)
        {
            if (parameter is T t) _execute(t);
        }

        /// <inheritdoc/>
        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
