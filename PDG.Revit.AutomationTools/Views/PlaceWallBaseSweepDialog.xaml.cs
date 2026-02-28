using PDG.Revit.AutomationTools.ViewModels;
using System;
using System.Windows;

namespace PDG.Revit.AutomationTools.Views
{
    /// <summary>
    /// Code-behind for the Wall Base Sweep configuration dialog.
    /// Wires the DataContext to the ViewModel and subscribes to the
    /// close request event emitted by the ViewModel commands.
    /// </summary>
    public partial class PlaceWallBaseSweepDialog : Window
    {
        public PlaceWallBaseSweepDialog(PlaceWallBaseSweepViewModel viewModel)
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
