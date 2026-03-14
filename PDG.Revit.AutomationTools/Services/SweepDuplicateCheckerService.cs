// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using Autodesk.Revit.DB;
using System;
using System.Linq;

namespace PDG.Revit.AutomationTools.Services
{
    /// <summary>
    /// Checks whether a wall instance already carries a WallSweep of the target type.
    /// Per spec: skip if any sweep of the same WallSweepType Id exists, regardless of offset.
    /// </summary>
    public class SweepDuplicateCheckerService
    {
        /// <summary>
        /// Returns <c>true</c> if the wall already has a WallSweep whose type matches
        /// <paramref name="targetSweepTypeId"/>, indicating the wall should be skipped.
        /// </summary>
        /// <remarks>
        /// WallSweep elements do not expose a direct wall reference property.
        /// The correct navigation is <c>WallSweep.GetHostIds()</c>, which returns
        /// the ElementIds of all walls hosting the sweep.
        /// </remarks>
        public bool IsDuplicate(Document doc, Wall wall, ElementId targetSweepTypeId)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (wall == null) throw new ArgumentNullException(nameof(wall));
            if (targetSweepTypeId == null) throw new ArgumentNullException(nameof(targetSweepTypeId));

            // PDG API NOTE 2026-03-01: FilteredElementCollector(doc).OfClass(typeof(WallSweep))
            //   Verified: revitapidocs.com/2024/ — WallSweep (class) is a valid OfClass target.
            //   Returns all standalone wall sweep instances in the document.
            // PDG API NOTE 2026-03-01: WallSweep.GetHostIds()
            //   Verified: revitapidocs.com/2024/ — returns ElementIds of all host walls for the sweep.
            // PDG API NOTE 2026-03-01: WallSweep.GetTypeId()
            //   Verified: revitapidocs.com/2024/ — returns the ElementId of the sweep's type element.
            // Collect all standalone WallSweep elements in the document,
            // then filter to those hosted on this wall with the matching type.
            // WallSweep.GetHostIds() is the API-correct way to navigate from
            // a WallSweep back to its host wall(s).
            return new FilteredElementCollector(doc)
                .OfClass(typeof(WallSweep))
                .Cast<WallSweep>()
                .Where(ws => ws.GetHostIds().Contains(wall.Id))
                .Any(ws => ws.GetTypeId() == targetSweepTypeId);
        }
    }
}
