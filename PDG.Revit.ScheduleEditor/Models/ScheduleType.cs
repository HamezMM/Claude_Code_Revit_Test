// PDG.Revit.ScheduleEditor | Revit 2024 | net48
namespace PDG.Revit.ScheduleEditor.Models
{
    /// <summary>
    /// Classifies a <c>ViewSchedule</c> into one of the four schedule varieties
    /// that the editor handles.
    /// </summary>
    public enum ScheduleType
    {
        /// <summary>Standard element schedule (the most common type).</summary>
        Standard,

        /// <summary>Key schedule — rows are <c>ScheduleKeyEntry</c> elements.</summary>
        Key,

        /// <summary>Material takeoff — each element may produce multiple rows.</summary>
        MaterialTakeoff,

        /// <summary>Note block schedule — rows are <c>AnnotationSymbol</c> elements.</summary>
        NoteBlock
    }
}
