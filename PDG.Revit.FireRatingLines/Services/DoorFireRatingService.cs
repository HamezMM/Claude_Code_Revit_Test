// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
// All Revit API calls are confined to this service. No business logic in the Command class.

using Autodesk.Revit.DB;
using PDG.Revit.FireRatingLines.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDG.Revit.FireRatingLines.Services
{
    /// <summary>
    /// Encapsulates the two stages of the door fire rating update workflow:
    /// 1. Discover doors in fire-rated walls and compute the required door rating.
    /// 2. Write the door rating to each door's Fire Rating instance parameter.
    /// </summary>
    public class DoorFireRatingService
    {
        // ─────────────────────────────────────────────────────────────────────
        // Stage 1 — Discover Doors in Fire-Rated Walls
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Collects all door instances whose host wall has a standard fire rating,
        /// maps each wall rating to the corresponding door rating via
        /// <see cref="FireRatingStandards.WallToDoorRating"/>, and returns one
        /// <see cref="DoorFireRatingUpdate"/> record per door.
        ///
        /// Doors hosted in non-Wall elements (e.g. curtain walls) are silently skipped.
        /// Doors in walls without a recognised fire rating are counted separately in
        /// the result's <see cref="DoorFireRatingResult.DoorsInUnratedWalls"/> tally.
        /// </summary>
        // PDG API NOTE 2026-03-01: FilteredElementCollector.OfCategory(OST_Doors).OfClass(FamilyInstance)
        //   Verified: revitapidocs.com/2024/ — returns door FamilyInstance elements only.
        //   OfCategory before OfClass is slightly more efficient when filtering by both.
        // PDG API NOTE 2026-03-01: FamilyInstance.Host
        //   Verified: revitapidocs.com/2024/ — returns the host element (Wall, Floor, etc.).
        //   Cast to Wall; non-Wall hosts (curtain walls hosted as panels) return null on cast.
        // PDG API NOTE 2026-03-01: Wall.WallType.Id.Value
        //   Use .Value (Int64) — never deprecated .IntegerValue.
        public List<DoorFireRatingUpdate> GetDoorFireRatingUpdates(
            Document doc,
            Dictionary<long, string> wallTypeIdToRating)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (wallTypeIdToRating == null) throw new ArgumentNullException(nameof(wallTypeIdToRating));

            var updates = new List<DoorFireRatingUpdate>();

            var doors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>();

            foreach (var door in doors)
            {
                // Only process doors hosted in standard Wall elements.
                if (!(door.Host is Wall hostWall)) continue;

                long wallTypeId = hostWall.WallType?.Id.Value ?? -1L;

                // Wall has no fire rating — door is not in scope.
                if (!wallTypeIdToRating.TryGetValue(wallTypeId, out var wallRating)) continue;

                // Map wall rating → door rating (all standard wall ratings are covered).
                if (!FireRatingStandards.WallToDoorRating.TryGetValue(wallRating, out var doorRating))
                {
                    PDGLogger.Warning(
                        $"PDG DoorFireRating: No door rating mapping for wall rating '{wallRating}' " +
                        $"on door Id={door.Id.Value}. Door skipped.");
                    continue;
                }

                updates.Add(new DoorFireRatingUpdate(door.Id, hostWall.Id, wallRating, doorRating));
            }

            return updates;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 2 — Write Door Fire Ratings
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes the computed door fire rating to each door's Fire Rating instance
        /// parameter inside a single named Transaction.
        /// Sets <c>BuiltInParameter.DOOR_FIRE_RATING</c> first; falls back to
        /// <c>LookupParameter("Fire Rating")</c> for project/shared parameters using
        /// the same display name.
        /// Doors whose Fire Rating parameter is missing or read-only are skipped
        /// and recorded in <see cref="DoorFireRatingResult.DoorsSkipped"/>.
        /// </summary>
        // PDG API NOTE 2026-03-01: BuiltInParameter.DOOR_FIRE_RATING on FamilyInstance
        //   Verified: revitapidocs.com/2024/ — DOOR_FIRE_RATING exists as an INSTANCE parameter
        //   on door elements, allowing per-door overrides independent of the door type.
        //   Setting the instance parameter does NOT modify the door type or affect other doors.
        // PDG API NOTE 2026-03-01: Parameter.IsReadOnly
        //   Verified: revitapidocs.com/2024/ — check before calling Set() to avoid exceptions
        //   on formula-driven or locked parameters.
        // PDG API NOTE 2026-03-01: Transaction("PDG: Update Door Fire Ratings")
        //   Verified: revitapidocs.com/2024/ — single Transaction gives the user one Undo entry.
        public DoorFireRatingResult ApplyDoorFireRatings(
            Document doc,
            List<DoorFireRatingUpdate> updates)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (updates == null) throw new ArgumentNullException(nameof(updates));

            var result = new DoorFireRatingResult();

            using (var tx = new Transaction(doc, "PDG: Update Door Fire Ratings"))
            {
                tx.Start();

                foreach (var update in updates)
                {
                    var door = doc.GetElement(update.DoorId) as FamilyInstance;
                    if (door == null || !door.IsValidObject)
                    {
                        result.DoorsSkipped++;
                        PDGLogger.Warning(
                            $"PDG DoorFireRating: Door Id={update.DoorId.Value} is no longer valid — skipped.");
                        continue;
                    }

                    // Prefer the built-in instance parameter; fall back to display-name lookup
                    // for shared/project parameters exposed under the same name.
                    var param = door.get_Parameter(BuiltInParameter.DOOR_FIRE_RATING)
                             ?? door.LookupParameter("Fire Rating");

                    if (param == null || param.IsReadOnly || param.StorageType != StorageType.String)
                    {
                        result.DoorsSkipped++;
                        result.Warnings.Add(
                            $"Door Id={update.DoorId.Value}: Fire Rating parameter missing or read-only.");
                        PDGLogger.Warning(
                            $"PDG DoorFireRating: Door Id={update.DoorId.Value} — " +
                            $"Fire Rating parameter not found or read-only. Door skipped.");
                        continue;
                    }

                    param.Set(update.DoorRating);
                    result.DoorsUpdated++;
                    PDGLogger.Info(
                        $"PDG DoorFireRating: Door Id={update.DoorId.Value} — " +
                        $"set Fire Rating to '{update.DoorRating}' (wall: '{update.WallRating}').");
                }

                tx.Commit();
            }

            return result;
        }
    }
}
