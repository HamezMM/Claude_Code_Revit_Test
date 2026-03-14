// PDG GENERATED: 2026-03-01 | Revit 2024
// Pure data carrier — NO Revit API calls.

using System.Collections.Generic;

namespace PDG.Revit.FireRatingLines.Models
{
    /// <summary>
    /// Aggregated summary of a single ApplyDoorFireRatings run.
    /// Populated by DoorFireRatingService and displayed in the TaskDialog summary.
    /// </summary>
    public class DoorFireRatingResult
    {
        /// <summary>Number of door instances whose Fire Rating parameter was successfully set.</summary>
        public int DoorsUpdated { get; set; }

        /// <summary>
        /// Number of doors skipped because their Fire Rating parameter was missing,
        /// read-only, or not of StorageType.String.
        /// </summary>
        public int DoorsSkipped { get; set; }

        /// <summary>
        /// Number of door instances whose host wall has no fire rating (unrated walls).
        /// These doors are intentionally ignored and not counted as skipped.
        /// </summary>
        public int DoorsInUnratedWalls { get; set; }

        /// <summary>
        /// Per-door warning messages accumulated during the run.
        /// Displayed in the TaskDialog when non-empty.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
