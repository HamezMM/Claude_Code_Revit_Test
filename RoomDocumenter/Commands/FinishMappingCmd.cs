// RoomDocumenter | Revit 2024 | net48
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RoomDocumenter.Services;
using RoomDocumenter.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;

namespace RoomDocumenter.Commands
{
    /// <summary>
    /// Opens the Finish Mapping dialog, collects unique finish strings from the
    /// current project, and persists the user's mapping to Extensible Storage.
    ///
    /// <para>No Revit API geometry calls are made here.  All data collection
    /// and storage is delegated to <see cref="FinishMappingService"/>.</para>
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FinishMappingCmd : IExternalCommand
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
                    TaskDialog.Show("Room Documenter", "No active document. Please open a project.");
                    return Result.Cancelled;
                }

                var doc = uiDoc.Document;

                if (doc.IsFamilyDocument)
                {
                    TaskDialog.Show("Room Documenter",
                        "Finish Mapping is only available in a project document.");
                    return Result.Cancelled;
                }

                var svc = new FinishMappingService();

                // Collect current finish strings from all placed rooms
                var finishStrings = svc.CollectUniqueFinishStrings(doc);

                // Collect available Revit types
                var floorTypes     = CollectTypes<FloorType>(doc);
                var ceilingTypes   = CollectTypes<CeilingType>(doc);
                var sweepTypes     = CollectWallSweepTypes(doc);

                // Load existing mapping (null if none persisted yet)
                var existingMapping = svc.Load(doc);

                // Build ViewModel and show dialog
                var vm = new FinishMappingViewModel(
                    finishStrings, floorTypes, ceilingTypes, sweepTypes, existingMapping);

                var dialog = new FinishMappingDialog(vm);
                SetRevitOwner(dialog, uiApp.MainWindowHandle);

                var confirmed = dialog.ShowDialog();
                if (confirmed != true || vm.ResultMapping == null)
                    return Result.Cancelled;

                // Persist mapping inside a transaction
                using var trans = new Transaction(doc, "RoomDocumenter — Save Finish Mapping");
                trans.Start();
                svc.Save(doc, vm.ResultMapping);
                trans.Commit();

                TaskDialog.Show("Room Documenter — Finish Mapping",
                    "Finish mapping saved successfully.");
                return Result.Succeeded;
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
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static IList<TypeItem> CollectTypes<T>(Document doc)
            where T : ElementType
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(T))
                .Cast<T>()
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new TypeItem { Name = t.Name, IdValue = t.Id.Value })
                .ToList();
        }

        private static IList<TypeItem> CollectWallSweepTypes(Document doc)
        {
            // API NOTE: WallSweepType is an enum in Revit 2024 (Sweep/Reveal), NOT a class.
            // Wall sweep profile type definitions are stored under OST_Cornices.
            // Verified: revitapidocs.com/2024/
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Cornices)
                .WhereElementIsElementType()
                .Cast<ElementType>()
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new TypeItem { Name = t.Name, IdValue = t.Id.Value })
                .ToList();
        }

        private static void SetRevitOwner(System.Windows.Window window, IntPtr ownerHandle)
        {
            if (ownerHandle == IntPtr.Zero) return;
            new WindowInteropHelper(window).Owner = ownerHandle;
        }
    }
}
