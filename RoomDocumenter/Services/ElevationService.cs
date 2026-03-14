// RoomDocumenter | Revit 2024 | net48
// API NOTES:
//   ElevationMarker.CreateElevationMarker(doc, viewFamilyTypeId, XYZ, int scale)
//     — Verified: revitapidocs.com/2024/
//   ElevationMarker.CreateElevation(doc, activeViewId, int index)
//     — Verified: revitapidocs.com/2024/
//   View.CropBox / CropBoxActive / CropBoxVisible
//     — Verified: revitapidocs.com/2024/
//   doc.Regenerate() required before reading CropBox on newly created elevation views
//     — Verified: revitapidocs.com/2024/
//   ElementId.Value (Int64) — Verified: revitapidocs.com/2024/ (never IntegerValue)
//   UnitUtils.ConvertToInternalUnits — Verified: revitapidocs.com/2024/
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using RoomDocumenter.Helpers;
using RoomDocumenter.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoomDocumenter.Services
{
    /// <summary>
    /// Creates four interior elevation views per room, applies crop boxes sized
    /// to the room boundary plus a user-defined offset, and names each view
    /// using the compass direction derived from the stored
    /// <see cref="ProjectNorthOrientation"/>.
    ///
    /// <para>Transaction scope: all elevation operations for all selected rooms
    /// are wrapped in a single TransactionGroup by the caller
    /// (<see cref="Commands.InteriorElevationCmd"/>).  Individual Transactions
    /// are started per room by this service.</para>
    /// </summary>
    public class ElevationService
    {
        private readonly Document _doc;
        private readonly ElementId _interiorElevationVftId;
        private readonly ProjectNorthOrientation _orientation;
        private readonly bool _orientationFallback;

        // ─────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the service, resolves the orientation from Extensible
        /// Storage, and caches the Interior Elevation ViewFamilyType.
        /// Throws <see cref="InvalidOperationException"/> if the "Interior
        /// Elevation" ViewFamilyType cannot be found.
        /// </summary>
        /// <param name="doc">Active Revit document.</param>
        public ElevationService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));

            // Resolve ViewFamilyType
            _interiorElevationVftId = FindInteriorElevationVft(doc)
                ?? throw new InvalidOperationException(
                    "No ViewFamilyType named \"Interior Elevation\" was found in this project. " +
                    "Load or create that type before running this command.");

            // Read orientation
            (_orientation, _orientationFallback) = ReadOrientation(doc);
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates four interior elevation views for all <paramref name="rooms"/>.
        /// Each room is processed inside its own Transaction.
        /// </summary>
        /// <param name="rooms">Selected rooms to document.</param>
        /// <param name="activeViewId">
        /// Id of the plan view used when creating elevation markers.
        /// </param>
        /// <param name="cropOffsetMm">
        /// Crop expansion in millimetres applied to all sides of the room boundary.
        /// </param>
        /// <returns>Aggregated <see cref="ElevationResult"/>.</returns>
        public ElevationResult CreateElevationsForRooms(
            IList<Room> rooms,
            ElementId activeViewId,
            double cropOffsetMm)
        {
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));

            var result = new ElevationResult();
            double offsetFeet = UnitUtils.ConvertToInternalUnits(cropOffsetMm, UnitTypeId.Millimeters);

            if (_orientationFallback)
                result.SkipReasons.Add(
                    "WARNING: Building orientation could not be read from Extensible Storage — " +
                    "elevation views are named using fallback index-based direction labels.");

            foreach (var room in rooms)
            {
                if (room == null || room.Area <= 0)
                {
                    result.RoomsSkipped++;
                    result.SkipReasons.Add(
                        $"Room {room?.Number} {room?.Name}: not placed — skipped.");
                    continue;
                }

                using var trans = new Transaction(_doc,
                    $"RoomDocumenter — Create Interior Elevations ({room.Number} {room.Name})");
                trans.Start();
                SetFailureHandler(trans);

                try
                {
                    var viewIds = CreateForRoom(room, activeViewId, offsetFeet, result.SkipReasons);
                    trans.Commit();

                    result.RoomsProcessed++;
                    result.ElevationsCreated += viewIds.Count;
                    if (viewIds.Count > 0)
                        result.ViewIdsByRoom[room.Id] = viewIds;
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    result.RoomsSkipped++;
                    result.SkipReasons.Add(
                        $"Room {room.Number} {room.Name}: exception — {ex.Message}");
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // Core per-room logic
        // ─────────────────────────────────────────────────────────────────

        private List<ElementId> CreateForRoom(
            Room room,
            ElementId activeViewId,
            double offsetFeet,
            List<string> log)
        {
            var createdIds = new List<ElementId>();

            // Compute centroid
            var bb = room.get_BoundingBox(null);
            if (bb == null)
            {
                log.Add($"Room {room.Number} {room.Name}: no bounding box — skipped.");
                return createdIds;
            }

            double levelElev = 0;
            var level = _doc.GetElement(room.LevelId) as Level;
            if (level != null) levelElev = level.Elevation;

            var centroid = CurveLoopHelper.ComputeCentroid(bb, levelElev);

            // Create elevation marker
            // API NOTE: ElevationMarker.CreateElevationMarker(doc, vftId, XYZ, scale)
            // Verified: revitapidocs.com/2024/
            ElevationMarker marker;
            try
            {
                marker = ElevationMarker.CreateElevationMarker(
                    _doc, _interiorElevationVftId, centroid, 100);
            }
            catch (Exception ex)
            {
                log.Add($"Room {room.Number} {room.Name}: ElevationMarker creation failed — {ex.Message}");
                return createdIds;
            }

            // Create four elevation views (indices 0–3)
            var views = new ViewSection[4];
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    // API NOTE: marker.CreateElevation(doc, activeViewId, index)
                    // Verified: revitapidocs.com/2024/
                    views[i] = marker.CreateElevation(_doc, activeViewId, i);
                }
                catch (Exception ex)
                {
                    log.Add($"Room {room.Number} {room.Name}: CreateElevation(index={i}) failed — {ex.Message}");
                }
            }

            // Regenerate required before accessing CropBox on newly created views
            // Verified: revitapidocs.com/2024/
            _doc.Regenerate();

            // Get room height
            double roomHeight = 0;
            var heightParam = room.get_Parameter(BuiltInParameter.ROOM_HEIGHT);
            if (heightParam != null && heightParam.HasValue)
                roomHeight = heightParam.AsDouble();
            if (roomHeight <= 0) roomHeight = UnitUtils.ConvertToInternalUnits(2700, UnitTypeId.Millimeters);

            for (int i = 0; i < 4; i++)
            {
                var view = views[i];
                if (view == null) continue;

                // Apply crop box
                ApplyCropBox(view, bb, levelElev, roomHeight, offsetFeet, log,
                    $"Room {room.Number} {room.Name}");

                // Name the view
                var direction = GetCompassLabel(view.ViewDirection, _orientation);
                var proposedName = $"{room.Number} - {room.Name} - {direction}";
                view.Name = EnsureUniqueName(proposedName);

                createdIds.Add(view.Id);
            }

            return createdIds;
        }

        // ─────────────────────────────────────────────────────────────────
        // Crop box calculation
        // ─────────────────────────────────────────────────────────────────

        private static void ApplyCropBox(
            ViewSection view,
            BoundingBoxXYZ roomBb,
            double levelElev,
            double roomHeight,
            double offsetFeet,
            List<string> log,
            string roomLabel)
        {
            // CropBox is in view-local coordinates:
            //   X = horizontal (view right direction)
            //   Y = vertical   (up)
            //   Z = depth      (far clip, negative = into scene)
            //
            // Strategy: project room XY extents onto the view's right axis for width;
            // use room height for vertical; offset is added on all sides.
            //
            // API NOTE: View.CropBox, CropBoxActive, CropBoxVisible
            // Verified: revitapidocs.com/2024/

            try
            {
                var existingBox = view.CropBox;
                var viewDir   = view.ViewDirection.Normalize();   // into the wall
                var rightDir  = view.RightDirection.Normalize();  // horizontal

                // Project room corners onto right axis
                var corners = new[]
                {
                    new XYZ(roomBb.Min.X, roomBb.Min.Y, 0),
                    new XYZ(roomBb.Max.X, roomBb.Min.Y, 0),
                    new XYZ(roomBb.Max.X, roomBb.Max.Y, 0),
                    new XYZ(roomBb.Min.X, roomBb.Max.Y, 0)
                };

                // Room centre in world XY
                double cx = (roomBb.Min.X + roomBb.Max.X) / 2.0;
                double cy = (roomBb.Min.Y + roomBb.Max.Y) / 2.0;

                double minProj = double.MaxValue;
                double maxProj = double.MinValue;

                foreach (var corner in corners)
                {
                    double proj = rightDir.X * (corner.X - cx) + rightDir.Y * (corner.Y - cy);
                    if (proj < minProj) minProj = proj;
                    if (proj > maxProj) maxProj = proj;
                }

                double halfWidth   = Math.Max(maxProj - minProj, 0) / 2.0 + offsetFeet;
                double minHeight   = levelElev - offsetFeet;
                double maxHeight   = levelElev + roomHeight + offsetFeet;
                double depthExtent = Math.Max(roomBb.Max.X - roomBb.Min.X,
                                              roomBb.Max.Y - roomBb.Min.Y)
                                   + offsetFeet * 2.0;

                // Clamp to minimum size (100 mm) to avoid zero/inverted box
                double minSizeFeet = UnitUtils.ConvertToInternalUnits(100, UnitTypeId.Millimeters);
                if (halfWidth * 2 < minSizeFeet)
                {
                    halfWidth = minSizeFeet / 2.0;
                    log.Add($"{roomLabel}: Crop box width clamped to 100 mm minimum.");
                }
                if (maxHeight - minHeight < minSizeFeet)
                {
                    maxHeight = minHeight + minSizeFeet;
                    log.Add($"{roomLabel}: Crop box height clamped to 100 mm minimum.");
                }

                var newBox = new BoundingBoxXYZ
                {
                    Transform = existingBox.Transform,
                    Min = new XYZ(-halfWidth, minHeight - levelElev, existingBox.Min.Z),
                    Max = new XYZ( halfWidth, maxHeight - levelElev, depthExtent)
                };

                view.CropBox         = newBox;
                view.CropBoxActive   = true;
                view.CropBoxVisible  = true;
            }
            catch (Exception ex)
            {
                log.Add($"{roomLabel}: CropBox assignment failed — {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Compass direction mapping
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts a world-space view direction vector to a compass label
        /// ("North", "South", "East", "West") adjusted for the building's
        /// stored <see cref="ProjectNorthOrientation"/>.
        /// </summary>
        private static string GetCompassLabel(XYZ viewDir, ProjectNorthOrientation orientation)
        {
            var norm  = viewDir.Normalize();
            // Angle from +Y axis, clockwise (same convention as the PDG Elevation Builder)
            double angle = (Math.Atan2(norm.Y, norm.X) * 180.0 / Math.PI) + 90.0;

            switch (orientation)
            {
                case ProjectNorthOrientation.East:  angle -= 270; break;
                case ProjectNorthOrientation.South: angle -= 180; break;
                case ProjectNorthOrientation.West:  angle -= 90;  break;
                // North: no adjustment
            }

            angle = (angle + 360) % 360;

            if (angle >= 315 || angle < 45)  return "North";
            if (angle >= 45  && angle < 135) return "West";
            if (angle >= 135 && angle < 225) return "South";
            return "East";
        }

        // ─────────────────────────────────────────────────────────────────
        // Orientation reader
        // ─────────────────────────────────────────────────────────────────

        private static (ProjectNorthOrientation Orientation, bool Fallback) ReadOrientation(Document doc)
        {
            try
            {
                var schema = Schema.Lookup(OrientationSchemaConstants.SchemaGuid);
                if (schema == null) return (ProjectNorthOrientation.North, true);

                var entity = doc.GetEntity(schema);
                if (!entity.IsValid()) return (ProjectNorthOrientation.North, true);

                int raw = entity.Get<int>(OrientationSchemaConstants.FieldName);
                if (Enum.IsDefined(typeof(ProjectNorthOrientation), raw))
                    return ((ProjectNorthOrientation)raw, false);

                return (ProjectNorthOrientation.North, true);
            }
            catch
            {
                return (ProjectNorthOrientation.North, true);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Utilities
        // ─────────────────────────────────────────────────────────────────

        private static ElementId? FindInteriorElevationVft(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft =>
                    vft.ViewFamily == ViewFamily.Elevation &&
                    vft.Name.IndexOf("Interior Elevation",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                ?.Id;
        }

        private string EnsureUniqueName(string proposed)
        {
            var existing = new HashSet<string>(
                new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => v.Name != null)
                    .Select(v => v.Name),
                StringComparer.OrdinalIgnoreCase);

            if (!existing.Contains(proposed)) return proposed;

            int counter = 1;
            string candidate;
            do { candidate = $"{proposed} ({counter++})"; }
            while (existing.Contains(candidate));
            return candidate;
        }

        private static void SetFailureHandler(Transaction trans)
        {
            var options = trans.GetFailureHandlingOptions();
            options.SetFailuresPreprocessor(new IgnoreWarningsPreprocessor());
            trans.SetFailureHandlingOptions(options);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Shared IFailuresPreprocessor — suppresses expected auto-join warnings
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Suppresses Revit warning-level failures that arise during automated
    /// geometry creation (floor/ceiling auto-join, etc.).  Error-level failures
    /// are passed through unchanged.
    /// </summary>
    internal sealed class IgnoreWarningsPreprocessor : IFailuresPreprocessor
    {
        /// <inheritdoc />
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            foreach (var msg in failuresAccessor.GetFailureMessages())
            {
                if (msg.GetSeverity() == FailureSeverity.Warning)
                    failuresAccessor.DeleteWarning(msg);
            }
            return FailureProcessingResult.Continue;
        }
    }
}
