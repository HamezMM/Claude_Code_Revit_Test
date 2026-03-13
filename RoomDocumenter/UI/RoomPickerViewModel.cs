// RoomDocumenter | Revit 2024 | net48
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
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
    // Room row item
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// One selectable row in the room picker list.
    /// </summary>
    public class RoomPickerItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        /// <summary>Revit ElementId of the room.</summary>
        public ElementId RoomId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Room Number parameter value.</summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>Room Name parameter value.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Name of the room's associated Level.</summary>
        public string LevelName { get; set; } = string.Empty;

        /// <summary>Combined display label for search and list.</summary>
        public string DisplayLabel => $"{Number} — {Name}";

        /// <summary>Whether this room is checked for processing.</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
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
    /// ViewModel for <see cref="RoomPickerDialog"/>.
    /// Exposes a searchable, multi-selectable list of rooms and a numeric
    /// crop offset in millimetres.  Raises <see cref="CloseRequested"/> when
    /// the user confirms or cancels.
    /// </summary>
    public class RoomPickerViewModel : INotifyPropertyChanged
    {
        private string _searchText    = string.Empty;
        private double _cropOffsetMm  = 300.0;
        private string _validationMsg = string.Empty;

        private readonly ObservableCollection<RoomPickerItem> _allRooms;

        // ── Filtered view ─────────────────────────────────────────────────

        /// <summary>Rooms currently visible in the list (filtered by search text).</summary>
        public ObservableCollection<RoomPickerItem> FilteredRooms { get; }
            = new ObservableCollection<RoomPickerItem>();

        // ── Properties ────────────────────────────────────────────────────

        /// <summary>
        /// Text typed into the search box; updates <see cref="FilteredRooms"/>.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                RefreshFilter();
            }
        }

        /// <summary>
        /// Crop expansion in millimetres applied to the room boundary on all sides.
        /// Default: 300 mm.
        /// </summary>
        public double CropOffsetMm
        {
            get => _cropOffsetMm;
            set { _cropOffsetMm = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Inline validation message shown when the user tries to confirm with
        /// no rooms selected.
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMsg;
            private set { _validationMsg = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────────────

        /// <summary>Confirms the selection and closes the dialog.</summary>
        public ICommand ConfirmCommand  { get; }

        /// <summary>Cancels and closes the dialog.</summary>
        public ICommand CancelCommand   { get; }

        /// <summary>Selects all rooms currently visible in the filtered list.</summary>
        public ICommand SelectAllCommand  { get; }

        /// <summary>Deselects all rooms currently visible in the filtered list.</summary>
        public ICommand ClearAllCommand   { get; }

        // ── Results ───────────────────────────────────────────────────────

        /// <summary>
        /// Rooms the user confirmed for processing.  Populated when
        /// <see cref="CloseRequested"/> fires with <c>true</c>.
        /// </summary>
        public IList<RoomPickerItem> SelectedRooms { get; private set; }
            = Array.Empty<RoomPickerItem>();

        // ── Events ────────────────────────────────────────────────────────

        /// <summary>Raised with <c>true</c> on confirm, <c>false</c> on cancel.</summary>
        public event Action<bool>? CloseRequested;

        // ─────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the ViewModel with all placed rooms from the document.
        /// </summary>
        /// <param name="allRooms">All placed rooms (area &gt; 0) in the project.</param>
        public RoomPickerViewModel(IList<RoomPickerItem> allRooms)
        {
            _allRooms = new ObservableCollection<RoomPickerItem>(
                allRooms.OrderBy(r => r.LevelName).ThenBy(r => r.Number));

            RefreshFilter();

            ConfirmCommand  = new RelayCommand(_ => OnConfirm());
            CancelCommand   = new RelayCommand(_ => CloseRequested?.Invoke(false));
            SelectAllCommand  = new RelayCommand(_ =>
            {
                foreach (var r in FilteredRooms) r.IsSelected = true;
            });
            ClearAllCommand = new RelayCommand(_ =>
            {
                foreach (var r in FilteredRooms) r.IsSelected = false;
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private void RefreshFilter()
        {
            FilteredRooms.Clear();
            var q = string.IsNullOrWhiteSpace(_searchText)
                ? _allRooms
                : _allRooms.Where(r =>
                    r.Number.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase)   >= 0 ||
                    r.LevelName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var item in q)
                FilteredRooms.Add(item);
        }

        private void OnConfirm()
        {
            // Gather checked rooms from _allRooms (not just the filtered subset)
            var sel = _allRooms.Where(r => r.IsSelected).ToList();
            if (sel.Count == 0)
            {
                ValidationMessage = "Please select at least one room before confirming.";
                return;
            }
            ValidationMessage = string.Empty;
            SelectedRooms = sel;
            CloseRequested?.Invoke(true);
        }

        // ── INotifyPropertyChanged ─────────────────────────────────────────

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
