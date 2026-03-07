// GridBuilderAddin.Tests | Revit 2024 | net48
// Pure-logic tests for the GetAlphaLabel helper.
// No Revit API dependency — these run in any xUnit host.
using GridBuilderAddin.Services;
using GridBuilderAddin.UI;
using Xunit;

namespace GridBuilderAddin.Tests
{
    /// <summary>
    /// Verifies <see cref="GridBuilderService.GetAlphaLabel"/> and
    /// <see cref="GridBuilderViewModel.GetAlphaLabel"/> return identical,
    /// correct results for all boundary values.
    /// </summary>
    public class GetAlphaLabelTests
    {
        // ── Single-letter range (A–Z, indices 0–25) ─────────────────────────

        [Fact]
        public void Index0_ReturnsA()
        {
            Assert.Equal("A", GridBuilderService.GetAlphaLabel(0));
            Assert.Equal("A", GridBuilderViewModel.GetAlphaLabel(0));
        }

        [Fact]
        public void Index1_ReturnsB()
        {
            Assert.Equal("B", GridBuilderService.GetAlphaLabel(1));
            Assert.Equal("B", GridBuilderViewModel.GetAlphaLabel(1));
        }

        [Fact]
        public void Index25_ReturnsZ()
        {
            Assert.Equal("Z", GridBuilderService.GetAlphaLabel(25));
            Assert.Equal("Z", GridBuilderViewModel.GetAlphaLabel(25));
        }

        // ── First double-letter rollover (AA, index 26) ─────────────────────

        [Fact]
        public void Index26_ReturnsAA()
        {
            Assert.Equal("AA", GridBuilderService.GetAlphaLabel(26));
            Assert.Equal("AA", GridBuilderViewModel.GetAlphaLabel(26));
        }

        [Fact]
        public void Index27_ReturnsAB()
        {
            Assert.Equal("AB", GridBuilderService.GetAlphaLabel(27));
            Assert.Equal("AB", GridBuilderViewModel.GetAlphaLabel(27));
        }

        [Fact]
        public void Index51_ReturnsAZ()
        {
            // A=0…25, AA=26…51 ⟹ AZ = 26 + 25 = 51
            Assert.Equal("AZ", GridBuilderService.GetAlphaLabel(51));
            Assert.Equal("AZ", GridBuilderViewModel.GetAlphaLabel(51));
        }

        [Fact]
        public void Index52_ReturnsBA()
        {
            Assert.Equal("BA", GridBuilderService.GetAlphaLabel(52));
            Assert.Equal("BA", GridBuilderViewModel.GetAlphaLabel(52));
        }

        // ── Last double-letter value (ZZ, index 701) ─────────────────────────

        [Fact]
        public void Index701_ReturnsZZ()
        {
            // ZZ = 26 + 26*26 - 1 = 26 + 676 - 1 = 701
            Assert.Equal("ZZ", GridBuilderService.GetAlphaLabel(701));
            Assert.Equal("ZZ", GridBuilderViewModel.GetAlphaLabel(701));
        }

        // ── First triple-letter value (AAA, index 702) ───────────────────────

        [Fact]
        public void Index702_ReturnsAAA()
        {
            Assert.Equal("AAA", GridBuilderService.GetAlphaLabel(702));
            Assert.Equal("AAA", GridBuilderViewModel.GetAlphaLabel(702));
        }

        // ── Consecutive labels form the expected sequence ────────────────────

        [Fact]
        public void ConsecutiveLabels_MatchExpectedSequence()
        {
            var expected = new[]
            {
                "A","B","C","D","E","F","G","H","I","J","K","L","M",
                "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
                "AA","AB","AC"
            };

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], GridBuilderService.GetAlphaLabel(i));
                Assert.Equal(expected[i], GridBuilderViewModel.GetAlphaLabel(i));
            }
        }

        // ── Service and ViewModel implementations always agree ───────────────

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(25)]
        [InlineData(26)]
        [InlineData(51)]
        [InlineData(52)]
        [InlineData(100)]
        [InlineData(701)]
        [InlineData(702)]
        public void ServiceAndViewModel_ReturnIdenticalResults(int index)
        {
            Assert.Equal(
                GridBuilderService.GetAlphaLabel(index),
                GridBuilderViewModel.GetAlphaLabel(index));
        }
    }
}
