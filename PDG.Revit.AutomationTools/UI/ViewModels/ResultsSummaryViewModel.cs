// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using PDG.Revit.AutomationTools.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PDG.Revit.AutomationTools.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the post-run results summary dialog.
    /// Exposes aggregate counts and a filtered list of non-placed results.
    /// </summary>
    public class ResultsSummaryViewModel
    {
        /// <summary>Count of wall sweep placements that succeeded.</summary>
        public int PlacedCount { get; }

        /// <summary>Count of wall instances skipped because a sweep of the same type already existed.</summary>
        public int SkippedCount { get; }

        /// <summary>Count of wall instances where placement failed due to an API error.</summary>
        public int FailedCount { get; }

        /// <summary>
        /// Results where the sweep was either skipped or failed — shown in the detail list.
        /// Successfully placed walls are summarised in the header count only.
        /// </summary>
        public ObservableCollection<SweepPlacementResult> NonPlacedResults { get; }

        /// <summary>
        /// Human-readable summary line shown in the results dialog header
        /// (e.g., "3 sweeps placed, 1 skipped, 0 failed.").
        /// </summary>
        public string SummaryText =>
            $"{PlacedCount} sweep{(PlacedCount != 1 ? "s" : "")} placed, " +
            $"{SkippedCount} skipped, " +
            $"{FailedCount} failed.";

        /// <summary>
        /// True when at least one result was skipped or failed; used to show the detail list in the UI.
        /// </summary>
        public bool HasNonPlacedResults => NonPlacedResults.Count > 0;

        public ResultsSummaryViewModel(IEnumerable<SweepPlacementResult> results)
        {
            var list = results.ToList();

            PlacedCount = list.Count(r => r.Status == PlacementStatus.Placed);
            SkippedCount = list.Count(r => r.Status == PlacementStatus.Skipped);
            FailedCount = list.Count(r => r.Status == PlacementStatus.Failed);

            NonPlacedResults = new ObservableCollection<SweepPlacementResult>(
                list.Where(r => r.Status != PlacementStatus.Placed));
        }
    }
}
