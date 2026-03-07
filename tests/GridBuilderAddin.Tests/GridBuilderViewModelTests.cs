// GridBuilderAddin.Tests | Revit 2024 | net48
// Tests for GridBuilderViewModel validation logic and row collection behaviour.
// These tests exercise pure C# logic only — WPF controls are not instantiated.
// CommandManager.InvalidateRequerySuggested() is a no-op outside a WPF dispatcher,
// so these tests run safely in the xUnit host.
using GridBuilderAddin;
using GridBuilderAddin.UI;
using System.Linq;
using Xunit;

namespace GridBuilderAddin.Tests
{
    /// <summary>
    /// Verifies <see cref="GridBuilderViewModel"/> validation rules,
    /// spacing row collection management, and <see cref="GridBuilderViewModel.BuildConfig"/>.
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
