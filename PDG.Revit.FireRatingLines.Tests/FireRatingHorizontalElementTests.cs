// PDG GENERATED: 2026-03-01 | Revit 2024
// Test stubs for horizontal element stages (floors, ceilings, roofs) of FireRatingLinesService.
// All Revit-dependent tests are marked [Fact(Skip = "Requires live Revit")].

using PDG.Revit.FireRatingLines.Models;
using Xunit;

namespace PDG.Revit.FireRatingLines.Tests
{
    public class FireRatingHorizontalElementTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Stage A — GetFireRatedHorizontalTypes
        // ─────────────────────────────────────────────────────────────────────

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalTypes_ReturnsFloorTypesWithRating()
        {
            // Arrange: document with a FloorType whose "Fire Rating" parameter = "1-HR".
            // Act: call service.GetFireRatedHorizontalTypes(doc).
            // Assert: result contains an entry with a value of "1-HR" whose key matches
            //         the FloorType ElementId.Value.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalTypes_ReturnsCeilingTypesWithRating()
        {
            // Arrange: document with a CeilingType whose "Fire Rating" parameter = "2-HR".
            // Act: call service.GetFireRatedHorizontalTypes(doc).
            // Assert: result contains the CeilingType Id.Value mapped to "2-HR".
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalTypes_ReturnsRoofTypesWithRating()
        {
            // Arrange: document with a RoofType whose "Fire Rating" parameter = "1-HR".
            // Act: call service.GetFireRatedHorizontalTypes(doc).
            // Assert: result contains the RoofType Id.Value mapped to "1-HR".
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalTypes_TrimsWhitespaceFromRatingKey()
        {
            // Arrange: a FloorType with "Fire Rating" parameter value " 1-HR " (padded).
            // Act: call service.GetFireRatedHorizontalTypes(doc).
            // Assert: the returned rating value is "1-HR" (trimmed).
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalTypes_ReturnsEmptyWhenNoRatedTypes()
        {
            // Arrange: document with no fire-rated floor, ceiling, or roof types.
            // Act: call service.GetFireRatedHorizontalTypes(doc).
            // Assert: result.Count == 0.
            throw new System.NotImplementedException();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage B — GetFireRatedHorizontalElementsInSectionViews
        // ─────────────────────────────────────────────────────────────────────

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalElementsInSectionViews_FiltersToSectionViewsOnly()
        {
            // Arrange: document with a fire-rated floor visible in both a plan view
            //          and a section view. Both views are on sheets.
            // Act: call service.GetFireRatedHorizontalElementsInSectionViews(doc, types, out _).
            // Assert: result contains only the section view entry; plan view entry is absent.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalElementsInSectionViews_DeduplicatesByElementAndViewId()
        {
            // Arrange: the same section view placed on two different sheets. A fire-rated
            //          floor is visible in that view.
            // Act: call service.GetFireRatedHorizontalElementsInSectionViews(doc, types, out _).
            // Assert: result contains exactly ONE entry for that (floor, view) pair.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalElementsInSectionViews_SkipsExtrusionRoofs()
        {
            // Arrange: document with a fire-rated ExtrusionRoof visible in a sheeted section view.
            // Act: call service.GetFireRatedHorizontalElementsInSectionViews(doc, types, out skipped).
            // Assert: result does NOT contain the ExtrusionRoof; skipped == 1.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalElementsInSectionViews_SkipsSlopedFootPrintRoofs()
        {
            // Arrange: document with a fire-rated pitched FootPrintRoof whose world bbox
            //          height significantly exceeds the compound structure thickness.
            // Act: call service.GetFireRatedHorizontalElementsInSectionViews(doc, types, out skipped).
            // Assert: result does NOT contain the sloped roof; skipped >= 1.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetFireRatedHorizontalElementsInSectionViews_IncludesFlatFootPrintRoofs()
        {
            // Arrange: document with a fire-rated flat FootPrintRoof visible in a section view.
            //          World bbox height ≈ compound structure thickness (flat roof).
            // Act: call service.GetFireRatedHorizontalElementsInSectionViews(doc, types, out skipped).
            // Assert: result contains the flat roof; skipped == 0.
            throw new System.NotImplementedException();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Pure-logic tests — no Revit required
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void FireRatingHorizontalElement_StoresAllPropertiesCorrectly()
        {
            // Verifies constructor and property accessors on the pure data carrier.
            var wallId  = new Autodesk.Revit.DB.ElementId(42L);
            var viewId  = new Autodesk.Revit.DB.ElementId(99L);
            var element = new FireRatingHorizontalElement(
                wallId, viewId, "1-HR", HorizontalElementCategory.Floor);

            Assert.Equal(42L, element.ElementId.Value);
            Assert.Equal(99L, element.ViewId.Value);
            Assert.Equal("1-HR", element.RatingKey);
            Assert.Equal(HorizontalElementCategory.Floor, element.Category);
        }

        [Fact]
        public void FireRatingLinesResult_HorizontalCountsDefaultToZero()
        {
            // Verifies that the new horizontal element properties default correctly.
            var result = new FireRatingLinesResult();

            Assert.Equal(0, result.HorizontalLinesDrawn);
            Assert.Equal(0, result.HorizontalElementsProcessed);
            Assert.Equal(0, result.SkippedSlopedRoofs);
        }
    }
}
