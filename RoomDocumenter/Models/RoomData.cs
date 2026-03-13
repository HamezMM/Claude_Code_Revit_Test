// RoomDocumenter | Revit 2024 | net48
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Collections.Generic;

namespace RoomDocumenter.Models
{
    /// <summary>
    /// Snapshot of a Room element and all data required for finish documentation.
    /// Populated once per room before any transactions begin; passed to services.
    /// </summary>
    public class RoomData
    {
        /// <summary>ElementId of the source Room.</summary>
        public ElementId RoomId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Room Number parameter value.</summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>Room Name parameter value.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Raw value of ROOM_FINISH_FLOOR. Empty if the parameter is unset.
        /// </summary>
        public string FloorFinish { get; set; } = string.Empty;

        /// <summary>
        /// Raw value of ROOM_FINISH_CEILING. Empty if the parameter is unset.
        /// </summary>
        public string CeilingFinish { get; set; } = string.Empty;

        /// <summary>
        /// Raw value of ROOM_FINISH_WALL. Empty if the parameter is unset.
        /// Stored for scheduling; does not drive geometry in this version.
        /// </summary>
        public string WallFinish { get; set; } = string.Empty;

        /// <summary>
        /// Raw value of ROOM_BASE_FINISH. Empty if the parameter is unset.
        /// Drives baseboard WallSweep creation.
        /// </summary>
        public string BaseFinish { get; set; } = string.Empty;

        /// <summary>
        /// Outer boundary loop (index [0]) from Room.GetBoundarySegments.
        /// May be null if the room boundary cannot be retrieved.
        /// </summary>
        public IList<BoundarySegment>? OuterBoundary { get; set; }

        /// <summary>ElementId of the room's associated Level.</summary>
        public ElementId LevelId { get; set; } = ElementId.InvalidElementId;

        /// <summary>
        /// Elevation of the room's base level in internal units (decimal feet).
        /// </summary>
        public double LevelElevation { get; set; }

        /// <summary>
        /// Room height in internal units (decimal feet) from ROOM_HEIGHT parameter.
        /// </summary>
        public double RoomHeight { get; set; }

        /// <summary>
        /// Room bounding box in world coordinates. Used for floor/ceiling pre-filters
        /// and elevation crop box calculations.
        /// </summary>
        public BoundingBoxXYZ? BoundingBox { get; set; }

        /// <summary>Display label used in log / summary messages.</summary>
        public string DisplayLabel => $"Room {Number} {Name}";
    }
}
