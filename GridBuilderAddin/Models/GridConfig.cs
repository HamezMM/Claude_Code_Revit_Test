// GridBuilderAddin | Revit 2024 | net48
// Pure C# data model — no Revit API dependency.
using System.Collections.Generic;

namespace GridBuilderAddin.Models
{
    /// <summary>
    /// Immutable data transfer object carrying all user-configured grid parameters
    /// from the WPF dialog to <see cref="GridBuilderAddin.Services.GridBuilderService"/>.
    /// All spacing values are in <b>millimetres</b>; the service converts to internal feet.
    /// </summary>
    public class GridConfig
    {
        /// <summary>
        /// Number of X-axis (numerical) grid lines to create.
        /// Must be ≥ 2 (enforced by the ViewModel before building this object).
        /// </summary>
        public int XCount { get; set; }

        /// <summary>
        /// Number of Y-axis (alphabetical) grid lines to create.
        /// Must be ≥ 2 (enforced by the ViewModel before building this object).
        /// </summary>
        public int YCount { get; set; }

        /// <summary>
        /// Default spacing applied to all intervals at startup, in millimetres.
        /// Stored here for reference; the actual per-interval values are in
        /// <see cref="XSpacingsMm"/> and <see cref="YSpacingsMm"/>.
        /// </summary>
        public double DefaultSpacingMm { get; set; }

        /// <summary>
        /// Per-interval X spacings in millimetres.
        /// Length = <see cref="XCount"/> − 1.
        /// <c>XSpacingsMm[0]</c> is the distance from Grid "1" to Grid "2", etc.
        /// </summary>
        public List<double> XSpacingsMm { get; set; } = new List<double>();

        /// <summary>
        /// Per-interval Y spacings in millimetres.
        /// Length = <see cref="YCount"/> − 1.
        /// <c>YSpacingsMm[0]</c> is the distance from Grid "A" to Grid "B", etc.
        /// Y grids progress in the <b>negative Y direction</b> from origin.
        /// </summary>
        public List<double> YSpacingsMm { get; set; } = new List<double>();
    }
}
