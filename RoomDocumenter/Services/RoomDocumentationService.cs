// RoomDocumenter | Revit 2024 | net48
// API NOTES:
//   TransactionGroup — Verified: revitapidocs.com/2024/
//   Transaction      — Verified: revitapidocs.com/2024/
//   BuiltInParameter.ROOM_FINISH_FLOOR / CEILING / WALL / BASE_FINISH
//     — Verified: revitapidocs.com/2024/
//   ElementId.Value (Int64) — Verified: revitapidocs.com/2024/ (never IntegerValue)
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomDocumenter.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoomDocumenter.Services
{
    /// <summary>
    /// Orchestrates finish documentation for a collection of rooms.
    /// Resolves the FinishMapping, builds RoomData snapshots, and delegates
    /// per-element reconciliation to <see cref="FloorService"/>,
    /// <see cref="CeilingService"/>, and <see cref="BaseboardService"/>.
    ///
    /// <para>Transaction scope: a single TransactionGroup wraps the entire run.
    /// Each discrete element operation (floor / ceiling / baseboard, per room)
    /// is wrapped in an individual Transaction for clean partial rollback.</para>
    /// </summary>
    public class RoomDocumentationService
    {
        private readonly FloorService     _floorSvc     = new FloorService();
        private readonly CeilingService   _ceilingSvc   = new CeilingService();
        private readonly BaseboardService _baseboardSvc = new BaseboardService();
        private readonly FinishMappingService _mappingSvc = new FinishMappingService();

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Executes finish documentation for <paramref name="rooms"/>.
        /// All element operations are wrapped in a single TransactionGroup named
        /// "RoomDocumenter — Document Room Finishes".
        /// </summary>
        /// <param name="doc">Active Revit document.</param>
        /// <param name="rooms">Rooms to process (pre-filtered: area &gt; 0).</param>
        /// <returns>Aggregated <see cref="DocumentationResult"/>.</returns>
        public DocumentationResult Execute(Document doc, IList<Room> rooms)
        {
            if (doc == null)   throw new ArgumentNullException(nameof(doc));
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));

            var result = new DocumentationResult();

            var mapping = _mappingSvc.Load(doc);
            if (mapping == null)
                throw new InvalidOperationException(
                    "No finish mapping found. Please run Finish Mapping first.");

            using var tg = new TransactionGroup(doc,
                "RoomDocumenter — Document Room Finishes");
            tg.Start();

            foreach (var room in rooms)
            {
                if (room == null || room.Area <= 0)
                {
                    result.RoomsSkipped++;
                    result.SkipReasons.Add(
                        $"Room {room?.Number} {room?.Name}: not placed — skipped.");
                    continue;
                }

                ProcessRoom(doc, room, mapping, result);
                result.RoomsProcessed++;
            }

            tg.Assimilate();
            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // Per-room processing
        // ─────────────────────────────────────────────────────────────────

        private void ProcessRoom(
            Document doc,
            Room room,
            FinishMapping mapping,
            DocumentationResult result)
        {
            var roomData = BuildRoomData(doc, room, result.SkipReasons);

            // ── Floor ──────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(roomData.FloorFinish))
            {
                if (mapping.FloorMappings.TryGetValue(roomData.FloorFinish, out long floorTypeIdVal))
                {
                    var floorTypeId = new ElementId(floorTypeIdVal);
                    if (doc.GetElement(floorTypeId) == null)
                    {
                        result.SkipReasons.Add(
                            $"{roomData.DisplayLabel}: Floor — mapped type id {floorTypeIdVal} not found in project.");
                    }
                    else
                    {
                        var outcome = RunInTransaction(doc,
                            "RoomDocumenter — Create Floor",
                            "RoomDocumenter — Remove Existing Floor",
                            () => _floorSvc.ReconcileFloor(doc, roomData, floorTypeId,
                                    result.SkipReasons));
                        ApplyFloorCounts(outcome, result);
                    }
                }
                else
                {
                    result.SkipReasons.Add(
                        $"{roomData.DisplayLabel}: Floor — no mapping for finish '{roomData.FloorFinish}'.");
                }
            }
            // FloorFinish is empty string when the parameter is unset — nothing to log.

            // ── Ceiling ────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(roomData.CeilingFinish))
            {
                if (mapping.CeilingMappings.TryGetValue(roomData.CeilingFinish, out long ceilTypeIdVal))
                {
                    var ceilTypeId = new ElementId(ceilTypeIdVal);
                    if (doc.GetElement(ceilTypeId) == null)
                    {
                        result.SkipReasons.Add(
                            $"{roomData.DisplayLabel}: Ceiling — mapped type id {ceilTypeIdVal} not found in project.");
                    }
                    else
                    {
                        var outcome = RunInTransaction(doc,
                            "RoomDocumenter — Create Ceiling",
                            "RoomDocumenter — Remove Existing Ceiling",
                            () => _ceilingSvc.ReconcileCeiling(doc, roomData, ceilTypeId,
                                    result.SkipReasons));
                        ApplyCeilingCounts(outcome, result);
                    }
                }
                else
                {
                    result.SkipReasons.Add(
                        $"{roomData.DisplayLabel}: Ceiling — no mapping for finish '{roomData.CeilingFinish}'.");
                }
            }

            // ── Baseboard ──────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(roomData.BaseFinish))
            {
                if (mapping.BaseboardMappings.TryGetValue(roomData.BaseFinish, out long sweepTypeIdVal))
                {
                    var sweepTypeId = new ElementId(sweepTypeIdVal);
                    if (doc.GetElement(sweepTypeId) == null)
                    {
                        result.SkipReasons.Add(
                            $"{roomData.DisplayLabel}: Baseboard — mapped type id {sweepTypeIdVal} not found in project.");
                    }
                    else
                    {
                        using var trans = new Transaction(doc,
                            "RoomDocumenter — Create Baseboard");
                        trans.Start();
                        SetFailureHandler(trans);
                        var (c, u, uc) = _baseboardSvc.ReconcileBaseboards(
                            doc, roomData, sweepTypeId, result.SkipReasons);
                        trans.Commit();

                        result.BaseboardsCreated   += c;
                        result.BaseboardsUpdated   += u;
                        result.BaseboardsUnchanged += uc;
                    }
                }
                else
                {
                    result.SkipReasons.Add(
                        $"{roomData.DisplayLabel}: Baseboard — no mapping for finish '{roomData.BaseFinish}'.");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // RoomData builder
        // ─────────────────────────────────────────────────────────────────

        private static RoomData BuildRoomData(Document doc, Room room, List<string> log)
        {
            var data = new RoomData
            {
                RoomId  = room.Id,
                Number  = room.Number,
                Name    = room.Name,
                LevelId = room.LevelId
            };

            // Finish parameters
            data.FloorFinish   = GetFinishParam(room, BuiltInParameter.ROOM_FINISH_FLOOR);
            data.CeilingFinish = GetFinishParam(room, BuiltInParameter.ROOM_FINISH_CEILING);
            data.WallFinish    = GetFinishParam(room, BuiltInParameter.ROOM_FINISH_WALL);
            data.BaseFinish    = GetFinishParam(room, BuiltInParameter.ROOM_BASE_FINISH);

            // Level elevation
            var level = doc.GetElement(room.LevelId) as Level;
            if (level != null) data.LevelElevation = level.Elevation;

            // Room height
            var heightParam = room.get_Parameter(BuiltInParameter.ROOM_HEIGHT);
            if (heightParam != null && heightParam.HasValue)
                data.RoomHeight = heightParam.AsDouble();

            // Boundary
            var opts = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };
            var boundaries = room.GetBoundarySegments(opts);
            if (boundaries != null && boundaries.Count > 0)
                data.OuterBoundary = boundaries[0];
            else
                log.Add($"{data.DisplayLabel}: could not retrieve boundary segments.");

            // Bounding box
            data.BoundingBox = room.get_BoundingBox(null);

            return data;
        }

        private static string GetFinishParam(Room room, BuiltInParameter bip)
        {
            var p = room.get_Parameter(bip);
            if (p == null) return string.Empty;
            return p.AsString() ?? string.Empty;
        }

        // ─────────────────────────────────────────────────────────────────
        // Transaction helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs <paramref name="work"/> inside a transaction.  If the outcome
        /// is <see cref="ReconcileOutcome.Updated"/>, the remove was already
        /// performed inside the transaction; we commit that work.
        /// </summary>
        private static ReconcileOutcome RunInTransaction(
            Document doc,
            string createTransName,
            string removeTransName,
            Func<ReconcileOutcome> work)
        {
            using var trans = new Transaction(doc, createTransName);
            trans.Start();
            SetFailureHandler(trans);
            var outcome = work();
            trans.Commit();
            return outcome;
        }

        private static void SetFailureHandler(Transaction trans)
        {
            var opts = trans.GetFailureHandlingOptions();
            opts.SetFailuresPreprocessor(new IgnoreWarningsPreprocessor());
            trans.SetFailureHandlingOptions(opts);
        }

        // ─────────────────────────────────────────────────────────────────
        // Count appliers
        // ─────────────────────────────────────────────────────────────────

        private static void ApplyFloorCounts(ReconcileOutcome outcome, DocumentationResult r)
        {
            switch (outcome)
            {
                case ReconcileOutcome.Created:   r.FloorsCreated++;   break;
                case ReconcileOutcome.Updated:   r.FloorsUpdated++;   break;
                case ReconcileOutcome.Unchanged: r.FloorsUnchanged++; break;
            }
        }

        private static void ApplyCeilingCounts(ReconcileOutcome outcome, DocumentationResult r)
        {
            switch (outcome)
            {
                case ReconcileOutcome.Created:   r.CeilingsCreated++;   break;
                case ReconcileOutcome.Updated:   r.CeilingsUpdated++;   break;
                case ReconcileOutcome.Unchanged: r.CeilingsUnchanged++; break;
            }
        }
    }
}
