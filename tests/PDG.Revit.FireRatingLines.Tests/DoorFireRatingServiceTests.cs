// PDG GENERATED: 2026-03-01 | Revit 2024
// Test stubs for DoorFireRatingService — at least one [Fact] per service stage.
// Tests that require a live Revit Document are marked [Fact(Skip = "Requires live Revit")].
// Pure-logic tests (mapping verification) run without Revit.

using PDG.Revit.FireRatingLines.Models;
using System.Collections.Generic;
using Xunit;

namespace PDG.Revit.FireRatingLines.Tests
{
    public class DoorFireRatingServiceTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Pure-logic tests — no Revit required
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void WallToDoorRating_MappingHasSixEntries()
        {
            // The mapping must cover all six standard wall ratings.
            Assert.Equal(6, FireRatingStandards.WallToDoorRating.Count);
        }

        [Fact]
        public void WallToDoorRating_AllStandardWallRatingsAreMapped()
        {
            // Every entry in StandardRatings must appear as a key in WallToDoorRating.
            foreach (var wallRating in FireRatingStandards.StandardRatings)
                Assert.True(
                    FireRatingStandards.WallToDoorRating.ContainsKey(wallRating),
                    $"StandardRatings entry '{wallRating}' is missing from WallToDoorRating.");
        }

        [Fact]
        public void WallToDoorRating_ValuesMatchDoorRatingsArray()
        {
            // DoorRatings[i] must equal WallToDoorRating[StandardRatings[i]] for every index.
            for (int i = 0; i < FireRatingStandards.StandardRatings.Length; i++)
            {
                var wallRating = FireRatingStandards.StandardRatings[i];
                var expectedDoor = FireRatingStandards.DoorRatings[i];
                Assert.Equal(expectedDoor, FireRatingStandards.WallToDoorRating[wallRating]);
            }
        }

        [Fact]
        public void WallToDoorRating_IsCaseInsensitive()
        {
            // Lookup must succeed regardless of case.
            Assert.True(FireRatingStandards.WallToDoorRating.ContainsKey("1 hr"));
            Assert.True(FireRatingStandards.WallToDoorRating.ContainsKey("2 HR"));
            Assert.True(FireRatingStandards.WallToDoorRating.ContainsKey("45 Min"));
        }

        [Fact]
        public void WallToDoorRating_SpecificMappingsAreCorrect()
        {
            // Spot-check all six entries against the PDG standard table.
            Assert.Equal("20 MIN",  FireRatingStandards.WallToDoorRating["45 MIN"]);
            Assert.Equal("45 MIN",  FireRatingStandards.WallToDoorRating["1 HR"]);
            Assert.Equal("1 HR",    FireRatingStandards.WallToDoorRating["1.5 HR"]);
            Assert.Equal("1.5 HR",  FireRatingStandards.WallToDoorRating["2 HR"]);
            Assert.Equal("2 HR",    FireRatingStandards.WallToDoorRating["3 HR"]);
            Assert.Equal("3 HR",    FireRatingStandards.WallToDoorRating["4 HR"]);
        }

        [Fact]
        public void DoorFireRatingResult_DefaultsToZeroCounts()
        {
            var result = new DoorFireRatingResult();

            Assert.Equal(0, result.DoorsUpdated);
            Assert.Equal(0, result.DoorsSkipped);
            Assert.Equal(0, result.DoorsInUnratedWalls);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void DoorFireRatingUpdate_StoresAllFields()
        {
            var doorId   = new Autodesk.Revit.DB.ElementId(1001L);
            var wallId   = new Autodesk.Revit.DB.ElementId(2001L);
            var update   = new DoorFireRatingUpdate(doorId, wallId, "2 HR", "1.5 HR");

            Assert.Equal(doorId,   update.DoorId);
            Assert.Equal(wallId,   update.HostWallId);
            Assert.Equal("2 HR",   update.WallRating);
            Assert.Equal("1.5 HR", update.DoorRating);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 1 — GetDoorFireRatingUpdates
        // ─────────────────────────────────────────────────────────────────────

        [Fact(Skip = "Requires live Revit")]
        public void GetDoorFireRatingUpdates_ReturnsDoorInFireRatedWall()
        {
            // Arrange: document with one door hosted in a wall whose type has Fire Rating = "2 HR".
            // Act: call service.GetDoorFireRatingUpdates(doc, wallTypeIdToRating).
            // Assert: result contains one DoorFireRatingUpdate with DoorRating == "1.5 HR".
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetDoorFireRatingUpdates_SkipsDoorInUnratedWall()
        {
            // Arrange: document with a door in a wall that has no Fire Rating parameter set.
            // Act: call service.GetDoorFireRatingUpdates(doc, wallTypeIdToRating).
            // Assert: result is empty (door not in scope).
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetDoorFireRatingUpdates_SkipsDoorWithNonWallHost()
        {
            // Arrange: document with a door hosted in a curtain wall panel (non-Wall host).
            // Act: call service.GetDoorFireRatingUpdates(doc, wallTypeIdToRating).
            // Assert: that door is not in the result.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void GetDoorFireRatingUpdates_ReturnsMultipleDoorsInSameWall()
        {
            // Arrange: document with two doors hosted in the same fire-rated wall.
            // Act: call service.GetDoorFireRatingUpdates(doc, wallTypeIdToRating).
            // Assert: result contains two updates, both with the same DoorRating.
            throw new System.NotImplementedException();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stage 2 — ApplyDoorFireRatings
        // ─────────────────────────────────────────────────────────────────────

        [Fact(Skip = "Requires live Revit")]
        public void ApplyDoorFireRatings_SetsFireRatingParameterOnDoor()
        {
            // Arrange: document with a door whose Fire Rating instance parameter is writable.
            //   updates = [DoorFireRatingUpdate(doorId, wallId, "2 HR", "1.5 HR")]
            // Act: call service.ApplyDoorFireRatings(doc, updates).
            // Assert: door's DOOR_FIRE_RATING parameter value == "1.5 HR";
            //         result.DoorsUpdated == 1; result.DoorsSkipped == 0.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void ApplyDoorFireRatings_SkipsDoorWithMissingParameter()
        {
            // Arrange: a door family without a Fire Rating instance parameter.
            // Act: call service.ApplyDoorFireRatings(doc, updates).
            // Assert: result.DoorsSkipped == 1; result.DoorsUpdated == 0;
            //         result.Warnings is non-empty.
            throw new System.NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void ApplyDoorFireRatings_ReturnsCorrectCountsForMixedDoors()
        {
            // Arrange: two doors — one with a writable Fire Rating param, one without.
            // Act: call service.ApplyDoorFireRatings(doc, updates).
            // Assert: result.DoorsUpdated == 1; result.DoorsSkipped == 1.
            throw new System.NotImplementedException();
        }
    }
}
