using Autodesk.Revit.DB;
using PDG.Revit.AutomationTools.Models;
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
            // Wall sweep profile types live in the OST_Cornices built-in category.
            // Using WhereElementIsElementType() limits results to type definitions,
            // excluding any placed instances.
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
