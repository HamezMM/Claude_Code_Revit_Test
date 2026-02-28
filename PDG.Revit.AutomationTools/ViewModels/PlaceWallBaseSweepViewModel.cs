using Autodesk.Revit.DB;
using PDG.Revit.AutomationTools.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PDG.Revit.AutomationTools.ViewModels
{
    /// <summary>
    /// ViewModel for the Wall Base Sweep configuration dialog.
    /// Follows MVVM: no direct Revit API calls — all data is pre-loaded by the command.
    /// </summary>
    public class PlaceWallBaseSweepViewModel : INotifyPropertyChanged
    {
        // ── Observable collections bound to the dialog ────────────────────────

        public ObservableCollection<WallTypeSummary> WallTypes { get; }
        public ObservableCollection<SweepTypeSummary> SweepTypes { get; }

        // ── Selected sweep type (ComboBox) ────────────────────────────────────

        private SweepTypeSummary? _selectedSweepType;
        public SweepTypeSummary? SelectedSweepType
        {
            get => _selectedSweepType;
            set
            {
                _selectedSweepType = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ── Offset input ──────────────────────────────────────────────────────

        private string _offsetText = "0";
        public string OffsetText
        {
            get => _offsetText;
            set
            {
                _offsetText = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ── Scope (radio buttons) ─────────────────────────────────────────────

        private PlacementScope _scope = PlacementScope.EntireModel;
        public PlacementScope Scope
        {
            get => _scope;
            set { _scope = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScopeIsEntireModel)); OnPropertyChanged(nameof(ScopeIsActiveView)); OnPropertyChanged(nameof(ScopeIsCurrentSelection)); }
        }

        // Convenience bool properties for radio button two-way binding
        public bool ScopeIsEntireModel
        {
            get => _scope == PlacementScope.EntireModel;
            set { if (value) Scope = PlacementScope.EntireModel; }
        }
        public bool ScopeIsActiveView
        {
            get => _scope == PlacementScope.ActiveView;
            set { if (value) Scope = PlacementScope.ActiveView; }
        }
        public bool ScopeIsCurrentSelection
        {
            get => _scope == PlacementScope.CurrentSelection;
            set { if (value) Scope = PlacementScope.CurrentSelection; }
        }

        // ── Validation ────────────────────────────────────────────────────────

        public string ValidationMessage
        {
            get
            {
                if (!WallTypes.Any(wt => wt.IsSelected))
                    return "Select at least one wall type.";
                if (SelectedSweepType == null)
                    return "Select a sweep profile type.";
                if (!double.TryParse(OffsetText, out _))
                    return "Offset must be a valid number.";
                return string.Empty;
            }
        }

        public bool IsValid => string.IsNullOrEmpty(ValidationMessage);

        // ── Commands ──────────────────────────────────────────────────────────

        public ICommand PlaceCommand { get; }
        public ICommand CancelCommand { get; }

        // ── Dialog result flag ────────────────────────────────────────────────

        public bool? DialogResult { get; private set; }
        public event EventHandler? RequestClose;

        // ── Constructor ───────────────────────────────────────────────────────

        public PlaceWallBaseSweepViewModel(
            IEnumerable<WallTypeSummary> wallTypes,
            IEnumerable<SweepTypeSummary> sweepTypes)
        {
            WallTypes = new ObservableCollection<WallTypeSummary>(wallTypes);
            SweepTypes = new ObservableCollection<SweepTypeSummary>(sweepTypes);
            SelectedSweepType = SweepTypes.FirstOrDefault();

            PlaceCommand = new RelayCommand(OnPlace, () => IsValid);
            CancelCommand = new RelayCommand(OnCancel);
        }

        // ── Command handlers ──────────────────────────────────────────────────

        private void OnPlace()
        {
            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        // ── Build options from current dialog state ───────────────────────────

        /// <summary>
        /// Assembles a <see cref="SweepPlacementOptions"/> from the current dialog state.
        /// Call only after confirming <see cref="IsValid"/> is true.
        /// </summary>
        public SweepPlacementOptions BuildOptions()
        {
            double.TryParse(OffsetText, out var offsetMm);

            return new SweepPlacementOptions
            {
                SelectedWallTypeIds = WallTypes
                    .Where(wt => wt.IsSelected)
                    .Select(wt => wt.ElementId)
                    .ToList(),
                SelectedSweepTypeId = SelectedSweepType!.ElementId,
                OffsetFromBaseMm = offsetMm,
                Scope = Scope
            };
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Keep validation message in sync
            if (propertyName != nameof(ValidationMessage) && propertyName != nameof(IsValid))
            {
                OnPropertyChanged(nameof(ValidationMessage));
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }

    /// <summary>
    /// Minimal ICommand implementation for ViewModel commands.
    /// </summary>
    internal class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
