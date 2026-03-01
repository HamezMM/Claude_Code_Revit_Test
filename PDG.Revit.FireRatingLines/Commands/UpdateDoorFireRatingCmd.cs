// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
// Entry point only — NO business logic, NO transactions.
// All Revit work is delegated to FireRatingLinesService and DoorFireRatingService.

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
    /// IExternalCommand entry point for the Update Door Fire Ratings tool.
    /// Reads each door's host wall fire rating, maps it to the required door rating
    /// using the PDG standard table, and writes the value to the door's Fire Rating
    /// instance parameter so that door schedules reflect the correct rating.
    /// No business logic or transaction management lives here — see DoorFireRatingService.
    /// </summary>
    // PDG API NOTE 2026-03-01: [Transaction(TransactionMode.Manual)]
    //   Verified: revitapidocs.com/2024/ — required; the service manages its own Transaction.
    // PDG API NOTE 2026-03-01: [Regeneration(RegenerationOption.Manual)]
    //   Verified: revitapidocs.com/2024/ — suppress automatic document regeneration between API calls.
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class UpdateDoorFireRatingCmd : IExternalCommand
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
                        "PDG: Update Door Fire Ratings",
                        "No Revit document is currently open. Please open a project and retry.");
                    return Result.Cancelled;
                }

                // ── Stage 1: Discover fire-rated wall types ───────────────────
                // Reuse FireRatingLinesService to build the wallTypeId → ratingKey map.
                var wallService = new FireRatingLinesService();
                var wallTypeIdToRating = wallService.GetFireRatedWallTypes(doc);

                if (wallTypeIdToRating.Count == 0)
                {
                    TaskDialog.Show(
                        "PDG: Update Door Fire Ratings",
                        "No fire-rated wall types were found in this document.\n\n" +
                        "To use this tool, assign a Fire Rating value to one or more wall types:\n" +
                        "  Manage tab → Settings → Object Styles (or edit the wall type directly)\n" +
                        "  Set the 'Fire Rating' parameter to one of the standard values:\n" +
                        "  " + string.Join(", ", FireRatingStandards.StandardRatings));
                    return Result.Succeeded;
                }

                // ── Stage 2: Compute door fire rating updates ─────────────────
                // Finds all doors whose host wall has a fire rating and maps each to
                // the corresponding door rating per FireRatingStandards.WallToDoorRating.
                var doorService = new DoorFireRatingService();
                var updates = doorService.GetDoorFireRatingUpdates(doc, wallTypeIdToRating);

                if (updates.Count == 0)
                {
                    TaskDialog.Show(
                        "PDG: Update Door Fire Ratings",
                        "No doors were found in fire-rated walls.\n\n" +
                        "Ensure that door instances are hosted in walls whose 'Fire Rating'\n" +
                        "type parameter is set to one of:\n" +
                        "  " + string.Join(", ", FireRatingStandards.StandardRatings));
                    return Result.Succeeded;
                }

                // ── Stage 3: Write door fire ratings ─────────────────────────
                var result = doorService.ApplyDoorFireRatings(doc, updates);

                // ── TaskDialog summary ────────────────────────────────────────
                var sb = new StringBuilder();
                sb.AppendLine($"Doors updated: {result.DoorsUpdated}");
                sb.AppendLine($"Doors skipped: {result.DoorsSkipped}");

                if (result.DoorsInUnratedWalls > 0)
                    sb.AppendLine($"Doors in unrated walls (ignored): {result.DoorsInUnratedWalls}");

                if (result.Warnings.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("WARNINGS:");
                    foreach (var w in result.Warnings.Take(10))
                        sb.AppendLine($"  {w}");
                    if (result.Warnings.Count > 10)
                        sb.AppendLine($"  … and {result.Warnings.Count - 10} more (see PDG log).");
                    sb.AppendLine();
                    sb.AppendLine("For skipped doors, confirm the door family has a writable");
                    sb.AppendLine("'Fire Rating' instance parameter (not read-only or formula-driven).");
                }

                TaskDialog.Show("PDG: Update Door Fire Ratings — Complete", sb.ToString());
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("PDG: Update Door Fire Ratings — Error", ex.Message);
                return Result.Failed;
            }
        }
    }
}
