// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using System.Collections.Generic;

namespace PDG.Revit.ScheduleEditor.Models
{
    /// <summary>
    /// Represents one row in the schedule editor grid — i.e. one Revit element
    /// (or key entry / annotation symbol) visible in the selected schedule.
    /// </summary>
    public sealed class ScheduleRowModel
    {
        /// <summary>
        /// The <c>ElementId.Value</c> (long) of the element backing this row.
        /// For key schedules this is the <c>ScheduleKeyEntry</c> element id;
        /// for note blocks it is the <c>AnnotationSymbol</c> element id.
        /// </summary>
        public long ElementId { get; init; }

        /// <summary>Zero-based row index within the list returned by the reader service.</summary>
        public int RowIndex { get; init; }

        /// <summary>
        /// Cell data keyed by <see cref="ScheduleColumnModel.ColumnIndex"/>.
        /// Every column defined in the schedule is present; cells with no
        /// matching parameter have <see cref="ScheduleCellModel.IsReadOnly"/> = <c>true</c>
        /// and an empty display value.
        /// </summary>
        public Dictionary<int, ScheduleCellModel> Cells { get; init; } = new();

        /// <summary>The variety of schedule this row belongs to.</summary>
        public ScheduleType ScheduleType { get; init; }

        /// <summary>
        /// For material-takeoff schedules only: the zero-based index of the
        /// material layer within the element that this row represents.
        /// -1 for all other schedule types.
        /// </summary>
        public int MaterialLayerIndex { get; init; } = -1;
    }
}
