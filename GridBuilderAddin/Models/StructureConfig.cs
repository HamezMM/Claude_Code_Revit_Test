// GridBuilderAddin | Revit 2024 | net48
// Pure C# data model — no Revit API dependency.

namespace GridBuilderAddin.Models
{
    /// <summary>
    /// Data transfer object carrying all user-configured structure parameters from the
    /// Structure Builder dialog to <see cref="GridBuilderAddin.Services.StructureBuilderService"/>.
    /// All offset and spacing values are in <b>millimetres</b>; the service converts to internal feet.
    /// Level and type references use Revit internal element IDs (<see cref="long"/>).
    /// </summary>
    public class StructureConfig
    {
        // ── Floor ─────────────────────────────────────────────────────────────

        /// <summary>Element ID of the selected <c>FloorType</c>.</summary>
        public long FloorTypeId { get; set; }

        /// <summary>Element ID of the level the floor is hosted on.</summary>
        public long FloorLevelId { get; set; }

        /// <summary>
        /// Vertical offset from the host level to the floor surface, in millimetres.
        /// Positive = above level; negative = below level.
        /// </summary>
        public double FloorLevelOffsetMm { get; set; }

        // ── Roof ──────────────────────────────────────────────────────────────

        /// <summary>Element ID of the selected roof type (e.g. a <c>RoofType</c>).</summary>
        public long RoofTypeId { get; set; }

        /// <summary>Element ID of the level the roof is hosted on.</summary>
        public long RoofLevelId { get; set; }

        /// <summary>
        /// Vertical offset from the host level to the roof base, in millimetres.
        /// Positive = above level; negative = below level.
        /// </summary>
        public double RoofLevelOffsetMm { get; set; }

        // ── Exterior walls ────────────────────────────────────────────────────

        /// <summary>Element ID of the selected <c>WallType</c>.</summary>
        public long WallTypeId { get; set; }

        /// <summary>
        /// Distance from the perimeter grid line to the exterior face of the wall, in millimetres.
        /// Must be ≥ 0. Zero means the exterior face coincides with the grid line.
        /// </summary>
        public double WallExteriorOffsetMm { get; set; }

        /// <summary>Element ID of the bottom constraint level for exterior walls.</summary>
        public long WallBottomLevelId { get; set; }

        /// <summary>Offset from the bottom level in millimetres (positive or negative).</summary>
        public double WallBottomOffsetMm { get; set; }

        /// <summary>Element ID of the top constraint level for exterior walls.</summary>
        public long WallTopLevelId { get; set; }

        /// <summary>Offset from the top level in millimetres (positive or negative).</summary>
        public double WallTopOffsetMm { get; set; }

        // ── Columns — shared level constraints ────────────────────────────────

        /// <summary>Element ID of the base level for all structural columns.</summary>
        public long ColumnBottomLevelId { get; set; }

        /// <summary>Offset from the base level in millimetres (positive or negative).</summary>
        public double ColumnBottomOffsetMm { get; set; }

        /// <summary>Element ID of the top level for all structural columns.</summary>
        public long ColumnTopLevelId { get; set; }

        /// <summary>Offset from the top level in millimetres (positive or negative).</summary>
        public double ColumnTopOffsetMm { get; set; }

        // ── Perimeter columns ─────────────────────────────────────────────────

        /// <summary><c>true</c> to place columns at perimeter grid intersections.</summary>
        public bool HasPerimeterColumns { get; set; }

        /// <summary>Element ID of the <c>FamilySymbol</c> used for perimeter columns.</summary>
        public long PerimeterColumnTypeId { get; set; }

        /// <summary>
        /// Distance the perimeter column centerline is offset inward from the perimeter grid line,
        /// in millimetres. Must be ≥ 0. Zero places the column centerline on the grid.
        /// </summary>
        public double PerimeterInteriorOffsetMm { get; set; }

        // ── Midpoint perimeter columns ────────────────────────────────────────

        /// <summary>
        /// <c>true</c> to place additional columns at the midpoint between consecutive
        /// perimeter grid intersections, along the perimeter grid line.
        /// </summary>
        public bool HasMidpointColumns { get; set; }

        /// <summary>
        /// Element ID of the <c>FamilySymbol</c> used for midpoint perimeter columns.
        /// Only relevant when <see cref="HasMidpointColumns"/> is <c>true</c>.
        /// </summary>
        public long MidpointColumnTypeId { get; set; }

        // ── Interior field columns ────────────────────────────────────────────

        /// <summary>
        /// <c>true</c> to place columns at all interior grid intersections
        /// (intersections not on any perimeter grid line).
        /// </summary>
        public bool HasInteriorColumns { get; set; }

        /// <summary>
        /// Element ID of the <c>FamilySymbol</c> used for interior columns.
        /// Only relevant when <see cref="HasInteriorColumns"/> is <c>true</c>.
        /// </summary>
        public long InteriorColumnTypeId { get; set; }
    }
}
