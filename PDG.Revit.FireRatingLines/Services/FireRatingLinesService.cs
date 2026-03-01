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
    /// Encapsulates all four stages of the fire-rating lines workflow:
    /// 1. Discover fire-rated wall types — returns wallTypeId → ratingKey for every rated type.
    /// 2. Resolve matching GraphicsStyle line styles.
    /// 3. Collect fire-rated walls visible in any non-template plan/section view.
    /// 4. Delete stale lines and draw fresh detail lines.
    /// </summary>
    public class FireRatingLinesService
    {
        // ─────────────────────────────────────────────────────────────────────
        // Stage 1 — Fire-Rated WallType Discovery
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a mapping of WallType.Id.Value → trimmed fire rating key for every WallType
        /// in the document whose Fire Rating type parameter is non-null and non-empty.
        ///
        /// Using wallTypeId as key (rather than ratingKey) ensures that ALL rated wall types
        /// are represented — including multiple wall types that share the same rating string.
        /// Stage 3 consumes this dictionary directly for O(1) wall-type lookup.
        /// Stage 2 receives the unique rating keys via .Values.Distinct().
        /// </summary>
        // PDG API NOTE 2026-03-01: FilteredElementCollector.OfClass(typeof(WallType)).WhereElementIsElementType()
        //   Verified: revitapidocs.com/2024/ — returns WallType elements only.
        // PDG API NOTE 2026-03-01: BuiltInParameter.DOOR_FIRE_RATING
        //   Verified: revitapidocs.com/2024/ — DOOR_FIRE_RATING is the correct built-in enum member
        //   for the "Fire Rating" type parameter on WallType (and Door) elements in Revit 2024.
        //   The earlier WALL_ATTR_FIRE_RATING_PARAM name does not exist in the Revit 2024 API.
        //   get_Parameter(BuiltInParameter) is locale-independent and always preferred over
        //   LookupParameter("Fire Rating"), which searches by display name and may return null
        //   in non-English Revit installations or on types where no value has been set yet.
        //   LookupParameter("Fire Rating") is kept as a fallback to support shared/project
        //   parameters added under the same display name.
        // PDG API NOTE 2026-03-01: ElementId.Value (Int64)
        //   Use .Value throughout. Never deprecated .IntegerValue.
        public Dictionary<long, string> GetFireRatedWallTypes(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var result = new Dictionary<long, string>();

            var wallTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .WhereElementIsElementType()
                .Cast<WallType>();

            foreach (var wt in wallTypes)
            {
                // PDG API NOTE 2026-03-01: BuiltInParameter.DOOR_FIRE_RATING
                //   Verified: revitapidocs.com/2024/ — primary lookup by internal enum (locale-safe).
                //   LookupParameter("Fire Rating") is the fallback for shared/project parameters.
                var param = wt.get_Parameter(BuiltInParameter.DOOR_FIRE_RATING)
                          ?? wt.LookupParameter("Fire Rating");
                if (param == null || param.StorageType != StorageType.String) continue;

                var ratingValue = param.AsString();
                if (string.IsNullOrWhiteSpace(ratingValue)) continue;

                result[wt.Id.Value] = ratingValue.Trim();
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 2 — Line Style Resolution
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// For each unique rating key, finds the GraphicsStyle (OST_Lines projection subcategory)
        /// whose name matches the key (case-insensitive, trimmed — F-11 enhanced).
        /// Returns only the keys that have a matching style; unmatched keys are omitted and
        /// the caller is responsible for recording them as warnings.
        /// </summary>
        // PDG API NOTE 2026-03-01: Category.GetCategory(doc, BuiltInCategory.OST_Lines)
        //   Verified: revitapidocs.com/2024/ — returns the "Lines" drafting category.
        // PDG API NOTE 2026-03-01: GraphicsStyleType.Projection
        //   Verified: revitapidocs.com/2024/ — use Projection for detail lines (not Cut).
        // PDG API NOTE 2026-03-01: gs.GraphicsStyleCategory.Id.Value
        //   Use .Value (Int64) — never deprecated .IntegerValue.
        // PDG: Check shared/PDG.Revit.Shared/ — GetLineStyleByName() may already exist.
        public Dictionary<string, GraphicsStyle> GetMatchingLineStyles(
            Document doc,
            IEnumerable<string> ratingKeys)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (ratingKeys == null) throw new ArgumentNullException(nameof(ratingKeys));

            var result = new Dictionary<string, GraphicsStyle>(StringComparer.OrdinalIgnoreCase);

            // Get the ElementId of the OST_Lines drafting category.
            var ostLinesCategory = Category.GetCategory(doc, BuiltInCategory.OST_Lines);
            if (ostLinesCategory == null)
            {
                PDGLogger.Warning("PDG FireRatingLines: OST_Lines category not found in document.");
                return result;
            }
            long ostLinesId = ostLinesCategory.Id.Value;

            // Collect all projection GraphicsStyles that belong to OST_Lines or its subcategories.
            // Line styles in Revit are stored as GraphicsStyle elements whose category is either:
            //   - OST_Lines itself (for built-in "Lines" style), or
            //   - a subcategory of OST_Lines (for user-defined line styles in Settings > Line Styles).
            var allStyles = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle))
                .Cast<GraphicsStyle>()
                .Where(gs =>
                {
                    if (gs.GraphicsStyleType != GraphicsStyleType.Projection) return false;
                    var cat = gs.GraphicsStyleCategory;
                    if (cat == null) return false;
                    // Direct OST_Lines membership.
                    if (cat.Id.Value == ostLinesId) return true;
                    // Subcategory of OST_Lines (user-defined line styles).
                    if (cat.Parent != null && cat.Parent.Id.Value == ostLinesId) return true;
                    return false;
                })
                .ToList();

            // F-11 enhanced: case-insensitive + trimmed name match.
            foreach (var key in ratingKeys.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var trimmedKey = key.Trim();
                var match = allStyles.FirstOrDefault(gs =>
                    string.Equals(gs.Name.Trim(), trimmedKey, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    result[key] = match;
                else
                    PDGLogger.Warning($"PDG FireRatingLines: No line style found for rating '{key}'.");
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 2b — Ensure Standard Fire Rating Line Styles Exist
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Ensures that a line style (OST_Lines projection subcategory) exists in the
        /// document for every entry in <see cref="FireRatingStandards.StandardRatings"/>.
        /// Any missing styles are created in a single Transaction before drawing begins.
        /// Returns a dictionary of ratingName → GraphicsStyle for all standard ratings.
        /// </summary>
        // PDG API NOTE 2026-03-01: doc.Settings.Categories.NewSubcategory(parentCategory, name)
        //   Verified: revitapidocs.com/2024/ — creates a new named subcategory under the given
        //   parent category. Must be called inside a Transaction.
        // PDG API NOTE 2026-03-01: Category.GetGraphicsStyle(GraphicsStyleType.Projection)
        //   Verified: revitapidocs.com/2024/ — returns the projection GraphicsStyle element for
        //   the subcategory. Revit creates both Projection and Cut styles automatically.
        public Dictionary<string, GraphicsStyle> EnsureFireRatingLineStyles(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // Find any already-existing standard line styles.
            var result = GetMatchingLineStyles(doc, FireRatingStandards.StandardRatings);

            var missing = FireRatingStandards.StandardRatings
                .Where(r => !result.ContainsKey(r))
                .ToList();

            if (missing.Count == 0) return result;

            // Create a subcategory under OST_Lines for each missing rating name.
            using (var tx = new Transaction(doc, "PDG: Create Fire Rating Line Styles"))
            {
                tx.Start();

                var linesCategory = Category.GetCategory(doc, BuiltInCategory.OST_Lines);
                if (linesCategory == null)
                {
                    PDGLogger.Warning(
                        "PDG FireRatingLines: OST_Lines category not found — cannot create line styles.");
                    tx.RollBack();
                    return result;
                }

                foreach (var name in missing)
                {
                    var subCat = doc.Settings.Categories.NewSubcategory(linesCategory, name);
                    var gs = subCat.GetGraphicsStyle(GraphicsStyleType.Projection);
                    if (gs != null)
                    {
                        result[name] = gs;
                        PDGLogger.Info($"PDG FireRatingLines: Created line style '{name}'.");
                    }
                    else
                    {
                        PDGLogger.Warning(
                            $"PDG FireRatingLines: Created subcategory '{name}' but could not " +
                            $"retrieve its projection GraphicsStyle.");
                    }
                }

                tx.Commit();
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 3 — Walls in Sheeted Views
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Collects all fire-rated wall instances that are visible in any non-template
        /// FloorPlan, CeilingPlan, or Section view that is currently placed on a sheet.
        /// Applies cut-plane straddling checks for plan views. Deduplicates by (wallId, viewId)
        /// so each annotation is drawn exactly once even if a wall appears in multiple views.
        /// </summary>
        // PDG API NOTE 2026-03-01: FilteredElementCollector(doc).OfClass(typeof(ViewSheet))
        //   Verified: revitapidocs.com/2024/ — returns all ViewSheet elements.
        //   sheet.GetAllViewports() returns the ElementIds of Viewport instances on the sheet.
        //   Each Viewport.ViewId identifies the view displayed in that viewport.
        // PDG API NOTE 2026-03-01: new FilteredElementCollector(doc, view.Id).OfClass(typeof(Wall))
        //   Verified: revitapidocs.com/2024/ — view-scoped collector returns elements visible in that view.
        //   ⚠ CRITICAL: This includes walls shown in projection below the cut plane.
        //   The straddling check is mandatory to annotate only walls that are actually cut.
        // PDG API NOTE 2026-03-01: ViewPlan.GetViewRange().GetOffset(PlanViewPlane.CutPlane)
        //   Verified: revitapidocs.com/2024/ — returns vertical offset from associated level.
        // PDG API NOTE 2026-03-01: wall.get_BoundingBox(null) — world-space bounding box.
        //   Verified: revitapidocs.com/2024/ — pass null for world space (not view-local).
        // TODO v2: RevitLinkInstance support — walls in linked models are not collected here.
        //   See RevitLinkInstance.GetTotalTransform() for the v2 approach.
        public List<FireRatingWall> GetFireRatedWallsInViews(
            Document doc,
            Dictionary<long, string> wallTypeIdToRating)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (wallTypeIdToRating == null) throw new ArgumentNullException(nameof(wallTypeIdToRating));

            var result = new List<FireRatingWall>();
            // Dedup key: (wallId, viewId) — prevents drawing the same line twice if the
            // same wall appears in multiple overlapping views or on multiple sheets.
            var seen = new HashSet<(long wallId, long viewId)>();

            // Build the set of view IDs that are placed on at least one sheet.
            // A view placed on two sheets still produces only one (wall, view) entry
            // because the dedup set above handles it.
            var sheetedViewIds = new HashSet<long>();
            foreach (var sheet in new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>())
            {
                foreach (var vpId in sheet.GetAllViewports())
                {
                    var vp = doc.GetElement(vpId) as Viewport;
                    if (vp != null)
                        sheetedViewIds.Add(vp.ViewId.Value);
                }
            }

            // Collect non-template plan and section views that are placed on sheets.
            var targetViews = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate &&
                            sheetedViewIds.Contains(v.Id.Value) &&
                            (v.ViewType == ViewType.FloorPlan ||
                             v.ViewType == ViewType.CeilingPlan ||
                             v.ViewType == ViewType.Section))
                .ToList();

            // For each view, collect walls and apply filters.
            foreach (var view in targetViews)
            {
                // Determine cut-plane elevation for plan views.
                double? cutElevation = null;
                if (view.ViewType == ViewType.FloorPlan || view.ViewType == ViewType.CeilingPlan)
                {
                    cutElevation = GetPlanCutElevation(view);
                    if (cutElevation == null) continue; // malformed view range — skip
                }

                var walls = new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(Wall))
                    .Cast<Wall>();

                foreach (var wall in walls)
                {
                    // Filter: only fire-rated wall types.
                    long wallTypeId = wall.WallType?.Id.Value ?? -1L;
                    if (!wallTypeIdToRating.TryGetValue(wallTypeId, out var ratingKey)) continue;

                    // Dedup check.
                    var dedupKey = (wall.Id.Value, view.Id.Value);
                    if (!seen.Add(dedupKey)) continue;

                    // Plan view: straddling check.
                    if (cutElevation.HasValue)
                    {
                        if (!WallStraddlesCutPlane(wall, cutElevation.Value)) continue;
                    }

                    // Curved-wall guard: skip Arc-based walls in v1.
                    if (wall.Location is LocationCurve locCurve && !(locCurve.Curve is Line))
                    {
                        PDGLogger.Info(
                            $"PDG FireRatingLines: Skipping curved wall Id={wall.Id.Value} " +
                            $"in view Id={view.Id.Value} (Arc-based — v1 limitation).");
                        continue;
                    }

                    result.Add(new FireRatingWall(wall.Id, view.Id, ratingKey, view.ViewType));
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 4 — Draw Detail Lines
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Deletes all existing fire-rating detail lines then draws fresh ones.
        /// Wrapped in a TransactionGroup → two named Transactions → Assimilate()
        /// so the user gets a single Undo entry in Revit's undo stack.
        /// </summary>
        // PDG API NOTE 2026-03-01: doc.Create.NewDetailCurve(view, curve)
        //   Verified: revitapidocs.com/2024/ — curve MUST lie in the view's sketch plane.
        //   For plan views: flatten Z to GenLevel.Elevation.
        //   For section views: use Z=0 in view-local space, then CropBox.Transform.
        // PDG API NOTE 2026-03-01: CurveElement.LineStyle (property setter)
        //   Verified: revitapidocs.com/2024/ — accepts a GraphicsStyle element.
        // PDG API NOTE 2026-03-01: TransactionGroup.Assimilate()
        //   Verified: revitapidocs.com/2024/ — merges child transactions into one undo entry.
        //   Both child transactions MUST be committed before calling Assimilate().
        // PDG: Check shared/PDG.Revit.Shared/ — ProjectCurveToViewSketchPlane() may exist.
        public FireRatingLinesResult DrawFireRatingLines(
            Document doc,
            List<FireRatingWall> wallsInViews,
            Dictionary<string, GraphicsStyle> lineStyles)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (wallsInViews == null) throw new ArgumentNullException(nameof(wallsInViews));
            if (lineStyles == null) throw new ArgumentNullException(nameof(lineStyles));

            var result = new FireRatingLinesResult();

            // Record which rating keys have no matching line style.
            var unmatchedKeys = wallsInViews
                .Select(w => w.RatingKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(k => !lineStyles.ContainsKey(k))
                .ToList();
            result.UnmatchedRatings.AddRange(unmatchedKeys);

            // Build the set of fire-rating GraphicsStyle Ids for the delete sweep.
            var fireRatingStyleIds = new HashSet<long>(
                lineStyles.Values.Select(gs => gs.Id.Value));

            // Collect view Ids that appear in wallsInViews (to scope the delete sweep).
            var viewIds = wallsInViews.Select(w => w.ViewId).Distinct().ToList();

            using (var tg = new TransactionGroup(doc, "PDG: Fire Rating Lines"))
            {
                tg.Start();

                // ── Transaction 1: Delete existing fire-rating detail lines ──────
                using (var txDelete = new Transaction(doc, "PDG: Delete Existing Fire Rating Lines"))
                {
                    txDelete.Start();
                    result.LinesDeleted = DeleteExistingFireRatingLines(doc, viewIds, fireRatingStyleIds);
                    txDelete.Commit();
                }

                // ── Transaction 2: Create fresh detail lines ─────────────────────
                using (var txCreate = new Transaction(doc, "PDG: Create Fire Rating Detail Lines"))
                {
                    txCreate.Start();

                    foreach (var fw in wallsInViews)
                    {
                        result.WallsProcessed++;

                        // Skip walls whose rating has no matching line style.
                        if (!lineStyles.TryGetValue(fw.RatingKey, out var lineStyle)) continue;

                        var wall = doc.GetElement(fw.WallId) as Wall;
                        var view = doc.GetElement(fw.ViewId) as View;
                        if (wall == null || !wall.IsValidObject || view == null || !view.IsValidObject) continue;

                        // Curved-wall guard (defensive second check).
                        if (!(wall.Location is LocationCurve locCurve) || !(locCurve.Curve is Line line3d))
                        {
                            result.SkippedCurvedWalls++;
                            continue;
                        }

                        try
                        {
                            Curve? sketchCurve = fw.ViewType == ViewType.Section
                                ? BuildSectionCurve(wall, view)
                                : BuildPlanCurve(line3d, view);

                            if (sketchCurve == null) continue;

                            var detailLine = doc.Create.NewDetailCurve(view, sketchCurve);
                            detailLine.LineStyle = lineStyle;
                            result.LinesDrawn++;
                        }
                        catch (Exception ex)
                        {
                            PDGLogger.Warning(
                                $"PDG FireRatingLines: NewDetailCurve failed for wall " +
                                $"Id={fw.WallId.Value} in view Id={fw.ViewId.Value}. {ex.Message}");
                        }
                    }

                    txCreate.Commit();
                }

                tg.Assimilate();
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Deletes all DetailCurve elements in the given views whose LineStyle is one of
        /// the fire-rating styles. Returns the count of successfully deleted elements.
        /// Per-element try/catch: protected or non-deletable elements are logged and skipped.
        /// </summary>
        // PDG API NOTE 2026-03-01: FilteredElementCollector(doc, viewId).OfClass(typeof(CurveElement))
        //   Verified: revitapidocs.com/2024/ — CurveElement is base class for DetailCurve and ModelCurve.
        //   Must check `is DetailCurve` to avoid deleting model lines.
        // PDG API NOTE 2026-03-01: doc.Delete(ElementId)
        //   Verified: revitapidocs.com/2024/ — throws InvalidOperationException if element is protected.
        private static int DeleteExistingFireRatingLines(
            Document doc,
            IEnumerable<ElementId> viewIds,
            HashSet<long> fireRatingStyleIds)
        {
            int deleted = 0;

            foreach (var viewId in viewIds)
            {
                // Materialise to a list before deletion to avoid modifying the collection mid-flight.
                var toDelete = new FilteredElementCollector(doc, viewId)
                    .OfClass(typeof(CurveElement))
                    .Cast<CurveElement>()
                    .Where(ce =>
                        ce is DetailCurve &&
                        ce.LineStyle != null &&
                        fireRatingStyleIds.Contains(ce.LineStyle.Id.Value))
                    .Select(ce => ce.Id)
                    .ToList();

                foreach (var id in toDelete)
                {
                    try
                    {
                        doc.Delete(id);
                        deleted++;
                    }
                    catch (Exception ex)
                    {
                        PDGLogger.Warning(
                            $"PDG FireRatingLines: Could not delete detail line Id={id.Value}. {ex.Message}");
                    }
                }
            }

            return deleted;
        }

        /// <summary>
        /// Projects a wall's 3-D centreline onto the plan view's sketch plane.
        /// The sketch plane for a FloorPlan / CeilingPlan is at Z = GenLevel.Elevation.
        /// ⚠ CRITICAL: Do NOT use cut-plane elevation — the sketch plane is at the level.
        /// Returns null if the flattened line is degenerate (zero length).
        /// </summary>
        // PDG API NOTE 2026-03-01: ViewPlan.GenLevel.Elevation
        //   Verified: revitapidocs.com/2024/ — elevation of the level associated with the view, in feet.
        // PDG: Check shared/PDG.Revit.Shared/ — ProjectCurveToViewSketchPlane() may already exist.
        private static Curve? BuildPlanCurve(Line line3d, View view)
        {
            var viewPlan = (ViewPlan)view;
            double levelZ = viewPlan.GenLevel.Elevation;

            var p0 = line3d.GetEndPoint(0);
            var p1 = line3d.GetEndPoint(1);

            var flatStart = new XYZ(p0.X, p0.Y, levelZ);
            var flatEnd   = new XYZ(p1.X, p1.Y, levelZ);

            if (flatStart.DistanceTo(flatEnd) < 1e-6)
            {
                PDGLogger.Warning("PDG FireRatingLines: Degenerate (zero-length) plan curve — skipped.");
                return null;
            }

            return Line.CreateBound(flatStart, flatEnd);
        }

        /// <summary>
        /// Builds a detail-line curve in a section view at the wall's centreline.
        /// The wall's bounding box in view-local coordinates gives horizontal extent (X)
        /// and height (Y). We draw a vertical line at the wall's X midpoint, from bbox.Min.Y
        /// to bbox.Max.Y, at Z=0 (on the view's sketch plane), then transform to world space
        /// via CropBox.Transform.
        /// Returns null if the bounding box is unavailable or the line is degenerate.
        /// </summary>
        // PDG API NOTE 2026-03-01: wall.get_BoundingBox(view) — view-local bounding box.
        //   Verified: revitapidocs.com/2024/ — returns bbox in view local coordinates when view is passed.
        //   Returns null if the element is not visible in the view.
        // PDG API NOTE 2026-03-01: View.CropBox.Transform
        //   Verified: revitapidocs.com/2024/ — Transform from view local coords to world coords.
        //   BasisZ = view depth direction. Z=0 in local space = the view's sketch plane.
        private static Curve? BuildSectionCurve(Wall wall, View view)
        {
            var bbox = wall.get_BoundingBox(view);
            if (bbox == null)
            {
                PDGLogger.Warning(
                    $"PDG FireRatingLines: No bounding box for wall Id={wall.Id.Value} " +
                    $"in section view Id={view.Id.Value} — skipped.");
                return null;
            }

            // Horizontal midpoint of the wall's cross-section in view-local space.
            double centerX = (bbox.Min.X + bbox.Max.X) / 2.0;

            // Z = 0 keeps the points on the view's sketch plane.
            var localBottom = new XYZ(centerX, bbox.Min.Y, 0.0);
            var localTop    = new XYZ(centerX, bbox.Max.Y, 0.0);

            if (localBottom.DistanceTo(localTop) < 1e-6)
            {
                PDGLogger.Warning(
                    $"PDG FireRatingLines: Degenerate section curve for wall Id={wall.Id.Value} — skipped.");
                return null;
            }

            // Transform from view-local to world coordinates.
            var transform = view.CropBox.Transform;
            var worldBottom = transform.OfPoint(localBottom);
            var worldTop    = transform.OfPoint(localTop);

            return Line.CreateBound(worldBottom, worldTop);
        }

        /// <summary>
        /// Returns the absolute cut-plane elevation (in Revit internal feet) for a plan view.
        /// Returns null if the view range or associated level cannot be resolved.
        /// </summary>
        // PDG API NOTE 2026-03-01: ViewPlan.GetViewRange().GetOffset(PlanViewPlane.CutPlane)
        //   Verified: revitapidocs.com/2024/ — offset is relative to the associated level.
        //   Absolute elevation = GenLevel.Elevation + offset.
        private static double? GetPlanCutElevation(View view)
        {
            if (!(view is ViewPlan viewPlan)) return null;

            var level = viewPlan.GenLevel;
            if (level == null) return null;

            var viewRange = viewPlan.GetViewRange();
            if (viewRange == null) return null;

            double levelElevation = level.Elevation;
            double cutOffset = viewRange.GetOffset(PlanViewPlane.CutPlane);
            return levelElevation + cutOffset;
        }

        /// <summary>
        /// Returns true if the wall's world-space bounding box straddles the cut elevation.
        /// A wall is "cut" if Min.Z &lt; cutElevation AND Max.Z &gt; cutElevation.
        /// </summary>
        private static bool WallStraddlesCutPlane(Wall wall, double cutElevation)
        {
            var bbox = wall.get_BoundingBox(null); // world space
            if (bbox == null) return false;
            return bbox.Min.Z < cutElevation && bbox.Max.Z > cutElevation;
        }
    }
}
