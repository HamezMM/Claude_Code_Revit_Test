// RoomDocumenter | Revit 2024 | net48
using System.Windows;

namespace RoomDocumenter.UI
{
    /// <summary>
    /// Code-behind for <see cref="FinishMappingDialog"/>.
    /// All business logic is in <see cref="FinishMappingViewModel"/>;
    /// this file only wires the ViewModel's CloseRequested event to the
    /// WPF DialogResult.
    /// </summary>
    public partial class FinishMappingDialog : Window
    {
        /// <summary>
        /// Initialises the dialog and binds to <paramref name="viewModel"/>.
        /// </summary>
        /// <param name="viewModel">Pre-populated ViewModel injected by the Command.</param>
        public FinishMappingDialog(FinishMappingViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseRequested += OnCloseRequested;
        }

        private void OnCloseRequested(bool confirmed)
        {
            DialogResult = confirmed;
            Close();
        }
    }
}
