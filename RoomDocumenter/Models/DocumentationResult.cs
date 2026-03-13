// RoomDocumenter | Revit 2024 | net48
using System.Collections.Generic;

namespace RoomDocumenter.Models
{
    /// <summary>
    /// Aggregated result returned by <c>RoomDocumentationService.Execute</c>.
    /// Carries per-category counters and a list of skip/warning messages for
    /// the final TaskDialog summary.
    /// </summary>
    public class DocumentationResult
    {
        /// <summary>Number of rooms for which processing was attempted.</summary>
        public int RoomsProcessed { get; set; }

        /// <summary>Number of rooms skipped before any element work began.</summary>
        public int RoomsSkipped { get; set; }

        // ── Floors ────────────────────────────────────────────────────────

        /// <summary>Floors created from scratch.</summary>
        public int FloorsCreated { get; set; }

        /// <summary>Existing floors whose type was mismatched — deleted and recreated.</summary>
        public int FloorsUpdated { get; set; }

        /// <summary>Existing floors whose type already matched — left untouched.</summary>
        public int FloorsUnchanged { get; set; }

        // ── Ceilings ──────────────────────────────────────────────────────

        /// <summary>Ceilings created from scratch.</summary>
        public int CeilingsCreated { get; set; }

        /// <summary>Existing ceilings whose type was mismatched — deleted and recreated.</summary>
        public int CeilingsUpdated { get; set; }

        /// <summary>Existing ceilings whose type already matched — left untouched.</summary>
        public int CeilingsUnchanged { get; set; }

        // ── Baseboards ────────────────────────────────────────────────────

        /// <summary>Baseboard WallSweeps created from scratch.</summary>
        public int BaseboardsCreated { get; set; }

        /// <summary>Existing baseboards whose type was mismatched — deleted and recreated.</summary>
        public int BaseboardsUpdated { get; set; }

        /// <summary>Existing baseboards whose type already matched — left untouched.</summary>
        public int BaseboardsUnchanged { get; set; }

        // ── Log ───────────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable skip / warning messages accumulated during the run.
        /// Each entry identifies the affected room and reason.
        /// </summary>
        public List<string> SkipReasons { get; set; } = new List<string>();

        /// <summary>
        /// Formats a multi-line summary suitable for display in a Revit TaskDialog.
        /// </summary>
        public string FormatSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Rooms processed : {RoomsProcessed}");
            sb.AppendLine($"Rooms skipped   : {RoomsSkipped}");
            sb.AppendLine();
            sb.AppendLine("Floors    — Created: {0}  Updated: {1}  Unchanged: {2}"
                .Replace("{0}", FloorsCreated.ToString())
                .Replace("{1}", FloorsUpdated.ToString())
                .Replace("{2}", FloorsUnchanged.ToString()));
            sb.AppendLine("Ceilings  — Created: {0}  Updated: {1}  Unchanged: {2}"
                .Replace("{0}", CeilingsCreated.ToString())
                .Replace("{1}", CeilingsUpdated.ToString())
                .Replace("{2}", CeilingsUnchanged.ToString()));
            sb.AppendLine("Baseboards— Created: {0}  Updated: {1}  Unchanged: {2}"
                .Replace("{0}", BaseboardsCreated.ToString())
                .Replace("{1}", BaseboardsUpdated.ToString())
                .Replace("{2}", BaseboardsUnchanged.ToString()));

            if (SkipReasons.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Skip / Warning log:");
                int max = System.Math.Min(SkipReasons.Count, 20);
                for (int i = 0; i < max; i++)
                    sb.AppendLine($"  • {SkipReasons[i]}");
                if (SkipReasons.Count > max)
                    sb.AppendLine($"  … and {SkipReasons.Count - max} more (see Output Window).");
            }

            return sb.ToString();
        }
    }
}
