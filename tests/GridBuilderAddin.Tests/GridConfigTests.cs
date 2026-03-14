// GridBuilderAddin.Tests | Revit 2024 | net48
// Pure-logic tests for GridConfig and GridBuilderConstants.
// No Revit API dependency — these run in any xUnit host.
using GridBuilderAddin;
using GridBuilderAddin.Models;
using System.Collections.Generic;
using Xunit;

namespace GridBuilderAddin.Tests
{
    /// <summary>
    /// Verifies <see cref="GridConfig"/> property defaults and
    /// <see cref="GridBuilderConstants"/> named constant values.
    /// </summary>
    public class GridConfigTests
    {
        // ── GridConfig defaults ──────────────────────────────────────────────

        [Fact]
        public void GridConfig_XSpacingsMm_DefaultsToEmptyList()
        {
            var config = new GridConfig();
            Assert.NotNull(config.XSpacingsMm);
            Assert.Empty(config.XSpacingsMm);
        }

        [Fact]
        public void GridConfig_YSpacingsMm_DefaultsToEmptyList()
        {
            var config = new GridConfig();
            Assert.NotNull(config.YSpacingsMm);
            Assert.Empty(config.YSpacingsMm);
        }

        [Fact]
        public void GridConfig_PropertiesRoundtrip()
        {
            var config = new GridConfig
            {
                XCount          = 5,
                YCount          = 3,
                DefaultSpacingMm = 6000,
                XSpacingsMm     = new List<double> { 6000, 6000, 6000, 6000 },
                YSpacingsMm     = new List<double> { 6000, 6000 }
            };

            Assert.Equal(5, config.XCount);
            Assert.Equal(3, config.YCount);
            Assert.Equal(6000, config.DefaultSpacingMm);
            Assert.Equal(4, config.XSpacingsMm.Count);   // XCount - 1
            Assert.Equal(2, config.YSpacingsMm.Count);   // YCount - 1
        }

        [Fact]
        public void GridConfig_XSpacingsMm_CountEqualsXCountMinusOne()
        {
            // For XCount=4, there are 3 intervals → 3 spacings.
            var config = new GridConfig
            {
                XCount      = 4,
                XSpacingsMm = new List<double> { 8000, 8000, 8000 }
            };

            Assert.Equal(config.XCount - 1, config.XSpacingsMm.Count);
        }

        [Fact]
        public void GridConfig_YSpacingsMm_CountEqualsYCountMinusOne()
        {
            var config = new GridConfig
            {
                YCount      = 4,
                YSpacingsMm = new List<double> { 8000, 8000, 8000 }
            };

            Assert.Equal(config.YCount - 1, config.YSpacingsMm.Count);
        }

        // ── GridBuilderConstants values ──────────────────────────────────────

        [Fact]
        public void Constants_DefaultXCount_IsFour()
        {
            Assert.Equal(4, GridBuilderConstants.DefaultXCount);
        }

        [Fact]
        public void Constants_DefaultYCount_IsFour()
        {
            Assert.Equal(4, GridBuilderConstants.DefaultYCount);
        }

        [Fact]
        public void Constants_MinGridCount_IsTwo()
        {
            Assert.Equal(2, GridBuilderConstants.MinGridCount);
        }

        [Fact]
        public void Constants_DefaultSpacingMm_IsEightThousand()
        {
            Assert.Equal(8000.0, GridBuilderConstants.DefaultSpacingMm);
        }

        [Fact]
        public void Constants_ExtentBeyondGridMm_IsTwoThousand()
        {
            Assert.Equal(2000.0, GridBuilderConstants.ExtentBeyondGridMm);
        }

        [Fact]
        public void Constants_CollisionSuffix_IsUnderscore_new()
        {
            Assert.Equal("_new", GridBuilderConstants.CollisionSuffix);
        }

        [Fact]
        public void Constants_TransactionName_IsExpectedString()
        {
            Assert.Equal("Create Structural Grid", GridBuilderConstants.TransactionName);
        }
    }
}
