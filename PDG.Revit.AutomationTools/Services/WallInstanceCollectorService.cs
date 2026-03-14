// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PDG.Revit.AutomationTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDG.Revit.AutomationTools.Services
{
    /// <summary>
    /// Collects Wall instances according to the user-selected scope and
    /// filters them down to the target wall type(s).
    /// </summary>
    public class WallInstanceCollectorService
    {
        /// <summary>
        /// Returns all Wall instances matching the target wall types within the specified scope.
        /// </summary>
        public List<Wall> GetTargetWalls(
            Document doc,
            UIDocument uiDoc,
            SweepPlacementOptions options)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var allWalls = CollectWallsByScope(doc, uiDoc, options.Scope);

            var targetTypeIds = new HashSet<ElementId>(options.SelectedWallTypeIds);

            return allWalls
                .Where(w => targetTypeIds.Contains(w.GetTypeId()))
                .ToList();
        }

        private IEnumerable<Wall> CollectWallsByScope(
            Document doc,
            UIDocument uiDoc,
            PlacementScope scope)
        {
            switch (scope)
            {
                case PlacementScope.ActiveView:
                    // PDG API NOTE 2026-03-01: FilteredElementCollector(doc, doc.ActiveView.Id).OfClass(typeof(Wall))
                    //   Verified: revitapidocs.com/2024/ — view-scoped collector returns Wall instances visible in the active view.
                    return new FilteredElementCollector(doc, doc.ActiveView.Id)
                        .OfClass(typeof(Wall))
                        .Cast<Wall>();

                case PlacementScope.CurrentSelection:
                    return CollectFromSelection(doc, uiDoc);

                default: // EntireModel
                    // PDG API NOTE 2026-03-01: FilteredElementCollector(doc).OfClass(typeof(Wall))
                    //   Verified: revitapidocs.com/2024/ — document-scoped collector returns all Wall instances in the model.
                    return new FilteredElementCollector(doc)
                        .OfClass(typeof(Wall))
                        .Cast<Wall>();
            }
        }

        private IEnumerable<Wall> CollectFromSelection(Document doc, UIDocument uiDoc)
        {
            // PDG API NOTE 2026-03-01: UIDocument.Selection.GetElementIds()
            //   Verified: revitapidocs.com/2024/ — returns the currently selected element IDs in the active document.
            var selectedIds = uiDoc.Selection.GetElementIds();

            return selectedIds
                .Select(id => doc.GetElement(id))
                .OfType<Wall>();
        }
    }
}
