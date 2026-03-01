// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/

using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace PDG.Revit.FireRatingLines
{
    /// <summary>
    /// Revit IExternalApplication entry point.
    /// Registers the "Fire Rating Lines" button under the "PDG Tools" tab → "Fire Safety" panel.
    /// </summary>
    // PDG API NOTE 2026-03-01: UIControlledApplication.CreateRibbonTab()
    //   Verified: revitapidocs.com/2024/ — throws ArgumentException if tab already exists.
    //   Wrap in try/catch: AutomationTools may have already created "PDG Tools".
    // PDG API NOTE 2026-03-01: UIControlledApplication.CreateRibbonPanel(tabName, panelName)
    //   Verified: revitapidocs.com/2024/ — creates a panel on the named tab.
    // PDG API NOTE 2026-03-01: UIControlledApplication.GetRibbonPanels(tabName)
    //   Verified: revitapidocs.com/2024/ — returns List<RibbonPanel> for the named tab.
    public class App : IExternalApplication
    {
        private const string RibbonTabName  = "PDG Tools";
        private const string PanelName      = "Fire Safety";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                CreateRibbonPanel(application);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("PDG FireRatingLines — Startup Error", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static void CreateRibbonPanel(UIControlledApplication application)
        {
            // Create the "PDG Tools" tab — catch ArgumentException if it already exists
            // (e.g. PDG.Revit.AutomationTools was loaded first and created the tab).
            try
            {
                application.CreateRibbonTab(RibbonTabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Tab already exists — continue.
            }

            var panel = GetOrCreatePanel(application, RibbonTabName, PanelName);

            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            // PDG: No icon — PDG.Revit.Shared IconHelper is not available in this project.
            // Add a LargeImage / Image assignment here when icons are available.
            var fireRatingLinesBtn = new PushButtonData(
                name:         "FireRatingLines",
                text:         "Fire Rating\nLines",
                assemblyName: assemblyPath,
                className:    "PDG.Revit.FireRatingLines.Commands.FireRatingLinesCmd")
            {
                ToolTip = "Draw fire-rating annotation detail lines at wall centrelines in sheeted plan and section views.",
                LongDescription =
                    "Annotates every fire-rated wall visible in a sheeted plan or section view. " +
                    "Ensures standard line styles exist (45 MIN, 1 HR, 1.5 HR, 2 HR, 3 HR, 4 HR), " +
                    "creating any that are missing, then deletes stale lines and redraws fresh ones."
            };

            var updateDoorRatingsBtn = new PushButtonData(
                name:         "UpdateDoorFireRatings",
                text:         "Door Fire\nRatings",
                assemblyName: assemblyPath,
                className:    "PDG.Revit.FireRatingLines.Commands.UpdateDoorFireRatingCmd")
            {
                ToolTip = "Set the Fire Rating parameter on every door in a fire-rated wall.",
                LongDescription =
                    "Reads each door's host wall fire rating and writes the required door rating " +
                    "to the door's Fire Rating instance parameter using the PDG standard mapping " +
                    "(e.g. 2 HR wall → 1.5 HR door). Updates door schedules automatically."
            };

            panel.AddItem(fireRatingLinesBtn);
            panel.AddItem(updateDoorRatingsBtn);
        }

        private static RibbonPanel GetOrCreatePanel(
            UIControlledApplication application,
            string tabName,
            string panelName)
        {
            foreach (var existing in application.GetRibbonPanels(tabName))
            {
                if (existing.Name == panelName)
                    return existing;
            }

            return application.CreateRibbonPanel(tabName, panelName);
        }
    }
}
