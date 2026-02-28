using Autodesk.Revit.DB;

namespace PDG.Revit.AutomationTools.Models
{
    /// <summary>
    /// Lightweight representation of a Revit WallType for dialog binding.
    /// </summary>
    public class WallTypeSummary
    {
        /// <summary>ElementId of the WallType in the Revit document.</summary>
        public ElementId ElementId { get; set; }

        /// <summary>Display name of the wall type.</summary>
        public string Name { get; set; }

        /// <summary>Whether this wall type is selected in the dialog checkbox list.</summary>
        public bool IsSelected { get; set; }

        public WallTypeSummary(ElementId elementId, string name)
        {
            ElementId = elementId;
            Name = name;
            IsSelected = false;
        }
    }
}
