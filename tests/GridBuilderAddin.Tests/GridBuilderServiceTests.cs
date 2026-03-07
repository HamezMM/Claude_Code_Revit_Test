// GridBuilderAddin.Tests | Revit 2024 | net48
// Tests for GridBuilderService.
// Revit-dependent methods are marked [Fact(Skip = "Requires live Revit")]
// so the test assembly compiles and loads without Revit DLLs present.
using GridBuilderAddin.Models;
using GridBuilderAddin.Services;
using System;
using Xunit;

namespace GridBuilderAddin.Tests
{
    /// <summary>
    /// Tests for <see cref="GridBuilderService"/>.
    /// Pure-logic tests (e.g. <see cref="GridBuilderService.GetAlphaLabel"/>)
    /// run without Revit. All methods that touch the Revit API are skipped.
    /// </summary>
    public class GridBuilderServiceTests
    {
        // ── GetAlphaLabel — pure logic, no Revit ────────────────────────────
        // (Comprehensive coverage is in GetAlphaLabelTests.cs)

        [Fact]
        public void GetAlphaLabel_IsPublicStatic_AccessibleWithoutInstance()
        {
            // Verify the helper can be called as a static utility from test code.
            var label = GridBuilderService.GetAlphaLabel(0);
            Assert.Equal("A", label);
        }

        [Fact]
        public void GetAlphaLabel_NeverReturnsNullOrEmpty()
        {
            for (int i = 0; i < 800; i++)
            {
                var label = GridBuilderService.GetAlphaLabel(i);
                Assert.False(string.IsNullOrEmpty(label),
                    $"GetAlphaLabel({i}) returned null or empty.");
            }
        }

        [Fact]
        public void GetAlphaLabel_AllLabelsContainOnlyUppercaseLetters()
        {
            for (int i = 0; i < 800; i++)
            {
                var label = GridBuilderService.GetAlphaLabel(i);
                foreach (var ch in label)
                    Assert.True(char.IsUpper(ch),
                        $"GetAlphaLabel({i})=\"{label}\" contains non-uppercase char '{ch}'.");
            }
        }

        [Fact]
        public void GetAlphaLabel_LengthIncreases_AtExpectedBoundaries()
        {
            // Single-letter: indices 0–25
            Assert.Equal(1, GridBuilderService.GetAlphaLabel(0).Length);
            Assert.Equal(1, GridBuilderService.GetAlphaLabel(25).Length);

            // Double-letter: indices 26–701
            Assert.Equal(2, GridBuilderService.GetAlphaLabel(26).Length);
            Assert.Equal(2, GridBuilderService.GetAlphaLabel(701).Length);

            // Triple-letter: index 702+
            Assert.Equal(3, GridBuilderService.GetAlphaLabel(702).Length);
        }

        [Fact]
        public void GetAlphaLabel_ConsecutiveIndices_ProduceSortableSequence()
        {
            // The string sequence A, B, …, Z, AA, AB, … should be monotonically
            // increasing in string ordinal comparison.
            string? previous = null;
            for (int i = 0; i < 100; i++)
            {
                var current = GridBuilderService.GetAlphaLabel(i);
                if (previous != null)
                    Assert.True(
                        string.Compare(current, previous, StringComparison.Ordinal) > 0,
                        $"Label at index {i} (\"{current}\") is not > label at {i - 1} (\"{previous}\").");
                previous = current;
            }
        }

        // ── CreateGrid — requires live Revit document ────────────────────────

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_NullDocument_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new GridBuilderService();
            var config  = new GridConfig
            {
                XCount      = 4, YCount = 4,
                DefaultSpacingMm = 8000,
                XSpacingsMm = new System.Collections.Generic.List<double> { 8000, 8000, 8000 },
                YSpacingsMm = new System.Collections.Generic.List<double> { 8000, 8000, 8000 }
            };

            // Act & Assert: null document must throw
            Assert.Throws<ArgumentNullException>(() => service.CreateGrid(null!, config));
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange: requires a real Document — skipped without Revit.
            // Assert: service.CreateGrid(doc, null) throws ArgumentNullException.
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_Creates_CorrectNumberOfGridElements()
        {
            // Arrange: open a blank Revit document.
            // Act: call service.CreateGrid(doc, config) with XCount=4, YCount=4.
            // Assert: FilteredElementCollector(doc).OfClass(typeof(Grid)).Count() == 8.
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_XGridNames_AreNumericalSequence()
        {
            // Arrange: blank Revit document, config with XCount=3.
            // Act: CreateGrid.
            // Assert: grids named "1", "2", "3" exist in the document.
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_YGridNames_AreAlphabeticalSequence()
        {
            // Arrange: blank Revit document, config with YCount=3.
            // Act: CreateGrid.
            // Assert: grids named "A", "B", "C" exist in the document.
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_Grid1_IsAtXEqualsZero()
        {
            // Arrange: blank Revit document.
            // Act: CreateGrid with any XCount ≥ 2.
            // Assert: the grid named "1" has its line at X = 0.0 (internal units).
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_GridA_IsAtYEqualsZero()
        {
            // Arrange: blank Revit document.
            // Act: CreateGrid with any YCount ≥ 2.
            // Assert: the grid named "A" has its line at Y = 0.0 (internal units).
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_YGrids_ProgressInNegativeYDirection()
        {
            // Arrange: blank Revit document, config with YCount=3, spacing 8000 mm each.
            // Act: CreateGrid.
            // Assert:
            //   Grid "A" Y-position == 0
            //   Grid "B" Y-position == -Convert(8000mm)
            //   Grid "C" Y-position == -Convert(16000mm)
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_GridLines_ExtendBeyondOutermostGrid_By2000mm()
        {
            // Arrange: blank Revit document, config 4×4 at 8000 mm.
            // Act: CreateGrid.
            // Assert: each X grid line length in Y direction ==
            //   Convert(totalYSpan + 2 * ExtentBeyondGridMm).
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_CollidingName_AppendsSuffix_And_ShowsWarning()
        {
            // Arrange: Revit document that already contains a grid named "1".
            // Act: CreateGrid with XCount=2, so grid "1" would collide.
            // Assert:
            //   - A grid named "1_new" exists.
            //   - The original "1" was not modified.
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_YCountGreaterThan26_UsesDoubleLetterLabels()
        {
            // Arrange: blank Revit document, YCount = 27, spacing 1000 mm each.
            // Act: CreateGrid.
            // Assert: a grid named "AA" exists in the document.
            throw new NotImplementedException();
        }

        [Fact(Skip = "Requires live Revit")]
        public void CreateGrid_OnException_RollsBackTransaction()
        {
            // Arrange: a document where Grid.Create will throw (e.g. zero-length line).
            // Act: CreateGrid returns false.
            // Assert: no Grid elements were added to the document (transaction rolled back).
            throw new NotImplementedException();
        }
    }
}
