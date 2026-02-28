using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PDG.Revit.AutomationTools.Models;
using System.Collections.Generic;

namespace PDG.Revit.AutomationTools.Services
{
    /// <summary>
    /// Coordinates the full placement run across all target wall instances.
    /// All WallSweep.Create calls are wrapped in a single named transaction
    /// so that a single Undo in Revit reverses every placed sweep.
    /// </summary>
    public class PlacementOrchestrationService
    {
        private const string TransactionName = "PDG: Place Wall Base Sweeps";

        /// <summary>
        /// Executes the placement run and returns a result record for every
        /// wall instance that was processed.
        /// </summary>
        public List<SweepPlacementResult> Execute(
            Document doc,
            UIDocument uiDoc,
            SweepPlacementOptions options)
        {
            var results = new List<SweepPlacementResult>();

            var wallCollector = new WallInstanceCollectorService();
            var duplicateChecker = new SweepDuplicateCheckerService();
            var placementService = new SweepPlacementService(duplicateChecker);

            var targetWalls = wallCollector.GetTargetWalls(doc, uiDoc, options);

            using (var transaction = new Transaction(doc, TransactionName))
            {
                transaction.Start();

                foreach (var wall in targetWalls)
                {
                    var result = placementService.PlaceSweep(doc, wall, options);
                    results.Add(result);
                }

                transaction.Commit();
            }

            return results;
        }
    }
}
