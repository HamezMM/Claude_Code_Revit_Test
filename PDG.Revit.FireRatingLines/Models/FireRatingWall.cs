// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
// Pure data carrier — NO Revit API calls. ElementId is stored only; no document queries here.

using Autodesk.Revit.DB;

namespace PDG.Revit.FireRatingLines.Models
{
    /// <summary>
    /// Lightweight data record representing a single fire-rated wall that must be
    /// annotated with a detail line in a specific sheeted view.
    /// </summary>
    /// <remarks>
    /// Immutable after construction. Created by FireRatingLinesService.GetFireRatedWallsInViews()
    /// and consumed by FireRatingLinesService.DrawFireRatingLines().
    /// </remarks>
    public class FireRatingWall
    {
        /// <summary>ElementId of the Wall instance to annotate.</summary>
        public ElementId WallId { get; }

        /// <summary>ElementId of the View in which the detail line is drawn.</summary>
        public ElementId ViewId { get; }

        /// <summary>
        /// Fire rating key read from WALL_ATTR_FIRE_RATING_PARAM (e.g. "1-HR").
        /// Used to look up the matching GraphicsStyle line style.
        /// </summary>
        public string RatingKey { get; }

        /// <summary>
        /// ViewType of the target view — drives geometry projection strategy.
        /// Only FloorPlan, CeilingPlan, and Section are used in v1.
        /// </summary>
        public ViewType ViewType { get; }

        public FireRatingWall(ElementId wallId, ElementId viewId, string ratingKey, ViewType viewType)
        {
            WallId = wallId;
            ViewId = viewId;
            RatingKey = ratingKey;
            ViewType = viewType;
        }
    }
}
