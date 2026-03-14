// RoomDocumenter | Revit 2024 | net48
using System.Collections.Generic;

namespace RoomDocumenter.Models
{
    /// <summary>
    /// Persisted finish-to-type mapping for all finish categories.
    /// Keys are raw finish parameter strings; values are Revit ElementId.Value (Int64).
    /// Wall mappings are read and stored for scheduling / future use but do not
    /// drive geometry creation in this version.
    /// Serialised to JSON and written to Extensible Storage on the Document.
    /// </summary>
    public class FinishMapping
    {
        /// <summary>Floor finish string → FloorType ElementId (Int64).</summary>
        public Dictionary<string, long> FloorMappings { get; set; } = new Dictionary<string, long>();

        /// <summary>Ceiling finish string → CeilingType ElementId (Int64).</summary>
        public Dictionary<string, long> CeilingMappings { get; set; } = new Dictionary<string, long>();

        /// <summary>
        /// Base finish string → WallSweepType ElementId (Int64).
        /// Drives baseboard WallSweep creation.
        /// </summary>
        public Dictionary<string, long> BaseboardMappings { get; set; } = new Dictionary<string, long>();

        /// <summary>
        /// Wall finish string → future use (no geometry created in this version).
        /// Retained for scheduling and downstream processing.
        /// </summary>
        public Dictionary<string, long> WallMappings { get; set; } = new Dictionary<string, long>();
    }
}
