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
            var buttonData = new PushButtonData(
                name:         "FireRatingLines",
                text:         "Fire Rating\nLines",
                assemblyName: assemblyPath,
                className:    "PDG.Revit.FireRatingLines.Commands.FireRatingLinesCmd")
            {
                ToolTip = "Draw fire-rating annotation detail lines at wall centrelines in plan and section views.",
                LongDescription =
                    "Annotates every fire-rated wall that is cut in a plan view or visible in a section view " +
                    "currently placed on a sheet. The line style applied matches the wall type's Fire Rating " +
                    "parameter value (e.g. '1-HR'). Existing fire-rating lines are deleted and redrawn on each run."
            };

            panel.AddItem(buttonData);
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
