// GridBuilderAddin | Revit 2024 | net48
// All Revit API calls are marked with their source class/method.
// revitapidocs.com/2024/ was unreachable (HTTP 403) at generation time —
// each affected API call carries: // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GridBuilderAddin.Services;
using GridBuilderAddin.UI;
using System;
using System.Diagnostics;

namespace GridBuilderAddin.Commands
{
    /// <summary>
    /// Revit <see cref="IExternalCommand"/> entry point for the Grid Builder tool.
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Validating that an active document is open.</item>
    ///   <item>Showing the <see cref="GridBuilderWindow"/> modal dialog.</item>
    ///   <item>On confirmation, delegating grid creation to <see cref="GridBuilderService"/>.</item>
    /// </list>
    /// No business logic or Revit API model operations live here.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GridBuilderCmd : IExternalCommand
    {
        /// <summary>
        /// Called by Revit when the user invokes the Grid Builder command.
        /// </summary>
        /// <param name="commandData">Provides access to the Revit application and active document.</param>
        /// <param name="message">Set to a user-readable error message on failure.</param>
        /// <param name="elements">Not used by this command.</param>
        /// <returns>
        /// <see cref="Result.Succeeded"/> if grids were created;
        /// <see cref="Result.Cancelled"/> if the user dismissed the dialog;
        /// <see cref="Result.Failed"/> on unexpected exception.
        /// </returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Debug.WriteLine("[GridBuilder] GridBuilderCmd.Execute — start.");

                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // ExternalCommandData.Application — revitapidocs.com/2024/
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;

                if (uiDoc == null)
                {
                    // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                    // TaskDialog.Show(string, string) — revitapidocs.com/2024/
                    TaskDialog.Show(
                        "Grid Builder",
                        "No active document found.\nPlease open a Revit project and try again.");

                    return Result.Cancelled;
                }

                var doc = uiDoc.Document;

                // ── Show configuration dialog ────────────────────────────────
                var viewModel = new GridBuilderViewModel();
                var window    = new GridBuilderWindow(viewModel);

                window.SetRevitOwner(uiApp.MainWindowHandle);

                Debug.WriteLine("[GridBuilder] Showing GridBuilderWindow dialog.");
                var confirmed = window.ShowDialog();

                if (confirmed != true)
                {
                    Debug.WriteLine("[GridBuilder] Dialog cancelled by user.");
                    return Result.Cancelled;
                }

                // ── Delegate grid creation to service ────────────────────────
                var config  = viewModel.BuildConfig();
                var service = new GridBuilderService();

                Debug.WriteLine($"[GridBuilder] Calling GridBuilderService.CreateGrid with XCount={config.XCount}, YCount={config.YCount}.");
                var success = service.CreateGrid(doc, config);

                Debug.WriteLine($"[GridBuilder] GridBuilderService.CreateGrid returned: {success}.");
                return success ? Result.Succeeded : Result.Failed;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                Debug.WriteLine("[GridBuilder] OperationCanceledException caught — returning Cancelled.");
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GridBuilder] Unhandled exception: {ex}");
                message = ex.Message;

                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                // TaskDialog.Show(string, string) — revitapidocs.com/2024/
                TaskDialog.Show("Grid Builder — Unexpected Error", ex.Message);

                return Result.Failed;
            }
        }
    }
}
