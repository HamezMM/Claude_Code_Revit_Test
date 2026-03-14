// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using PDG.Revit.ScheduleEditor.Models;
using PDG.Revit.ScheduleEditor.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace PDG.Revit.ScheduleEditor.Views
{
    /// <summary>
    /// Code-behind for the schedule picker dialog.
    /// Contains only view-layer logic (event handlers that delegate to the ViewModel
    /// or close the window).  No business logic here.
    /// </summary>
    public partial class SchedulePickerWindow : Window
    {
        private readonly SchedulePickerViewModel _viewModel;

        /// <summary>
        /// The schedule the user selected and confirmed, or <c>null</c> when the
        /// dialog was cancelled.
        /// </summary>
        public ScheduleListItem? SelectedSchedule { get; private set; }

        /// <summary>
        /// Initialises the window and wires up the ViewModel's <c>Confirmed</c> event.
        /// </summary>
        /// <param name="viewModel">Pre-built picker ViewModel.</param>
        public SchedulePickerWindow(SchedulePickerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            viewModel.Confirmed += item =>
            {
                SelectedSchedule = item;
                DialogResult     = true;
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // View-layer event handlers
        // ─────────────────────────────────────────────────────────────────

        private void ScheduleListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.HandleDoubleClick();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
