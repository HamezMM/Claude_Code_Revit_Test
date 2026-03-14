// GridBuilderAddin | Revit 2024 | net48
// Pure C# model — no Revit API dependency.

namespace GridBuilderAddin.Models
{
    /// <summary>
    /// Lightweight representation of a Revit family type (floor type, wall type, roof type,
    /// or structural column symbol) for display in the Structure Builder drop-down pickers.
    /// </summary>
    public class FamilyTypeItem
    {
        /// <summary>Revit internal element ID of the type or family symbol.</summary>
        public long Id { get; set; }

        /// <summary>Type name (e.g. "Generic - 200mm", "W14x82").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Parent family name for loadable families (e.g. "W-Wide Flange").
        /// Empty string for system families (floors, walls, roofs) where the family is implicit.
        /// </summary>
        public string FamilyName { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable label shown in the UI.
        /// Formats as "FamilyName : TypeName" when a family name is present, otherwise just the type name.
        /// </summary>
        public string DisplayName =>
            string.IsNullOrEmpty(FamilyName) ? Name : $"{FamilyName} : {Name}";
    }
}
