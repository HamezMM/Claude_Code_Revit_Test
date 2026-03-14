// GridBuilderAddin | Revit 2024 | net48
// API unverified — check https://www.revitapidocs.com/2024/ before compiling.
using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace GridBuilderAddin
{
    /// <summary>
    /// Revit <see cref="IExternalApplication"/> entry point for the Grid Builder addin.
    /// Registers the "Grid Builder" push button on the "PDG Tools" ribbon tab
    /// under a dedicated "Structural" panel, mirroring the pattern used by
    /// <c>PDG.Revit.AutomationTools.App</c>.
    /// </summary>
    public class App : IExternalApplication
    {
        private const string RibbonTabName = "PDG Tools";
        private const string PanelName     = "Structural";

        /// <summary>
        /// Called by Revit at startup. Creates (or joins) the PDG Tools ribbon tab,
        /// creates the Structural panel if absent, and registers the Grid Builder button.
        /// </summary>
        /// <param name="application">Revit's controlled application handle.</param>
        /// <returns><see cref="Result.Succeeded"/> on success; <see cref="Result.Failed"/> on error.</returns>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                RegisterRibbonButton(application);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // TaskDialog.Show(string, string) — revitapidocs.com/2024/
                TaskDialog.Show("Grid Builder — Startup Error", ex.Message);
                return Result.Failed;
            }
        }

        /// <summary>Called by Revit at shutdown. No cleanup required.</summary>
        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

        // ── Private helpers ───────────────────────────────────────────────────

        private static void RegisterRibbonButton(UIControlledApplication application)
        {
            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // UIControlledApplication.CreateRibbonTab(string) — revitapidocs.com/2024/
            // Create the "PDG Tools" tab if it does not already exist.
            // ArgumentException is thrown when the tab name is already taken — silently continue.
            try
            {
                application.CreateRibbonTab(RibbonTabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Tab already registered by another PDG addin — continue.
            }

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // UIControlledApplication.GetRibbonPanels(string) — revitapidocs.com/2024/
            // UIControlledApplication.CreateRibbonPanel(string, string) — revitapidocs.com/2024/
            var panel = GetOrCreatePanel(application, RibbonTabName, PanelName);

            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // PushButtonData(string, string, string, string) — revitapidocs.com/2024/
            var buttonData = new PushButtonData(
                name:         "GridBuilder",
                text:         "Grid\nBuilder",
                assemblyName: assemblyPath,
                className:    "GridBuilderAddin.Commands.GridBuilderCmd")
            {
                ToolTip         = "Configure and create a rectilinear structural grid.",
                LongDescription =
                    "Opens the Grid Builder dialog to set X/Y grid counts, " +
                    "per-interval spacing overrides (mm or ft-in), and a live " +
                    "schematic preview before placing all grid lines in the document."
            };

            // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
            // RibbonPanel.AddItem(RibbonItemData) — revitapidocs.com/2024/
            panel.AddItem(buttonData);
        }

        /// <summary>
        /// Returns an existing <see cref="RibbonPanel"/> whose <see cref="RibbonPanel.Name"/>
        /// matches <paramref name="panelName"/>, or creates a new one if none is found.
        /// </summary>
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
