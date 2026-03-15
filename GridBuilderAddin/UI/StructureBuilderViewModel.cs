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
    /// <summary>
    /// MVVM ViewModel for <see cref="StructureBuilderWindow"/>.
    /// Exposes all user-configurable options for floor, roof, exterior walls, and structural columns.
    /// No Revit API references — all values are returned in millimetres and as element ID longs.
    /// </summary>
    public class StructureBuilderViewModel : INotifyPropertyChanged
    {
        // ── Available family type collections ─────────────────────────────────

        public ObservableCollection<FamilyTypeItem> FloorTypes   { get; } = new ObservableCollection<FamilyTypeItem>();
        public ObservableCollection<FamilyTypeItem> WallTypes    { get; } = new ObservableCollection<FamilyTypeItem>();
        public ObservableCollection<FamilyTypeItem> RoofTypes    { get; } = new ObservableCollection<FamilyTypeItem>();
        public ObservableCollection<FamilyTypeItem> ColumnTypes  { get; } = new ObservableCollection<FamilyTypeItem>();
        public ObservableCollection<FamilyTypeItem> LevelItems   { get; } = new ObservableCollection<FamilyTypeItem>();

        // ── Floor ─────────────────────────────────────────────────────────────

        private FamilyTypeItem? _selectedFloorType;
        public FamilyTypeItem? SelectedFloorType
        {
            get => _selectedFloorType;
            set { _selectedFloorType = value; OnPropertyChanged(); Revalidate(); }
        }

        private FamilyTypeItem? _floorLevel;
        public FamilyTypeItem? FloorLevel
        {
            get => _floorLevel;
            set { _floorLevel = value; OnPropertyChanged(); Revalidate(); }
        }

        private string _floorOffsetText = "0";
        public string FloorOffsetText
        {
            get => _floorOffsetText;
            set { _floorOffsetText = value; OnPropertyChanged(); Revalidate(); }
        }

        // ── Roof ──────────────────────────────────────────────────────────────

        private FamilyTypeItem? _selectedRoofType;
        public FamilyTypeItem? SelectedRoofType
        {
            get => _selectedRoofType;
            set { _selectedRoofType = value; OnPropertyChanged(); Revalidate(); }
        }

        private FamilyTypeItem? _roofLevel;
        public FamilyTypeItem? RoofLevel
        {
            get => _roofLevel;
            set { _roofLevel = value; OnPropertyChanged(); Revalidate(); }
        }

        private string _roofOffsetText = "0";
        public string RoofOffsetText
        {
            get => _roofOffsetText;
            set { _roofOffsetText = value; OnPropertyChanged(); Revalidate(); }
        }

        // ── Exterior walls ────────────────────────────────────────────────────

        private FamilyTypeItem? _selectedWallType;
        public FamilyTypeItem? SelectedWallType
        {
            get => _selectedWallType;
            set { _selectedWallType = value; OnPropertyChanged(); Revalidate(); }
        }

        private string _wallExteriorOffsetText = "0";
        /// <summary>Exterior wall offset from perimeter grid in mm (≥ 0).</summary>
        public string WallExteriorOffsetText
        {
            get => _wallExteriorOffsetText;
            set { _wallExteriorOffsetText = value; OnPropertyChanged(); Revalidate(); }
        }

        private FamilyTypeItem? _wallBottomLevel;
        public FamilyTypeItem? WallBottomLevel
        {
            get => _wallBottomLevel;
            set { _wallBottomLevel = value; OnPropertyChanged(); Revalidate(); }
        }

        private string _wallBottomOffsetText = "0";
        public string WallBottomOffsetText
        {
            get => _wallBottomOffsetText;
            set { _wallBottomOffsetText = value; OnPropertyChanged(); Revalidate(); }
        }

        private FamilyTypeItem? _wallTopLevel;
        public FamilyTypeItem? WallTopLevel
        {
            get => _wallTopLevel;
            set { _wallTopLevel = value; OnPropertyChanged(); Revalidate(); }
        }

        private string _wallTopOffsetText = "0";
        public string WallTopOffsetText
        {
            get => _wallTopOffsetText;
            set { _wallTopOffsetText = value; OnPropertyChanged(); Revalidate(); }
        }

        // ── Column shared level constraints ───────────────────────────────────

        private FamilyTypeItem? _columnBottomLevel;
        public FamilyTypeItem? ColumnBottomLevel
        {
            get => _columnBottomLevel;
            set { _columnBottomLevel = value; OnPropertyChanged(); Revalidate(); }
        }

        private string _columnBottomOffsetText = "0";
        public string ColumnBottomOffsetText
        {
            get => _columnBottomOffsetText;
            set { _columnBottomOffsetText = value; OnPropertyChanged(); Revalidate(); }
        }

        private FamilyTypeItem? _columnTopLevel;
        public FamilyTypeItem? ColumnTopLevel
        {
            get => _columnTopLevel;
            set { _columnTopLevel = value; OnPropertyChanged(); Revalidate(); }
        }

        private string _columnTopOffsetText = "0";
        public string ColumnTopOffsetText
        {
            get => _columnTopOffsetText;
            set { _columnTopOffsetText = value; OnPropertyChanged(); Revalidate(); }
        }

        // ── Perimeter columns ─────────────────────────────────────────────────
        // Perimeter columns are always placed — no optional toggle.

        private FamilyTypeItem? _perimeterColumnType;
        public FamilyTypeItem? PerimeterColumnType
        {
            get => _perimeterColumnType;
            set { _perimeterColumnType = value; OnPropertyChanged(); Revalidate(); }
        }

        private string _perimeterOffsetText = "0";
        /// <summary>Interior offset of perimeter columns from the grid line, in mm (≥ 0).</summary>
        public string PerimeterOffsetText
        {
            get => _perimeterOffsetText;
            set { _perimeterOffsetText = value; OnPropertyChanged(); Revalidate(); }
        }

        // ── Midpoint perimeter columns ────────────────────────────────────────

        private bool _hasMidpointColumns = false;
        public bool HasMidpointColumns
        {
            get => _hasMidpointColumns;
            set { _hasMidpointColumns = value; OnPropertyChanged(); Revalidate(); }
        }

        private FamilyTypeItem? _midpointColumnType;
        public FamilyTypeItem? MidpointColumnType
        {
            get => _midpointColumnType;
            set { _midpointColumnType = value; OnPropertyChanged(); Revalidate(); }
        }

        // ── Interior columns ──────────────────────────────────────────────────
        // Interior (field) columns are always placed — no optional toggle.

        private FamilyTypeItem? _interiorColumnType;
        public FamilyTypeItem? InteriorColumnType
        {
            get => _interiorColumnType;
            set { _interiorColumnType = value; OnPropertyChanged(); Revalidate(); }
        }

        // ── Validation ────────────────────────────────────────────────────────

        public string ValidationMessage { get; private set; } = string.Empty;
        public bool   IsValid           { get; private set; }

        // ── Close action ──────────────────────────────────────────────────────

        public WindowCloseAction CloseAction { get; private set; } = WindowCloseAction.Cancelled;
        public event EventHandler? RequestClose;

        // ── Commands ──────────────────────────────────────────────────────────

        public ICommand BuildCommand  { get; }
        public ICommand BackCommand   { get; }
        public ICommand CancelCommand { get; }

        // ── Captured config (snapshot taken in OnBuild before window teardown) ─

        private StructureConfig? _capturedConfig;

        // ── Constructor ───────────────────────────────────────────────────────

        public StructureBuilderViewModel(
            IEnumerable<FamilyTypeItem> floorTypes,
            IEnumerable<FamilyTypeItem> wallTypes,
            IEnumerable<FamilyTypeItem> roofTypes,
            IEnumerable<FamilyTypeItem> columnTypes,
            IEnumerable<FamilyTypeItem> levelItems)
        {
            BuildCommand  = new RelayCommand(OnBuild, () => IsValid);
            BackCommand   = new RelayCommand(OnBack);
            CancelCommand = new RelayCommand(OnCancel);

            foreach (var x in floorTypes  ?? Enumerable.Empty<FamilyTypeItem>()) FloorTypes.Add(x);
            foreach (var x in wallTypes   ?? Enumerable.Empty<FamilyTypeItem>()) WallTypes.Add(x);
            foreach (var x in roofTypes   ?? Enumerable.Empty<FamilyTypeItem>()) RoofTypes.Add(x);
            foreach (var x in columnTypes ?? Enumerable.Empty<FamilyTypeItem>()) ColumnTypes.Add(x);
            foreach (var x in levelItems  ?? Enumerable.Empty<FamilyTypeItem>()) LevelItems.Add(x);

            // Pre-select first available items
            SelectedFloorType    = FloorTypes.FirstOrDefault();
            SelectedRoofType     = RoofTypes.FirstOrDefault();
            SelectedWallType     = WallTypes.FirstOrDefault();
            PerimeterColumnType  = ColumnTypes.FirstOrDefault();
            InteriorColumnType   = ColumnTypes.FirstOrDefault();

            // Pre-select bottom and top levels sensibly
            ColumnBottomLevel = WallBottomLevel = FloorLevel = LevelItems.FirstOrDefault();
            ColumnTopLevel    = WallTopLevel    = RoofLevel  = LevelItems.LastOrDefault();

            Revalidate();
            Debug.WriteLine("[StructureBuilder] ViewModel initialised.");
        }

        // ── Post-load initialisation ──────────────────────────────────────────

        /// <summary>
        /// Called from <see cref="StructureBuilderWindow"/> after the <c>Loaded</c> event fires.
        /// Re-applies default selections for any property that WPF nulled during binding
        /// initialisation (e.g. TwoWay SelectedItem write-back when ItemsSource first loads,
        /// or shared-CollectionView current-item synchronisation for the LevelItems pickers).
        /// </summary>
        public void OnLoaded()
        {
            if (SelectedFloorType   == null) SelectedFloorType   = FloorTypes.FirstOrDefault();
            if (FloorLevel          == null) FloorLevel          = LevelItems.FirstOrDefault();

            if (SelectedRoofType    == null) SelectedRoofType    = RoofTypes.FirstOrDefault();
            if (RoofLevel           == null) RoofLevel           = LevelItems.LastOrDefault();

            if (SelectedWallType    == null) SelectedWallType    = WallTypes.FirstOrDefault();
            if (WallBottomLevel     == null) WallBottomLevel     = LevelItems.FirstOrDefault();
            if (WallTopLevel        == null) WallTopLevel        = LevelItems.LastOrDefault();

            if (ColumnBottomLevel   == null) ColumnBottomLevel   = LevelItems.FirstOrDefault();
            if (ColumnTopLevel      == null) ColumnTopLevel      = LevelItems.LastOrDefault();

            if (PerimeterColumnType == null) PerimeterColumnType = ColumnTypes.FirstOrDefault();
            if (InteriorColumnType  == null) InteriorColumnType  = ColumnTypes.FirstOrDefault();

            Debug.WriteLine("[StructureBuilder] OnLoaded — selections refreshed after binding initialisation.");
        }

        // ── Command handlers ──────────────────────────────────────────────────

        private void OnBuild()
        {
            // Snapshot the config NOW, before WPF window teardown writes null back
            // through TwoWay ComboBox.SelectedItem bindings during Close().
            _capturedConfig = BuildConfig();
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

        // ── Build config ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="StructureConfig"/> captured when the user clicked Build.
        /// If called before the window confirms (unusual), falls back to reading live state.
        /// </summary>
        public StructureConfig BuildConfig() => _capturedConfig ?? BuildConfigFromState();

        /// <summary>Reads the current ViewModel state into a new <see cref="StructureConfig"/>.</summary>
        private StructureConfig BuildConfigFromState()
        {
            double.TryParse(FloorOffsetText,       out var floorOff);
            double.TryParse(RoofOffsetText,        out var roofOff);
            double.TryParse(WallExteriorOffsetText, out var wallExtOff);
            double.TryParse(WallBottomOffsetText,  out var wallBotOff);
            double.TryParse(WallTopOffsetText,     out var wallTopOff);
            double.TryParse(ColumnBottomOffsetText, out var colBotOff);
            double.TryParse(ColumnTopOffsetText,   out var colTopOff);
            double.TryParse(PerimeterOffsetText,   out var perimOff);

            return new StructureConfig
            {
                FloorTypeId              = SelectedFloorType?.Id ?? 0,
                FloorLevelId             = FloorLevel?.Id        ?? 0,
                FloorLevelOffsetMm       = floorOff,

                RoofTypeId               = SelectedRoofType?.Id  ?? 0,
                RoofLevelId              = RoofLevel?.Id         ?? 0,
                RoofLevelOffsetMm        = roofOff,

                WallTypeId               = SelectedWallType?.Id  ?? 0,
                WallExteriorOffsetMm     = Math.Max(0, wallExtOff),
                WallBottomLevelId        = WallBottomLevel?.Id   ?? 0,
                WallBottomOffsetMm       = wallBotOff,
                WallTopLevelId           = WallTopLevel?.Id      ?? 0,
                WallTopOffsetMm          = wallTopOff,

                ColumnBottomLevelId      = ColumnBottomLevel?.Id ?? 0,
                ColumnBottomOffsetMm     = colBotOff,
                ColumnTopLevelId         = ColumnTopLevel?.Id    ?? 0,
                ColumnTopOffsetMm        = colTopOff,

                HasPerimeterColumns      = true,   // always placed
                PerimeterColumnTypeId    = PerimeterColumnType?.Id ?? 0,
                PerimeterInteriorOffsetMm = Math.Max(0, perimOff),

                HasMidpointColumns       = _hasMidpointColumns,
                MidpointColumnTypeId     = MidpointColumnType?.Id ?? 0,

                HasInteriorColumns       = true,   // always placed
                InteriorColumnTypeId     = InteriorColumnType?.Id ?? 0
            };
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void Revalidate()
        {
            var msg = ComputeValidationMessage();
            IsValid            = string.IsNullOrEmpty(msg);
            ValidationMessage  = msg;
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(ValidationMessage));
            CommandManager.InvalidateRequerySuggested();
        }

        private string ComputeValidationMessage()
        {
            if (SelectedFloorType == null)
                return "Select a floor type.";
            if (FloorLevel == null)
                return "Select a host level for the floor.";

            if (SelectedRoofType == null)
                return "Select a roof type.";
            if (RoofLevel == null)
                return "Select a host level for the roof.";

            if (SelectedWallType == null)
                return "Select a wall type.";
            if (!double.TryParse(WallExteriorOffsetText, out var wallExt) || wallExt < 0)
                return "Wall exterior offset must be a non-negative number.";
            if (WallBottomLevel == null)
                return "Select a bottom level for exterior walls.";
            if (WallTopLevel == null)
                return "Select a top level for exterior walls.";

            // Perimeter and interior columns are always required.
            // Midpoint (half-grid perimeter) columns are optional.
            if (ColumnBottomLevel == null)
                return "Select a base level for structural columns.";
            if (ColumnTopLevel == null)
                return "Select a top level for structural columns.";

            if (PerimeterColumnType == null)
                return "Select a column type for perimeter columns.";
            if (!double.TryParse(PerimeterOffsetText, out var perimOff) || perimOff < 0)
                return "Perimeter column offset must be a non-negative number.";

            if (_hasMidpointColumns && MidpointColumnType == null)
                return "Select a column type for midpoint perimeter columns.";

            if (InteriorColumnType == null)
                return "Select a column type for interior (field) columns.";

            return string.Empty;
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
