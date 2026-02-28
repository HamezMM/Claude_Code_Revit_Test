using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PDG.Revit.AutomationTools.Services;
using PDG.Revit.AutomationTools.ViewModels;
using PDG.Revit.AutomationTools.Views;
using System;
using System.Windows.Interop;

namespace PDG.Revit.AutomationTools.Commands
{
    /// <summary>
    /// IExternalCommand entry point for the Wall Base Sweep Placer.
    /// Validates context, launches the configuration dialog, and delegates
    /// all business logic to services. No placement logic lives here.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PlaceWallBaseSweepCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show("PDG: Wall Base Sweep", "No active document found. Please open a project and try again.");
                    return Result.Cancelled;
                }

                var doc = uiDoc.Document;

                // Collect data for the dialog
                var wallTypeService = new WallTypeCollectorService();
                var sweepTypeService = new SweepTypeCollectorService();

                var wallTypes = wallTypeService.GetWallTypes(doc);
                var sweepTypes = sweepTypeService.GetSweepTypes(doc);

                if (sweepTypes.Count == 0)
                {
                    TaskDialog.Show(
                        "PDG: Wall Base Sweep",
                        "No wall sweep profile types were found in this model.\n\n" +
                        "Load at least one wall sweep type before running this tool.");
                    return Result.Cancelled;
                }

                // Build and show the configuration dialog
                var viewModel = new PlaceWallBaseSweepViewModel(wallTypes, sweepTypes);
                var dialog = new PlaceWallBaseSweepDialog(viewModel);

                SetRevitOwner(dialog, uiApp.MainWindowHandle);

                var confirmed = dialog.ShowDialog();

                if (confirmed != true)
                    return Result.Cancelled;

                // Execute placement via orchestration service
                var orchestrationService = new PlacementOrchestrationService();
                var results = orchestrationService.Execute(doc, uiDoc, viewModel.BuildOptions());

                // Show results summary
                var resultsViewModel = new ResultsSummaryViewModel(results);
                var resultsDialog = new ResultsSummaryDialog(resultsViewModel);

                SetRevitOwner(resultsDialog, uiApp.MainWindowHandle);
                resultsDialog.ShowDialog();

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("PDG: Wall Base Sweep — Error", ex.Message);
                return Result.Failed;
            }
        }

        private static void SetRevitOwner(System.Windows.Window window, IntPtr ownerHandle)
        {
            if (ownerHandle == IntPtr.Zero)
                return;

            var helper = new WindowInteropHelper(window);
            helper.Owner = ownerHandle;
        }
    }
}
