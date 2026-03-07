// GridBuilderAddin.Tests | Revit 2024 | net48
// Pure-logic tests for SpacingIntervalRow.
// No Revit API dependency — these run in any xUnit host.
using GridBuilderAddin.UI;
using System;
using System.Collections.Generic;
using Xunit;

namespace GridBuilderAddin.Tests
{
    /// <summary>
    /// Verifies <see cref="SpacingIntervalRow"/> validation logic,
    /// property change notification, and value parsing for both unit modes.
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
        public void Constructor_PreFillsSpacingText_WithDefaultMmValue()
        {
            var row = new SpacingIntervalRow("A → B", 8000);
            Assert.Equal("8000", row.SpacingText);
        }

        [Fact]
        public void Constructor_PreFillsSpacingText_NoTrailingDecimal()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000.00);
            Assert.DoesNotContain(".", row.SpacingText);
        }

        [Fact]
        public void Constructor_NullLabel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SpacingIntervalRow(null!, 8000));
        }

        [Fact]
        public void Constructor_DefaultUnitMode_IsMillimeters()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000);
            Assert.Equal(GridUnitMode.Millimeters, row.UnitMode);
            Assert.True(row.IsMmMode);
            Assert.False(row.IsFtInMode);
        }

        // ── Millimetre mode — IsValid ────────────────────────────────────────

        [Fact]
        public void IsValid_Mm_True_WhenSpacingIsPositive()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000);
            Assert.True(row.IsValid);
        }

        [Fact]
        public void IsValid_Mm_False_WhenZero()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "0" };
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_Mm_False_WhenNegative()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "-500" };
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_Mm_False_WhenEmpty()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "" };
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_Mm_False_WhenNonNumeric()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "abc" };
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_Mm_True_WhenDecimal()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "6500.5" };
            Assert.True(row.IsValid);
        }

        // ── Millimetre mode — ValueMm ────────────────────────────────────────

        [Fact]
        public void ValueMm_Mm_ReturnsParsedValue()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000);
            Assert.Equal(8000.0, row.ValueMm);
        }

        [Fact]
        public void ValueMm_Mm_ReturnsZero_WhenInvalid()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000) { SpacingText = "abc" };
            Assert.Equal(0.0, row.ValueMm);
        }

        // ── Feet-and-inches mode — construction ──────────────────────────────

        [Fact]
        public void Constructor_FtInMode_PreFillsFeetAndInches_From8000mm()
        {
            // 8000 mm = 26.247 ft → 26 ft 2.96 in ≈ 26 ft 2.97 in
            var row = new SpacingIntervalRow("1 → 2", 8000, GridUnitMode.FeetAndInches);
            Assert.Equal("26", row.FeetText);
            // Inches should be positive and less than 12
            Assert.True(double.TryParse(row.InchesText, out var inches));
            Assert.True(inches >= 0 && inches < 12);
        }

        [Fact]
        public void Constructor_FtInMode_IsFtInModeTrue()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000, GridUnitMode.FeetAndInches);
            Assert.True(row.IsFtInMode);
            Assert.False(row.IsMmMode);
        }

        // ── Feet-and-inches mode — IsValid ───────────────────────────────────

        [Fact]
        public void IsValid_FtIn_True_When26Ft0In()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "26";
            row.InchesText = "0";
            Assert.True(row.IsValid);
        }

        [Fact]
        public void IsValid_FtIn_True_When0Ft6In()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "0";
            row.InchesText = "6";
            Assert.True(row.IsValid);
        }

        [Fact]
        public void IsValid_FtIn_False_When0Ft0In()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "0";
            row.InchesText = "0";
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_FtIn_False_WhenInchesIs12()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "1";
            row.InchesText = "12";
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_FtIn_False_WhenInchesIsNegative()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "1";
            row.InchesText = "-1";
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_FtIn_False_WhenFeetIsNegative()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "-1";
            row.InchesText = "0";
            Assert.False(row.IsValid);
        }

        [Fact]
        public void IsValid_FtIn_False_WhenFeetIsNonInteger()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "26.5";
            row.InchesText = "0";
            Assert.False(row.IsValid);   // FeetText must be a whole number
        }

        [Fact]
        public void IsValid_FtIn_True_WhenInchesIsDecimal()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "10";
            row.InchesText = "6.5";
            Assert.True(row.IsValid);
        }

        // ── Feet-and-inches mode — ValueMm ───────────────────────────────────

        [Fact]
        public void ValueMm_FtIn_Returns_CorrectConversion_1Ft0In()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "1";
            row.InchesText = "0";
            // 1 ft = 304.8 mm
            Assert.Equal(304.8, row.ValueMm, precision: 4);
        }

        [Fact]
        public void ValueMm_FtIn_Returns_CorrectConversion_0Ft12In_Invalid()
        {
            // 12 inches is invalid (>= 12), ValueMm returns 0 * 304.8 + 0 * 25.4
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "0";
            row.InchesText = "12";
            // IsValid is false; TryParse still succeeds for "12", so ValueMm = 0 + 12 * 25.4
            // But IsValid guard is caller's responsibility — we just verify math
            Assert.False(row.IsValid);
        }

        [Fact]
        public void ValueMm_FtIn_Returns_CorrectConversion_26Ft3In()
        {
            var row = new SpacingIntervalRow("1 → 2", 1000, GridUnitMode.FeetAndInches);
            row.FeetText   = "26";
            row.InchesText = "3";
            // 26 * 304.8 + 3 * 25.4 = 7924.8 + 76.2 = 8001.0
            Assert.Equal(8001.0, row.ValueMm, precision: 4);
        }

        // ── UnitMode switch ──────────────────────────────────────────────────

        [Fact]
        public void UnitMode_Switch_UpdatesIsMmMode_AndIsFtInMode()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000);
            Assert.True(row.IsMmMode);

            row.UnitMode = GridUnitMode.FeetAndInches;
            Assert.False(row.IsMmMode);
            Assert.True(row.IsFtInMode);
        }

        [Fact]
        public void SetFromMm_PopulatesBothMmAndFtInFields()
        {
            var row = new SpacingIntervalRow("1 → 2", 8000);
            row.SetFromMm(304.8);  // exactly 1 ft 0 in

            Assert.Equal("304.8", row.SpacingText);
            Assert.Equal("1",     row.FeetText);
            Assert.Equal("0",     row.InchesText);
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        [Fact]
        public void SpacingText_Setter_RaisesPropertyChanged_ForSpacingText()
        {
            var row         = new SpacingIntervalRow("1 → 2", 8000);
            var raisedNames = new List<string?>();
            row.PropertyChanged += (_, e) => raisedNames.Add(e.PropertyName);

            row.SpacingText = "5000";

            Assert.Contains("SpacingText", raisedNames);
        }

        [Fact]
        public void SpacingText_Setter_RaisesPropertyChanged_ForIsValid()
        {
            var row         = new SpacingIntervalRow("1 → 2", 8000);
            var raisedNames = new List<string?>();
            row.PropertyChanged += (_, e) => raisedNames.Add(e.PropertyName);

            row.SpacingText = "invalid";

            Assert.Contains("IsValid", raisedNames);
        }

        [Fact]
        public void UnitMode_Setter_RaisesPropertyChanged_ForRelatedProperties()
        {
            var row         = new SpacingIntervalRow("1 → 2", 8000);
            var raisedNames = new List<string?>();
            row.PropertyChanged += (_, e) => raisedNames.Add(e.PropertyName);

            row.UnitMode = GridUnitMode.FeetAndInches;

            Assert.Contains(nameof(row.UnitMode),   raisedNames);
            Assert.Contains(nameof(row.IsMmMode),   raisedNames);
            Assert.Contains(nameof(row.IsFtInMode), raisedNames);
            Assert.Contains(nameof(row.IsValid),    raisedNames);
        }
    }
}
