// RoomDocumenter | Revit 2024 | net48
// API NOTES — Extensible Storage:
//   Schema.Lookup(Guid)            — Verified: revitapidocs.com/2024/
//   SchemaBuilder / Field<T>       — Verified: revitapidocs.com/2024/
//   Document.SetEntity             — Verified: revitapidocs.com/2024/
//   Document.GetEntity(schema)     — Verified: revitapidocs.com/2024/
//   ElementId.Value (Int64)        — Verified: revitapidocs.com/2024/ (never IntegerValue)
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using RoomDocumenter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RoomDocumenter.Services
{
    /// <summary>
    /// Reads and writes the <see cref="FinishMapping"/> to Extensible Storage on
    /// the <see cref="Document"/> root entity.
    ///
    /// <para>Transaction scope: the caller is responsible for wrapping
    /// <see cref="Save"/> inside an active Transaction.  <see cref="Load"/> is
    /// read-only and requires no transaction.</para>
    /// </summary>
    public class FinishMappingService
    {
        // ── Schema identity ───────────────────────────────────────────────
        private static readonly Guid SchemaGuid =
            new Guid("5D8A2F1C-3B4E-7A9D-C1F2-0E6B4D8C2A5F");

        private const string SchemaName = "RoomDocumenterFinishMapping";
        private const string FieldName  = "FinishMappingJson";
        private const string VendorId   = "PDG";

        // ── Room finish parameter BIPs ─────────────────────────────────────
        private static readonly BuiltInParameter[] FinishParams =
        {
            BuiltInParameter.ROOM_FINISH_FLOOR,
            BuiltInParameter.ROOM_FINISH_CEILING,
            BuiltInParameter.ROOM_FINISH_WALL
        };

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the stored <see cref="FinishMapping"/> from the document.
        /// Returns <c>null</c> if no mapping has been saved yet.
        /// </summary>
        /// <param name="doc">Active Revit project document.</param>
        public FinishMapping? Load(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var schema = Schema.Lookup(SchemaGuid);
            if (schema == null) return null;

            var entity = doc.ProjectInformation.GetEntity(schema);
            if (!entity.IsValid()) return null;

            var json = entity.Get<string>(FieldName);
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<FinishMapping>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Persists <paramref name="mapping"/> to Extensible Storage on the document.
        /// Must be called inside an active Revit Transaction.
        /// </summary>
        /// <param name="doc">Active Revit project document.</param>
        /// <param name="mapping">The mapping to store.</param>
        public void Save(Document doc, FinishMapping mapping)
        {
            if (doc == null)     throw new ArgumentNullException(nameof(doc));
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            var schema = GetOrCreateSchema();
            var entity = new Entity(schema);
            entity.Set(FieldName, JsonSerializer.Serialize(mapping));
            doc.ProjectInformation.SetEntity(entity);
        }

        /// <summary>
        /// Collects all unique, non-empty finish strings currently present in the
        /// document across all placed rooms, grouped by parameter category.
        /// </summary>
        /// <param name="doc">Active Revit project document.</param>
        /// <returns>
        /// Dictionary keyed by the finish category label ("Floor", "Ceiling",
        /// "Wall", "Base"), each containing a sorted list of unique finish strings.
        /// </returns>
        public Dictionary<string, List<string>> CollectUniqueFinishStrings(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var result = new Dictionary<string, List<string>>
            {
                { "Floor",   new List<string>() },
                { "Ceiling", new List<string>() },
                { "Wall",    new List<string>() },
                { "Base",    new List<string>() }
            };

            var rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Autodesk.Revit.DB.Architecture.Room>()
                .Where(r => r != null && r.Area > 0);

            var sets = new (BuiltInParameter Bip, string Key)[]
            {
                (BuiltInParameter.ROOM_FINISH_FLOOR,   "Floor"),
                (BuiltInParameter.ROOM_FINISH_CEILING, "Ceiling"),
                (BuiltInParameter.ROOM_FINISH_WALL,    "Wall")
            };

            foreach (var room in rooms)
            {
                foreach (var (bip, key) in sets)
                {
                    var param = room.get_Parameter(bip);
                    if (param == null) continue;

                    var val = param.AsString();
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    if (!result[key].Contains(val))
                        result[key].Add(val);
                }

                var baseParam = room.LookupParameter("Base Finish");
                if (baseParam != null)
                {
                    var val = baseParam.AsString();
                    if (!string.IsNullOrWhiteSpace(val) && !result["Base"].Contains(val))
                        result["Base"].Add(val);
                }
            }

            foreach (var list in result.Values)
                list.Sort(StringComparer.OrdinalIgnoreCase);

            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private static Schema GetOrCreateSchema()
        {
            var existing = Schema.Lookup(SchemaGuid);
            if (existing != null) return existing;

            var builder = new SchemaBuilder(SchemaGuid);
            builder.SetSchemaName(SchemaName);
            builder.SetVendorId(VendorId);
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField(FieldName, typeof(string));
            return builder.Finish();
        }
    }
}
