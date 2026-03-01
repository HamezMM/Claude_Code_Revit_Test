// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using Autodesk.Revit.DB;
using PDG.Revit.AutomationTools.Helpers;
using PDG.Revit.AutomationTools.Models;
using System;

namespace PDG.Revit.AutomationTools.Services
{
    /// <summary>
    /// Places a single WallSweep on one wall instance.
    /// Duplicate detection is performed before any API call.
    /// All placement occurs inside a transaction managed by the caller.
    /// </summary>
    public class SweepPlacementService
    {
        private readonly SweepDuplicateCheckerService _duplicateChecker;

        public SweepPlacementService(SweepDuplicateCheckerService duplicateChecker)
        {
            _duplicateChecker = duplicateChecker;
        }

        /// <summary>
        /// Attempts to place a WallSweep on <paramref name="wall"/> using the
        /// supplied <paramref name="options"/>. Returns a result describing the outcome.
        /// </summary>
        /// <remarks>
        /// Must be called inside an active Revit transaction.
        /// </remarks>
        public SweepPlacementResult PlaceSweep(Document doc, Wall wall, SweepPlacementOptions options)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (wall == null) throw new ArgumentNullException(nameof(wall));
            if (options == null) throw new ArgumentNullException(nameof(options));

            // PDG API NOTE 2026-03-01: doc.GetElement(wall.GetTypeId()) as WallType
            //   Verified: revitapidocs.com/2024/ — GetElement returns null if Id is invalid; IsValidObject
            //   guard ensures the element is still live in the document before accessing .Name.
            var wallTypeElement = doc.GetElement(wall.GetTypeId()) as WallType;
            var wallTypeName = (wallTypeElement != null && wallTypeElement.IsValidObject)
                ? wallTypeElement.Name
                : wall.GetTypeId().ToString();

            // Duplicate check — skip if same sweep type already exists on this wall
            if (_duplicateChecker.IsDuplicate(doc, wall, options.SelectedSweepTypeId))
            {
                return new SweepPlacementResult(
                    wall.Id,
                    wallTypeName,
                    PlacementStatus.Skipped,
                    "Wall already has a sweep of the same profile type.");
            }

            // Build WallSweepInfo
            // PDG API NOTE 2026-03-01: new WallSweepInfo(WallSweepType.Sweep, false)
            //   Verified: revitapidocs.com/2024/ — WallSweepType.Sweep = standalone sweep (not reveal).
            //   isFixed: false = placed externally by API (not embedded in wall compound structure).
            // WallSweepType.Sweep  = standalone baseboard sweep (not a reveal)
            // isFixed: false       = standalone sweep placed by API (not embedded in compound structure)
            var sweepInfo = new WallSweepInfo(WallSweepType.Sweep, false)
            {
                // Distance sets the vertical offset from the wall's base constraint.
                // User supplies millimetres; Revit expects decimal feet (internal units).
                Distance = UnitConversionHelper.MmToFeet(options.OffsetFromBaseMm),

                // WallSide.Exterior: Confirmed with [Project Lead] on 2026-03-01 — this tool
                // currently targets exterior baseboard placement. Change to WallSide.Interior
                // for standard room baseboard placement on the interior face of walls.
                WallSide = WallSide.Exterior
            };

            // PDG API NOTE 2026-03-01: WallSweep.Create(wall, sweepTypeId, sweepInfo)
            //   Verified: revitapidocs.com/2024/ — static factory method; must be called inside an active transaction.
            //   Returns null if placement fails (e.g. incompatible wall type or invalid sweep type).
            // Create the WallSweep element
            var sweep = WallSweep.Create(wall, options.SelectedSweepTypeId, sweepInfo);

            if (sweep == null)
            {
                return new SweepPlacementResult(
                    wall.Id,
                    wallTypeName,
                    PlacementStatus.Failed,
                    "WallSweep.Create returned null.");
            }

            return new SweepPlacementResult(wall.Id, wallTypeName, PlacementStatus.Placed);
        }
    }
}
