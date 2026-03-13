// RoomDocumenter | Revit 2024 | net48
// API NOTES:
//   FilteredElementCollector — Verified: revitapidocs.com/2024/
//   UIDocument.Selection     — Verified: revitapidocs.com/2024/
//   BuiltInParameter.ROOM_*  — Verified: revitapidocs.com/2024/
//   ElementId.Value (Int64)  — Verified: revitapidocs.com/2024/ (never IntegerValue)
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RoomDocumenter.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoomDocumenter.Commands
{
    /// <summary>
    /// Resolves room selection (pre-selection → all rooms on active level),
    /// validates that a finish mapping exists, then delegates all reconciliation
    /// work to <see cref="RoomDocumentationService"/>.
    ///
    /// <para>No geometry creation calls are made here; this class is a thin
    /// orchestration shell.</para>
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RoomDocumentationCmd : IExternalCommand
    {
        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show("Room Documenter", "No active document.");
                    return Result.Cancelled;
                }

                var doc = uiDoc.Document;

                if (doc.IsFamilyDocument)
                {
                    TaskDialog.Show("Room Documenter",
                        "Room Documentation is only available in a project document.");
                    return Result.Cancelled;
                }

                // Check mapping exists before doing any work
                var mappingSvc = new FinishMappingService();
                if (mappingSvc.Load(doc) == null)
                {
                    TaskDialog.Show("Room Documenter — No Mapping",
                        "No finish mapping found. Please run Finish Mapping first.");
                    return Result.Cancelled;
                }

                // Resolve room collection
                var rooms = ResolveRooms(doc, uiDoc);
                if (rooms.Count == 0)
                {
                    TaskDialog.Show("Room Documenter",
                        "No placed rooms found. Place rooms in the model and try again.");
                    return Result.Cancelled;
                }

                // Run documentation
                var svc    = new RoomDocumentationService();
                var result = svc.Execute(doc, rooms);

                // Show summary
                TaskDialog.Show(
                    "Room Documenter — Finish Documentation Complete",
                    result.FormatSummary());

                return Result.Succeeded;
            }
            catch (InvalidOperationException ioEx)
            {
                // Mapping-not-found or similar expected condition
                TaskDialog.Show("Room Documenter", ioEx.Message);
                return Result.Cancelled;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Room Documenter — Error", ex.Message);
                return Result.Failed;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Room resolution
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the rooms to process:
        /// <list type="number">
        ///   <item>Rooms currently selected in the active view (pre-selection).</item>
        ///   <item>All placed rooms (area &gt; 0) on the active level, if no rooms
        ///         are pre-selected.</item>
        /// </list>
        /// </summary>
        private static IList<Room> ResolveRooms(Document doc, UIDocument uiDoc)
        {
            // Check pre-selection
            var selected = uiDoc.Selection
                .GetElementIds()
                .Select(id => doc.GetElement(id))
                .OfType<Room>()
                .Where(r => r.Area > 0)
                .ToList();

            if (selected.Count > 0) return selected;

            // Fall back to all placed rooms on the active level
            ElementId? activeLevelId = null;
            if (uiDoc.ActiveView is ViewPlan plan)
                activeLevelId = plan.GenLevel?.Id;

            var allRooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .Where(r => r != null && r.Area > 0);

            if (activeLevelId != null && activeLevelId != ElementId.InvalidElementId)
                allRooms = allRooms.Where(r =>
                    r.LevelId != null && r.LevelId.Value == activeLevelId.Value);

            return allRooms.ToList();
        }
    }
}
