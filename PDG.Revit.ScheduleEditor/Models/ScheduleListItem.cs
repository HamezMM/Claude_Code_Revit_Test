// PDG.Revit.ScheduleEditor | Revit 2024 | net48
namespace PDG.Revit.ScheduleEditor.Models
{
    /// <summary>
    /// Lightweight display model representing one <c>ViewSchedule</c> in the
    /// schedule-picker list.  All Revit API types are stripped; only primitives
    /// are stored so that the ViewModel and View have no dependency on RevitAPI.dll.
    /// </summary>
    public sealed class ScheduleListItem
    {
        /// <summary>The <c>ElementId.Value</c> (long) of the underlying <c>ViewSchedule</c>.</summary>
        public long ViewScheduleId { get; init; }

        /// <summary>The schedule's display name (<c>ViewSchedule.Name</c>).</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Classified schedule variety used to display the type badge.</summary>
        public ScheduleType ScheduleType { get; init; }

        /// <summary>Short label shown as a coloured badge next to the schedule name.</summary>
        public string TypeBadge => ScheduleType switch
        {
            ScheduleType.Key            => "Key",
            ScheduleType.MaterialTakeoff => "Takeoff",
            ScheduleType.NoteBlock       => "Note Block",
            _                            => "Standard"
        };
    }
}
