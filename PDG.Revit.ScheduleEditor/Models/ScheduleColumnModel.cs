// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using Autodesk.Revit.DB;

namespace PDG.Revit.ScheduleEditor.Models
{
    /// <summary>
    /// Describes one column in the schedule editor grid.
    /// Built by <see cref="Services.ScheduleReaderService"/> from a
    /// <c>ScheduleField</c> and its backing <c>SchedulableField</c>.
    /// </summary>
    public sealed class ScheduleColumnModel
    {
        /// <summary>Zero-based index matching the field's position in <c>ScheduleDefinition.GetFieldOrder()</c>.</summary>
        public int ColumnIndex { get; init; }

        /// <summary>Column heading text shown in the grid header.</summary>
        public string FieldName { get; init; } = string.Empty;

        /// <summary>
        /// The <c>ElementId</c> of the parameter returned by
        /// <c>SchedulableField.ParameterId</c>.  May be <c>ElementId.InvalidElementId</c>
        /// for Formula and Count fields.
        /// </summary>
        public ElementId? ParameterId { get; init; }

        /// <summary>
        /// The corresponding <c>BuiltInParameter</c> when
        /// <c>ParameterId.Value &gt;= 0</c>; otherwise <c>null</c> for
        /// shared / project parameters.
        /// </summary>
        public BuiltInParameter? BuiltInParam { get; init; }

        /// <summary>The <c>ScheduleFieldType</c> of this field (Instance, Formula, Count, etc.).</summary>
        public ScheduleFieldType FieldType { get; init; }

        /// <summary>
        /// <c>true</c> when the column must never be edited —
        /// set when <c>ScheduleField.IsReadOnly</c> is <c>true</c>, or
        /// <c>FieldType</c> is Formula or Count.
        /// </summary>
        public bool IsReadOnly { get; init; }

        /// <summary>
        /// The <c>StorageType</c> of the backing parameter.
        /// Filled in by <see cref="Services.ScheduleReaderService"/> from
        /// the first element that exposes the parameter.
        /// Defaults to <c>StorageType.Unknown</c> for empty schedules.
        /// </summary>
        public StorageType StorageType { get; set; } = StorageType.None;

        /// <summary>
        /// The <c>ForgeTypeId</c> returned by <c>Parameter.Definition.GetDataType()</c>.
        /// Used for display-unit formatting and write-time conversion of
        /// <c>Double</c> parameters.  <c>null</c> when the parameter has no
        /// unit spec (e.g. integer counts, strings).
        /// </summary>
        public ForgeTypeId? ForgeTypeId { get; set; }
    }
}
