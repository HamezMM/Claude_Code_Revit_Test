// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
// Pure data carrier — NO Revit API calls.

using System.Collections.Generic;

namespace PDG.Revit.FireRatingLines.Models
{
    /// <summary>
    /// Aggregated summary of a single DrawFireRatingLines run covering walls,
    /// floors, ceilings, and roofs. Populated by FireRatingLinesService and
    /// displayed in the TaskDialog summary.
    /// </summary>
    public class FireRatingLinesResult
    {
        // ── Wall counts ───────────────────────────────────────────────────────

        /// <summary>Number of detail lines successfully created for walls in this run.</summary>
        public int LinesDrawn { get; set; }

        /// <summary>Total number of (wall, view) pairs processed (attempted, regardless of outcome).</summary>
        public int WallsProcessed { get; set; }

        /// <summary>
        /// Number of walls skipped because their LocationCurve is not a straight Line
        /// (e.g. Arc-based curved walls). v1 limitation — v2 will add Arc projection support.
        /// </summary>
        public int SkippedCurvedWalls { get; set; }

        // ── Horizontal element counts (floors, ceilings, roofs) ───────────────

        /// <summary>Number of detail lines successfully created for floors, ceilings, and roofs.</summary>
        public int HorizontalLinesDrawn { get; set; }

        /// <summary>Total number of (floor/ceiling/roof, view) pairs processed.</summary>
        public int HorizontalElementsProcessed { get; set; }

        /// <summary>
        /// Number of roofs skipped because they are sloped (ExtrusionRoof or pitched FootPrintRoof).
        /// v1 limitation — v2 will add sloped-roof geometry support.
        /// </summary>
        public int SkippedSlopedRoofs { get; set; }

        // ── Shared counts ─────────────────────────────────────────────────────

        /// <summary>
        /// Total existing fire-rating detail lines deleted across all views before redrawing.
        /// Covers lines from both wall and horizontal element passes.
        /// </summary>
        public int LinesDeleted { get; set; }

        /// <summary>
        /// Fire rating keys for which no matching GraphicsStyle line style was found.
        /// Applies to ratings found on any element type (wall, floor, ceiling, or roof).
        /// </summary>
        public List<string> UnmatchedRatings { get; set; } = new List<string>();
    }
}
