// RoomDocumenter | Revit 2024 | net48
// API NOTES:
//   FilteredElementCollector — Verified: revitapidocs.com/2024/
//   ViewFamilyType           — Verified: revitapidocs.com/2024/
//   ElementId.Value (Int64)  — Verified: revitapidocs.com/2024/ (never IntegerValue)
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
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
    /// Opens the Room Picker dialog, validates that an "Interior Elevation"
    /// ViewFamilyType exists, wraps all elevation work in a TransactionGroup,
    /// and delegates creation to <see cref="ElevationService"/>.
    ///
    /// <para>No Revit API geometry calls are made here.  All elevation logic
    /// lives in <see cref="ElevationService"/>.</para>
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class InteriorElevationCmd : IExternalCommand
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
                    TaskDialog.Show("Room Documenter", "No active document.");
                    return Result.Cancelled;
                }

                var doc = uiDoc.Document;

                if (doc.IsFamilyDocument)
                {
                    TaskDialog.Show("Room Documenter",
                        "Interior Elevations are only available in a project document.");
                    return Result.Cancelled;
                }

                // Collect all placed rooms for the picker
                var allRooms = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .Cast<Room>()
                    .Where(r => r != null && r.Area > 0)
                    .ToList();

                if (allRooms.Count == 0)
                {
                    TaskDialog.Show("Room Documenter",
                        "No placed rooms found in this project.");
                    return Result.Cancelled;
                }

                // Build picker items
                var pickerItems = allRooms.Select(r =>
                {
                    var level = doc.GetElement(r.LevelId) as Level;
                    return new RoomPickerItem
                    {
                        RoomId    = r.Id,
                        Number    = r.Number,
                        Name      = r.Name,
                        LevelName = level?.Name ?? "Unknown"
                    };
                }).ToList();

                var vm     = new RoomPickerViewModel(pickerItems);
                var dialog = new RoomPickerDialog(vm);
                SetRevitOwner(dialog, uiApp.MainWindowHandle);

                var confirmed = dialog.ShowDialog();
                if (confirmed != true) return Result.Cancelled;

                var selectedItems = vm.SelectedRooms;
                if (selectedItems.Count == 0) return Result.Cancelled;

                // Resolve Room elements from selected ids
                var selectedIds = new HashSet<long>(selectedItems.Select(i => i.RoomId.Value));
                var selectedRooms = allRooms
                    .Where(r => selectedIds.Contains(r.Id.Value))
                    .ToList();

                // Resolve active view id (used when creating elevation markers)
                var activeViewId = uiDoc.ActiveView?.Id ?? ElementId.InvalidElementId;
                if (activeViewId == ElementId.InvalidElementId ||
                    !(uiDoc.ActiveView is ViewPlan))
                {
                    // Find any floor plan view as fallback
                    activeViewId = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewPlan))
                        .Cast<ViewPlan>()
                        .Where(v => v.ViewType == ViewType.FloorPlan && !v.IsTemplate)
                        .Select(v => v.Id)
                        .FirstOrDefault() ?? ElementId.InvalidElementId;

                    if (activeViewId == ElementId.InvalidElementId)
                    {
                        TaskDialog.Show("Room Documenter",
                            "No floor plan view found. Open a floor plan view and try again.");
                        return Result.Cancelled;
                    }
                }

                // Construct service — validates Interior Elevation VFT exists
                ElevationService svc;
                try
                {
                    svc = new ElevationService(doc);
                }
                catch (InvalidOperationException ioEx)
                {
                    TaskDialog.Show("Room Documenter — Missing View Type", ioEx.Message);
                    return Result.Cancelled;
                }

                // Wrap all operations in a TransactionGroup
                using var tg = new TransactionGroup(doc,
                    "RoomDocumenter — Create Interior Elevations");
                tg.Start();

                var result = svc.CreateElevationsForRooms(
                    selectedRooms,
                    activeViewId,
                    vm.CropOffsetMm);

                tg.Assimilate();

                TaskDialog.Show(
                    "Room Documenter — Interior Elevations Complete",
                    result.FormatSummary());

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

        private static void SetRevitOwner(System.Windows.Window window, IntPtr ownerHandle)
        {
            if (ownerHandle == IntPtr.Zero) return;
            new WindowInteropHelper(window).Owner = ownerHandle;
        }
    }
}
