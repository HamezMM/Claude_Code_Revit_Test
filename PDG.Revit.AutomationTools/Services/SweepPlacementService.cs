using Autodesk.Revit.DB;
using PDG.Revit.AutomationTools.Helpers;
using PDG.Revit.AutomationTools.Models;

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
            var wallTypeName = (doc.GetElement(wall.GetTypeId()) as WallType)?.Name ?? wall.GetTypeId().ToString();

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
            // WallSweepType.Sweep  = standalone baseboard sweep (not a reveal)
            // isFixed: false       = standalone sweep placed by API (not embedded in compound structure)
            var sweepInfo = new WallSweepInfo(WallSweepType.Sweep, false)
            {
                // Distance sets the vertical offset from the wall's base constraint.
                // User supplies millimetres; Revit expects decimal feet (internal units).
                Distance = UnitConversionHelper.MmToFeet(options.OffsetFromBaseMm),

                // Exterior face placement ensures the baseboard is on the outside of the wall.
                // FlipSweep (set post-create below) further enforces the outward profile orientation.
                WallSide = WallSide.Exterior
            };

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

            // Enforce outward-facing profile orientation per spec (flips the sweep profile
            // so the baseboard face points away from the wall interior).
            sweep.FlipSweep = true;

            return new SweepPlacementResult(wall.Id, wallTypeName, PlacementStatus.Placed);
        }
    }
}
