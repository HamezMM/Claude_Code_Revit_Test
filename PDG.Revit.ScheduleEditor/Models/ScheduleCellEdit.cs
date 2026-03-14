// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using Autodesk.Revit.DB;

namespace PDG.Revit.ScheduleEditor.Models
{
    /// <summary>
    /// Immutable record describing a single pending cell write.
    /// Produced by <see cref="ViewModels.ScheduleEditorViewModel"/> when
    /// the user clicks Apply and consumed by
    /// <see cref="Services.ScheduleWriterService.ApplyEdits"/>.
    /// </summary>
    public sealed class ScheduleCellEdit
    {
        /// <summary><c>ElementId.Value</c> (long) of the element to modify.</summary>
        public long ElementId { get; }

        /// <summary>Zero-based column index matching <see cref="ScheduleColumnModel.ColumnIndex"/>.</summary>
        public int ColumnIndex { get; }

        /// <summary>
        /// The <c>ElementId</c> of the parameter to set, as returned by
        /// <c>SchedulableField.ParameterId</c>.
        /// </summary>
        public ElementId ParameterId { get; }

        /// <summary>
        /// The <c>BuiltInParameter</c> value when <c>ParameterId.Value &gt;= 0</c>;
        /// otherwise <c>null</c>.
        /// </summary>
        public BuiltInParameter? BuiltInParam { get; }

        /// <summary>The display-unit string typed by the user (e.g. "3600 mm" or "42").</summary>
        public string NewDisplayValue { get; }

        /// <summary>The parameter's <c>StorageType</c> — determines the Set() overload to call.</summary>
        public StorageType StorageType { get; }

        /// <summary>
        /// The <c>ForgeTypeId</c> spec type for unit conversion of Double parameters.
        /// <c>null</c> when the parameter has no unit (string, integer).
        /// </summary>
        public ForgeTypeId? ForgeTypeId { get; }

        /// <summary>
        /// Initialises all fields.
        /// </summary>
        public ScheduleCellEdit(
            long elementId,
            int columnIndex,
            ElementId parameterId,
            BuiltInParameter? builtInParam,
            string newDisplayValue,
            StorageType storageType,
            ForgeTypeId? forgeTypeId)
        {
            ElementId       = elementId;
            ColumnIndex     = columnIndex;
            ParameterId     = parameterId;
            BuiltInParam    = builtInParam;
            NewDisplayValue = newDisplayValue;
            StorageType     = storageType;
            ForgeTypeId     = forgeTypeId;
        }
    }
}
