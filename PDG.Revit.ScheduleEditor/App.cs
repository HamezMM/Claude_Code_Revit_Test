// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace PDG.Revit.ScheduleEditor
{
    /// <summary>
    /// Revit IExternalApplication entry point for the PDG Schedule Editor addin.
    /// Registers the Syncfusion licence key and adds a ribbon button to the
    /// shared "PDG Tools" tab.
    /// </summary>
    public class App : IExternalApplication
    {
        private const string RibbonTabName = "PDG Tools";
        private const string PanelName     = "Schedule Editor";

        /// <summary>
        /// TODO: Replace this placeholder with your Syncfusion Community Licence key.
        /// Obtain a free key at https://www.syncfusion.com/sales/communitylicense
        /// </summary>
        private const string SyncfusionLicenceKey = "YOUR_SYNCFUSION_LICENCE_KEY_HERE";

        /// <inheritdoc />
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Register Syncfusion licence key before any SfDataGrid control is created.
                // Without a valid key the grid displays a licence dialogue at runtime.
                Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(SyncfusionLicenceKey);

                CreateRibbonPanel(application);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("PDG Schedule Editor — Startup Error", ex.Message);
                return Result.Failed;
            }
        }

        /// <inheritdoc />
        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private static void CreateRibbonPanel(UIControlledApplication application)
        {
            try { application.CreateRibbonTab(RibbonTabName); }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { /* tab already exists */ }

            var panel    = GetOrCreatePanel(application, RibbonTabName, PanelName);
            var assembly = Assembly.GetExecutingAssembly().Location;

            var btn = new PushButtonData(
                name       : "ScheduleEditor_Open",
                text       : "Schedule\nEditor",
                assemblyName: assembly,
                className  : "PDG.Revit.ScheduleEditor.Commands.ScheduleEditorCmd")
            {
                ToolTip = "Open any ViewSchedule in an editable WPF grid.",
                LongDescription =
                    "Lists all ViewSchedules in the active document (standard, key, " +
                    "material takeoff, note block).  Select one and click Open to " +
                    "launch the Schedule Editor.  Edit cells and click Apply to write " +
                    "changes back in a single Revit transaction."
            };

            panel.AddItem(btn);
        }

        private static RibbonPanel GetOrCreatePanel(
            UIControlledApplication application,
            string tabName,
            string panelName)
        {
            foreach (var existing in application.GetRibbonPanels(tabName))
                if (existing.Name == panelName) return existing;

            return application.CreateRibbonPanel(tabName, panelName);
        }
    }
}
