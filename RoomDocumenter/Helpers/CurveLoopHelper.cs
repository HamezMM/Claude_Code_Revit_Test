// RoomDocumenter | Revit 2024 | net48
// API NOTE: Room.GetBoundarySegments — Verified: revitapidocs.com/2024/
//   Returns IList<IList<BoundarySegment>>; index [0] is the outer loop.
//   BoundarySegment.GetCurve() returns the geometry curve for that segment.
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;

namespace RoomDocumenter.Helpers
{
    /// <summary>
    /// Static geometry helpers for room boundary operations.
    /// All methods are pure (no Revit transactions required) and operate on
    /// data already extracted from the document.
    /// </summary>
    public static class CurveLoopHelper
    {
        /// <summary>
        /// Builds a <see cref="CurveLoop"/> from the outer boundary loop of a room.
        /// Returns <c>null</c> if the loop contains fewer than 3 segments or if any
        /// segment curve is null.
        /// </summary>
        /// <param name="outerLoop">
        /// The outer boundary segment list (index [0] from
        /// <c>Room.GetBoundarySegments</c>).
        /// </param>
        /// <param name="reason">
        /// When <c>null</c> is returned, contains a human-readable explanation.
        /// </param>
        public static CurveLoop? BuildCurveLoop(
            IList<BoundarySegment> outerLoop,
            out string reason)
        {
            reason = string.Empty;

            if (outerLoop == null || outerLoop.Count < 3)
            {
                reason = $"Boundary loop has only {outerLoop?.Count ?? 0} segments (minimum 3 required).";
                return null;
            }

            var curves = new List<Curve>(outerLoop.Count);
            foreach (var seg in outerLoop)
            {
                var curve = seg.GetCurve();
                if (curve == null)
                {
                    reason = "One or more boundary segments returned a null curve.";
                    return null;
                }
                curves.Add(curve);
            }

            try
            {
                var loop = new CurveLoop();
                foreach (var c in curves)
                    loop.Append(c);
                return loop;
            }
            catch (Exception ex)
            {
                reason = $"CurveLoop construction failed: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Computes the XY centroid of a room boundary as the midpoint of the
        /// room's world-space bounding box.  The Z coordinate is set to the
        /// room's level elevation (i.e., the floor plane).
        /// </summary>
        /// <param name="boundingBox">Room bounding box in world coordinates.</param>
        /// <param name="levelElevation">
        /// Elevation of the room's base level in internal units.
        /// </param>
        public static XYZ ComputeCentroid(BoundingBoxXYZ boundingBox, double levelElevation)
        {
            if (boundingBox == null) throw new ArgumentNullException(nameof(boundingBox));

            return new XYZ(
                (boundingBox.Min.X + boundingBox.Max.X) / 2.0,
                (boundingBox.Min.Y + boundingBox.Max.Y) / 2.0,
                levelElevation);
        }

        /// <summary>
        /// Extracts a 2-D bounding rectangle from an outer boundary segment loop.
        /// Returns min/max XY values in world coordinates; Z is set to zero.
        /// Returns <c>null</c> if the loop is null or empty.
        /// </summary>
        public static (XYZ Min, XYZ Max)? GetBoundaryExtents(IList<BoundarySegment> outerLoop)
        {
            if (outerLoop == null || outerLoop.Count == 0) return null;

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var seg in outerLoop)
            {
                var curve = seg.GetCurve();
                if (curve == null) continue;

                foreach (var pt in new[] { curve.GetEndPoint(0), curve.GetEndPoint(1) })
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y > maxY) maxY = pt.Y;
                }
            }

            if (minX == double.MaxValue) return null;
            return (new XYZ(minX, minY, 0), new XYZ(maxX, maxY, 0));
        }

        /// <summary>
        /// Tests whether a 2-D point (XY only) lies inside a convex or simple
        /// polygon described by the outer boundary segments.
        /// Uses a ray-casting algorithm. Returns <c>false</c> if the loop is null.
        /// </summary>
        /// <param name="point">World-space XY point to test.</param>
        /// <param name="outerLoop">Room outer boundary segments.</param>
        public static bool PointIsInsideBoundary(XYZ point, IList<BoundarySegment> outerLoop)
        {
            if (outerLoop == null || outerLoop.Count == 0) return false;

            // Collect polygon vertices
            var vertices = new List<XYZ>(outerLoop.Count);
            foreach (var seg in outerLoop)
            {
                var curve = seg.GetCurve();
                if (curve != null)
                    vertices.Add(curve.GetEndPoint(0));
            }

            if (vertices.Count < 3) return false;

            // Ray-casting in XY plane
            double px = point.X, py = point.Y;
            int n = vertices.Count;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double xi = vertices[i].X, yi = vertices[i].Y;
                double xj = vertices[j].X, yj = vertices[j].Y;

                bool intersect = ((yi > py) != (yj > py)) &&
                                 (px < (xj - xi) * (py - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }

            return inside;
        }
    }
}
