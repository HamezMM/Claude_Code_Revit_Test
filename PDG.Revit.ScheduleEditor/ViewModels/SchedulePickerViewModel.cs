// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using PDG.Revit.ScheduleEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PDG.Revit.ScheduleEditor.ViewModels
{
    /// <summary>
    /// ViewModel for the schedule picker dialog.
    /// Exposes a filterable list of all schedules in the document and
    /// notifies the View when the user confirms a selection.
    /// No Revit API types are referenced here.
    /// </summary>
    public sealed class SchedulePickerViewModel : INotifyPropertyChanged
    {
        private readonly List<ScheduleListItem> _allSchedules;

        // ─────────────────────────────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Filtered list of schedules currently visible in the picker.</summary>
        public ObservableCollection<ScheduleListItem> Schedules { get; } = new();

        private ScheduleListItem? _selectedSchedule;
        /// <summary>The schedule the user has highlighted in the list.</summary>
        public ScheduleListItem? SelectedSchedule
        {
            get => _selectedSchedule;
            set
            {
                _selectedSchedule = value;
                OnPropertyChanged();
                OpenCmd.RaiseCanExecuteChanged();
            }
        }

        private string _searchText = string.Empty;
        /// <summary>
        /// Text entered in the search box.
        /// Setting this value immediately re-filters <see cref="Schedules"/>.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterSchedules();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Commands
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Confirms the current selection and raises <see cref="Confirmed"/>.</summary>
        public RelayCommand OpenCmd { get; }

        // ─────────────────────────────────────────────────────────────────
        // Events
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when the user confirms a schedule selection.
        /// The argument is the chosen <see cref="ScheduleListItem"/>.
        /// </summary>
        public event Action<ScheduleListItem>? Confirmed;

        // ─────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the picker ViewModel with the full schedule list.
        /// </summary>
        /// <param name="schedules">
        /// All schedules returned by
        /// <see cref="Services.ScheduleReaderService.GetAllSchedules"/>.
        /// </param>
        public SchedulePickerViewModel(IEnumerable<ScheduleListItem> schedules)
        {
            _allSchedules = (schedules ?? Enumerable.Empty<ScheduleListItem>()).ToList();
            OpenCmd       = new RelayCommand(_ => ConfirmSelection(), _ => SelectedSchedule != null);
            FilterSchedules();
        }

        // ─────────────────────────────────────────────────────────────────
        // Public helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the View on a list-box double-click — confirms the current selection
        /// if one is present.
        /// </summary>
        public void HandleDoubleClick()
        {
            if (SelectedSchedule != null)
                ConfirmSelection();
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private void FilterSchedules()
        {
            Schedules.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allSchedules
                : _allSchedules
                    .Where(s => s.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            foreach (var item in filtered)
                Schedules.Add(item);
        }

        private void ConfirmSelection()
        {
            if (SelectedSchedule == null) return;
            Confirmed?.Invoke(SelectedSchedule);
        }

        // ─────────────────────────────────────────────────────────────────
        // INotifyPropertyChanged
        // ─────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
