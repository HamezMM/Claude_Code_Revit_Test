// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
// Pure data carrier — NO Revit API calls. ElementId is stored only; no document queries here.

using Autodesk.Revit.DB;

namespace PDG.Revit.FireRatingLines.Models
{
    /// <summary>
    /// Identifies which Revit host category the horizontal element belongs to.
    /// Used to drive geometry projection strategy in DrawFireRatingLines.
    /// </summary>
    public enum HorizontalElementCategory
    {
        Floor,
        Ceiling,
        Roof
    }

    /// <summary>
    /// Lightweight data record representing a single fire-rated Floor, Ceiling, or Roof
    /// instance that must be annotated with a horizontal detail line in a sheeted section view.
    /// </summary>
    /// <remarks>
    /// Immutable after construction. Created by
    /// FireRatingLinesService.GetFireRatedHorizontalElementsInSectionViews() and consumed by
    /// FireRatingLinesService.DrawFireRatingLines().
    /// Only section views are in scope for horizontal elements (plan views are out of scope).
    /// </remarks>
    public class FireRatingHorizontalElement
    {
        /// <summary>ElementId of the Floor, Ceiling, or RoofBase instance to annotate.</summary>
        public ElementId ElementId { get; }

        /// <summary>ElementId of the section View in which the detail line is drawn.</summary>
        public ElementId ViewId { get; }

        /// <summary>
        /// Fire rating key read from the element type's "Fire Rating" parameter (e.g. "1-HR").
        /// Used to look up the matching GraphicsStyle line style.
        /// </summary>
        public string RatingKey { get; }

        /// <summary>Revit category of the element — drives no geometry change in v1 but aids diagnostics.</summary>
        public HorizontalElementCategory Category { get; }

        public FireRatingHorizontalElement(
            ElementId elementId,
            ElementId viewId,
            string ratingKey,
            HorizontalElementCategory category)
        {
            ElementId = elementId;
            ViewId    = viewId;
            RatingKey = ratingKey;
            Category  = category;
        }
    }
}
