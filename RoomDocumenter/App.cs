// RoomDocumenter | Revit 2024 | net48
using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace RoomDocumenter
{
    /// <summary>
    /// Revit IExternalApplication entry point for the RoomDocumenter addin.
    /// Adds three PushButtons to the existing "PDG Tools" custom ribbon tab
    /// (creating the tab if it does not yet exist).
    /// </summary>
    public class App : IExternalApplication
    {
        private const string RibbonTabName = "PDG Tools";
        private const string PanelName     = "Room Documenter";

        /// <inheritdoc />
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                CreateRibbonPanel(application);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Room Documenter — Startup Error", ex.Message);
                return Result.Failed;
            }
        }

        /// <inheritdoc />
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private static void CreateRibbonPanel(UIControlledApplication application)
        {
            // Create the PDG Tools tab if it does not already exist
            try { application.CreateRibbonTab(RibbonTabName); }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { /* tab exists */ }

            var panel = GetOrCreatePanel(application, RibbonTabName, PanelName);
            var assembly = Assembly.GetExecutingAssembly().Location;

            // ── Button 1: Finish Mapping ───────────────────────────────────
            var finishMappingBtn = new PushButtonData(
                name       : "RoomDocumenter_FinishMapping",
                text       : "Finish\nMapping",
                assemblyName: assembly,
                className  : "RoomDocumenter.Commands.FinishMappingCmd")
            {
                ToolTip = "Map room finish parameter strings to Revit FloorType, " +
                          "CeilingType, and WallSweepType. Saved to Extensible Storage.",
                LongDescription =
                    "Opens a tabbed WPF dialog.  Discovers all unique finish strings " +
                    "from rooms in the project and lets you assign a matching Revit " +
                    "system family type to each string."
            };

            // ── Button 2: Room Documentation ──────────────────────────────
            var roomDocBtn = new PushButtonData(
                name       : "RoomDocumenter_RoomDocumentation",
                text       : "Document\nRooms",
                assemblyName: assembly,
                className  : "RoomDocumenter.Commands.RoomDocumentationCmd")
            {
                ToolTip = "Create or reconcile floors, ceilings, and baseboards for " +
                          "selected (or all) rooms based on the saved finish mapping.",
                LongDescription =
                    "Reads the saved finish mapping and, for each room, creates or " +
                    "reconciles Floor, Ceiling, and WallSweep elements. " +
                    "Existing elements of the correct type are left unchanged; " +
                    "mismatched elements are replaced."
            };

            // ── Button 3: Interior Elevations ─────────────────────────────
            var elevationBtn = new PushButtonData(
                name       : "RoomDocumenter_InteriorElevations",
                text       : "Interior\nElevations",
                assemblyName: assembly,
                className  : "RoomDocumenter.Commands.InteriorElevationCmd")
            {
                ToolTip = "Create 4-way interior elevation views for selected rooms, " +
                          "cropped to the room boundary and named by compass direction.",
                LongDescription =
                    "Opens a room picker dialog, then creates four interior elevation " +
                    "views per room.  Views are named using the building's cardinal " +
                    "orientation read from Extensible Storage, and crop boxes are " +
                    "sized to the room boundary plus a configurable offset."
            };

            panel.AddStackedItems(finishMappingBtn, roomDocBtn, elevationBtn);
        }

        private static RibbonPanel GetOrCreatePanel(
            UIControlledApplication application,
            string tabName,
            string panelName)
        {
            foreach (var existing in application.GetRibbonPanels(tabName))
            {
                if (existing.Name == panelName) return existing;
            }
            return application.CreateRibbonPanel(tabName, panelName);
        }
    }
}
