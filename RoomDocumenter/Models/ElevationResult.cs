// RoomDocumenter | Revit 2024 | net48
using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RoomDocumenter.Models
{
    /// <summary>
    /// Result returned by <c>ElevationService.CreateElevationsForRoom</c>.
    /// Carries the IDs of the four created elevation views (or fewer if some
    /// failed) and any skip / warning messages for the summary dialog.
    /// </summary>
    public class ElevationResult
    {
        /// <summary>Number of rooms for which elevation creation was attempted.</summary>
        public int RoomsProcessed { get; set; }

        /// <summary>Number of rooms skipped (unplaced, no valid boundary, etc.).</summary>
        public int RoomsSkipped { get; set; }

        /// <summary>Total elevation views successfully created across all rooms.</summary>
        public int ElevationsCreated { get; set; }

        /// <summary>
        /// ElementIds of elevation views created per room, keyed by room ElementId.
        /// </summary>
        public Dictionary<ElementId, List<ElementId>> ViewIdsByRoom { get; set; }
            = new Dictionary<ElementId, List<ElementId>>();

        /// <summary>
        /// Human-readable skip / warning messages accumulated during the run.
        /// </summary>
        public List<string> SkipReasons { get; set; } = new List<string>();

        /// <summary>
        /// Formats a multi-line summary suitable for display in a Revit TaskDialog.
        /// </summary>
        public string FormatSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Rooms processed   : {RoomsProcessed}");
            sb.AppendLine($"Rooms skipped     : {RoomsSkipped}");
            sb.AppendLine($"Elevations created: {ElevationsCreated}");

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
