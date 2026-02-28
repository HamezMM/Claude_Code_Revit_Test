using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace PDG.Revit.AutomationTools.Models
{
    /// <summary>
    /// Scope of wall instances to target during a placement run.
    /// </summary>
    public enum PlacementScope
    {
        EntireModel,
        ActiveView,
        CurrentSelection
    }

    /// <summary>
    /// User-configured options collected from the dialog before a placement run.
    /// </summary>
    public class SweepPlacementOptions
    {
        /// <summary>ElementIds of the wall types to target.</summary>
        public List<ElementId> SelectedWallTypeIds { get; set; }

        /// <summary>ElementId of the WallSweepType to place.</summary>
        public ElementId SelectedSweepTypeId { get; set; }

        /// <summary>
        /// Vertical offset from the wall's base constraint in millimetres (user-entered).
        /// Converted to internal Revit feet units before use.
        /// </summary>
        public double OffsetFromBaseMm { get; set; }

        /// <summary>Scope of wall instances to process.</summary>
        public PlacementScope Scope { get; set; }

        public SweepPlacementOptions()
        {
            SelectedWallTypeIds = new List<ElementId>();
            OffsetFromBaseMm = 0.0;
            Scope = PlacementScope.EntireModel;
        }
    }
}
