// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using PDG.Revit.AutomationTools.UI.ViewModels;
using System.Windows;

namespace PDG.Revit.AutomationTools.UI.Windows
{
    /// <summary>
    /// Code-behind for the post-run results summary window.
    /// Wires the DataContext to the ViewModel and handles the Close button.
    /// </summary>
    public partial class ResultsSummaryWindow : Window
    {
        public ResultsSummaryWindow(ResultsSummaryViewModel viewModel)
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
