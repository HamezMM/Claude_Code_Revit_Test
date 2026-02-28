using Autodesk.Revit.DB;

namespace PDG.Revit.AutomationTools.Models
{
    /// <summary>
    /// Lightweight representation of a Revit WallSweepType for dialog binding.
    /// </summary>
    public class SweepTypeSummary
    {
        /// <summary>ElementId of the WallSweepType in the Revit document.</summary>
        public ElementId ElementId { get; set; }

        /// <summary>Display name of the sweep profile type.</summary>
        public string Name { get; set; }

        public SweepTypeSummary(ElementId elementId, string name)
        {
            ElementId = elementId;
            Name = name;
        }
    }
}
