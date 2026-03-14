// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using System;
using System.Windows.Input;

namespace PDG.Revit.ScheduleEditor.ViewModels
{
    /// <summary>
    /// A simple <see cref="ICommand"/> implementation that delegates
    /// <c>Execute</c> and <c>CanExecute</c> to caller-supplied delegates.
    /// <c>CanExecuteChanged</c> is wired to <see cref="CommandManager.RequerySuggested"/>
    /// so that WPF button enabled-states refresh automatically.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="execute">The action to run when the command is invoked.</param>
        /// <param name="canExecute">
        /// Optional predicate controlling whether the command is enabled.
        /// When <c>null</c> the command is always enabled.
        /// </param>
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute    = execute    ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <inheritdoc />
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        /// <inheritdoc />
        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>
        /// Forces WPF to re-evaluate <see cref="CanExecute"/> for all bound controls.
        /// Call this after state changes that affect command availability.
        /// </summary>
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
