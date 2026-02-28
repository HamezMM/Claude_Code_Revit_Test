using PDG.Revit.AutomationTools.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PDG.Revit.AutomationTools.ViewModels
{
    /// <summary>
    /// ViewModel for the post-run results summary dialog.
    /// Exposes aggregate counts and a filtered list of non-placed results.
    /// </summary>
    public class ResultsSummaryViewModel
    {
        public int PlacedCount { get; }
        public int SkippedCount { get; }
        public int FailedCount { get; }

        /// <summary>
        /// Results where the sweep was either skipped or failed — shown in the detail list.
        /// Successfully placed walls are summarised in the header count only.
        /// </summary>
        public ObservableCollection<SweepPlacementResult> NonPlacedResults { get; }

        public string SummaryText =>
            $"{PlacedCount} sweep{(PlacedCount != 1 ? "s" : "")} placed, " +
            $"{SkippedCount} skipped, " +
            $"{FailedCount} failed.";

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
