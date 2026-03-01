// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
using Autodesk.Revit.DB;
using PDG.Revit.AutomationTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDG.Revit.AutomationTools.Services
{
    /// <summary>
    /// Queries the Revit document for all WallSweepType elements (profile types)
    /// loaded in the model and returns lightweight summary objects for dialog binding.
    /// </summary>
    public class SweepTypeCollectorService
    {
        /// <summary>
        /// Returns all wall sweep profile types loaded in the document,
        /// sorted alphabetically by name.
        /// </summary>
        public List<SweepTypeSummary> GetSweepTypes(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // PDG API NOTE 2026-03-01: FilteredElementCollector.OfCategory(OST_Cornices)
            //   Verified: revitapidocs.com/2024/ — WallSweepType in the Revit 2024 API is an enum
            //   (Sweep/Reveal), NOT a class usable with OfClass(). Wall sweep profile type definitions
            //   are stored under BuiltInCategory.OST_Cornices. OfCategory(OST_Cornices) is therefore
            //   the correct and only reliable filter for wall sweep profile types in Revit 2024.
            //   OfClass(typeof(WallSweepType)) would fail because WallSweepType is not an Element subclass.
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Cornices)
                .WhereElementIsElementType()
                .Cast<ElementType>()
                .OrderBy(t => t.Name)
                .Select(t => new SweepTypeSummary(t.Id, t.Name))
                .ToList();
        }
    }
}
