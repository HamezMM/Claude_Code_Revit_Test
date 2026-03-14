// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using System.Collections.Generic;

namespace PDG.Revit.ScheduleEditor.Models
{
    /// <summary>
    /// Summary of a single <see cref="Services.ScheduleWriterService.ApplyEdits"/> call.
    /// Returned to the ViewModel so that a status message can be displayed without
    /// blocking the UI with Revit dialogs.
    /// </summary>
    public sealed class WriteResult
    {
        /// <summary>Number of cells successfully written to Revit.</summary>
        public int SuccessCount { get; }

        /// <summary>Number of cells that were skipped (read-only, invalid value, not found, etc.).</summary>
        public int SkipCount { get; }

        /// <summary>
        /// Human-readable warning strings explaining each skipped or failed cell.
        /// Empty when every pending edit was applied successfully.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Initialises the result.
        /// </summary>
        public WriteResult(int successCount, int skipCount, IReadOnlyList<string> warnings)
        {
            SuccessCount = successCount;
            SkipCount    = skipCount;
            Warnings     = warnings;
        }
    }
}
