// GridBuilderAddin | Revit 2024 | net48
using System;
using System.Windows;
using System.Windows.Interop;

namespace GridBuilderAddin.UI
{
    /// <summary>
    /// Code-behind for <see cref="StructureBuilderWindow"/>.
    /// Responsibilities limited to setting the DataContext and forwarding the close request.
    /// </summary>
    public partial class StructureBuilderWindow : Window
    {
        /// <summary>Initialises the window and wires the ViewModel.</summary>
        public StructureBuilderWindow(StructureBuilderViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += ViewModel_RequestClose;

            // After all bindings are established, re-apply default selections.
            // WPF's TwoWay SelectedItem bindings can write null back to the ViewModel during
            // binding initialisation (e.g. when ItemsSource is set and clears SelectedItem, or when
            // multiple ComboBoxes share the same ItemsSource via a shared CollectionView).
            // Calling OnLoaded() at this point restores any properties that were incorrectly nulled.
            Loaded += (s, e) => viewModel.OnLoaded();
        }

        /// <summary>Sets the Win32 owner handle for correct modal parenting inside Revit.</summary>
        public void SetRevitOwner(IntPtr ownerHandle)
        {
            if (ownerHandle == IntPtr.Zero) return;
            new WindowInteropHelper(this).Owner = ownerHandle;
        }

        private void ViewModel_RequestClose(object? sender, EventArgs e) => Close();
    }
}
