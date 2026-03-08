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

namespace GridBuilderAddin.Services
{
    /// <summary>
    /// Contains all Revit API operations for the Level Builder step of the Building Builder.
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Fetching existing <see cref="Level"/> elements from the active document.</item>
    ///   <item>Creating new levels and modifying existing ones in a single transaction.</item>
    /// </list>
    /// This class has no WPF dependency and no UI logic.
    /// </summary>
    public class LevelBuilderService
    {
        // ── Fetch existing levels ────────────────────────────────────────────

        /// <summary>
        /// Returns all <see cref="Level"/> elements in <paramref name="doc"/> as
        /// <see cref="LevelRow"/> objects with <c>IsExisting = true</c>, sorted by elevation.
        /// </summary>
        /// <param name="doc">The active Revit document. Must not be <c>null</c>.</param>
        public List<LevelRow> FetchExistingLevels(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // FilteredElementCollector(Document).OfClass(Type) — revitapidocs.com/2024/
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            var rows = levels.Select(l => new LevelRow(
                name:        l.Name,
                elevationMm: UnitUtils.ConvertFromInternalUnits(l.Elevation, UnitTypeId.Millimeters),
                isExisting:  true,
                revitId:     l.Id.Value))
                .ToList();

            Debug.WriteLine($"[LevelBuilder] Fetched {rows.Count} existing level(s) from document.");
            return rows;
        }

        // ── Apply levels ─────────────────────────────────────────────────────

        /// <summary>
        /// Applies the <paramref name="levels"/> list to the Revit document:
        /// <list type="bullet">
        ///   <item>Existing levels (<see cref="LevelRow.RevitId"/> ≥ 0) have their name and
        ///         elevation updated if different from the model values.</item>
        ///   <item>New levels (<see cref="LevelRow.RevitId"/> == −1) are created with
        ///         <see cref="Level.Create(Document, double)"/>.</item>
        /// </list>
        /// All operations are wrapped in a single transaction named
        /// <see cref="GridBuilderConstants.LevelTransactionName"/>.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="levels">
        /// Validated level rows from the ViewModel. All rows must have <see cref="LevelRow.IsValid"/> == <c>true</c>.
        /// </param>
        /// <returns><c>true</c> if the transaction committed; <c>false</c> on error.</returns>
        public bool ApplyLevels(Document doc, IEnumerable<LevelRow> levels)
        {
            if (doc    == null) throw new ArgumentNullException(nameof(doc));
            if (levels == null) throw new ArgumentNullException(nameof(levels));

            var levelList = levels.ToList();
            Debug.WriteLine($"[LevelBuilder] ApplyLevels called — {levelList.Count} row(s).");

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // Transaction(Document, string) — revitapidocs.com/2024/
            var transaction = new Transaction(doc, GridBuilderConstants.LevelTransactionName);

            try
            {
                transaction.Start();

                foreach (var row in levelList)
                {
                    double elevationFt = UnitUtils.ConvertToInternalUnits(row.ElevationMm, UnitTypeId.Millimeters);

                    if (row.RevitId >= 0)
                    {
                        // ── Update existing level ────────────────────────────
                        // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                        // Document.GetElement(ElementId) — revitapidocs.com/2024/
                        var existingLevel = doc.GetElement(new ElementId(row.RevitId)) as Level;
                        if (existingLevel == null)
                        {
                            Debug.WriteLine($"[LevelBuilder] Could not find level with ID {row.RevitId} — skipping.");
                            continue;
                        }

                        // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                        // Level.Elevation (property setter) — revitapidocs.com/2024/
                        // Level.Name (property setter) — revitapidocs.com/2024/
                        existingLevel.Name      = row.Name;
                        existingLevel.Elevation = elevationFt;

                        Debug.WriteLine($"[LevelBuilder] Updated existing level ID={row.RevitId} Name=\"{row.Name}\" Elev={elevationFt:F4}ft.");
                    }
                    else
                    {
                        // ── Create new level ────────────────────────────────
                        // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                        // Level.Create(Document, double) — revitapidocs.com/2024/
                        var newLevel = Level.Create(doc, elevationFt);
                        newLevel.Name = row.Name;

                        Debug.WriteLine($"[LevelBuilder] Created new level \"{row.Name}\" at {elevationFt:F4}ft.");
                    }
                }

                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // Transaction.Commit() — revitapidocs.com/2024/
                transaction.Commit();

                Debug.WriteLine("[LevelBuilder] Transaction committed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LevelBuilder] Exception during level apply: {ex.Message}");

                if (transaction.HasStarted() && !transaction.HasEnded())
                    transaction.RollBack();

                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // TaskDialog.Show(string, string) — revitapidocs.com/2024/
                TaskDialog.Show(
                    "Level Builder — Error",
                    $"An error occurred while creating or modifying levels.\n\nDetails:\n{ex.Message}");

                return false;
            }
        }
    }
}
