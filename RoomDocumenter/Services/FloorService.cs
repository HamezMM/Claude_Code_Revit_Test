// RoomDocumenter | Revit 2024 | net48
// API NOTES:
//   Floor.Create(Document, IList<CurveLoop>, ElementId floorTypeId, ElementId levelId)
//     — Verified: revitapidocs.com/2024/
//   BoundingBoxIntersectsFilter   — Verified: revitapidocs.com/2024/
//   ElementId.Value (Int64)       — Verified: revitapidocs.com/2024/ (never IntegerValue)
using Autodesk.Revit.DB;
using RoomDocumenter.Helpers;
using RoomDocumenter.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RoomDocumenter.Services
{
    /// <summary>
    /// Handles floor reconciliation and creation for a single room.
    ///
    /// <para>Transaction scope: each discrete operation (remove / create) is
    /// wrapped in its own named Transaction by the caller
    /// (<see cref="RoomDocumentationService"/>).  The methods here execute
    /// within an already-started Transaction.</para>
    /// </summary>
    public class FloorService
    {
        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reconciles the floor finish for <paramref name="roomData"/>.
        /// <list type="bullet">
        ///   <item>If an existing floor of the correct type is found → logs unchanged, returns Unchanged.</item>
        ///   <item>If an existing floor of the wrong type is found → deletes it (in the current
        ///         transaction) and creates a new one, returns Updated.</item>
        ///   <item>If no floor is found → creates a new one, returns Created.</item>
        /// </list>
        /// Must be called inside an active Revit Transaction.
        /// </summary>
        /// <param name="doc">Active Revit document.</param>
        /// <param name="roomData">Pre-extracted room data including boundary.</param>
        /// <param name="floorTypeId">ElementId of the target FloorType.</param>
        /// <param name="log">Receives human-readable log messages.</param>
        /// <returns>Outcome of the reconciliation.</returns>
        public ReconcileOutcome ReconcileFloor(
            Document doc,
            RoomData roomData,
            ElementId floorTypeId,
            List<string> log)
        {
            if (doc == null)        throw new ArgumentNullException(nameof(doc));
            if (roomData == null)   throw new ArgumentNullException(nameof(roomData));
            if (floorTypeId == null) throw new ArgumentNullException(nameof(floorTypeId));
            if (log == null)        throw new ArgumentNullException(nameof(log));

            // Build CurveLoop from boundary
            if (roomData.OuterBoundary == null)
            {
                log.Add($"{roomData.DisplayLabel}: Floor skipped — outer boundary is null.");
                return ReconcileOutcome.Skipped;
            }

            var curveLoop = CurveLoopHelper.BuildCurveLoop(roomData.OuterBoundary, out var loopReason);
            if (curveLoop == null)
            {
                log.Add($"{roomData.DisplayLabel}: Floor skipped — {loopReason}");
                return ReconcileOutcome.Skipped;
            }

            // Find existing floor on the same level whose centroid is inside the room
            var existing = FindExistingFloor(doc, roomData);

            if (existing != null)
            {
                ElementId existingTypeId;
                try { existingTypeId = existing.GetTypeId(); }
                catch { existingTypeId = ElementId.InvalidElementId; }

                if (existingTypeId != null &&
                    existingTypeId.Value == floorTypeId.Value)
                {
                    Debug.WriteLine($"[RoomDocumenter] Floor unchanged for {roomData.DisplayLabel}");
                    log.Add($"Floor unchanged for {roomData.DisplayLabel}");
                    return ReconcileOutcome.Unchanged;
                }

                // Type mismatch — delete existing
                doc.Delete(existing.Id);
            }

            // Create new floor
            CreateFloor(doc, curveLoop, floorTypeId, roomData.LevelId);

            if (existing != null)
            {
                log.Add($"Floor updated (type changed) for {roomData.DisplayLabel}");
                return ReconcileOutcome.Updated;
            }

            return ReconcileOutcome.Created;
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private static Floor? FindExistingFloor(Document doc, RoomData roomData)
        {
            if (roomData.BoundingBox == null) return null;

            // Pre-filter with bounding box
            var bbFilter = new BoundingBoxIntersectsFilter(
                new Outline(roomData.BoundingBox.Min, roomData.BoundingBox.Max));

            var candidates = new FilteredElementCollector(doc)
                .OfClass(typeof(Floor))
                .WherePasses(bbFilter)
                .Cast<Floor>()
                .Where(f => f.LevelId != null &&
                            f.LevelId.Value == roomData.LevelId.Value);

            if (roomData.OuterBoundary == null) return null;

            foreach (var floor in candidates)
            {
                // Confirm centroid is inside the room boundary
                var bb = floor.get_BoundingBox(null);
                if (bb == null) continue;

                var centroid = new XYZ(
                    (bb.Min.X + bb.Max.X) / 2.0,
                    (bb.Min.Y + bb.Max.Y) / 2.0,
                    bb.Min.Z);

                if (CurveLoopHelper.PointIsInsideBoundary(centroid, roomData.OuterBoundary))
                    return floor;
            }

            return null;
        }

        private static void CreateFloor(
            Document doc,
            CurveLoop loop,
            ElementId floorTypeId,
            ElementId levelId)
        {
            // API NOTE: Floor.Create(Document, IList<CurveLoop>, ElementId, ElementId)
            // Verified: revitapidocs.com/2024/
            Floor.Create(doc, new List<CurveLoop> { loop }, floorTypeId, levelId);
        }
    }

    /// <summary>Outcome of a single reconciliation operation.</summary>
    public enum ReconcileOutcome
    {
        /// <summary>A new element was created.</summary>
        Created,
        /// <summary>An existing element of the wrong type was replaced.</summary>
        Updated,
        /// <summary>An existing element of the correct type was left untouched.</summary>
        Unchanged,
        /// <summary>The operation was skipped (missing data, empty finish, etc.).</summary>
        Skipped
    }
}
