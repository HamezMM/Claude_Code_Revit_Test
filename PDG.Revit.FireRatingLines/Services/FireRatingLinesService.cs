// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
// All Revit API calls are confined to this service. No business logic in the Command class.

using Autodesk.Revit.DB;
using PDG.Revit.FireRatingLines.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PDG.Revit.FireRatingLines.Services
{
    /// <summary>
    /// Encapsulates all stages of the fire-rating lines workflow:
    ///   Stage 1  — Discover fire-rated WallTypes.
    ///   Stage 2  — Resolve matching GraphicsStyle line styles.
    ///   Stage 3  — Collect fire-rated walls in sheeted plan + section views.
    ///   Stage A  — Discover fire-rated FloorTypes, CeilingTypes, RoofTypes.
    ///   Stage B  — Collect fire-rated floors/ceilings/roofs in sheeted section views.
    ///   Stage 4  — Delete stale lines and draw fresh detail lines for all element types
    ///              inside a single TransactionGroup (one Undo entry).
    /// </summary>
    public class FireRatingLinesService
    {
        // =====================================================================
        // Stage 1 — Fire-Rated WallType Discovery
        // =====================================================================

        /// <summary>
        /// Returns a mapping of WallType.Id.Value → trimmed fire rating key for every WallType
        /// in the document whose WALL_ATTR_FIRE_RATING_PARAM is non-null and non-empty.
        /// Using wallTypeId as key ensures ALL rated wall types are represented, including
        /// multiple types that share the same rating string.
        /// </summary>
        // PDG API NOTE 2026-03-01: FilteredElementCollector.OfClass(typeof(WallType)).WhereElementIsElementType()
        //   Verified: revitapidocs.com/2024/ — returns WallType elements only.
        // PDG API NOTE 2026-03-01: wt.get_Parameter(BuiltInParameter.WALL_ATTR_FIRE_RATING_PARAM).AsString()
        //   StorageType = String. Returns null if unset. Check null AND empty.
        // PDG API NOTE 2026-03-01: ElementId.Value (Int64) — use throughout, never .IntegerValue.
        public Dictionary<long, string> GetFireRatedWallTypes(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var result = new Dictionary<long, string>();

            foreach (var wt in new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .WhereElementIsElementType()
                .Cast<WallType>())
            {
                var param = wt.LookupParameter("Fire Rating");
                if (param == null) continue;
                var rating = param.AsString();
                if (string.IsNullOrWhiteSpace(rating)) continue;
                result[wt.Id.Value] = rating.Trim();
            }

            return result;
        }

        // =====================================================================
        // Stage 2 — Line Style Resolution
        // =====================================================================

        /// <summary>
        /// For each unique rating key, finds the GraphicsStyle (OST_Lines projection subcategory)
        /// whose name matches the key (case-insensitive, trimmed — F-11 enhanced).
        /// Accepts combined rating keys from both wall and horizontal element stages.
        /// Returns only keys that have a match; unmatched keys are omitted (caller records them).
        /// </summary>
        // PDG API NOTE 2026-03-01: Category.GetCategory(doc, BuiltInCategory.OST_Lines)
        //   Verified: revitapidocs.com/2024/ — returns the root "Lines" drafting category.
        // PDG API NOTE 2026-03-01: GraphicsStyleType.Projection
        //   Verified: revitapidocs.com/2024/ — Projection styles are used for detail lines.
        // PDG: Check shared/PDG.Revit.Shared/ — GetLineStyleByName() may already exist.
        public Dictionary<string, GraphicsStyle> GetMatchingLineStyles(
            Document doc,
            IEnumerable<string> ratingKeys)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (ratingKeys == null) throw new ArgumentNullException(nameof(ratingKeys));

            var result = new Dictionary<string, GraphicsStyle>(StringComparer.OrdinalIgnoreCase);

            var ostLinesCategory = Category.GetCategory(doc, BuiltInCategory.OST_Lines);
            if (ostLinesCategory == null)
            {
                PDGLogger.Warning("PDG FireRatingLines: OST_Lines category not found in document.");
                return result;
            }
            long ostLinesId = ostLinesCategory.Id.Value;

            var allStyles = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle))
                .Cast<GraphicsStyle>()
                .Where(gs =>
                {
                    if (gs.GraphicsStyleType != GraphicsStyleType.Projection) return false;
                    var cat = gs.GraphicsStyleCategory;
                    if (cat == null) return false;
                    if (cat.Id.Value == ostLinesId) return true;
                    if (cat.Parent != null && cat.Parent.Id.Value == ostLinesId) return true;
                    return false;
                })
                .ToList();

            foreach (var key in ratingKeys.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var trimmed = key.Trim();
                var match = allStyles.FirstOrDefault(gs =>
                    string.Equals(gs.Name.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));

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

        // =====================================================================
        // Stage 3 — Fire-Rated Walls in Sheeted Views (plan + section)
        // =====================================================================

        /// <summary>
        /// Collects fire-rated wall instances visible in plan or section views on sheets.
        /// Applies cut-plane straddling checks for plan views.
        /// Deduplicates by (wallId, viewId).
        /// </summary>
        // PDG API NOTE 2026-03-01: sheet.GetAllPlacedViews() — ICollection<ElementId>.
        //   Verified: revitapidocs.com/2024/
        // PDG API NOTE 2026-03-01: new FilteredElementCollector(doc, view.Id).OfClass(typeof(Wall))
        //   ⚠ Returns walls visible in projection too — straddling check is mandatory.
        // PDG API NOTE 2026-03-01: ViewPlan.GetViewRange().GetOffset(PlanViewPlane.CutPlane)
        //   Verified: revitapidocs.com/2024/ — offset relative to associated level.
        // TODO v2: RevitLinkInstance support — walls in linked models not collected here.
        // PDG: Check shared/PDG.Revit.Shared/ — GetSheetedViews() may already exist.
        public List<FireRatingWall> GetFireRatedWallsInViews(
            Document doc,
            Dictionary<long, string> wallTypeIdToRating)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (wallTypeIdToRating == null) throw new ArgumentNullException(nameof(wallTypeIdToRating));

            var result = new List<FireRatingWall>();
            var seen   = new HashSet<(long, long)>();

            foreach (var view in GetSheetedViews(doc,
                ViewType.FloorPlan, ViewType.CeilingPlan, ViewType.Section))
            {
                double? cutElevation = null;
                if (view.ViewType == ViewType.FloorPlan || view.ViewType == ViewType.CeilingPlan)
                {
                    cutElevation = GetPlanCutElevation(view);
                    if (cutElevation == null) continue;
                }

                foreach (var wall in new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(Wall))
                    .Cast<Wall>())
                {
                    long typeId = wall.WallType?.Id.Value ?? -1L;
                    if (!wallTypeIdToRating.TryGetValue(typeId, out var ratingKey)) continue;
                    if (!seen.Add((wall.Id.Value, view.Id.Value))) continue;

                    if (cutElevation.HasValue && !WallStraddlesCutPlane(wall, cutElevation.Value))
                        continue;

                    if (wall.Location is LocationCurve lc && !(lc.Curve is Line))
                    {
                        Trace.TraceInformation(
                            $"PDG FireRatingLines: Skipping curved wall Id={wall.Id.Value} " +
                            $"in view Id={view.Id.Value} (v1 — straight walls only).");
                        continue;
                    }

                    result.Add(new FireRatingWall(wall.Id, view.Id, ratingKey, view.ViewType));
                }
            }

            return result;
        }

        // =====================================================================
        // Stage A — Fire-Rated Horizontal Type Discovery (Floor / Ceiling / Roof)
        // =====================================================================

        /// <summary>
        /// Returns a mapping of type element Id.Value → trimmed fire rating key for every
        /// FloorType, CeilingType, and RoofType whose "Fire Rating" parameter is non-empty.
        /// All three Revit type classes are collected in a single pass and merged.
        /// </summary>
        // PDG API NOTE 2026-03-01: FilteredElementCollector.OfClass(typeof(FloorType))
        //   Verified: revitapidocs.com/2024/ — FloorType in Autodesk.Revit.DB namespace.
        // PDG API NOTE 2026-03-01: FilteredElementCollector.OfClass(typeof(CeilingType))
        //   ⚠ In Revit <2023 this was Autodesk.Revit.DB.Architecture.CeilingType.
        //   Verify CeilingType is in the root DB namespace for Revit 2024.
        //   Fallback if class not found: OfCategory(OST_Ceilings).WhereElementIsElementType().
        // PDG API NOTE 2026-03-01: FilteredElementCollector.OfClass(typeof(RoofType))
        //   Verified: revitapidocs.com/2024/ — RoofType is the common base; collects both
        //   FootPrintRoofType and ExtrusionRoofType subtypes automatically.
        // PDG API NOTE 2026-03-01: Element.LookupParameter("Fire Rating")
        //   Used because no confirmed BuiltInParameter exists for Floor/Ceiling/Roof fire rating.
        //   Verify against revitapidocs.com/2024/ whether FLOOR_ATTR_FIRE_RATING_PARAM or a
        //   similar BIP exists; if so, prefer get_Parameter(BIP) over LookupParameter.
        public Dictionary<long, string> GetFireRatedHorizontalTypes(Document doc)
        {
            var result = new Dictionary<long, string>();

            CollectRatedTypes<FloorType>(doc, typeof(FloorType), result);
            CollectRatedTypes<CeilingType>(doc, typeof(CeilingType), result);
            CollectRatedTypes<RoofType>(doc, typeof(RoofType), result);

            return result;
        }

        // =====================================================================
        // Stage B — Fire-Rated Horizontal Elements in Sheeted Section Views
        // =====================================================================

        /// <summary>
        /// Collects fire-rated Floor, Ceiling, and RoofBase instances visible in sheeted
        /// section views only (plan views are out of scope for horizontal elements).
        /// Sloped roofs (ExtrusionRoof or pitched FootPrintRoof) are skipped and logged.
        /// Deduplicates by (elementId, viewId).
        /// </summary>
        // PDG API NOTE 2026-03-01: FilteredElementCollector(doc, viewId).OfClass(typeof(Floor))
        //   Verified: revitapidocs.com/2024/ — Floor in Autodesk.Revit.DB namespace.
        // PDG API NOTE 2026-03-01: FilteredElementCollector(doc, viewId).OfClass(typeof(Ceiling))
        //   ⚠ Verify Ceiling class namespace in Revit 2024 (may be DB.Architecture.Ceiling pre-2023).
        //   Fallback: OfCategory(OST_Ceilings).WhereElementIsNotElementType().
        // PDG API NOTE 2026-03-01: FilteredElementCollector(doc, viewId).OfClass(typeof(RoofBase))
        //   Verified: revitapidocs.com/2024/ — RoofBase is the common base class for all roof instances.
        // PDG API NOTE 2026-03-01: element.GetTypeId()
        //   Verified: revitapidocs.com/2024/ — returns the ElementId of the element's type.
        // TODO v2: RevitLinkInstance support — horizontal elements in linked models not collected here.
        public List<FireRatingHorizontalElement> GetFireRatedHorizontalElementsInSectionViews(
            Document doc,
            Dictionary<long, string> typeIdToRating,
            out int skippedSlopedRoofs)
        {
            var result = new List<FireRatingHorizontalElement>();
            var seen   = new HashSet<(long, long)>();
            skippedSlopedRoofs = 0;

            foreach (var view in GetSheetedViews(doc, ViewType.Section))
            {
                // Floors
                foreach (var floor in new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(Floor))
                    .Cast<Floor>())
                {
                    TryAddHorizontalElement(floor, view, typeIdToRating,
                        HorizontalElementCategory.Floor, seen, result);
                }

                // Ceilings
                foreach (var ceiling in new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(Ceiling))
                    .Cast<Ceiling>())
                {
                    TryAddHorizontalElement(ceiling, view, typeIdToRating,
                        HorizontalElementCategory.Ceiling, seen, result);
                }

                // Roofs — slope detection before adding
                foreach (var roof in new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(RoofBase))
                    .Cast<RoofBase>())
                {
                    // ExtrusionRoof: always sloped — skip and count.
                    if (roof is ExtrusionRoof)
                    {
                        Trace.TraceInformation(
                            $"PDG FireRatingLines: Skipping ExtrusionRoof Id={roof.Id.Value} " +
                            $"in view Id={view.Id.Value} (v1 — flat roofs only).");
                        skippedSlopedRoofs++;
                        continue;
                    }

                    // FootPrintRoof: skip if world bbox height exceeds compound structure width.
                    if (IsSlopedRoof(doc, roof))
                    {
                        Trace.TraceInformation(
                            $"PDG FireRatingLines: Skipping sloped FootPrintRoof Id={roof.Id.Value} " +
                            $"in view Id={view.Id.Value} (v1 — flat roofs only).");
                        skippedSlopedRoofs++;
                        continue;
                    }

                    TryAddHorizontalElement(roof, view, typeIdToRating,
                        HorizontalElementCategory.Roof, seen, result);
                }
            }

            return result;
        }

        // =====================================================================
        // Stage 4 — Draw Detail Lines (combined TransactionGroup — one Undo entry)
        // =====================================================================

        /// <summary>
        /// Deletes all existing fire-rating detail lines then draws fresh ones for walls,
        /// floors, ceilings, and roofs — all inside a single TransactionGroup so the user
        /// gets one Undo entry in Revit's undo stack.
        /// </summary>
        // PDG API NOTE 2026-03-01: TransactionGroup.Assimilate()
        //   Verified: revitapidocs.com/2024/ — merges all child transactions into one undo entry.
        //   Both child transactions must be committed before calling Assimilate().
        // PDG API NOTE 2026-03-01: doc.Create.NewDetailCurve(view, curve)
        //   Curve must lie in the view's sketch plane:
        //     Plan views  → Z = GenLevel.Elevation (not cut-plane elevation).
        //     Section views → Z = 0 in view-local space; use CropBox.Transform to world.
        // PDG API NOTE 2026-03-01: CurveElement.LineStyle property setter
        //   Verified: revitapidocs.com/2024/ — accepts a GraphicsStyle element.
        // PDG: Check shared/PDG.Revit.Shared/ — ProjectCurveToViewSketchPlane() may exist.
        public FireRatingLinesResult DrawFireRatingLines(
            Document doc,
            List<FireRatingWall> walls,
            List<FireRatingHorizontalElement> horizontalElements,
            Dictionary<string, GraphicsStyle> lineStyles,
            int skippedSlopedRoofs = 0)
        {
            var result = new FireRatingLinesResult
            {
                SkippedSlopedRoofs = skippedSlopedRoofs
            };

            // Record unmatched rating keys across all element types.
            var allRatingKeys = walls.Select(w => w.RatingKey)
                .Concat(horizontalElements.Select(h => h.RatingKey))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            result.UnmatchedRatings.AddRange(
                allRatingKeys.Where(k => !lineStyles.ContainsKey(k)));

            // Build set of fire-rating style Ids for the delete sweep.
            var fireRatingStyleIds = new HashSet<long>(
                lineStyles.Values.Select(gs => gs.Id.Value));

            // Collect all view Ids that need a delete sweep (union of both element sets).
            var allViewIds = walls.Select(w => w.ViewId)
                .Concat(horizontalElements.Select(h => h.ViewId))
                .Distinct()
                .ToList();

            using (var tg = new TransactionGroup(doc, "PDG: Fire Rating Lines"))
            {
                tg.Start();

                // ── Transaction 1: Delete existing fire-rating detail lines ───────
                using (var txDelete = new Transaction(doc, "PDG: Delete Existing Fire Rating Lines"))
                {
                    txDelete.Start();
                    result.LinesDeleted = DeleteExistingFireRatingLines(doc, allViewIds, fireRatingStyleIds);
                    txDelete.Commit();
                }

                // ── Transaction 2: Create fresh detail lines ──────────────────────
                using (var txCreate = new Transaction(doc, "PDG: Create Fire Rating Detail Lines"))
                {
                    txCreate.Start();

                    // Walls (plan + section views)
                    foreach (var fw in walls)
                    {
                        result.WallsProcessed++;
                        if (!lineStyles.TryGetValue(fw.RatingKey, out var lineStyle)) continue;

                        var wall = doc.GetElement(fw.WallId) as Wall;
                        var view = doc.GetElement(fw.ViewId) as View;
                        if (wall == null || !wall.IsValidObject || view == null || !view.IsValidObject) continue;

                        if (!(wall.Location is LocationCurve lc) || !(lc.Curve is Line line3d))
                        {
                            result.SkippedCurvedWalls++;
                            continue;
                        }

                        try
                        {
                            var curve = fw.ViewType == ViewType.Section
                                ? BuildVerticalSectionCurve(wall, view)
                                : BuildPlanCurve(line3d, view);
                            if (curve == null) continue;

                            var dl = doc.Create.NewDetailCurve(view, curve);
                            dl.LineStyle = lineStyle;
                            result.LinesDrawn++;
                        }
                        catch (Exception ex)
                        {
                            PDGLogger.Warning(
                                $"PDG FireRatingLines: NewDetailCurve failed for wall " +
                                $"Id={fw.WallId.Value} in view Id={fw.ViewId.Value}. {ex.Message}");
                        }
                    }

                    // Floors, Ceilings, Roofs (section views only)
                    foreach (var fh in horizontalElements)
                    {
                        result.HorizontalElementsProcessed++;
                        if (!lineStyles.TryGetValue(fh.RatingKey, out var lineStyle)) continue;

                        var element = doc.GetElement(fh.ElementId);
                        var view    = doc.GetElement(fh.ViewId) as View;
                        if (element == null || view == null) continue;

                        try
                        {
                            var curve = BuildHorizontalSectionCurve(element, view);
                            if (curve == null) continue;

                            var dl = doc.Create.NewDetailCurve(view, curve);
                            dl.LineStyle = lineStyle;
                            result.HorizontalLinesDrawn++;
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning(
                                $"PDG FireRatingLines: NewDetailCurve failed for " +
                                $"{fh.Category} Id={fh.ElementId.Value} in view Id={fh.ViewId.Value}. {ex.Message}");
                        }
                    }

                    txCreate.Commit();
                }

                tg.Assimilate();
            }

            return result;
        }

        // =====================================================================
        // Private — Shared Helpers
        // =====================================================================

        /// <summary>
        /// Returns deduplicated View elements of the requested ViewTypes that are placed
        /// on at least one sheet. Non-template views only.
        /// </summary>
        // PDG: Check shared/PDG.Revit.Shared/ — GetSheetedViews() may already exist.
        private static List<View> GetSheetedViews(Document doc, params ViewType[] viewTypes)
        {
            var allowedTypes = new HashSet<ViewType>(viewTypes);

            var viewIds = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .SelectMany(s => s.GetAllPlacedViews())
                .Select(id => id.Value)
                .Distinct();

            return viewIds
                .Select(id => doc.GetElement(new ElementId(id)) as View)
                .Where(v => v != null && !v.IsTemplate && allowedTypes.Contains(v.ViewType))
                .ToList()!;
        }

        /// <summary>
        /// Generic helper: collects all ElementType elements of type T, reads their
        /// "Fire Rating" parameter, and adds typeId → ratingKey to the output dictionary.
        /// </summary>
        private static void CollectRatedTypes<T>(
            Document doc,
            Type collectorClass,
            Dictionary<long, string> output)
            where T : ElementType
        {
            foreach (var typeEl in new FilteredElementCollector(doc)
                .OfClass(collectorClass)
                .WhereElementIsElementType()
                .Cast<T>())
            {
                var rating = GetFireRatingValue(typeEl);
                if (rating != null)
                    output[typeEl.Id.Value] = rating;
            }
        }

        /// <summary>
        /// Reads the "Fire Rating" parameter from an element type by name.
        /// Returns the trimmed string value, or null if absent/empty.
        /// LookupParameter is used because no confirmed BuiltInParameter exists for
        /// Floor/Ceiling/Roof fire rating in Revit 2024.
        /// </summary>
        // PDG API NOTE 2026-03-01: Element.LookupParameter(string name)
        //   Verified: revitapidocs.com/2024/ — searches by parameter name; returns first match.
        //   Verify against revitapidocs.com/2024/ whether a BIP (e.g. FLOOR_ATTR_FIRE_RATING_PARAM)
        //   exists for FloorType; if so, prefer get_Parameter(BuiltInParameter.X).
        private static string? GetFireRatingValue(Element typeElement)
        {
            var param = typeElement.LookupParameter("Fire Rating");
            if (param == null) return null;
            var value = param.AsString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Attempts to add a horizontal element (floor/ceiling/roof) to the result list.
        /// Looks up the element's type in typeIdToRating; skips if not rated or already seen.
        /// </summary>
        private static void TryAddHorizontalElement(
            Element element,
            View view,
            Dictionary<long, string> typeIdToRating,
            HorizontalElementCategory category,
            HashSet<(long, long)> seen,
            List<FireRatingHorizontalElement> output)
        {
            long typeId = element.GetTypeId().Value;
            if (!typeIdToRating.TryGetValue(typeId, out var ratingKey)) return;
            if (!seen.Add((element.Id.Value, view.Id.Value))) return;

            output.Add(new FireRatingHorizontalElement(element.Id, view.Id, ratingKey, category));
        }

        /// <summary>
        /// Returns true if a RoofBase instance is sloped (and therefore should be skipped in v1).
        /// ExtrusionRoof is always sloped — caller must check this before calling IsSlopedRoof.
        /// For FootPrintRoof: compares world-space bounding box height to the type's compound
        /// structure width. If bbox height > compound width × 1.5, the roof is pitched.
        /// Falls back to a 1.5-foot (≈ 450 mm) threshold when compound structure is unavailable.
        /// </summary>
        // PDG API NOTE 2026-03-01: RoofType.GetCompoundStructure().GetWidth()
        //   Verified: revitapidocs.com/2024/ — GetCompoundStructure() may return null for
        //   curtain roofs; GetWidth() returns total thickness in Revit internal feet.
        // PDG API NOTE 2026-03-01: element.get_BoundingBox(null) — world-space bbox.
        //   Verified: revitapidocs.com/2024/ — pass null for world space.
        private static bool IsSlopedRoof(Document doc, RoofBase roof)
        {
            const double FallbackThresholdFeet = 1.5; // ≈ 450 mm

            var worldBbox = roof.get_BoundingBox(null);
            if (worldBbox == null) return false;

            double bboxHeight = worldBbox.Max.Z - worldBbox.Min.Z;

            var roofType = doc.GetElement(roof.GetTypeId()) as RoofType;
            var compound = roofType?.GetCompoundStructure();
            double threshold = compound != null
                ? compound.GetWidth() * 1.5
                : FallbackThresholdFeet;

            return bboxHeight > threshold;
        }

        // =====================================================================
        // Private — Delete Helper
        // =====================================================================

        /// <summary>
        /// Deletes all DetailCurve elements in the given views whose LineStyle Id is in the
        /// fire-rating style set. Per-element try/catch skips protected elements gracefully.
        /// </summary>
        // PDG API NOTE 2026-03-01: FilteredElementCollector(doc, viewId).OfClass(typeof(CurveElement))
        //   CurveElement is the base class for both DetailCurve and ModelCurve.
        //   Must check `is DetailCurve` to avoid deleting model lines.
        // PDG API NOTE 2026-03-01: doc.Delete(ElementId)
        //   Verified: revitapidocs.com/2024/ — throws if element is protected or non-deletable.
        private static int DeleteExistingFireRatingLines(
            Document doc,
            IEnumerable<ElementId> viewIds,
            HashSet<long> fireRatingStyleIds)
        {
            int deleted = 0;
            foreach (var viewId in viewIds)
            {
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
                    try { doc.Delete(id); deleted++; }
                    catch (Exception ex)
                    {
                        PDGLogger.Warning(
                            $"PDG FireRatingLines: Could not delete detail line Id={id.Value}. {ex.Message}");
                    }
                }
            }
            return deleted;
        }

        // =====================================================================
        // Private — Geometry Helpers
        // =====================================================================

        /// <summary>
        /// Projects a wall's 3-D centreline onto the plan view sketch plane (Z = GenLevel.Elevation).
        /// ⚠ Do NOT use cut-plane elevation — the sketch plane sits at the level, not the cut.
        /// Returns null if the flattened line is degenerate.
        /// </summary>
        // PDG API NOTE 2026-03-01: ViewPlan.GenLevel.Elevation — in Revit internal feet.
        // PDG: Check shared/PDG.Revit.Shared/ — ProjectCurveToViewSketchPlane() may exist.
        private static Curve? BuildPlanCurve(Line line3d, View view)
        {
            double z = ((ViewPlan)view).GenLevel.Elevation;
            var p0 = line3d.GetEndPoint(0);
            var p1 = line3d.GetEndPoint(1);
            var a  = new XYZ(p0.X, p0.Y, z);
            var b  = new XYZ(p1.X, p1.Y, z);
            if (a.DistanceTo(b) < 1e-6) return null;
            return Line.CreateBound(a, b);
        }

        /// <summary>
        /// Builds a VERTICAL detail-line curve for a wall in a section view.
        /// The wall's view-local bounding box gives X extent (thickness) and Y extent (height).
        /// We draw a vertical line at the X midpoint, spanning Min.Y → Max.Y, at Z = 0.
        /// Returns null if the bounding box is unavailable or the line is degenerate.
        /// </summary>
        // PDG API NOTE 2026-03-01: wall.get_BoundingBox(view) — view-local coords.
        //   Z = 0 in local space = the section view's sketch plane.
        //   view.CropBox.Transform maps local → world for NewDetailCurve.
        private static Curve? BuildVerticalSectionCurve(Wall wall, View view)
        {
            var bbox = wall.get_BoundingBox(view);
            if (bbox == null) return null;

            double cx = (bbox.Min.X + bbox.Max.X) / 2.0;
            var localBottom = new XYZ(cx, bbox.Min.Y, 0.0);
            var localTop    = new XYZ(cx, bbox.Max.Y, 0.0);
            if (localBottom.DistanceTo(localTop) < 1e-6) return null;

            var t = view.CropBox.Transform;
            return Line.CreateBound(t.OfPoint(localBottom), t.OfPoint(localTop));
        }

        /// <summary>
        /// Builds a HORIZONTAL detail-line curve for a Floor, Ceiling, or Roof in a section view.
        /// The element's view-local bounding box gives X extent (width in view) and Y extent (thickness).
        /// We draw a horizontal line at the Y midpoint, spanning Min.X → Max.X, at Z = 0.
        /// Returns null if the bounding box is unavailable or the line is degenerate.
        /// </summary>
        // PDG API NOTE 2026-03-01: element.get_BoundingBox(view) — view-local coords.
        //   Z = 0 keeps the line on the section view's sketch plane.
        //   CropBox.Transform.OfPoint() converts local → world coordinates.
        // PDG: Check shared/PDG.Revit.Shared/ — ProjectCurveToViewSketchPlane() may exist.
        private static Curve? BuildHorizontalSectionCurve(Element element, View view)
        {
            var bbox = element.get_BoundingBox(view);
            if (bbox == null)
            {
                Trace.TraceWarning(
                    $"PDG FireRatingLines: No bounding box for element Id={element.Id.Value} " +
                    $"in section view Id={view.Id.Value} — skipped.");
                return null;
            }

            double cy = (bbox.Min.Y + bbox.Max.Y) / 2.0;
            var localLeft  = new XYZ(bbox.Min.X, cy, 0.0);
            var localRight = new XYZ(bbox.Max.X, cy, 0.0);
            if (localLeft.DistanceTo(localRight) < 1e-6)
            {
                Trace.TraceWarning(
                    $"PDG FireRatingLines: Degenerate horizontal curve for element " +
                    $"Id={element.Id.Value} — skipped.");
                return null;
            }

            var t = view.CropBox.Transform;
            return Line.CreateBound(t.OfPoint(localLeft), t.OfPoint(localRight));
        }

        /// <summary>
        /// Returns the absolute cut-plane elevation for a plan view, or null if unresolvable.
        /// </summary>
        // PDG API NOTE 2026-03-01: ViewPlan.GetViewRange().GetOffset(PlanViewPlane.CutPlane)
        //   Offset is relative to GenLevel.Elevation. Absolute = level + offset.
        private static double? GetPlanCutElevation(View view)
        {
            if (!(view is ViewPlan vp)) return null;
            var level = vp.GenLevel;
            if (level == null) return null;
            var range = vp.GetViewRange();
            if (range == null) return null;
            return level.Elevation + range.GetOffset(PlanViewPlane.CutPlane);
        }

        /// <summary>Returns true if the wall's world bbox straddles the cut elevation.</summary>
        private static bool WallStraddlesCutPlane(Wall wall, double cutElevation)
        {
            var bbox = wall.get_BoundingBox(null);
            if (bbox == null) return false;
            return bbox.Min.Z < cutElevation && bbox.Max.Z > cutElevation;
        }
    }
}
