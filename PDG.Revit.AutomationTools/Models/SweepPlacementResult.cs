// PDG GENERATED: 2026-03-01 | Revit 2024
using Autodesk.Revit.DB;

namespace PDG.Revit.AutomationTools.Models
{
    /// <summary>
    /// Outcome of a single wall sweep placement attempt.
    /// </summary>
    public enum PlacementStatus
    {
        Placed,
        Skipped,
        Failed
    }

    /// <summary>
    /// Records the outcome for one wall instance processed during a placement run.
    /// </summary>
    public class SweepPlacementResult
    {
        /// <summary>ElementId of the Wall that was processed.</summary>
        public ElementId WallId { get; set; }

        /// <summary>Name of the wall type (for display in results dialog).</summary>
        public string WallTypeName { get; set; }

        /// <summary>Outcome of the placement attempt.</summary>
        public PlacementStatus Status { get; set; }

        /// <summary>Human-readable reason, populated for Skipped and Failed outcomes.</summary>
        public string Reason { get; set; }

        public SweepPlacementResult(ElementId wallId, string wallTypeName, PlacementStatus status, string reason = "")
        {
            WallId = wallId;
            WallTypeName = wallTypeName;
            Status = status;
            Reason = reason;
        }
    }
}
