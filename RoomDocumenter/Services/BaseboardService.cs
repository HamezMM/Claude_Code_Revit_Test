// RoomDocumenter | Revit 2024 | net48
// API NOTES:
//   WallSweep.Create(Wall, ElementId wallSweepTypeId, WallSweepInfo)
//     — Verified: revitapidocs.com/2024/
//   WallSweepInfo(WallSweepType, bool isFixed) — Verified: revitapidocs.com/2024/
//   BoundarySegment.ElementId — invalid (-1) means room separation line
//   ElementId.Value (Int64) — Verified: revitapidocs.com/2024/ (never IntegerValue)
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomDocumenter.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RoomDocumenter.Services
{
    /// <summary>
    /// Handles baseboard WallSweep reconciliation and creation for the bounding
    /// walls of a single room.
    ///
    /// <para>Transaction scope: each discrete operation (remove / create per wall)
    /// is wrapped in its own named Transaction by the caller
    /// (<see cref="RoomDocumentationService"/>).  Methods here execute inside an
    /// already-started Transaction.</para>
    /// </summary>
    public class BaseboardService
    {
        // Tolerance for "offset ≈ 0" check when finding existing baseboards (in feet)
        private const double OffsetTolerance = 0.01;

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reconciles baseboards for every bounding wall segment of the room.
        /// Skips room separation lines (ElementId == InvalidElementId) and
        /// logs linked-model walls (GetElement returns null) once per call.
        /// Must be called inside an active Revit Transaction.
        /// </summary>
        /// <param name="doc">Active Revit document.</param>
        /// <param name="roomData">Pre-extracted room data including outer boundary.</param>
        /// <param name="sweepTypeId">ElementId of the target WallSweepType.</param>
        /// <param name="log">Receives human-readable log messages.</param>
        /// <returns>Aggregated counts for created / updated / unchanged sweeps.</returns>
        public (int Created, int Updated, int Unchanged) ReconcileBaseboards(
            Document doc,
            RoomData roomData,
            ElementId sweepTypeId,
            List<string> log)
        {
            if (doc == null)        throw new ArgumentNullException(nameof(doc));
            if (roomData == null)   throw new ArgumentNullException(nameof(roomData));
            if (sweepTypeId == null) throw new ArgumentNullException(nameof(sweepTypeId));
            if (log == null)        throw new ArgumentNullException(nameof(log));

            int created = 0, updated = 0, unchanged = 0;

            if (roomData.OuterBoundary == null)
            {
                log.Add($"{roomData.DisplayLabel}: Baseboards skipped — outer boundary is null.");
                return (0, 0, 0);
            }

            bool linkedWallLoggedOnce = false;

            foreach (var seg in roomData.OuterBoundary)
            {
                // Room separation line — skip silently
                if (seg.ElementId == ElementId.InvalidElementId)
                    continue;

                var wallElement = doc.GetElement(seg.ElementId);

                // Linked-model wall — null guard, log once
                if (wallElement == null)
                {
                    if (!linkedWallLoggedOnce)
                    {
                        log.Add($"{roomData.DisplayLabel}: One or more boundary walls are from a linked model — baseboards skipped for those segments.");
                        linkedWallLoggedOnce = true;
                    }
                    continue;
                }

                if (wallElement is not Wall wall) continue;

                var outcome = ReconcileWallBaseboard(doc, wall, sweepTypeId, roomData, log);
                switch (outcome)
                {
                    case ReconcileOutcome.Created:   created++;   break;
                    case ReconcileOutcome.Updated:   updated++;   break;
                    case ReconcileOutcome.Unchanged: unchanged++; break;
                }
            }

            return (created, updated, unchanged);
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private ReconcileOutcome ReconcileWallBaseboard(
            Document doc,
            Wall wall,
            ElementId sweepTypeId,
            RoomData roomData,
            List<string> log)
        {
            var existing = FindExistingBaseboard(doc, wall);

            if (existing != null)
            {
                ElementId existingTypeId;
                try { existingTypeId = existing.GetTypeId(); }
                catch { existingTypeId = ElementId.InvalidElementId; }

                if (existingTypeId != null &&
                    existingTypeId.Value == sweepTypeId.Value)
                {
                    Debug.WriteLine(
                        $"[RoomDocumenter] Baseboard unchanged on wall {wall.Id.Value} in {roomData.DisplayLabel}");
                    log.Add(
                        $"Baseboard unchanged on wall {wall.Id.Value} in {roomData.DisplayLabel}");
                    return ReconcileOutcome.Unchanged;
                }

                // Type mismatch — delete existing
                doc.Delete(existing.Id);
            }

            // Create baseboard sweep
            // API NOTE: WallSweep.Create(Wall, ElementId, WallSweepInfo)
            // Verified: revitapidocs.com/2024/
            // WallSweepType.Sweep = standalone sweep (not a reveal)
            // isFixed: false      = placed externally by API
            var sweepInfo = new WallSweepInfo(WallSweepType.Sweep, false)
            {
                // Offset = 0 → sweep sits at the wall's base constraint
                Distance  = 0.0,
                WallSide  = WallSide.Interior
            };

            var newSweep = WallSweep.Create(wall, sweepTypeId, sweepInfo);
            if (newSweep == null)
            {
                log.Add(
                    $"{roomData.DisplayLabel}: WallSweep.Create returned null for wall {wall.Id.Value}.");
                return ReconcileOutcome.Skipped;
            }

            if (existing != null)
            {
                log.Add($"Baseboard updated (type changed) on wall {wall.Id.Value} in {roomData.DisplayLabel}");
                return ReconcileOutcome.Updated;
            }

            return ReconcileOutcome.Created;
        }

        /// <summary>
        /// Returns the first WallSweep on <paramref name="wall"/> whose Distance
        /// (vertical offset) is approximately zero — i.e. a baseboard-position sweep.
        /// </summary>
        private static WallSweep? FindExistingBaseboard(Document doc, Wall wall)
        {
            // Get all WallSweep elements that belong to this wall
            // WallSweep elements are owned by the wall and collected via the wall's
            // GetDependentElements method or via a FilteredElementCollector.
            var dependent = wall.GetDependentElements(
                new ElementClassFilter(typeof(WallSweep)));

            foreach (var id in dependent)
            {
                var elem = doc.GetElement(id);
                if (elem is not WallSweep sweep) continue;

                try
                {
                    // API NOTE: WallSweepType is an enum (Sweep/Reveal); WallSweepInfo.Distance
                    // is the vertical offset from the wall base in internal units (feet).
                    // We identify baseboards by their near-zero base offset.
                    // Verified: revitapidocs.com/2024/
                    var info = sweep.GetWallSweepInfo();
                    if (Math.Abs(info.Distance) < OffsetTolerance)
                    {
                        return sweep;
                    }
                }
                catch
                {
                    // If WallSweepInfo is unavailable treat as mismatch and return it
                    return sweep;
                }
            }

            return null;
        }
    }
}
