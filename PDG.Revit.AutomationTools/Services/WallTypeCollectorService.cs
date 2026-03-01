// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using Autodesk.Revit.DB;
using PDG.Revit.AutomationTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDG.Revit.AutomationTools.Services
{
    /// <summary>
    /// Queries the Revit document for all WallType elements and returns
    /// lightweight summary objects suitable for dialog binding.
    /// </summary>
    public class WallTypeCollectorService
    {
        /// <summary>
        /// Returns all wall types present in the document, sorted alphabetically by name.
        /// </summary>
        public List<WallTypeSummary> GetWallTypes(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // PDG API NOTE 2026-03-01: FilteredElementCollector(doc).OfClass(typeof(WallType))
            //   Verified: revitapidocs.com/2024/ — OfClass(WallType) returns all wall type elements.
            //   WhereElementIsElementType() not used here as OfClass(WallType) is already type-scoped.
            return new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .OrderBy(wt => wt.Name)
                .Select(wt => new WallTypeSummary(wt.Id, wt.Name))
                .ToList();
        }
    }
}
