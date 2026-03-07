// GridBuilderAddin.Tests | Revit 2024 | net48
// Pure-logic tests for SpacingIntervalRow.
// No Revit API dependency — these run in any xUnit host.
using GridBuilderAddin.UI;
using System;
using Xunit;

namespace GridBuilderAddin.Tests
{
    /// <summary>
    /// Verifies <see cref="SpacingIntervalRow"/> validation logic,
    /// property change notification, and value parsing.
    /// </summary>
    public class SpacingIntervalRowTests
    {
        // ── Constructor ──────────────────────────────────────────────────────

        [Fact]
        public void Constructor_SetsLabel()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000);
            Assert.Equal("1 → 2", row.Label);
        }

        [Fact]
        public void Constructor_PreFillsSpacingText_WithDefaultValue()
        {
            var row = new SpacingIntervalRow("A → B", 8000);
            Assert.Equal("8000", row.SpacingText);
        }

        [Fact]
        public void Constructor_PreFillsSpacingText_FormattedWithoutTrailingZeros()
        {
            // 8000.00 → "8000" (0.## format strips trailing zeros and decimal)
            var row = new SpacingIntervalRow("1 → 2", 8000.00);
            Assert.DoesNotContain(".", row.SpacingText);
        }

        [Fact]
        public void Constructor_NullLabel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SpacingIntervalRow(null!, 8000));
        }

        // ── IsValid ──────────────────────────────────────────────────────────

        [Fact]
        public void IsValid_True_WhenSpacingTextIsPositiveNumber()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000);
            Assert.True(row.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenSpacingTextIsZero()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "0" };
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenSpacingTextIsNegative()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "-500" };
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenSpacingTextIsEmpty()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "" };
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenSpacingTextIsNonNumeric()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "abc" };
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenSpacingTextIsWhitespaceOnly()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "   " };
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_True_WhenSpacingIsDecimal()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "6500.5" };
            Assert.True(row.IsValid);
        }

        [Fact]
        public void IsValid_True_WhenSpacingIsVerySmallPositive()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "0.001" };
            Assert.True(row.IsValid);
        }

        // ── ValueMm ──────────────────────────────────────────────────────────

        [Fact]
        public void ValueMm_ReturnsParsedValue_WhenValid()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000);
            Assert.Equal(8000.0, row.ValueMm);
        }

        [Fact]
        public void ValueMm_ReturnsZero_WhenInvalid()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "notANumber" };
            Assert.Equal(0.0, row.ValueMm);
        }

        [Fact]
        public void ValueMm_ReturnsCorrectDecimalValue()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "6500.75" };
            Assert.Equal(6500.75, row.ValueMm);
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        [Fact]
        public void SpacingText_Setter_RaisesPropertyChanged_ForSpacingText()
        {
            var row         = new SpacingIntervalRow("1 → 2", 8000);
            var raisedNames = new System.Collections.Generic.List<string?>();
            row.PropertyChanged += (_, e) => raisedNames.Add(e.PropertyName);

            row.SpacingText = "5000";

            Assert.Contains("SpacingText", raisedNames);
        }

        [Fact]
        public void SpacingText_Setter_RaisesPropertyChanged_ForIsValid()
        {
            var row         = new SpacingIntervalRow("1 → 2", 8000);
            var raisedNames = new System.Collections.Generic.List<string?>();
            row.PropertyChanged += (_, e) => raisedNames.Add(e.PropertyName);

            row.SpacingText = "invalid";

            Assert.Contains("IsValid", raisedNames);
        }
    }
}
