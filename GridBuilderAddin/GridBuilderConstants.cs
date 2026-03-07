// GridBuilderAddin | Revit 2024 | net48
namespace GridBuilderAddin
{
    /// <summary>
    /// Static class holding all default values and named constants used
    /// throughout the Grid Builder addin. No magic numbers elsewhere.
    /// </summary>
    public static class GridBuilderConstants
    {
        // ── Grid count defaults ─────────────────────────────────────────────

        /// <summary>Default number of X-axis (numerical) grid lines.</summary>
        public const int DefaultXCount = 4;

        /// <summary>Default number of Y-axis (alphabetical) grid lines.</summary>
        public const int DefaultYCount = 4;

        /// <summary>Minimum allowed count for either axis (must have at least 2 grids for any spacing).</summary>
        public const int MinGridCount = 2;

        // ── Spacing defaults ────────────────────────────────────────────────

        /// <summary>Default grid spacing in millimetres applied to all intervals at startup.</summary>
        public const double DefaultSpacingMm = 8000.0;

        /// <summary>
        /// Default spacing in whole feet when the unit mode is Feet &amp; Inches.
        /// 8000 mm ≈ 26 ft 3 in.
        /// </summary>
        public const int DefaultFeet = 26;

        /// <summary>
        /// Default spacing in decimal inches when the unit mode is Feet &amp; Inches.
        /// Combined with <see cref="DefaultFeet"/> this gives ≈ 8000 mm.
        /// </summary>
        public const double DefaultInches = 3.0;

        // ── Grid extent ─────────────────────────────────────────────────────

        /// <summary>
        /// Extra length in millimetres added beyond the outermost grid on each end of every
        /// grid line, ensuring all grids are visible in plan views.
        /// </summary>
        public const double ExtentBeyondGridMm = 2000.0;

        // ── Preview canvas ──────────────────────────────────────────────────

        /// <summary>Width in pixels of the live schematic preview canvas.</summary>
        public const double PreviewCanvasWidth = 340.0;

        /// <summary>Height in pixels of the live schematic preview canvas.</summary>
        public const double PreviewCanvasHeight = 220.0;

        /// <summary>Margin in pixels from the canvas edge to the first/last grid line in the preview.</summary>
        public const double PreviewMarginPx = 28.0;

        /// <summary>Extra pixel padding added beyond outermost grid in the preview, representing the extent.</summary>
        public const double PreviewExtentPx = 14.0;

        // ── Transaction ─────────────────────────────────────────────────────

        /// <summary>Name of the Revit transaction used when creating grid elements.</summary>
        public const string TransactionName = "Create Structural Grid";

        // ── Collision suffix ────────────────────────────────────────────────

        /// <summary>Suffix appended to grid names that collide with existing element names.</summary>
        public const string CollisionSuffix = "_new";
    }
}
