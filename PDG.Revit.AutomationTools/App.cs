// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace PDG.Revit.AutomationTools
{
    /// <summary>
    /// Revit IExternalApplication entry point.
    /// Registers the PDG Tools ribbon tab and adds the Wall Base Sweep button.
    /// </summary>
    public class App : IExternalApplication
    {
        private const string RibbonTabName = "PDG Tools";
        private const string PanelName = "Automation";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                CreateRibbonPanel(application);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("PDG AutomationTools — Startup Error", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static void CreateRibbonPanel(UIControlledApplication application)
        {
            // Create the PDG Tools tab if it does not already exist
            try
            {
                application.CreateRibbonTab(RibbonTabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Tab already exists — continue
            }

            var panel = GetOrCreatePanel(application, RibbonTabName, PanelName);

            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            var buttonData = new PushButtonData(
                name: "PlaceWallBaseSweep",
                text: "Wall Base\nSweep",
                assemblyName: assemblyPath,
                className: "PDG.Revit.AutomationTools.Commands.PlaceWallBaseSweepCmd")
            {
                ToolTip = "Automatically place baseboard wall sweeps at the base of selected wall types.",
                LongDescription =
                    "Opens the Wall Base Sweep dialog to configure target wall types, " +
                    "sweep profile, base offset, and scope before placing WallSweep elements."
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
