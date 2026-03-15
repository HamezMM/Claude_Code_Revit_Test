// GridBuilderAddin | Revit 2024 | net48
// All Revit API calls are marked with their source class/method.
// revitapidocs.com/2024/ was unreachable (HTTP 403) at generation time —
// each affected API call carries: // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using GridBuilderAddin.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GridBuilderAddin.Services
{
    /// <summary>
    /// Contains all Revit API operations for the Structure Builder step of the Building Builder.
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Fetching available floor, wall, roof, and structural column family types.</item>
    ///   <item>Creating the main slab, exterior walls, roof, and structural columns.</item>
    ///   <item>All element categories are placed in a single transaction per Building Builder
    ///         window, so the model is either fully built or fully rolled back on failure.</item>
    /// </list>
    /// This class has no WPF dependency and no UI logic.
    /// </summary>
    public class StructureBuilderService
    {
        // ── Family type fetchers ─────────────────────────────────────────────

        /// <summary>
        /// Returns all <see cref="FloorType"/> elements loaded in the document.
        /// </summary>
        public List<FamilyTypeItem> FetchFloorTypes(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // FilteredElementCollector.OfClass(typeof(FloorType)) — revitapidocs.com/2024/
            var items = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .Select(ft => new FamilyTypeItem
                {
                    Id         = ft.Id.Value,
                    Name       = ft.Name,
                    FamilyName = string.Empty   // FloorType is a system family
                })
                .OrderBy(x => x.Name)
                .ToList();

            Debug.WriteLine($"[StructureBuilder] Found {items.Count} floor type(s).");
            return items;
        }

        /// <summary>
        /// Returns all basic and compound <see cref="WallType"/> elements loaded in the document.
        /// Curtain wall types are excluded as they are not suitable for exterior structural walls.
        /// </summary>
        public List<FamilyTypeItem> FetchWallTypes(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // FilteredElementCollector.OfClass(typeof(WallType)) — revitapidocs.com/2024/
            // WallType.Kind — revitapidocs.com/2024/
            var items = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Where(wt => wt.Kind == WallKind.Basic || wt.Kind == WallKind.Stacked)
                .Select(wt => new FamilyTypeItem
                {
                    Id         = wt.Id.Value,
                    Name       = wt.Name,
                    FamilyName = string.Empty
                })
                .OrderBy(x => x.Name)
                .ToList();

            Debug.WriteLine($"[StructureBuilder] Found {items.Count} wall type(s).");
            return items;
        }

        /// <summary>
        /// Returns all <see cref="RoofType"/> elements loaded in the document.
        /// </summary>
        public List<FamilyTypeItem> FetchRoofTypes(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // FilteredElementCollector.OfClass(typeof(RoofType)) — revitapidocs.com/2024/
            var items = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .Cast<RoofType>()
                .Select(rt => new FamilyTypeItem
                {
                    Id         = rt.Id.Value,
                    Name       = rt.Name,
                    FamilyName = string.Empty
                })
                .OrderBy(x => x.Name)
                .ToList();

            Debug.WriteLine($"[StructureBuilder] Found {items.Count} roof type(s).");
            return items;
        }

        /// <summary>
        /// Returns all structural column <see cref="FamilySymbol"/> instances loaded in the document.
        /// </summary>
        public List<FamilyTypeItem> FetchStructuralColumnTypes(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // FilteredElementCollector.OfCategory + OfClass(typeof(FamilySymbol)) — revitapidocs.com/2024/
            // BuiltInCategory.OST_StructuralColumns — revitapidocs.com/2024/
            var items = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Select(fs => new FamilyTypeItem
                {
                    Id         = fs.Id.Value,
                    Name       = fs.Name,
                    FamilyName = fs.Family?.Name ?? string.Empty
                })
                .OrderBy(x => x.DisplayName)
                .ToList();

            Debug.WriteLine($"[StructureBuilder] Found {items.Count} structural column type(s).");
            return items;
        }

        /// <summary>
        /// Returns all <see cref="Level"/> elements in the document as <see cref="FamilyTypeItem"/> instances
        /// for use in level picker drop-downs.
        /// </summary>
        public List<FamilyTypeItem> FetchLevels(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // FilteredElementCollector.OfClass(typeof(Level)) — revitapidocs.com/2024/
            var items = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .Select(l => new FamilyTypeItem
                {
                    Id         = l.Id.Value,
                    Name       = l.Name,
                    FamilyName = string.Empty
                })
                .ToList();

            Debug.WriteLine($"[StructureBuilder] Found {items.Count} level(s) for pickers.");
            return items;
        }

        // ── Build structure ──────────────────────────────────────────────────

        /// <summary>
        /// Creates the base structure elements (slab, exterior walls, roof, and structural columns)
        /// described by <paramref name="config"/> in the active Revit document.
        /// Each element category is placed in its own independently-committed transaction so that
        /// a failure at any step leaves all previously-committed elements in the model.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="gridConfig">Grid configuration from the Grid Builder step.</param>
        /// <param name="config">Structure configuration from the Structure Builder dialog.</param>
        /// <returns><c>true</c> if all four transactions committed; <c>false</c> if any step failed
        /// (an error dialog has already been shown identifying the failing step).</returns>
        public bool BuildStructure(Document doc, GridConfig gridConfig, StructureConfig config)
        {
            if (doc        == null) throw new ArgumentNullException(nameof(doc));
            if (gridConfig == null) throw new ArgumentNullException(nameof(gridConfig));
            if (config     == null) throw new ArgumentNullException(nameof(config));

            Debug.WriteLine("[StructureBuilder] BuildStructure called.");

            // ── Pre-compute perimeter positions in internal units (feet) ──────

            // X positions: Grid 1 at x=0, Grid N at x=totalX
            var xPositions = ComputeCumulativePositionsFt(gridConfig.XSpacingsMm);
            // Y positions: Grid A at y=0, Grid M at y=-totalY  (WPF Y-down, Revit uses negative Y for south)
            var yMagnitudes = ComputeCumulativePositionsFt(gridConfig.YSpacingsMm);

            double x1 = xPositions.First();
            double xN = xPositions.Last();
            double y1 = 0.0;
            double yM = -yMagnitudes.Last();  // most southerly grid (most negative Y)

            double wallOffsetFt = UnitUtils.ConvertToInternalUnits(config.WallExteriorOffsetMm, UnitTypeId.Millimeters);

            // Validate that essential IDs are non-zero before constructing ElementIds.
            // A zero ID means the UI selection was not captured correctly (e.g. WPF binding null
            // write-back during ComboBox initialisation). Provide a clear error rather than letting
            // Revit throw a generic ArgumentNullException from doc.GetElement.
            if (config.RoofTypeId  <= 0) throw new InvalidOperationException(
                "No roof type was captured from the UI (ID = 0). " +
                "Please re-open the Structure Builder and ensure a roof type is selected.");
            if (config.RoofLevelId <= 0) throw new InvalidOperationException(
                "No roof host level was captured from the UI (ID = 0). " +
                "Please re-open the Structure Builder and ensure a roof host level is selected.");

            // Resolve type and level ElementIds
            var floorTypeId   = new ElementId(config.FloorTypeId);
            var floorLevelId  = new ElementId(config.FloorLevelId);
            var roofTypeId    = new ElementId(config.RoofTypeId);
            var roofLevelId   = new ElementId(config.RoofLevelId);
            var wallTypeId    = new ElementId(config.WallTypeId);
            var wallBotLvlId  = new ElementId(config.WallBottomLevelId);
            var wallTopLvlId  = new ElementId(config.WallTopLevelId);
            var colBotLvlId   = new ElementId(config.ColumnBottomLevelId);
            var colTopLvlId   = new ElementId(config.ColumnTopLevelId);

            double wallBotOffFt = UnitUtils.ConvertToInternalUnits(config.WallBottomOffsetMm, UnitTypeId.Millimeters);
            double wallTopOffFt = UnitUtils.ConvertToInternalUnits(config.WallTopOffsetMm,    UnitTypeId.Millimeters);
            double colBotOffFt  = UnitUtils.ConvertToInternalUnits(config.ColumnBottomOffsetMm, UnitTypeId.Millimeters);
            double colTopOffFt  = UnitUtils.ConvertToInternalUnits(config.ColumnTopOffsetMm,    UnitTypeId.Millimeters);
            double perimOffFt   = UnitUtils.ConvertToInternalUnits(config.PerimeterInteriorOffsetMm, UnitTypeId.Millimeters);

            // Look up wall thickness to compute interior face for floor/roof boundary
            double wallThickFt = 0.0;
            var wallType = doc.GetElement(wallTypeId) as WallType;
            if (wallType != null)
            {
                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // WallType.Width — revitapidocs.com/2024/
                wallThickFt = wallType.Width;
            }

            // Interior face boundaries of exterior walls (floor/roof sketch boundary)
            // Left wall  (x = x1 side): exterior face at x1 - wallOffsetFt, interior face moves right by wallThickFt
            // Right wall (x = xN side): exterior face at xN + wallOffsetFt, interior face moves left by wallThickFt
            // North wall (y = y1 side): exterior face at y1 + wallOffsetFt, interior face moves south (negative)
            // South wall (y = yM side): exterior face at yM - wallOffsetFt, interior face moves north (positive)
            double floorXMin = x1 - wallOffsetFt + wallThickFt;
            double floorXMax = xN + wallOffsetFt - wallThickFt;
            double floorYMax = y1 + wallOffsetFt - wallThickFt;  // northernmost (most positive y)
            double floorYMin = yM - wallOffsetFt + wallThickFt;  // southernmost (most negative y)

            // ── Four independent transactions — one per element category ─────
            // Each transaction commits before the next begins.  A failure at any step
            // rolls back only that step; all previously-committed elements remain in the model.

            if (!RunTransaction(doc, GridBuilderConstants.FloorTransactionName,
                    () => CreateFloor(doc, config, floorTypeId, floorLevelId,
                                      floorXMin, floorXMax, floorYMin, floorYMax)))
                return false;

            if (!RunTransaction(doc, GridBuilderConstants.WallsTransactionName,
                    () => CreateExteriorWalls(doc, config, wallTypeId, wallBotLvlId, wallBotOffFt,
                                              wallTopLvlId, wallTopOffFt,
                                              x1, xN, y1, yM, wallOffsetFt)))
                return false;

            if (!RunTransaction(doc, GridBuilderConstants.RoofTransactionName,
                    () => CreateRoof(doc, config, roofTypeId, roofLevelId,
                                     floorXMin, floorXMax, floorYMin, floorYMax)))
                return false;

            if (!RunTransaction(doc, GridBuilderConstants.ColumnsTransactionName,
                    () => CreateColumns(doc, config, colBotLvlId, colBotOffFt, colTopLvlId, colTopOffFt,
                                        xPositions, yMagnitudes, perimOffFt)))
                return false;

            Debug.WriteLine("[StructureBuilder] All four structure transactions committed.");
            return true;
        }

        // ── Transaction helper ───────────────────────────────────────────────

        /// <summary>
        /// Runs <paramref name="body"/> inside a named <see cref="Transaction"/> and commits it.
        /// If an exception is thrown the transaction is rolled back, an error dialog is shown,
        /// and <c>false</c> is returned.  All previously-committed transactions are unaffected.
        /// </summary>
        private static bool RunTransaction(Document doc, string transactionName, Action body)
        {
            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // Transaction implements IDisposable; using ensures handles are released on every path.
            using (var transaction = new Transaction(doc, transactionName))
            {
                try
                {
                    transaction.Start();
                    body();
                    transaction.Commit();
                    Debug.WriteLine($"[StructureBuilder] Transaction '{transactionName}' committed.");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[StructureBuilder] Transaction '{transactionName}' failed: {ex.Message}");
                    if (transaction.HasStarted() && !transaction.HasEnded())
                        transaction.RollBack();

                    TaskDialog.Show(
                        "Structure Builder — Error",
                        $"{transactionName} failed.\n\n" +
                        $"Check that all selected types and levels are valid.\n\n" +
                        $"Details:\n{ex.Message}");
                    return false;
                }
            }
        }

        // ── Private element-creation helpers ────────────────────────────────
        // These helpers contain the Revit API calls and are called from within
        // the per-step transaction wrappers above.

        private static void CreateFloor(
            Document doc, StructureConfig config,
            ElementId floorTypeId, ElementId levelId,
            double xMin, double xMax, double yMin, double yMax)
        {
            double offsetFt = UnitUtils.ConvertToInternalUnits(config.FloorLevelOffsetMm, UnitTypeId.Millimeters);

            // Build a rectangular CurveLoop for the floor boundary
            // Corners: (xMin, yMax), (xMax, yMax), (xMax, yMin), (xMin, yMin)
            var loop = new CurveLoop();
            loop.Append(Line.CreateBound(new XYZ(xMin, yMax, 0), new XYZ(xMax, yMax, 0)));
            loop.Append(Line.CreateBound(new XYZ(xMax, yMax, 0), new XYZ(xMax, yMin, 0)));
            loop.Append(Line.CreateBound(new XYZ(xMax, yMin, 0), new XYZ(xMin, yMin, 0)));
            loop.Append(Line.CreateBound(new XYZ(xMin, yMin, 0), new XYZ(xMin, yMax, 0)));

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // Floor.Create(Document, IList<CurveLoop>, ElementId, ElementId) — revitapidocs.com/2024/
            var floor = Floor.Create(doc, new List<CurveLoop> { loop }, floorTypeId, levelId);

            if (floor == null)
                throw new InvalidOperationException(
                    "Floor.Create returned null. " +
                    "The floor type or host level may be invalid or incompatible with the current document.");

            // Set height offset from level
            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM — revitapidocs.com/2024/
            floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.Set(offsetFt);

            Debug.WriteLine("[StructureBuilder] Floor created.");
        }

        private static void CreateExteriorWalls(
            Document doc, StructureConfig config,
            ElementId wallTypeId, ElementId botLvlId, double botOffFt, ElementId topLvlId, double topOffFt,
            double x1, double xN, double y1, double yM, double wallOffsetFt)
        {
            // Wall exterior face positions:
            //   Left:  x = x1 - wallOffsetFt
            //   Right: x = xN + wallOffsetFt
            //   North: y = y1 + wallOffsetFt
            //   South: y = yM - wallOffsetFt
            // The wall lines run along the exterior face; we set location line to exterior face finish
            // so the wall body is inward.

            double wX1 = x1 - wallOffsetFt;
            double wXN = xN + wallOffsetFt;
            double wY1 = y1 + wallOffsetFt;
            double wYM = yM - wallOffsetFt;

            // Compute height from bottom level offset to top level elevation + top offset
            // For simplicity, we compute height as the difference in elevation between top and bottom levels
            // plus the respective offsets. The caller must ensure top > bottom.
            var botLevel = doc.GetElement(botLvlId) as Level;
            var topLevel = doc.GetElement(topLvlId) as Level;
            double heightFt = 0.0;
            if (botLevel != null && topLevel != null)
                heightFt = (topLevel.Elevation + topOffFt) - (botLevel.Elevation + botOffFt);
            if (heightFt <= 0) heightFt = UnitUtils.ConvertToInternalUnits(3000, UnitTypeId.Millimeters);

            // The four perimeter wall lines at the exterior face
            var wallLines = new[]
            {
                // North wall: left to right along y = wY1
                (Line.CreateBound(new XYZ(wX1, wY1, 0), new XYZ(wXN, wY1, 0)), false),
                // East wall: north to south along x = wXN
                (Line.CreateBound(new XYZ(wXN, wY1, 0), new XYZ(wXN, wYM, 0)), false),
                // South wall: right to left along y = wYM
                (Line.CreateBound(new XYZ(wXN, wYM, 0), new XYZ(wX1, wYM, 0)), false),
                // West wall: south to north along x = wX1
                (Line.CreateBound(new XYZ(wX1, wYM, 0), new XYZ(wX1, wY1, 0)), false)
            };

            foreach (var (line, flip) in wallLines)
            {
                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // Wall.Create(Document, Curve, ElementId, ElementId, double, double, bool, bool)
                // — revitapidocs.com/2024/
                var wall = Wall.Create(doc, line, wallTypeId, botLvlId, heightFt, botOffFt, flip, true);

                if (wall == null)
                    throw new InvalidOperationException(
                        $"Wall.Create returned null for the wall at " +
                        $"({line.GetEndPoint(0).X:F2}, {line.GetEndPoint(0).Y:F2}) → " +
                        $"({line.GetEndPoint(1).X:F2}, {line.GetEndPoint(1).Y:F2}). " +
                        $"The wall type or level constraints may be invalid.");

                // Set top constraint to the selected top level
                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // BuiltInParameter.WALL_HEIGHT_TYPE — revitapidocs.com/2024/
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.Set(topLvlId);
                wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET)?.Set(topOffFt);

                Debug.WriteLine($"[StructureBuilder] Created exterior wall at {line.GetEndPoint(0)} → {line.GetEndPoint(1)}.");
            }
        }

        private static void CreateRoof(
            Document doc, StructureConfig config,
            ElementId roofTypeId, ElementId levelId,
            double xMin, double xMax, double yMin, double yMax)
        {
            double offsetFt = UnitUtils.ConvertToInternalUnits(config.RoofLevelOffsetMm, UnitTypeId.Millimeters);

            var roofType  = doc.GetElement(roofTypeId) as RoofType;
            var roofLevel = doc.GetElement(levelId) as Level;

            if (roofType == null)
                throw new InvalidOperationException(
                    "The selected roof type could not be found in the document (ElementId may be stale). " +
                    "Please re-open the Structure Builder and re-select the roof type.");

            if (roofLevel == null)
                throw new InvalidOperationException(
                    "The selected roof host level could not be found in the document (ElementId may be stale). " +
                    "Please re-open the Structure Builder and re-select the roof level.");

            // Build a flat footprint roof using CurveArray
            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // Document.Create.NewFootPrintRoof(CurveArray, Level, RoofType, out ModelCurveArray)
            // — revitapidocs.com/2024/
            var curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(xMin, yMax, 0), new XYZ(xMax, yMax, 0)));
            curveArray.Append(Line.CreateBound(new XYZ(xMax, yMax, 0), new XYZ(xMax, yMin, 0)));
            curveArray.Append(Line.CreateBound(new XYZ(xMax, yMin, 0), new XYZ(xMin, yMin, 0)));
            curveArray.Append(Line.CreateBound(new XYZ(xMin, yMin, 0), new XYZ(xMin, yMax, 0)));

            ModelCurveArray roofProfile;
            var roof = doc.Create.NewFootPrintRoof(curveArray, roofLevel, roofType, out roofProfile);

            if (roof == null)
                throw new InvalidOperationException(
                    "NewFootPrintRoof returned null. " +
                    "The roof type or host level may be incompatible with the boundary sketch.");

            // Set level offset and ensure zero slope for a flat roof
            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM — revitapidocs.com/2024/
            roof.get_Parameter(BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM)?.Set(offsetFt);

            // Set slope of all profile curves to zero (flat roof)
            foreach (ModelCurve mc in roofProfile)
            {
                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // ModelCurve parameters for slope — revitapidocs.com/2024/
                mc.get_Parameter(BuiltInParameter.ROOF_SLOPE)?.Set(0.0);
            }

            Debug.WriteLine("[StructureBuilder] Footprint roof created.");
        }

        private static void CreateColumns(
            Document doc, StructureConfig config,
            ElementId botLvlId, double botOffFt, ElementId topLvlId, double topOffFt,
            List<double> xPositions, List<double> yMagnitudes, double perimOffFt)
        {
            int xCount = xPositions.Count;  // includes origin
            int yCount = yMagnitudes.Count;

            // Identify perimeter indices (first/last x, first/last y)
            int xFirst = 0, xLast = xCount - 1;
            int yFirst = 0, yLast = yCount - 1;

            var botLevel = doc.GetElement(botLvlId) as Level;
            if (botLevel == null)
                throw new InvalidOperationException(
                    "The column base level could not be found in the document. " +
                    "Please re-open the Structure Builder and re-select the column base level.");

            // ── Perimeter columns ─────────────────────────────────────────────
            if (config.HasPerimeterColumns && config.PerimeterColumnTypeId != 0)
            {
                var symbol = doc.GetElement(new ElementId(config.PerimeterColumnTypeId)) as FamilySymbol;
                if (symbol == null)
                    throw new InvalidOperationException(
                        $"Perimeter column family symbol (ID {config.PerimeterColumnTypeId}) could not be found. " +
                        $"Ensure the column family is loaded in the document.");

                // Activate symbol if not already active
                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // FamilySymbol.Activate() — revitapidocs.com/2024/
                if (!symbol.IsActive) symbol.Activate();

                // Perimeter intersections: all (xi, yj) where xi is first or last OR yj is first or last
                for (int xi = 0; xi < xCount; xi++)
                {
                    for (int yj = 0; yj < yCount; yj++)
                    {
                        bool isPerimeter = (xi == xFirst || xi == xLast || yj == yFirst || yj == yLast);
                        if (!isPerimeter) continue;

                        double x = xPositions[xi];
                        double y = -yMagnitudes[yj];

                        // Apply interior offset inward from perimeter grid
                        double px = ApplyPerimXOffset(x, xi, xFirst, xLast, perimOffFt);
                        double py = ApplyPerimYOffset(y, yj, yFirst, yLast, perimOffFt);

                        PlaceColumn(doc, symbol, botLevel, botOffFt, topLvlId, topOffFt, new XYZ(px, py, 0));
                    }
                }
                Debug.WriteLine("[StructureBuilder] Perimeter columns placed.");
            }

            // ── Midpoint perimeter columns ────────────────────────────────────
            if (config.HasMidpointColumns && config.MidpointColumnTypeId != 0)
            {
                var symbol = doc.GetElement(new ElementId(config.MidpointColumnTypeId)) as FamilySymbol;
                if (symbol == null)
                    throw new InvalidOperationException(
                        $"Midpoint column family symbol (ID {config.MidpointColumnTypeId}) could not be found. " +
                        $"Ensure the column family is loaded in the document.");

                if (!symbol.IsActive) symbol.Activate();

                PlaceMidpointColumns(doc, symbol, botLevel, botOffFt, topLvlId, topOffFt,
                    xPositions, yMagnitudes, perimOffFt, xFirst, xLast, yFirst, yLast);
                Debug.WriteLine("[StructureBuilder] Midpoint perimeter columns placed.");
            }

            // ── Interior columns ──────────────────────────────────────────────
            if (config.HasInteriorColumns && config.InteriorColumnTypeId != 0)
            {
                var symbol = doc.GetElement(new ElementId(config.InteriorColumnTypeId)) as FamilySymbol;
                if (symbol == null)
                    throw new InvalidOperationException(
                        $"Interior column family symbol (ID {config.InteriorColumnTypeId}) could not be found. " +
                        $"Ensure the column family is loaded in the document.");

                if (!symbol.IsActive) symbol.Activate();

                for (int xi = 1; xi < xCount - 1; xi++)
                {
                    for (int yj = 1; yj < yCount - 1; yj++)
                    {
                        double x = xPositions[xi];
                        double y = -yMagnitudes[yj];
                        PlaceColumn(doc, symbol, botLevel, botOffFt, topLvlId, topOffFt, new XYZ(x, y, 0));
                    }
                }
                Debug.WriteLine("[StructureBuilder] Interior columns placed.");
            }
        }

        private static void PlaceColumn(
            Document doc, FamilySymbol symbol,
            Level botLevel, double botOffFt,
            ElementId topLvlId, double topOffFt,
            XYZ point)
        {
            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // Document.Create.NewFamilyInstance(XYZ, FamilySymbol, Level, StructuralType)
            // — revitapidocs.com/2024/
            var col = doc.Create.NewFamilyInstance(
                point, symbol, botLevel, StructuralType.Column);

            if (col == null)
                throw new InvalidOperationException(
                    $"NewFamilyInstance returned null for column at ({point.X:F2}, {point.Y:F2}). " +
                    $"The column family symbol '{symbol.Name}' may not be fully activated or may be " +
                    $"incompatible with the selected base level.");

            // Set base offset
            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM — revitapidocs.com/2024/
            col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(botOffFt);

            // Set top level and offset
            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // BuiltInParameter.FAMILY_TOP_LEVEL_PARAM / FAMILY_TOP_LEVEL_OFFSET_PARAM — revitapidocs.com/2024/
            col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(topLvlId);
            col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topOffFt);
        }

        private static void PlaceMidpointColumns(
            Document doc, FamilySymbol symbol,
            Level botLevel, double botOffFt, ElementId topLvlId, double topOffFt,
            List<double> xPositions, List<double> yMagnitudes, double perimOffFt,
            int xFirst, int xLast, int yFirst, int yLast)
        {
            int xCount = xPositions.Count;
            int yCount = yMagnitudes.Count;

            // Along the top and bottom perimeter edges (yFirst and yLast): midpoints between x positions
            foreach (int yj in new[] { yFirst, yLast })
            {
                double y = -yMagnitudes[yj];
                double py = ApplyPerimYOffset(y, yj, yFirst, yLast, perimOffFt);

                for (int xi = 0; xi < xCount - 1; xi++)
                {
                    double midX = (xPositions[xi] + xPositions[xi + 1]) / 2.0;
                    PlaceColumn(doc, symbol, botLevel, botOffFt, topLvlId, topOffFt, new XYZ(midX, py, 0));
                }
            }

            // Along the left and right perimeter edges (xFirst and xLast): midpoints between y positions
            foreach (int xi in new[] { xFirst, xLast })
            {
                double x = xPositions[xi];
                double px = ApplyPerimXOffset(x, xi, xFirst, xLast, perimOffFt);

                for (int yj = 0; yj < yCount - 1; yj++)
                {
                    double midY = -(yMagnitudes[yj] + yMagnitudes[yj + 1]) / 2.0;
                    PlaceColumn(doc, symbol, botLevel, botOffFt, topLvlId, topOffFt, new XYZ(px, midY, 0));
                }
            }
        }

        /// <summary>Applies interior offset inward along X for a perimeter column on the left or right edge.</summary>
        private static double ApplyPerimXOffset(double x, int xi, int xFirst, int xLast, double offsetFt)
        {
            if (xi == xFirst) return x + offsetFt;  // left edge: move right (positive X = inward)
            if (xi == xLast)  return x - offsetFt;  // right edge: move left (negative X = inward)
            return x;  // not on X perimeter
        }

        /// <summary>Applies interior offset inward along Y for a perimeter column on the north or south edge.</summary>
        private static double ApplyPerimYOffset(double y, int yj, int yFirst, int yLast, double offsetFt)
        {
            if (yj == yFirst) return y - offsetFt;  // north edge (y=0): move south (negative Y = inward)
            if (yj == yLast)  return y + offsetFt;  // south edge: move north (positive Y = inward)
            return y;  // not on Y perimeter
        }

        /// <summary>
        /// Builds a list of cumulative positions in Revit internal units (feet).
        /// Index 0 is always 0.0 (the origin grid); each subsequent value is the running sum.
        /// </summary>
        private static List<double> ComputeCumulativePositionsFt(List<double> spacingsMm)
        {
            var positions = new List<double> { 0.0 };
            double cumulative = 0.0;
            foreach (var mm in spacingsMm)
            {
                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // UnitUtils.ConvertToInternalUnits(double, UnitTypeId) — revitapidocs.com/2024/
                cumulative += UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
                positions.Add(cumulative);
            }
            return positions;
        }
    }
}
