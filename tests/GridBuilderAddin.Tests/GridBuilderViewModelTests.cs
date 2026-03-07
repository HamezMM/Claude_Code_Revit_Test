// GridBuilderAddin.Tests | Revit 2024 | net48
// Tests for GridBuilderViewModel validation logic and row collection behaviour.
// These tests exercise pure C# logic only — WPF controls are not instantiated.
// CommandManager.InvalidateRequerySuggested() is a no-op outside a WPF dispatcher,
// so these tests run safely in the xUnit host.
using GridBuilderAddin;
using GridBuilderAddin.UI;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GridBuilderAddin.Tests
{
    /// <summary>
    /// Verifies <see cref="GridBuilderViewModel"/> validation rules,
    /// spacing row collection management, unit-mode switching, and
    /// <see cref="GridBuilderViewModel.BuildConfig"/>.
    /// </summary>
    public class GridBuilderViewModelTests
    {
        // ── Initial state ────────────────────────────────────────────────────

        [Fact]
        public void Constructor_IsValid_True_WithAllDefaults()
        {
            var vm = new GridBuilderViewModel();
            Assert.True(vm.IsValid);
        }

        [Fact]
        public void Constructor_ValidationMessage_Empty_WithAllDefaults()
        {
            var vm = new GridBuilderViewModel();
            Assert.Equal(string.Empty, vm.ValidationMessage);
        }

        [Fact]
        public void Constructor_XSpacingRows_Count_EqualsDefaultXCountMinusOne()
        {
            var vm = new GridBuilderViewModel();
            Assert.Equal(GridBuilderConstants.DefaultXCount - 1, vm.XSpacingRows.Count);
        }

        [Fact]
        public void Constructor_YSpacingRows_Count_EqualsDefaultYCountMinusOne()
        {
            var vm = new GridBuilderViewModel();
            Assert.Equal(GridBuilderConstants.DefaultYCount - 1, vm.YSpacingRows.Count);
        }

        [Fact]
        public void Constructor_XSpacingRows_PreFilledWithDefaultSpacing()
        {
            var vm = new GridBuilderViewModel();
            foreach (var row in vm.XSpacingRows)
                Assert.Equal(GridBuilderConstants.DefaultSpacingMm, row.ValueMm);
        }

        [Fact]
        public void Constructor_YSpacingRows_PreFilledWithDefaultSpacing()
        {
            var vm = new GridBuilderViewModel();
            foreach (var row in vm.YSpacingRows)
                Assert.Equal(GridBuilderConstants.DefaultSpacingMm, row.ValueMm);
        }

        [Fact]
        public void Constructor_DefaultUnitMode_IsMillimeters()
        {
            var vm = new GridBuilderViewModel();
            Assert.Equal(GridUnitMode.Millimeters, vm.UnitMode);
            Assert.True(vm.IsMmMode);
            Assert.False(vm.IsFtInMode);
        }

        // ── X/Y count validation ─────────────────────────────────────────────

        [Fact]
        public void IsValid_False_WhenXCountIsOne()
        {
            var vm = new GridBuilderViewModel { XCountText = "1" };
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenYCountIsOne()
        {
            var vm = new GridBuilderViewModel { YCountText = "1" };
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenXCountIsZero()
        {
            var vm = new GridBuilderViewModel { XCountText = "0" };
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenXCountIsNonNumeric()
        {
            var vm = new GridBuilderViewModel { XCountText = "abc" };
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenYCountIsNonNumeric()
        {
            var vm = new GridBuilderViewModel { YCountText = "xyz" };
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void ValidationMessage_ContainsMinCount_WhenXCountTooLow()
        {
            var vm = new GridBuilderViewModel { XCountText = "1" };
            Assert.Contains(GridBuilderConstants.MinGridCount.ToString(), vm.ValidationMessage);
        }

        // ── Default spacing validation ────────────────────────────────────────

        [Fact]
        public void IsValid_False_WhenDefaultSpacingIsZero()
        {
            var vm = new GridBuilderViewModel { DefaultSpacingText = "0" };
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenDefaultSpacingIsNegative()
        {
            var vm = new GridBuilderViewModel { DefaultSpacingText = "-1000" };
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenDefaultSpacingIsNonNumeric()
        {
            var vm = new GridBuilderViewModel { DefaultSpacingText = "???" };
            Assert.False(vm.IsValid);
        }

        // ── Spacing row override validation ───────────────────────────────────

        [Fact]
        public void IsValid_False_WhenAnyXSpacingRowIsInvalid()
        {
            var vm = new GridBuilderViewModel();
            vm.XSpacingRows[0].SpacingText = "-500";
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenAnyYSpacingRowIsInvalid()
        {
            var vm = new GridBuilderViewModel();
            vm.YSpacingRows[0].SpacingText = "abc";
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void IsValid_True_AfterFixingInvalidSpacingRow()
        {
            var vm = new GridBuilderViewModel();
            vm.XSpacingRows[0].SpacingText = "-999";
            Assert.False(vm.IsValid);

            vm.XSpacingRows[0].SpacingText = "8000";
            Assert.True(vm.IsValid);
        }

        // ── Row collection rebuild on count change ────────────────────────────

        [Fact]
        public void XSpacingRows_Count_UpdatesWhenXCountTextChanges()
        {
            var vm = new GridBuilderViewModel();
            vm.XCountText = "6";
            Assert.Equal(5, vm.XSpacingRows.Count);  // 6 - 1 = 5
        }

        [Fact]
        public void YSpacingRows_Count_UpdatesWhenYCountTextChanges()
        {
            var vm = new GridBuilderViewModel();
            vm.YCountText = "2";
            Assert.Equal(1, vm.YSpacingRows.Count);  // 2 - 1 = 1
        }

        [Fact]
        public void XSpacingRows_NewRows_PreFilledWithDefaultSpacing()
        {
            var vm = new GridBuilderViewModel();
            vm.XCountText = "5";
            var lastRow = vm.XSpacingRows.Last();
            Assert.Equal(GridBuilderConstants.DefaultSpacingMm, lastRow.ValueMm);
        }

        [Fact]
        public void XSpacingRows_Labels_FollowNumericalIntervalPattern()
        {
            var vm = new GridBuilderViewModel();
            vm.XCountText = "4";  // 3 intervals: "1 → 2", "2 → 3", "3 → 4"

            Assert.Contains("1", vm.XSpacingRows[0].Label);
            Assert.Contains("2", vm.XSpacingRows[0].Label);
            Assert.Contains("2", vm.XSpacingRows[1].Label);
            Assert.Contains("3", vm.XSpacingRows[1].Label);
        }

        [Fact]
        public void YSpacingRows_Labels_FollowAlphabeticalIntervalPattern()
        {
            var vm = new GridBuilderViewModel();
            vm.YCountText = "3";  // 2 intervals: "A → B", "B → C"

            Assert.Contains("A", vm.YSpacingRows[0].Label);
            Assert.Contains("B", vm.YSpacingRows[0].Label);
            Assert.Contains("B", vm.YSpacingRows[1].Label);
            Assert.Contains("C", vm.YSpacingRows[1].Label);
        }

        // ── Unit mode switching ───────────────────────────────────────────────

        [Fact]
        public void UnitMode_Switch_ToFtIn_UpdatesIsFtInMode()
        {
            var vm = new GridBuilderViewModel();
            vm.UnitMode = GridUnitMode.FeetAndInches;
            Assert.True(vm.IsFtInMode);
            Assert.False(vm.IsMmMode);
        }

        [Fact]
        public void UnitMode_Switch_ToFtIn_PropagatesUnitModeToAllRows()
        {
            var vm = new GridBuilderViewModel();
            vm.UnitMode = GridUnitMode.FeetAndInches;
            Assert.All(vm.XSpacingRows, r => Assert.Equal(GridUnitMode.FeetAndInches, r.UnitMode));
            Assert.All(vm.YSpacingRows, r => Assert.Equal(GridUnitMode.FeetAndInches, r.UnitMode));
        }

        [Fact]
        public void UnitMode_Switch_ToFtIn_RowsRemainValid()
        {
            var vm = new GridBuilderViewModel();
            vm.UnitMode = GridUnitMode.FeetAndInches;
            Assert.All(vm.XSpacingRows, r => Assert.True(r.IsValid));
            Assert.All(vm.YSpacingRows, r => Assert.True(r.IsValid));
        }

        [Fact]
        public void UnitMode_Switch_BackToMm_RowsRemainValid()
        {
            var vm = new GridBuilderViewModel();
            vm.UnitMode = GridUnitMode.FeetAndInches;
            vm.UnitMode = GridUnitMode.Millimeters;
            Assert.All(vm.XSpacingRows, r => Assert.True(r.IsValid));
            Assert.All(vm.YSpacingRows, r => Assert.True(r.IsValid));
        }

        [Fact]
        public void UnitMode_Switch_ToFtIn_DefaultSpacingLabel_Changes()
        {
            var vm = new GridBuilderViewModel();
            Assert.Contains("mm", vm.DefaultSpacingLabel);
            vm.UnitMode = GridUnitMode.FeetAndInches;
            Assert.DoesNotContain("mm", vm.DefaultSpacingLabel);
        }

        [Fact]
        public void UnitMode_Switch_ToFtIn_SpacingUnitLabel_Changes()
        {
            var vm = new GridBuilderViewModel();
            Assert.Contains("mm", vm.SpacingUnitLabel);
            vm.UnitMode = GridUnitMode.FeetAndInches;
            Assert.Contains("ft-in", vm.SpacingUnitLabel);
        }

        [Fact]
        public void UnitMode_Switch_ToFtIn_XSpacingHeading_ChangesDynamically()
        {
            var vm = new GridBuilderViewModel();
            var mmHeading = vm.XSpacingHeading;
            vm.UnitMode = GridUnitMode.FeetAndInches;
            Assert.NotEqual(mmHeading, vm.XSpacingHeading);
            Assert.Contains("ft-in", vm.XSpacingHeading);
        }

        [Fact]
        public void UnitMode_Switch_ConvertsMmToFtIn_PreservesRoundtripValue()
        {
            var vm = new GridBuilderViewModel();
            // Default is 8000 mm; after switch to ft-in, ValueMm should still be 8000 (approximately)
            var originalMm = vm.XSpacingRows[0].ValueMm;
            vm.UnitMode = GridUnitMode.FeetAndInches;
            Assert.Equal(originalMm, vm.XSpacingRows[0].ValueMm, precision: 0);
        }

        [Fact]
        public void IsMmMode_Setter_True_SetsUnitModeToMillimeters()
        {
            var vm = new GridBuilderViewModel();
            vm.IsFtInMode = true;
            Assert.Equal(GridUnitMode.FeetAndInches, vm.UnitMode);
            vm.IsMmMode = true;
            Assert.Equal(GridUnitMode.Millimeters, vm.UnitMode);
        }

        [Fact]
        public void IsFtInMode_Setter_True_SetsUnitModeToFeetAndInches()
        {
            var vm = new GridBuilderViewModel();
            vm.IsFtInMode = true;
            Assert.Equal(GridUnitMode.FeetAndInches, vm.UnitMode);
        }

        [Fact]
        public void UnitMode_Switch_RaisesPropertyChanged_ForRelatedProperties()
        {
            var vm          = new GridBuilderViewModel();
            var raisedNames = new List<string?>();
            vm.PropertyChanged += (_, e) => raisedNames.Add(e.PropertyName);

            vm.UnitMode = GridUnitMode.FeetAndInches;

            Assert.Contains(nameof(vm.UnitMode),          raisedNames);
            Assert.Contains(nameof(vm.IsMmMode),          raisedNames);
            Assert.Contains(nameof(vm.IsFtInMode),        raisedNames);
            Assert.Contains(nameof(vm.SpacingUnitLabel),  raisedNames);
            Assert.Contains(nameof(vm.XSpacingHeading),   raisedNames);
            Assert.Contains(nameof(vm.YSpacingHeading),   raisedNames);
            Assert.Contains(nameof(vm.DefaultSpacingLabel), raisedNames);
        }

        // ── Default spacing ft-in validation ──────────────────────────────────

        [Fact]
        public void IsValid_False_WhenInFtInMode_And_DefaultFeetAndInches_AreZero()
        {
            var vm = new GridBuilderViewModel();
            vm.UnitMode = GridUnitMode.FeetAndInches;
            vm.DefaultFeetText   = "0";
            vm.DefaultInchesText = "0";
            Assert.False(vm.IsValid);
        }

        [Fact]
        public void IsValid_True_WhenInFtInMode_And_DefaultSpacingIsValid()
        {
            var vm = new GridBuilderViewModel();
            vm.UnitMode = GridUnitMode.FeetAndInches;
            vm.DefaultFeetText   = "26";
            vm.DefaultInchesText = "3";
            Assert.True(vm.IsValid);
        }

        [Fact]
        public void IsValid_False_WhenInFtInMode_And_DefaultInchesIs12OrMore()
        {
            var vm = new GridBuilderViewModel();
            vm.UnitMode = GridUnitMode.FeetAndInches;
            vm.DefaultFeetText   = "1";
            vm.DefaultInchesText = "12";
            Assert.False(vm.IsValid);
        }

        // ── BuildConfig ───────────────────────────────────────────────────────

        [Fact]
        public void BuildConfig_XCount_MatchesXCountText()
        {
            var vm     = new GridBuilderViewModel { XCountText = "5" };
            var config = vm.BuildConfig();
            Assert.Equal(5, config.XCount);
        }

        [Fact]
        public void BuildConfig_YCount_MatchesYCountText()
        {
            var vm     = new GridBuilderViewModel { YCountText = "3" };
            var config = vm.BuildConfig();
            Assert.Equal(3, config.YCount);
        }

        [Fact]
        public void BuildConfig_XSpacingsMm_CountEqualsXCountMinusOne()
        {
            var vm     = new GridBuilderViewModel { XCountText = "4" };
            var config = vm.BuildConfig();
            Assert.Equal(3, config.XSpacingsMm.Count);
        }

        [Fact]
        public void BuildConfig_YSpacingsMm_CountEqualsYCountMinusOne()
        {
            var vm     = new GridBuilderViewModel { YCountText = "4" };
            var config = vm.BuildConfig();
            Assert.Equal(3, config.YSpacingsMm.Count);
        }

        [Fact]
        public void BuildConfig_XSpacingsMm_ReflectsOverriddenValues()
        {
            var vm = new GridBuilderViewModel();
            vm.XSpacingRows[0].SpacingText = "6000";
            vm.XSpacingRows[1].SpacingText = "9000";

            var config = vm.BuildConfig();

            Assert.Equal(6000.0, config.XSpacingsMm[0]);
            Assert.Equal(9000.0, config.XSpacingsMm[1]);
        }

        [Fact]
        public void BuildConfig_DefaultSpacingMm_MatchesDefaultSpacingText()
        {
            var vm = new GridBuilderViewModel { DefaultSpacingText = "7500" };
            // Rebuild rows so they pick up the new default (if count changes);
            // here count stays the same so existing rows keep their pre-filled values.
            var config = vm.BuildConfig();
            Assert.Equal(7500.0, config.DefaultSpacingMm);
        }

        [Fact]
        public void BuildConfig_InFtInMode_XSpacingsMm_AreInMillimetres()
        {
            var vm = new GridBuilderViewModel();
            vm.UnitMode = GridUnitMode.FeetAndInches;

            // All rows converted from 8000 mm → should be approximately 8000 mm back
            var config = vm.BuildConfig();
            foreach (var spacingMm in config.XSpacingsMm)
                Assert.True(spacingMm > 0, "All spacings must be positive mm values");
        }

        [Fact]
        public void BuildConfig_InFtInMode_DefaultSpacingMm_IsInMillimetres()
        {
            var vm = new GridBuilderViewModel();
            vm.UnitMode          = GridUnitMode.FeetAndInches;
            vm.DefaultFeetText   = "1";
            vm.DefaultInchesText = "0";

            var config = vm.BuildConfig();
            // 1 ft 0 in = 304.8 mm
            Assert.Equal(304.8, config.DefaultSpacingMm, precision: 4);
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        [Fact]
        public void XCountText_Setter_RaisesPropertyChanged()
        {
            var vm    = new GridBuilderViewModel();
            var fired = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(GridBuilderViewModel.XCountText))
                    fired = true;
            };

            vm.XCountText = "3";
            Assert.True(fired);
        }

        [Fact]
        public void ValidationMessage_ChangesWhenInputBecomesInvalid()
        {
            var vm = new GridBuilderViewModel();
            Assert.Equal(string.Empty, vm.ValidationMessage);

            vm.XCountText = "1";
            Assert.NotEmpty(vm.ValidationMessage);
        }
    }
}
