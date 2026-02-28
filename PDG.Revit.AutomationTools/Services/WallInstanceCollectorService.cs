using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PDG.Revit.AutomationTools.Models;
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
                    return new FilteredElementCollector(doc, doc.ActiveView.Id)
                        .OfClass(typeof(Wall))
                        .Cast<Wall>();

                case PlacementScope.CurrentSelection:
                    return CollectFromSelection(doc, uiDoc);

                default: // EntireModel
                    return new FilteredElementCollector(doc)
                        .OfClass(typeof(Wall))
                        .Cast<Wall>();
            }
        }

        private IEnumerable<Wall> CollectFromSelection(Document doc, UIDocument uiDoc)
        {
            var selectedIds = uiDoc.Selection.GetElementIds();

            return selectedIds
                .Select(id => doc.GetElement(id))
                .OfType<Wall>();
        }
    }
}
