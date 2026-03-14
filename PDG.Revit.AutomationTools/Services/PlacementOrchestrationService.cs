// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PDG.Revit.AutomationTools.Models;
using System;
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
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var results = new List<SweepPlacementResult>();

            var wallCollector = new WallInstanceCollectorService();
            var duplicateChecker = new SweepDuplicateCheckerService();
            var placementService = new SweepPlacementService(duplicateChecker);

            var targetWalls = wallCollector.GetTargetWalls(doc, uiDoc, options);

            // PDG API NOTE 2026-03-01: new Transaction(doc, TransactionName)
            //   Verified: revitapidocs.com/2024/ — Transaction wraps all WallSweep.Create calls
            //   so the entire placement run is a single Undo entry for the user.
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
