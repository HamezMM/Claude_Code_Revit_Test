// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
// Pure data carrier — NO Revit API calls.

using System.Collections.Generic;

namespace PDG.Revit.FireRatingLines.Models
{
    /// <summary>
    /// Aggregated summary of a single DrawFireRatingLines run.
    /// Populated by FireRatingLinesService and displayed in the TaskDialog summary.
    /// </summary>
    public class FireRatingLinesResult
    {
        /// <summary>Number of detail lines successfully created in this run.</summary>
        public int LinesDrawn { get; set; }

        /// <summary>Number of existing fire-rating detail lines deleted before redrawing.</summary>
        public int LinesDeleted { get; set; }

        /// <summary>Total number of (wall, view) pairs processed (attempted, regardless of outcome).</summary>
        public int WallsProcessed { get; set; }

        /// <summary>
        /// Number of walls skipped because their LocationCurve is not a straight Line
        /// (e.g. Arc-based curved walls). v1 limitation — v2 will add Arc projection support.
        /// </summary>
        public int SkippedCurvedWalls { get; set; }

        /// <summary>
        /// Fire rating keys for which no matching GraphicsStyle line style was found.
        /// These ratings were present on wall types but had no corresponding line style name.
        /// </summary>
        public List<string> UnmatchedRatings { get; set; } = new List<string>();
    }
}
