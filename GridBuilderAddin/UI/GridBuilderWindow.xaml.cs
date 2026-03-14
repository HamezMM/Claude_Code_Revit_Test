// GridBuilderAddin | Revit 2024 | net48
using System;
using System.Windows;
using System.Windows.Interop;

namespace GridBuilderAddin.UI
{
    /// <summary>
    /// Code-behind for <see cref="GridBuilderWindow"/>.
    /// Responsibilities limited to:
    /// <list type="bullet">
    ///   <item>Setting the <see cref="DataContext"/> to the supplied ViewModel.</item>
    ///   <item>Subscribing to <see cref="GridBuilderViewModel.RequestClose"/> and propagating
    ///         the dialog result.</item>
    /// </list>
    /// No business logic lives here.
    /// </summary>
    public partial class GridBuilderWindow : Window
    {
        /// <summary>
        /// Initialises the window and wires up the ViewModel.
        /// </summary>
        /// <param name="viewModel">Pre-constructed ViewModel instance.</param>
        public GridBuilderWindow(GridBuilderViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            viewModel.RequestClose += ViewModel_RequestClose;
        }

        /// <summary>
        /// Sets the native Win32 owner handle so the WPF window is parented correctly
        /// inside the Revit process and behaves as a modal dialog.
        /// </summary>
        /// <param name="ownerHandle">The <c>MainWindowHandle</c> from <c>UIApplication</c>.</param>
        public void SetRevitOwner(IntPtr ownerHandle)
        {
            if (ownerHandle == IntPtr.Zero)
                return;

            var helper = new WindowInteropHelper(this);
            helper.Owner = ownerHandle;
        }

        private void ViewModel_RequestClose(object? sender, EventArgs e)
        {
            if (sender is GridBuilderViewModel vm)
                DialogResult = vm.DialogResult;

            Close();
        }
    }
}
