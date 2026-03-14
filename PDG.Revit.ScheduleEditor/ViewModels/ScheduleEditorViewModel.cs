// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using Autodesk.Revit.DB;
using PDG.Revit.ScheduleEditor.Models;
using PDG.Revit.ScheduleEditor.Services;
using PDG.Revit.ScheduleEditor.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PDG.Revit.ScheduleEditor.ViewModels
{
    /// <summary>
    /// ViewModel for the schedule editor window.
    /// Exposes a <see cref="DataTable"/> (<see cref="GridData"/>) as the
    /// <c>ItemsSource</c> for the Syncfusion SfDataGrid.  Columns are auto-generated
    /// from the <c>DataTable</c>'s columns; the View's code-behind applies read-only
    /// styling by inspecting <c>DataColumn.ReadOnly</c> in the
    /// <c>AutoGeneratingColumn</c> and <c>QueryCellInfo</c> events.
    /// <para>
    /// No Revit API types are used in this class — all Revit access is delegated
    /// to <see cref="ScheduleWriterService"/>.
    /// </para>
    /// </summary>
    public sealed class ScheduleEditorViewModel : INotifyPropertyChanged
    {
        // ── Revit-side services and data ──────────────────────────────────
        private readonly ScheduleWriterService _writerService;

        // ── Column / row metadata (parallel to DataTable structure) ───────
        private readonly List<ScheduleColumnModel> _columnModels;

        /// <summary>
        /// Element IDs (long) in the same order as <c>DataTable.Rows</c>.
        /// Used when building <see cref="ScheduleCellEdit"/> records on Apply.
        /// </summary>
        private readonly List<long> _rowElementIds = new();

        /// <summary>
        /// Dirty cells tracked as (rowIndex, columnIndex) pairs.
        /// Cleared after a successful Apply.
        /// </summary>
        private readonly HashSet<(int row, int col)> _dirtyCells = new();

        // ─────────────────────────────────────────────────────────────────
        // Bindable properties
        // ─────────────────────────────────────────────────────────────────

        /// <summary>The name of the open schedule — displayed in the window title bar.</summary>
        public string ScheduleName { get; }

        private DataTable _gridData = new();
        /// <summary>
        /// The <c>DataTable</c> that drives the SfDataGrid.
        /// One <c>DataColumn</c> per <see cref="ScheduleColumnModel"/>;
        /// <c>DataColumn.ReadOnly</c> reflects the column's read-only state.
        /// </summary>
        public DataTable GridData
        {
            get => _gridData;
            private set { _gridData = value; OnPropertyChanged(); }
        }

        private bool _hasUnsavedChanges;
        /// <summary>
        /// <c>true</c> when the user has edited at least one cell since the last
        /// successful Apply (or since the window opened).
        /// Controls the Close confirmation prompt and the Apply button's enabled state.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set { _hasUnsavedChanges = value; OnPropertyChanged(); }
        }

        private string _lastApplyResult = string.Empty;
        /// <summary>
        /// Human-readable summary of the last Apply operation, shown in the status bar.
        /// </summary>
        public string LastApplyResult
        {
            get => _lastApplyResult;
            private set { _lastApplyResult = value; OnPropertyChanged(); }
        }

        // ─────────────────────────────────────────────────────────────────
        // Commands
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes all dirty cells to Revit.  Enabled only when
        /// <see cref="HasUnsavedChanges"/> is <c>true</c>.
        /// </summary>
        public RelayCommand ApplyCmd { get; }

        /// <summary>Raises <see cref="CloseRequested"/> so the View can close the window.</summary>
        public RelayCommand CloseCmd { get; }

        // ─────────────────────────────────────────────────────────────────
        // Events
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when the user activates the Close command.
        /// The View is responsible for checking <see cref="HasUnsavedChanges"/> before
        /// actually closing — the ViewModel does not close the window directly.
        /// </summary>
        public event Action? CloseRequested;

        // ─────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the ViewModel from pre-built column and row models.
        /// </summary>
        /// <param name="scheduleName">Display name shown in the window title.</param>
        /// <param name="columns">Column metadata from <see cref="ScheduleReaderService"/>.</param>
        /// <param name="rows">Row data from <see cref="ScheduleReaderService"/>.</param>
        /// <param name="writerService">The service used to write changes back to Revit.</param>
        public ScheduleEditorViewModel(
            string scheduleName,
            IReadOnlyList<ScheduleColumnModel> columns,
            IReadOnlyList<ScheduleRowModel>    rows,
            ScheduleWriterService              writerService)
        {
            ScheduleName    = scheduleName ?? string.Empty;
            _columnModels   = columns?.ToList() ?? new List<ScheduleColumnModel>();
            _writerService  = writerService ?? throw new ArgumentNullException(nameof(writerService));

            ApplyCmd = new RelayCommand(_ => ApplyChanges(), _ => HasUnsavedChanges);
            CloseCmd = new RelayCommand(_ => CloseRequested?.Invoke());

            GridData = BuildDataTable(_columnModels, rows ?? Array.Empty<ScheduleRowModel>());
            GridData.ColumnChanged += OnGridDataColumnChanged;
        }

        // ─────────────────────────────────────────────────────────────────
        // DataTable construction
        // ─────────────────────────────────────────────────────────────────

        private DataTable BuildDataTable(
            IReadOnlyList<ScheduleColumnModel> columns,
            IReadOnlyList<ScheduleRowModel>    rows)
        {
            var dt = new DataTable();

            foreach (var col in columns)
            {
                var dc = dt.Columns.Add(col.FieldName, typeof(string));
                dc.ReadOnly = col.IsReadOnly;
                dc.Caption  = col.FieldName;
                // Store the ColumnIndex in ExtendedProperties so that ColumnChanged
                // can map back to the ScheduleColumnModel without a linear search.
                dc.ExtendedProperties["ColumnIndex"] = col.ColumnIndex;
            }

            _rowElementIds.Clear();

            foreach (var row in rows.OrderBy(r => r.RowIndex))
            {
                _rowElementIds.Add(row.ElementId);

                var dr = dt.NewRow();
                foreach (var col in columns)
                {
                    if (row.Cells.TryGetValue(col.ColumnIndex, out var cell))
                        dr[col.FieldName] = (object?)cell.DisplayValue ?? DBNull.Value;
                    else
                        dr[col.FieldName] = DBNull.Value;
                }
                dt.Rows.Add(dr);
            }

            // Accept all changes so the initial load does not mark rows as Modified.
            dt.AcceptChanges();
            return dt;
        }

        // ─────────────────────────────────────────────────────────────────
        // Dirty tracking
        // ─────────────────────────────────────────────────────────────────

        private void OnGridDataColumnChanged(object? sender, DataColumnChangeEventArgs e)
        {
            // Read-only columns should never fire this event, but guard anyway.
            if (e.Column.ReadOnly) return;

            int rowIdx = GridData.Rows.IndexOf(e.Row);
            int colIdx = e.Column.ExtendedProperties.Contains("ColumnIndex")
                ? (int)e.Column.ExtendedProperties["ColumnIndex"]!
                : -1;

            if (rowIdx < 0 || colIdx < 0) return;

            _dirtyCells.Add((rowIdx, colIdx));
            HasUnsavedChanges = true;
        }

        // ─────────────────────────────────────────────────────────────────
        // Apply
        // ─────────────────────────────────────────────────────────────────

        private void ApplyChanges()
        {
            try
            {
                var edits = BuildEdits();

                if (edits.Count == 0)
                {
                    LastApplyResult = "No pending changes to apply.";
                    return;
                }

                var result = _writerService.ApplyEdits(edits);

                // Clear dirty state after a successful commit (even if some cells were skipped).
                GridData.AcceptChanges();
                _dirtyCells.Clear();
                HasUnsavedChanges = false;

                LastApplyResult = FormatApplyResult(result);
                Logger.Log($"[ScheduleEditorViewModel] Apply complete: {LastApplyResult}");
            }
            catch (Exception ex)
            {
                LastApplyResult = $"Apply failed: {ex.Message}";
                Logger.Log("[ScheduleEditorViewModel] ApplyChanges threw", ex);
            }
        }

        private List<ScheduleCellEdit> BuildEdits()
        {
            var edits = new List<ScheduleCellEdit>(_dirtyCells.Count);

            foreach (var (rowIdx, colIdx) in _dirtyCells)
            {
                if (rowIdx >= GridData.Rows.Count || rowIdx >= _rowElementIds.Count)
                    continue;

                var colModel = _columnModels.FirstOrDefault(c => c.ColumnIndex == colIdx);
                if (colModel == null || colModel.IsReadOnly) continue;

                // Find the DataColumn by its stored ColumnIndex.
                DataColumn? dc = GridData.Columns
                    .Cast<DataColumn>()
                    .FirstOrDefault(c => c.ExtendedProperties.Contains("ColumnIndex")
                                      && (int)c.ExtendedProperties["ColumnIndex"]! == colIdx);
                if (dc == null) continue;

                var newValue  = GridData.Rows[rowIdx][dc.ColumnName]?.ToString() ?? string.Empty;
                var paramId   = colModel.ParameterId ?? ElementId.InvalidElementId;

                edits.Add(new ScheduleCellEdit(
                    elementId      : _rowElementIds[rowIdx],
                    columnIndex    : colIdx,
                    parameterId    : paramId,
                    builtInParam   : colModel.BuiltInParam,
                    newDisplayValue: newValue,
                    storageType    : colModel.StorageType,
                    forgeTypeId    : colModel.ForgeTypeId));
            }

            return edits;
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static string FormatApplyResult(WriteResult result)
        {
            var sb = new StringBuilder();
            sb.Append($"Applied: {result.SuccessCount} cell{(result.SuccessCount == 1 ? "" : "s")} written");

            if (result.SkipCount > 0)
                sb.Append($", {result.SkipCount} skipped");

            if (result.Warnings.Count > 0)
            {
                sb.AppendLine();
                const int maxShow = 3;
                sb.Append("Warnings: ");
                sb.Append(string.Join("; ", result.Warnings.Take(maxShow)));
                if (result.Warnings.Count > maxShow)
                    sb.Append($" … (+{result.Warnings.Count - maxShow} more)");
            }

            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────
        // INotifyPropertyChanged
        // ─────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
