// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PDG.Revit.ScheduleEditor.Models;
using PDG.Revit.ScheduleEditor.Services;
using PDG.Revit.ScheduleEditor.Utilities;
using PDG.Revit.ScheduleEditor.ViewModels;
using PDG.Revit.ScheduleEditor.Views;
using System;
using System.Windows.Interop;

namespace PDG.Revit.ScheduleEditor.Commands
{
    /// <summary>
    /// Revit <c>IExternalCommand</c> entry point for the Schedule Editor feature.
    /// <para>
    /// Responsibilities:
    /// <list type="number">
    ///   <item>Validate the active document context.</item>
    ///   <item>Use <see cref="ScheduleReaderService"/> to enumerate all schedules.</item>
    ///   <item>Show the <see cref="SchedulePickerWindow"/> for schedule selection.</item>
    ///   <item>Read the selected schedule's data via <see cref="ScheduleReaderService"/>.</item>
    ///   <item>Launch the <see cref="ScheduleEditorWindow"/> for interactive editing.</item>
    /// </list>
    /// No business logic lives here — this class is a thin orchestration shell.
    /// </para>
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ScheduleEditorCmd : IExternalCommand
    {
        /// <inheritdoc />
        public Result Execute(
            ExternalCommandData commandData,
            ref string          message,
            ElementSet          elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show("PDG Schedule Editor",
                        "No active document. Please open a project before running the Schedule Editor.");
                    return Result.Cancelled;
                }

                var doc = uiDoc.Document;

                if (doc.IsFamilyDocument)
                {
                    TaskDialog.Show("PDG Schedule Editor",
                        "The Schedule Editor is only available in project documents, not family files.");
                    return Result.Cancelled;
                }

                // ── Step 1: Collect all schedules ────────────────────────
                var readerService = new ScheduleReaderService(doc);
                var allSchedules  = readerService.GetAllSchedules();

                if (allSchedules.Count == 0)
                {
                    TaskDialog.Show("PDG Schedule Editor",
                        "No schedules were found in the active document.");
                    return Result.Cancelled;
                }

                Logger.Log($"[ScheduleEditorCmd] Found {allSchedules.Count} schedule(s).");

                // ── Step 2: Show schedule picker ─────────────────────────
                var pickerVm     = new SchedulePickerViewModel(allSchedules);
                var pickerWindow = new SchedulePickerWindow(pickerVm);
                SetRevitOwner(pickerWindow, uiApp.MainWindowHandle);

                var pickerResult = pickerWindow.ShowDialog();
                if (pickerResult != true || pickerWindow.SelectedSchedule == null)
                    return Result.Cancelled;

                var selectedItem = pickerWindow.SelectedSchedule;
                Logger.Log($"[ScheduleEditorCmd] User selected schedule: '{selectedItem.Name}' (id={selectedItem.ViewScheduleId}).");

                // ── Step 3: Read schedule data ───────────────────────────
                var (scheduleName, columns, rows) =
                    readerService.ReadScheduleData(selectedItem.ViewScheduleId);

                Logger.Log($"[ScheduleEditorCmd] Loaded {columns.Count} columns, {rows.Count} rows.");

                // ── Step 4: Launch editor ────────────────────────────────
                var writerService = new ScheduleWriterService(doc);
                var editorVm      = new ScheduleEditorViewModel(scheduleName, columns, rows, writerService);
                var editorWindow  = new ScheduleEditorWindow(editorVm);
                SetRevitOwner(editorWindow, uiApp.MainWindowHandle);

                // ShowDialog blocks until the editor is closed.
                editorWindow.ShowDialog();

                Logger.Log("[ScheduleEditorCmd] Editor closed.");
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                Logger.Log("[ScheduleEditorCmd] Unhandled exception", ex);
                message = ex.Message;
                TaskDialog.Show("PDG Schedule Editor — Error",
                    $"An unexpected error occurred:\n\n{ex.Message}\n\n" +
                    "Please check the Debug output for details.");
                return Result.Failed;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the WPF window's Win32 owner to the Revit main window so that
        /// the dialog is properly modal and stays in front of Revit.
        /// </summary>
        private static void SetRevitOwner(System.Windows.Window window, IntPtr ownerHandle)
        {
            if (ownerHandle == IntPtr.Zero) return;
            new WindowInteropHelper(window).Owner = ownerHandle;
        }
    }
}
