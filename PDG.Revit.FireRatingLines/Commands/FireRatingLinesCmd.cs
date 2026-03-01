// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
// Entry point only — NO business logic, NO transactions.
// All Revit work is delegated to FireRatingLinesService.

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PDG.Revit.FireRatingLines.Models;
using PDG.Revit.FireRatingLines.Services;
using System;
using System.Linq;
using System.Text;

namespace PDG.Revit.FireRatingLines.Commands
{
    /// <summary>
    /// IExternalCommand entry point for the Fire Rating Lines tool.
    /// Orchestrates the service stages and presents a TaskDialog summary.
    /// No business logic or transaction management lives here — see FireRatingLinesService.
    /// </summary>
    // PDG API NOTE 2026-03-01: [Transaction(TransactionMode.Manual)]
    //   Verified: revitapidocs.com/2024/ — required; the service manages its own transactions.
    // PDG API NOTE 2026-03-01: [Regeneration(RegenerationOption.Manual)]
    //   Verified: revitapidocs.com/2024/ — suppress automatic document regeneration between API calls.
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FireRatingLinesCmd : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // ── Guard: require an open document ──────────────────────────
                var doc = commandData.Application.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    TaskDialog.Show(
                        "PDG: Fire Rating Lines",
                        "No Revit document is currently open. Please open a project and retry.");
                    return Result.Cancelled;
                }

                var service = new FireRatingLinesService();

                // ── Stage 0: Ensure all standard line styles exist ────────────
                // Creates any of the six standard fire rating line styles that are
                // missing from the document, then returns the full style dictionary.
                // Standard names: 45 MIN, 1 HR, 1.5 HR, 2 HR, 3 HR, 4 HR
                var lineStyles = service.EnsureFireRatingLineStyles(doc);

                // ── Stage 1: Discover fire-rated wall types ───────────────────
                // Returns Dictionary<long wallTypeId, string ratingKey> for ALL rated types.
                var wallTypeIdToRating = service.GetFireRatedWallTypes(doc);

                // F-10 guard: exit cleanly with guidance if no fire-rated types found.
                if (wallTypeIdToRating.Count == 0)
                {
                    TaskDialog.Show(
                        "PDG: Fire Rating Lines",
                        "No fire-rated wall types were found in this document.\n\n" +
                        "To use this tool, assign a Fire Rating value to one or more wall types:\n" +
                        "  Manage tab → Settings → Object Styles (or edit the wall type directly)\n" +
                        "  Set the 'Fire Rating' parameter to one of the standard values:\n" +
                        "  " + string.Join(", ", FireRatingStandards.StandardRatings));
                    return Result.Succeeded;
                }

                // ── Stage 3: Collect walls in sheeted views ───────────────────
                var wallsInViews = service.GetFireRatedWallsInViews(doc, wallTypeIdToRating);

                // ── Stage 4: Delete old lines + draw new lines ────────────────
                var result = service.DrawFireRatingLines(doc, wallsInViews, lineStyles);

                // ── F-09: TaskDialog summary ──────────────────────────────────
                var sb = new StringBuilder();
                sb.AppendLine($"Lines drawn:     {result.LinesDrawn}");
                sb.AppendLine($"Lines deleted:   {result.LinesDeleted}");
                sb.AppendLine($"Walls processed: {result.WallsProcessed}");

                if (result.SkippedCurvedWalls > 0)
                    sb.AppendLine($"Curved walls skipped (v1 — straight walls only): {result.SkippedCurvedWalls}");

                if (result.UnmatchedRatings.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("WARNING: The following fire rating values on wall types do not match");
                    sb.AppendLine("any of the standard names. Update the wall type's Fire Rating parameter");
                    sb.AppendLine("to one of: " + string.Join(", ", FireRatingStandards.StandardRatings));
                    sb.AppendLine($"  Unmatched: {string.Join(", ", result.UnmatchedRatings)}");
                }

                TaskDialog.Show("PDG: Fire Rating Lines — Complete", sb.ToString());
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("PDG: Fire Rating Lines — Error", ex.Message);
                return Result.Failed;
            }
        }
    }
}
