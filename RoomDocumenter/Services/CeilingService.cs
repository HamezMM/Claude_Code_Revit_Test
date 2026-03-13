// RoomDocumenter | Revit 2024 | net48
// API NOTES:
//   Ceiling.Create(Document, IList<CurveLoop>, ElementId ceilingTypeId, ElementId levelId)
//     — Verified: revitapidocs.com/2024/
//   BoundingBoxIntersectsFilter   — Verified: revitapidocs.com/2024/
//   ElementId.Value (Int64)       — Verified: revitapidocs.com/2024/ (never IntegerValue)
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomDocumenter.Helpers;
using RoomDocumenter.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RoomDocumenter.Services
{
    /// <summary>
    /// Handles ceiling reconciliation and creation for a single room.
    ///
    /// <para>Transaction scope: each discrete operation (remove / create) is
    /// wrapped in its own named Transaction by the caller
    /// (<see cref="RoomDocumentationService"/>).  The methods here execute
    /// within an already-started Transaction.</para>
    /// </summary>
    public class CeilingService
    {
        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reconciles the ceiling finish for <paramref name="roomData"/>.
        /// <list type="bullet">
        ///   <item>Correct type exists → logs unchanged, returns Unchanged.</item>
        ///   <item>Wrong type exists → deletes and recreates, returns Updated.</item>
        ///   <item>None exists → creates, returns Created.</item>
        /// </list>
        /// Must be called inside an active Revit Transaction.
        /// </summary>
        /// <param name="doc">Active Revit document.</param>
        /// <param name="roomData">Pre-extracted room data.</param>
        /// <param name="ceilingTypeId">ElementId of the target CeilingType.</param>
        /// <param name="log">Receives human-readable log messages.</param>
        /// <returns>Outcome of the reconciliation.</returns>
        public ReconcileOutcome ReconcileCeiling(
            Document doc,
            RoomData roomData,
            ElementId ceilingTypeId,
            List<string> log)
        {
            if (doc == null)          throw new ArgumentNullException(nameof(doc));
            if (roomData == null)     throw new ArgumentNullException(nameof(roomData));
            if (ceilingTypeId == null) throw new ArgumentNullException(nameof(ceilingTypeId));
            if (log == null)          throw new ArgumentNullException(nameof(log));

            if (roomData.OuterBoundary == null)
            {
                log.Add($"{roomData.DisplayLabel}: Ceiling skipped — outer boundary is null.");
                return ReconcileOutcome.Skipped;
            }

            var curveLoop = CurveLoopHelper.BuildCurveLoop(roomData.OuterBoundary, out var loopReason);
            if (curveLoop == null)
            {
                log.Add($"{roomData.DisplayLabel}: Ceiling skipped — {loopReason}");
                return ReconcileOutcome.Skipped;
            }

            var existing = FindExistingCeiling(doc, roomData);

            if (existing != null)
            {
                ElementId existingTypeId;
                try { existingTypeId = existing.GetTypeId(); }
                catch { existingTypeId = ElementId.InvalidElementId; }

                if (existingTypeId != null &&
                    existingTypeId.Value == ceilingTypeId.Value)
                {
                    Debug.WriteLine($"[RoomDocumenter] Ceiling unchanged for {roomData.DisplayLabel}");
                    log.Add($"Ceiling unchanged for {roomData.DisplayLabel}");
                    return ReconcileOutcome.Unchanged;
                }

                doc.Delete(existing.Id);
            }

            CreateCeiling(doc, curveLoop, ceilingTypeId, roomData.LevelId);

            if (existing != null)
            {
                log.Add($"Ceiling updated (type changed) for {roomData.DisplayLabel}");
                return ReconcileOutcome.Updated;
            }

            return ReconcileOutcome.Created;
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private static Ceiling? FindExistingCeiling(Document doc, RoomData roomData)
        {
            if (roomData.BoundingBox == null) return null;

            var bbFilter = new BoundingBoxIntersectsFilter(
                new Outline(roomData.BoundingBox.Min, roomData.BoundingBox.Max));

            var candidates = new FilteredElementCollector(doc)
                .OfClass(typeof(Ceiling))
                .WherePasses(bbFilter)
                .Cast<Ceiling>()
                .Where(c => c.LevelId != null &&
                            c.LevelId.Value == roomData.LevelId.Value);

            if (roomData.OuterBoundary == null) return null;

            foreach (var ceiling in candidates)
            {
                var bb = ceiling.get_BoundingBox(null);
                if (bb == null) continue;

                var centroid = new XYZ(
                    (bb.Min.X + bb.Max.X) / 2.0,
                    (bb.Min.Y + bb.Max.Y) / 2.0,
                    bb.Min.Z);

                if (CurveLoopHelper.PointIsInsideBoundary(centroid, roomData.OuterBoundary))
                    return ceiling;
            }

            return null;
        }

        private static void CreateCeiling(
            Document doc,
            CurveLoop loop,
            ElementId ceilingTypeId,
            ElementId levelId)
        {
            // API NOTE: Ceiling.Create(Document, IList<CurveLoop>, ElementId, ElementId)
            // Verified: revitapidocs.com/2024/
            Ceiling.Create(doc, new List<CurveLoop> { loop }, ceilingTypeId, levelId);
        }
    }
}
