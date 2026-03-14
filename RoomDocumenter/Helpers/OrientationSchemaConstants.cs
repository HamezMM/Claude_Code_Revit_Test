// RoomDocumenter | Revit 2024 | net48
//
// SHARED CONSTANTS — single source of truth for the ProjectNorth orientation
// Extensible Storage schema used by the PDG Elevation Builder addin.
//
// ┌─────────────────────────────────────────────────────────────────────┐
// │  IMPORTANT: SchemaGuid MUST match the Schema GUID registered by the │
// │  PDG Elevation Builder / ProjectParametersUtil in PDG_Shared_Methods.│
// │  If the two GUIDs diverge, RoomDocumenter will fail silently to read │
// │  the orientation and fall back to index-based direction labels.      │
// │                                                                      │
// │  To find the authoritative GUID, open ProjectParametersUtil.cs in   │
// │  the PDG_Shared_Methods project and locate the Schema.Lookup / new  │
// │  SchemaBuilder call.  Copy that GUID here.                          │
// └─────────────────────────────────────────────────────────────────────┘

using System;

namespace RoomDocumenter.Helpers
{
    /// <summary>
    /// Shared constants for the Extensible Storage schema that persists the
    /// building's <see cref="ProjectNorthOrientation"/> value.
    /// Both RoomDocumenter and the PDG Elevation Builder reference this file
    /// (or a linked copy of it) so the GUID and field name remain in sync.
    /// </summary>
    public static class OrientationSchemaConstants
    {
        /// <summary>
        /// GUID of the Extensible Storage schema that holds the project north
        /// orientation.  Must match the GUID used in PDG_Shared_Methods /
        /// ProjectParametersUtil when the schema was originally registered.
        /// </summary>
        /// <remarks>
        /// Default value shown here is a placeholder generated for this addin.
        /// Replace it with the GUID from ProjectParametersUtil.cs in the
        /// PDG_Shared_Methods shared library before deploying.
        /// </remarks>
        public static readonly Guid SchemaGuid =
            new Guid("3C4E7F1A-0B2D-4A8C-9E6F-5D1B8C2A4F3E");

        /// <summary>
        /// Name of the <c>int</c> field within the schema that stores the
        /// <see cref="ProjectNorthOrientation"/> cast to an integer.
        /// Must match the field name used in ProjectParametersUtil.cs.
        /// </summary>
        public const string FieldName = "ProjectNorthOrientationValue";
    }

    /// <summary>
    /// Cardinal orientation of project north relative to the screen "up" direction.
    /// Mirrors the enum defined in PDG_Elevation_Builder.UI.RoomSelectionWindow —
    /// kept here so RoomDocumenter has no compile-time dependency on that assembly.
    /// </summary>
    public enum ProjectNorthOrientation
    {
        /// <summary>Project north faces screen-up (default).</summary>
        North = 0,

        /// <summary>Project north faces screen-right.</summary>
        East = 1,

        /// <summary>Project north faces screen-down.</summary>
        South = 2,

        /// <summary>Project north faces screen-left.</summary>
        West = 3
    }
}
