using PDG.Revit.AutomationTools.ViewModels;
using System.Windows;

namespace PDG.Revit.AutomationTools.Views
{
    /// <summary>
    /// Code-behind for the post-run results summary dialog.
    /// Wires the DataContext to the ViewModel and handles the Close button.
    /// </summary>
    public partial class ResultsSummaryDialog : Window
    {
        public ResultsSummaryDialog(ResultsSummaryViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
