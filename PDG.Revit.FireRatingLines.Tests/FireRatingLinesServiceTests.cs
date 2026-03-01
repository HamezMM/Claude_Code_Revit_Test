// PDG GENERATED: 2026-03-01 | Revit 2024
// Test stubs for FireRatingLinesService — at least one [Fact] per service stage.
// Tests that require a live Revit Document are marked [Fact(Skip = "Requires live Revit")].
// The one pure-logic test (FireRatingLinesResult_DefaultsToZeroCounts) runs without Revit.

using PDG.Revit.FireRatingLines.Models;
using System.Collections.Generic;
using Xunit;

namespace PDG.Revit.FireRatingLines.Tests
{
    public class FireRatingLinesServiceTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Stage 1 — GetFireRatedWallTypes
        // ─────────────────────────────────────────────────────────────────────

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedWallTypes_ReturnsOnlyWallTypesWithRatingParam()
        {
            // Arrange: open a Revit document containing at least one wall type with
            // WALL_ATTR_FIRE_RATING_PARAM set (e.g. "1-HR") and one without.
            // Act: call service.GetFireRatedWallTypes(doc).
            // Assert: result contains only the rated type; unrated type is absent.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedWallTypes_ReturnsEmptyDictionary_WhenNoRatedTypes()
        {
            // Arrange: Revit document with no wall types having a fire rating parameter.
            // Act: call service.GetFireRatedWallTypes(doc).
            // Assert: result.Count == 0.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedWallTypes_TrimsWhitespaceFromRatingKey()
        {
            // Arrange: a wall type with WALL_ATTR_FIRE_RATING_PARAM = " 1-HR " (padded).
            // Act: call service.GetFireRatedWallTypes(doc).
            // Assert: result key is "1-HR" (trimmed), not " 1-HR ".
            throw new System.NotImplementedException();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 2 — GetMatchingLineStyles
        // ─────────────────────────────────────────────────────────────────────

        [Fact(Skip = "Requires live Revit")]
        public void GetMatchingLineStyles_ReturnsMatchedProjectionStyles()
        {
            // Arrange: document with an OST_Lines projection GraphicsStyle named "1-HR".
            // Act: call service.GetMatchingLineStyles(doc, new[]{"1-HR"}).
            // Assert: result["1-HR"] is not null; result["1-HR"].GraphicsStyleType == Projection.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetMatchingLineStyles_IsCaseInsensitive()
        {
            // Arrange: line style named "1-hr" (lower-case); rating key is "1-HR".
            // Act: call service.GetMatchingLineStyles(doc, new[]{"1-HR"}).
            // Assert: result.ContainsKey("1-HR") == true (F-11 enhanced matching).
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetMatchingLineStyles_IgnoresNonOstLinesGraphicsStyles()
        {
            // Arrange: a GraphicsStyle whose category is NOT OST_Lines (e.g. OST_Walls).
            // Its name happens to match a rating key.
            // Act: call service.GetMatchingLineStyles(doc, new[]{"SomeRating"}).
            // Assert: that non-OST_Lines style is NOT returned.
            throw new System.NotImplementedException();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 3 — GetFireRatedWallsInViews
        // ─────────────────────────────────────────────────────────────────────

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedWallsInViews_FiltersToSheetedViewsOnly()
        {
            // Arrange: document with two plan views — one placed on a sheet, one not.
            // Both contain fire-rated walls.
            // Act: call service.GetFireRatedWallsInViews(doc, ratedTypes).
            // Assert: result only contains entries for the sheeted view.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedWallsInViews_DeduplicatesByWallAndViewId()
        {
            // Arrange: the same view is placed on two different sheets.
            // A fire-rated wall is visible in that view.
            // Act: call service.GetFireRatedWallsInViews(doc, ratedTypes).
            // Assert: result contains exactly ONE FireRatingWall entry for that (wall, view) pair.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedWallsInViews_SkipsCurvedWalls()
        {
            // Arrange: document with a curved (Arc-based) fire-rated wall visible in a sheeted plan view.
            // Act: call service.GetFireRatedWallsInViews(doc, ratedTypes).
            // Assert: the curved wall is NOT in the result (v1 limitation).
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedWallsInViews_AppliesCutPlaneStraddlingCheckForPlanViews()
        {
            // Arrange: two fire-rated walls — one above the cut plane, one straddling it.
            // Both are "visible" in the plan view (shown in projection).
            // Act: call service.GetFireRatedWallsInViews(doc, ratedTypes).
            // Assert: only the straddling wall is in the result.
            throw new System.NotImplementedException();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 4 — DrawFireRatingLines
        // ─────────────────────────────────────────────────────────────────────

        [Fact(Skip = "Requires live Revit")]
        public void DrawFireRatingLines_ReturnsExpectedLineCounts()
        {
            // Arrange: a document with two fire-rated walls in one sheeted plan view,
            // both with a matching line style, neither previously annotated.
            // Act: call service.DrawFireRatingLines(doc, wallsInViews, lineStyles).
            // Assert: result.LinesDrawn == 2, result.LinesDeleted == 0.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void DrawFireRatingLines_RecordsUnmatchedRatings()
        {
            // Arrange: a FireRatingWall with RatingKey "2-HR" but lineStyles dictionary
            // has no entry for "2-HR".
            // Act: call service.DrawFireRatingLines(doc, wallsInViews, incompleteLineStyles).
            // Assert: result.UnmatchedRatings contains "2-HR".
            throw new System.NotImplementedException();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Pure-logic test — no Revit required
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void FireRatingLinesResult_DefaultsToZeroCounts()
        {
            // Verifies that the result data carrier initialises correctly.
            var result = new FireRatingLinesResult();

            Assert.Equal(0, result.LinesDrawn);
            Assert.Equal(0, result.LinesDeleted);
            Assert.Equal(0, result.WallsProcessed);
            Assert.Equal(0, result.SkippedCurvedWalls);
            Assert.NotNull(result.UnmatchedRatings);
            Assert.Empty(result.UnmatchedRatings);
        }
    }
}
