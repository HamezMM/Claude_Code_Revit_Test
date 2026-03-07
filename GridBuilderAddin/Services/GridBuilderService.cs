// GridBuilderAddin | Revit 2024 | net48
// All Revit API calls are marked with their source class/method.
// revitapidocs.com/2024/ was unreachable (HTTP 403) at generation time —
// each affected API call carries: // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GridBuilderAddin.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GridBuilderAddin.Services
{
    /// <summary>
    /// Contains all Revit API operations for the Grid Builder addin.
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Converting millimetre values to Revit internal units (feet).</item>
    ///   <item>Building <see cref="Line"/> objects for each grid position.</item>
    ///   <item>Calling <c>Grid.Create()</c> and assigning names.</item>
    ///   <item>Detecting and handling grid-name collisions.</item>
    ///   <item>Managing the enclosing <see cref="Transaction"/>.</item>
    /// </list>
    /// This class has no WPF dependency and no UI logic.
    /// </summary>
    public class GridBuilderService
    {
        // ── Public entry point ───────────────────────────────────────────────

        /// <summary>
        /// Creates all X-axis (numerical) and Y-axis (alphabetical) grid lines described
        /// by <paramref name="config"/> in the active Revit <paramref name="doc"/>.
        /// All <c>Grid.Create</c> calls are wrapped in a single transaction named
        /// <see cref="GridBuilderConstants.TransactionName"/>.
        /// </summary>
        /// <param name="doc">The active Revit document. Must not be <c>null</c>.</param>
        /// <param name="config">
        /// Fully validated grid configuration supplied by the ViewModel.
        /// All spacing values are in millimetres.
        /// </param>
        /// <returns>
        /// <c>true</c> if the transaction committed successfully; <c>false</c> if it was
        /// rolled back due to an exception (the error has already been shown to the user).
        /// </returns>
        public bool CreateGrid(Document doc, GridConfig config)
        {
            if (doc    == null) throw new ArgumentNullException(nameof(doc));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Debug.WriteLine($"[GridBuilder] CreateGrid called — XCount={config.XCount}, YCount={config.YCount}");

            // ── Pre-compute cumulative positions in internal units (feet) ────

            // X-axis: Grid "1" at X=0; each subsequent grid at cumulative X offset
            var xPositionsFt  = ComputeCumulativePositionsFt(config.XSpacingsMm);

            // Y-axis: Grid "A" at Y=0; each subsequent grid at cumulative negative Y offset
            // We store the magnitude; sign applied when building the line endpoint.
            var yMagnitudesFt = ComputeCumulativePositionsFt(config.YSpacingsMm);

            // ── Compute grid line extent ─────────────────────────────────────
            // Each grid line extends 2000 mm beyond the outermost perpendicular grid.

            double totalXFt = xPositionsFt.Count > 0 ? xPositionsFt.Last() : 0.0;
            double totalYFt = yMagnitudesFt.Count > 0 ? yMagnitudesFt.Last() : 0.0;

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // UnitUtils.ConvertToInternalUnits(double, UnitTypeId) — revitapidocs.com/2024/
            double extentFt = UnitUtils.ConvertToInternalUnits(
                GridBuilderConstants.ExtentBeyondGridMm,
                UnitTypeId.Millimeters);

            // X grid lines (vertical): span Y from -(totalY + extent) to +(extent)
            double xLineYMin = -(totalYFt + extentFt);  // extends beyond last Y grid (negative)
            double xLineYMax = extentFt;                  // extends above origin

            // Y grid lines (horizontal): span X from -(extent) to +(totalX + extent)
            double yLineXMin = -extentFt;
            double yLineXMax = totalXFt + extentFt;

            // ── Collision detection ──────────────────────────────────────────

            var plannedXNames = Enumerable.Range(1, config.XCount).Select(i => i.ToString()).ToList();
            var plannedYNames = Enumerable.Range(0, config.YCount).Select(GetAlphaLabel).ToList();

            var existingGridNames = CollectExistingGridNames(doc);
            var collisions        = new List<string>();

            var finalXNames = ResolveNames(plannedXNames, existingGridNames, collisions);
            var finalYNames = ResolveNames(plannedYNames, existingGridNames, collisions);

            if (collisions.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("The following grid names already exist in this document.");
                sb.AppendLine($"They will be created with the \"{GridBuilderConstants.CollisionSuffix}\" suffix:\n");
                foreach (var name in collisions)
                    sb.AppendLine($"  • {name}");

                Debug.WriteLine($"[GridBuilder] {collisions.Count} name collision(s) detected.");

                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // TaskDialog.Show(string, string) — revitapidocs.com/2024/
                TaskDialog.Show("Grid Builder — Name Collisions", sb.ToString());
            }

            // ── Transaction ──────────────────────────────────────────────────

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // Transaction(Document, string) — revitapidocs.com/2024/
            var transaction = new Transaction(doc, GridBuilderConstants.TransactionName);

            try
            {
                // Transaction.Start() — revitapidocs.com/2024/
                transaction.Start();

                Debug.WriteLine("[GridBuilder] Transaction started.");

                // ── Create X-axis grids (vertical lines) ─────────────────────
                for (int i = 0; i < config.XCount; i++)
                {
                    double x = xPositionsFt[i];

                    // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                    // Line.CreateBound(XYZ, XYZ) — revitapidocs.com/2024/
                    // Numerical grids are vertical lines placed along the X axis.
                    // XYZ(x, yMin, 0) → XYZ(x, yMax, 0)
                    var line = Line.CreateBound(
                        new XYZ(x, xLineYMin, 0),
                        new XYZ(x, xLineYMax, 0));

                    // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                    // Grid.Create(Document, Line) — revitapidocs.com/2024/8a44a4e5-eecc-1b26-b0a2-3f9f4fe24062.htm
                    var grid = Grid.Create(doc, line);

                    // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                    // Grid.Name (property setter) — revitapidocs.com/2024/
                    grid.Name = finalXNames[i];

                    Debug.WriteLine($"[GridBuilder] Created X grid \"{finalXNames[i]}\" at X={x:F4} ft.");
                }

                // ── Create Y-axis grids (horizontal lines) ────────────────────
                for (int i = 0; i < config.YCount; i++)
                {
                    // Y grids progress in the negative Y direction from origin.
                    // yMagnitudesFt[0] = 0 (origin), yMagnitudesFt[1] = spacing[0], etc.
                    double y = -yMagnitudesFt[i];  // negate to place in negative Y direction

                    // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                    // Line.CreateBound(XYZ, XYZ) — revitapidocs.com/2024/
                    // Alphabetical grids are horizontal lines placed along the Y axis.
                    // XYZ(xMin, y, 0) → XYZ(xMax, y, 0)
                    var line = Line.CreateBound(
                        new XYZ(yLineXMin, y, 0),
                        new XYZ(yLineXMax, y, 0));

                    // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                    // Grid.Create(Document, Line) — revitapidocs.com/2024/8a44a4e5-eecc-1b26-b0a2-3f9f4fe24062.htm
                    var grid = Grid.Create(doc, line);

                    // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                    // Grid.Name (property setter) — revitapidocs.com/2024/
                    grid.Name = finalYNames[i];

                    Debug.WriteLine($"[GridBuilder] Created Y grid \"{finalYNames[i]}\" at Y={y:F4} ft.");
                }

                // Transaction.Commit() — revitapidocs.com/2024/
                transaction.Commit();

                Debug.WriteLine("[GridBuilder] Transaction committed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GridBuilder] Exception during grid creation: {ex.Message}");

                // Transaction.RollBack() — revitapidocs.com/2024/
                if (transaction.HasStarted() && !transaction.HasEnded())
                    transaction.RollBack();

                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // TaskDialog.Show(string, string) — revitapidocs.com/2024/
                TaskDialog.Show(
                    "Grid Builder — Error",
                    $"An error occurred while creating the structural grid.\n\nDetails:\n{ex.Message}");

                return false;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a list of cumulative positions in Revit internal units (feet).
        /// Index 0 is always 0.0 (the origin grid); each subsequent value is the
        /// running sum of the converted spacings.
        /// </summary>
        /// <param name="spacingsMm">Per-interval spacings in millimetres.</param>
        private static List<double> ComputeCumulativePositionsFt(List<double> spacingsMm)
        {
            var positions = new List<double> { 0.0 };
            double cumulative = 0.0;

            foreach (var spacingMm in spacingsMm)
            {
                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // UnitUtils.ConvertToInternalUnits(double, UnitTypeId) — revitapidocs.com/2024/
                // UnitTypeId.Millimeters — revitapidocs.com/2024/
                cumulative += UnitUtils.ConvertToInternalUnits(spacingMm, UnitTypeId.Millimeters);
                positions.Add(cumulative);
            }

            return positions;
        }

        /// <summary>
        /// Collects the <c>Name</c> property of every existing <see cref="Grid"/> element
        /// in the document into a <see cref="HashSet{T}"/> for O(1) collision lookup.
        /// </summary>
        private static HashSet<string> CollectExistingGridNames(Document doc)
        {
            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // FilteredElementCollector(Document) — revitapidocs.com/2024/
            // OfClass(Type) — revitapidocs.com/2024/
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Grid))
                .Cast<Grid>();

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var g in collector)
            {
                if (!string.IsNullOrEmpty(g.Name))
                    names.Add(g.Name);
            }

            Debug.WriteLine($"[GridBuilder] Found {names.Count} existing grid name(s) in document.");
            return names;
        }

        /// <summary>
        /// Resolves final names for a list of planned grid names against the set of
        /// names that already exist in the document. Colliding names receive the
        /// <see cref="GridBuilderConstants.CollisionSuffix"/> suffix; all collisions
        /// are appended to <paramref name="collisions"/> for the warning dialog.
        /// </summary>
        private static List<string> ResolveNames(
            IEnumerable<string>  plannedNames,
            HashSet<string>      existingNames,
            List<string>         collisions)
        {
            var resolved = new List<string>();

            foreach (var name in plannedNames)
            {
                if (existingNames.Contains(name))
                {
                    var newName = name + GridBuilderConstants.CollisionSuffix;
                    collisions.Add(name);
                    resolved.Add(newName);
                    Debug.WriteLine($"[GridBuilder] Name collision: \"{name}\" → \"{newName}\"");
                }
                else
                {
                    resolved.Add(name);
                }
            }

            return resolved;
        }

        /// <summary>
        /// Converts a zero-based index to an alphabetical grid label.
        /// <para>
        /// 0 → "A", 1 → "B", …, 25 → "Z", 26 → "AA", 27 → "AB", …, 701 → "ZZ", 702 → "AAA", etc.
        /// Supports more than 26 Y grids using the double/triple-letter convention.
        /// </para>
        /// </summary>
        /// <param name="zeroBasedIndex">Zero-based index of the Y-axis grid (0 = first grid "A").</param>
        /// <returns>Alphabetical label string.</returns>
        public static string GetAlphaLabel(int zeroBasedIndex)
        {
            var result = string.Empty;
            var n      = zeroBasedIndex + 1;  // convert to 1-based for the algorithm

            while (n > 0)
            {
                n--;
                result = (char)('A' + n % 26) + result;
                n     /= 26;
            }

            return result;
        }
    }
}
