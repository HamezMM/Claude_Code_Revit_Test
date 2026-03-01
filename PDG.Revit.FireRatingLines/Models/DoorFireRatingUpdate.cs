// PDG GENERATED: 2026-03-01 | Revit 2024
// Pure data carrier — NO Revit API calls. ElementIds are stored only; no document queries here.

using Autodesk.Revit.DB;

namespace PDG.Revit.FireRatingLines.Models
{
    /// <summary>
    /// Lightweight record representing a single door whose Fire Rating instance parameter
    /// should be updated to match its host wall's fire rating.
    /// Created by DoorFireRatingService.GetDoorFireRatingUpdates() and consumed by
    /// DoorFireRatingService.ApplyDoorFireRatings().
    /// </summary>
    public class DoorFireRatingUpdate
    {
        /// <summary>ElementId of the door FamilyInstance to update.</summary>
        public ElementId DoorId { get; }

        /// <summary>ElementId of the host Wall that carries the fire rating.</summary>
        public ElementId HostWallId { get; }

        /// <summary>
        /// Wall fire rating key read from the host wall type's Fire Rating parameter
        /// (e.g. "2 HR"). Used for display in the TaskDialog summary.
        /// </summary>
        public string WallRating { get; }

        /// <summary>
        /// Door fire rating string to be written to the door's Fire Rating parameter
        /// (e.g. "1.5 HR" for a "2 HR" wall, per the PDG rating mapping).
        /// </summary>
        public string DoorRating { get; }

        public DoorFireRatingUpdate(
            ElementId doorId,
            ElementId hostWallId,
            string wallRating,
            string doorRating)
        {
            DoorId     = doorId;
            HostWallId = hostWallId;
            WallRating = wallRating;
            DoorRating = doorRating;
        }
    }
}
