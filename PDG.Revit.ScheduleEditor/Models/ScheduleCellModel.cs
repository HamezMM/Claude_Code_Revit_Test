// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using Autodesk.Revit.DB;

namespace PDG.Revit.ScheduleEditor.Models
{
    /// <summary>
    /// Holds the read-time state of a single schedule cell.
    /// Produced by <see cref="Services.ScheduleReaderService"/> when building rows;
    /// consumed by <see cref="ViewModels.ScheduleEditorViewModel"/> to populate the
    /// <c>DataTable</c> that drives the SfDataGrid.
    /// </summary>
    public sealed class ScheduleCellModel
    {
        /// <summary>Formatted string shown in the grid (with units where applicable).</summary>
        public string DisplayValue { get; init; } = string.Empty;

        /// <summary>The raw Revit value before unit formatting (double, int, string, or long for ElementId).</summary>
        public object? RawValue { get; init; }

        /// <summary>
        /// <c>true</c> when this cell cannot be edited — derived from the
        /// column's <see cref="ScheduleColumnModel.IsReadOnly"/> flag combined with
        /// the runtime <c>Parameter.IsReadOnly</c> value.
        /// </summary>
        public bool IsReadOnly { get; init; }

        /// <summary>Tracks whether the user has modified this cell since the last Apply.</summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// The formatted display value captured at load time or after a successful Apply.
        /// Used to detect whether the current value actually changed.
        /// </summary>
        public string OriginalDisplayValue { get; init; } = string.Empty;

        /// <summary>
        /// The <c>StorageType</c> of the backing parameter — carried here so that
        /// the reader service can propagate it to the column model on first encounter.
        /// </summary>
        public StorageType StorageType { get; init; } = StorageType.Unknown;

        /// <summary>
        /// The <c>ForgeTypeId</c> spec type of the parameter — carried here so that
        /// the reader service can propagate it to the column model on first encounter.
        /// </summary>
        public ForgeTypeId? ForgeTypeId { get; init; }
    }
}
