// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using PDG.Revit.ScheduleEditor.ViewModels;
using Syncfusion.UI.Xaml.Grid;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Media;

namespace PDG.Revit.ScheduleEditor.Views
{
    /// <summary>
    /// Code-behind for the schedule editor window.
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Wire up the ViewModel's <c>CloseRequested</c> event.</item>
    ///   <item>
    ///     Customise auto-generated SfDataGrid columns in
    ///     <see cref="ScheduleGrid_AutoGeneratingColumn"/>:
    ///     read-only columns have <c>AllowEditing = false</c>.
    ///   </item>
    ///   <item>
    ///     Apply grey background to read-only cells via
    ///     <see cref="ScheduleGrid_QueryCellInfo"/>.
    ///   </item>
    ///   <item>Show an unsaved-changes confirmation prompt on close.</item>
    /// </list>
    /// No business logic is present here — all data work is in the ViewModel and Services.
    /// </summary>
    public partial class ScheduleEditorWindow : Window
    {
        private ScheduleEditorViewModel? _viewModel;

        private static readonly SolidColorBrush ReadOnlyCellBrush =
            new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));   // #F0F0F0 light grey

        /// <summary>
        /// Initialises the window and binds it to <paramref name="viewModel"/>.
        /// </summary>
        /// <param name="viewModel">The editor ViewModel created by the command.</param>
        public ScheduleEditorWindow(ScheduleEditorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel         = viewModel;
            DataContext        = viewModel;
            viewModel.CloseRequested += () => Close();
        }

        // ─────────────────────────────────────────────────────────────────
        // SfDataGrid — column generation
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the SfDataGrid for each column it auto-generates from the
        /// bound DataTable.  Marks read-only columns as non-editable.
        /// </summary>
        private void ScheduleGrid_AutoGeneratingColumn(
            object sender, AutoGeneratingColumnArgs e) // API note: Syncfusion type
        {
            if (_viewModel?.GridData == null) return;
            if (e.Column?.MappingName == null) return;

            var dt = _viewModel.GridData;
            if (!dt.Columns.Contains(e.Column.MappingName)) return;

            var dc = dt.Columns[e.Column.MappingName];
            if (dc == null) return;

            if (dc.ReadOnly)
            {
                e.Column.AllowEditing = false;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // SfDataGrid — per-cell styling
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the SfDataGrid for every cell before it is rendered.
        /// Applies a light-grey background to read-only cells.
        /// </summary>
        private void ScheduleGrid_QueryCellInfo(
            object sender, QueryCellInfoEventArgs e) // API note: Syncfusion type
        {
            if (_viewModel?.GridData == null) return;
            if (e.Column?.MappingName == null) return;

            var dt = _viewModel.GridData;
            if (!dt.Columns.Contains(e.Column.MappingName)) return;

            var dc = dt.Columns[e.Column.MappingName];
            if (dc != null && dc.ReadOnly)
            {
                e.Style.Background = ReadOnlyCellBrush;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Window closing
        // ─────────────────────────────────────────────────────────────────

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_viewModel?.HasUnsavedChanges != true) return;

            var result = MessageBox.Show(
                this,
                "You have unsaved changes that have not been applied to Revit.\n\n" +
                "Close anyway and discard unsaved changes?",
                "Schedule Editor — Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }
    }
}
