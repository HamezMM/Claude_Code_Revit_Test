// GridBuilderAddin | Revit 2024 | net48
// All Revit API calls are marked with their source class/method.
// revitapidocs.com/2024/ was unreachable (HTTP 403) at generation time —
// each affected API call carries: // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GridBuilderAddin.Models;
using GridBuilderAddin.Services;
using GridBuilderAddin.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GridBuilderAddin.Commands
{
    /// <summary>
    /// Revit <see cref="IExternalCommand"/> entry point for the Grid Builder / Building Builder tool.
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Validating that an active document is open.</item>
    ///   <item>Running the single-step Grid Builder, or the three-step Building Builder flow
    ///         (Grid Builder → Level Builder → Structure Builder) when the toggle is enabled.</item>
    ///   <item>Delegating all model operations to the respective service classes.</item>
    ///   <item>Handling Back-navigation, which deletes and re-creates elements from the re-entered step.</item>
    /// </list>
    /// No business logic or Revit API model operations live directly in this class.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GridBuilderCmd : IExternalCommand
    {
        /// <inheritdoc/>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Debug.WriteLine("[GridBuilder] GridBuilderCmd.Execute — start.");

                // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;

                if (uiDoc == null)
                {
                    // API unverified — check https://www.revitapidocs.com/2024/ before compiling.
                    TaskDialog.Show(
                        "Grid Builder",
                        "No active document found.\nPlease open a Revit project and try again.");
                    return Result.Cancelled;
                }

                var doc    = uiDoc.Document;
                var handle = uiApp.MainWindowHandle;

                // ── State machine: 0=Grid, 1=Level, 2=Structure, -1=done ──────
                int step = 0;

                GridConfig?       gridConfig    = null;
                List<ElementId>?  createdGridIds = null;
                List<LevelRow>?   levelRows     = null;

                while (step >= 0)
                {
                    switch (step)
                    {
                        // ── Step 0: Grid Builder ──────────────────────────────
                        case 0:
                        {
                            var gridVm  = new GridBuilderViewModel();
                            var gridWin = new GridBuilderWindow(gridVm);
                            gridWin.SetRevitOwner(handle);

                            Debug.WriteLine("[GridBuilder] Showing GridBuilderWindow.");
                            if (gridWin.ShowDialog() != true)
                                return Result.Cancelled;

                            // If re-entering (user navigated Back from Level Builder), delete old grids
                            if (createdGridIds != null && createdGridIds.Count > 0)
                            {
                                Debug.WriteLine("[GridBuilder] Back navigation — deleting previous grids.");
                                new GridBuilderService().DeleteElements(doc, createdGridIds);
                                createdGridIds = null;
                            }

                            gridConfig = gridVm.BuildConfig();
                            var gridSvc = new GridBuilderService();
                            createdGridIds = gridSvc.CreateGridWithIds(doc, gridConfig);

                            if (createdGridIds == null)
                                return Result.Failed;

                            // If Building Builder mode is off, we're done after creating grids
                            step = gridVm.IsBuildingBuilderEnabled ? 1 : -1;
                            break;
                        }

                        // ── Step 1: Level Builder ─────────────────────────────
                        case 1:
                        {
                            var levelSvc     = new LevelBuilderService();
                            var existingLvls = levelSvc.FetchExistingLevels(doc);
                            var levelVm      = new LevelBuilderViewModel(existingLvls);
                            var levelWin     = new LevelBuilderWindow(levelVm);
                            levelWin.SetRevitOwner(handle);

                            Debug.WriteLine("[GridBuilder] Showing LevelBuilderWindow.");
                            levelWin.ShowDialog();

                            switch (levelVm.CloseAction)
                            {
                                case WindowCloseAction.Cancelled:
                                    return Result.Cancelled;

                                case WindowCloseAction.GoBack:
                                    step = 0;  // re-show Grid Builder (will delete & recreate grids)
                                    break;

                                case WindowCloseAction.Confirmed:
                                    levelRows = levelVm.GetLevels();
                                    bool levelsOk = levelSvc.ApplyLevels(doc, levelRows);
                                    if (!levelsOk) return Result.Failed;
                                    step = 2;
                                    break;
                            }
                            break;
                        }

                        // ── Step 2: Structure Builder ─────────────────────────
                        case 2:
                        {
                            var structSvc   = new StructureBuilderService();
                            var floorTypes  = structSvc.FetchFloorTypes(doc);
                            var wallTypes   = structSvc.FetchWallTypes(doc);
                            var roofTypes   = structSvc.FetchRoofTypes(doc);
                            var colTypes    = structSvc.FetchStructuralColumnTypes(doc);
                            var levelItems  = structSvc.FetchLevels(doc);

                            var structVm  = new StructureBuilderViewModel(floorTypes, wallTypes, roofTypes, colTypes, levelItems);
                            var structWin = new StructureBuilderWindow(structVm);
                            structWin.SetRevitOwner(handle);

                            Debug.WriteLine("[GridBuilder] Showing StructureBuilderWindow.");
                            structWin.ShowDialog();

                            switch (structVm.CloseAction)
                            {
                                case WindowCloseAction.Cancelled:
                                    return Result.Cancelled;

                                case WindowCloseAction.GoBack:
                                    step = 1;  // re-show Level Builder (levels already in model — will be re-applied on Next)
                                    break;

                                case WindowCloseAction.Confirmed:
                                    var structConfig = structVm.BuildConfig();
                                    bool structOk = structSvc.BuildStructure(doc, gridConfig!, structConfig);
                                    if (!structOk) return Result.Failed;
                                    step = -1;  // done
                                    break;
                            }
                            break;
                        }
                    }
                }

                Debug.WriteLine("[GridBuilder] Building Builder flow completed successfully.");
                return Result.Succeeded;
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
                TaskDialog.Show("Grid Builder — Unexpected Error", ex.Message);

                return Result.Failed;
            }
        }
    }
}
