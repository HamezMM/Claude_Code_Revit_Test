// RoomDocumenter | Revit 2024 | net48
using Autodesk.Revit.DB;
using RoomDocumenter.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RoomDocumenter.UI
{
    // ─────────────────────────────────────────────────────────────────────
    // Supporting types
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A selectable Revit element type (FloorType, CeilingType, WallSweepType)
    /// displayed in a dropdown.
    /// </summary>
    public class TypeItem
    {
        /// <summary>Display name shown in the dropdown.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Underlying ElementId.Value for serialisation.</summary>
        public long IdValue { get; set; }

        /// <summary>Underlying ElementId (not serialised directly).</summary>
        public ElementId ElementId => new ElementId(IdValue);

        /// <inheritdoc />
        public override string ToString() => Name;
    }

    /// <summary>
    /// One row in a finish-mapping DataGrid.
    /// Binds a discovered finish string to a user-selected Revit type.
    /// </summary>
    public class FinishMappingRow : INotifyPropertyChanged
    {
        private TypeItem? _selectedType;

        /// <summary>Finish string discovered in the project.</summary>
        public string FinishString { get; set; } = string.Empty;

        /// <summary>Available Revit types for this finish category.</summary>
        public ObservableCollection<TypeItem> AvailableTypes { get; set; }
            = new ObservableCollection<TypeItem>();

        /// <summary>Currently selected Revit type; null = unmapped.</summary>
        public TypeItem? SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(); }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ViewModel
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ViewModel for <see cref="FinishMappingDialog"/>.
    /// Exposes four tab collections (Floor, Ceiling, Baseboard, Wall) and a
    /// Save command that builds and returns the <see cref="FinishMapping"/> POCO.
    /// No Revit API calls are made here; all data is injected by the Command.
    /// </summary>
    public class FinishMappingViewModel : INotifyPropertyChanged
    {
        // ── Observable row collections per tab ────────────────────────────

        /// <summary>Floor finish string → FloorType rows.</summary>
        public ObservableCollection<FinishMappingRow> FloorRows    { get; }
            = new ObservableCollection<FinishMappingRow>();

        /// <summary>Ceiling finish string → CeilingType rows.</summary>
        public ObservableCollection<FinishMappingRow> CeilingRows  { get; }
            = new ObservableCollection<FinishMappingRow>();

        /// <summary>Base finish string → WallSweepType rows.</summary>
        public ObservableCollection<FinishMappingRow> BaseboardRows { get; }
            = new ObservableCollection<FinishMappingRow>();

        /// <summary>
        /// Wall finish string rows (no type assignment in this version;
        /// present for visibility and future use).
        /// </summary>
        public ObservableCollection<FinishMappingRow> WallRows      { get; }
            = new ObservableCollection<FinishMappingRow>();

        // ── Commands ──────────────────────────────────────────────────────

        /// <summary>
        /// Save command.  Sets <see cref="ResultMapping"/> and closes the dialog
        /// via the <see cref="CloseRequested"/> event.
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>Cancel command — closes the dialog without saving.</summary>
        public ICommand CancelCommand { get; }

        // ── Result ────────────────────────────────────────────────────────

        /// <summary>
        /// Set by <see cref="SaveCommand"/>.  Null until the user confirms.
        /// </summary>
        public FinishMapping? ResultMapping { get; private set; }

        // ── Events ────────────────────────────────────────────────────────

        /// <summary>Raised by Save/Cancel to signal the view to close.</summary>
        public event Action<bool>? CloseRequested;

        // ─────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the ViewModel and populates rows from the injected data.
        /// </summary>
        /// <param name="finishStrings">
        /// Unique finish strings per category, keyed by "Floor"/"Ceiling"/"Wall"/"Base".
        /// </param>
        /// <param name="floorTypes">Available FloorType items.</param>
        /// <param name="ceilingTypes">Available CeilingType items.</param>
        /// <param name="wallSweepTypes">Available WallSweepType items.</param>
        /// <param name="existingMapping">
        /// Previously persisted mapping to pre-populate selections; may be null.
        /// </param>
        public FinishMappingViewModel(
            Dictionary<string, List<string>> finishStrings,
            IList<TypeItem> floorTypes,
            IList<TypeItem> ceilingTypes,
            IList<TypeItem> wallSweepTypes,
            FinishMapping? existingMapping)
        {
            PopulateRows(FloorRows,    finishStrings["Floor"],
                         floorTypes,   existingMapping?.FloorMappings);
            PopulateRows(CeilingRows,  finishStrings["Ceiling"],
                         ceilingTypes, existingMapping?.CeilingMappings);
            PopulateRows(BaseboardRows, finishStrings["Base"],
                         wallSweepTypes, existingMapping?.BaseboardMappings);
            PopulateRows(WallRows, finishStrings["Wall"],
                         Array.Empty<TypeItem>(), existingMapping?.WallMappings);

            SaveCommand   = new RelayCommand(_ => OnSave());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(false));
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private static void PopulateRows(
            ObservableCollection<FinishMappingRow> rows,
            IEnumerable<string> finishStrings,
            IEnumerable<TypeItem> types,
            Dictionary<string, long>? existing)
        {
            rows.Clear();
            var typeList = types?.ToList() ?? new List<TypeItem>();

            foreach (var fs in finishStrings)
            {
                var row = new FinishMappingRow { FinishString = fs };
                foreach (var t in typeList) row.AvailableTypes.Add(t);

                if (existing != null && existing.TryGetValue(fs, out long idVal))
                    row.SelectedType = typeList.FirstOrDefault(t => t.IdValue == idVal);

                rows.Add(row);
            }
        }

        private void OnSave()
        {
            var mapping = new FinishMapping();

            foreach (var row in FloorRows)
                if (row.SelectedType != null)
                    mapping.FloorMappings[row.FinishString] = row.SelectedType.IdValue;

            foreach (var row in CeilingRows)
                if (row.SelectedType != null)
                    mapping.CeilingMappings[row.FinishString] = row.SelectedType.IdValue;

            foreach (var row in BaseboardRows)
                if (row.SelectedType != null)
                    mapping.BaseboardMappings[row.FinishString] = row.SelectedType.IdValue;

            foreach (var row in WallRows)
                if (row.SelectedType != null)
                    mapping.WallMappings[row.FinishString] = row.SelectedType.IdValue;

            ResultMapping = mapping;
            CloseRequested?.Invoke(true);
        }

        // ── INotifyPropertyChanged ─────────────────────────────────────────

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Minimal RelayCommand
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Simple ICommand implementation for ViewModel commands.</summary>
    internal sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        /// <inheritdoc />
        public void Execute(object? parameter) => _execute(parameter);

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
