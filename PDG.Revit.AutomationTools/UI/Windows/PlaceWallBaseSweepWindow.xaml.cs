// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using PDG.Revit.AutomationTools.UI.ViewModels;
using System;
using System.Windows;

namespace PDG.Revit.AutomationTools.UI.Windows
{
    /// <summary>
    /// Code-behind for the Wall Base Sweep configuration window.
    /// Wires the DataContext to the ViewModel and subscribes to the
    /// close request event emitted by the ViewModel commands.
    /// </summary>
    public partial class PlaceWallBaseSweepWindow : Window
    {
        public PlaceWallBaseSweepWindow(PlaceWallBaseSweepViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            viewModel.RequestClose += ViewModel_RequestClose;
        }

        private void ViewModel_RequestClose(object? sender, EventArgs e)
        {
            if (sender is PlaceWallBaseSweepViewModel vm)
                DialogResult = vm.DialogResult;

            Close();
        }
    }
}
