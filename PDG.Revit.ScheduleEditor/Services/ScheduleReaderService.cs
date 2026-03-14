// PDG.Revit.ScheduleEditor | Revit 2024 | net48
// API NOTE: All Revit API calls below are marked where verification against
// https://www.revitapidocs.com/2024/ was attempted but returned HTTP 403.
// Lines carrying "// API unverified" must be confirmed before first compile.
using Autodesk.Revit.DB;
using PDG.Revit.ScheduleEditor.Models;
using PDG.Revit.ScheduleEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDG.Revit.ScheduleEditor.Services
{
    /// <summary>
    /// Reads all <c>ViewSchedule</c> instances from a Revit document and, for a
    /// selected schedule, builds the column metadata and row data structures consumed
    /// by the editor ViewModel.
    /// <para>
    /// All Revit API access is confined to this class — no ViewModel or View may
    /// call RevitAPI.dll directly.
    /// </para>
    /// </summary>
    public sealed class ScheduleReaderService
    {
        private readonly Document _doc;

        /// <summary>Initialises the service with the active project document.</summary>
        /// <param name="doc">The Revit project document.  Must not be a family document.</param>
        public ScheduleReaderService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }

        // ─────────────────────────────────────────────────────────────────
        // Schedule discovery
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns every <c>ViewSchedule</c> in the document as a lightweight
        /// <see cref="ScheduleListItem"/> list sorted alphabetically by name.
        /// </summary>
        public List<ScheduleListItem> GetAllSchedules()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .OrderBy(vs => vs.Name, StringComparer.OrdinalIgnoreCase)
                .Select(vs => new ScheduleListItem
                {
                    ViewScheduleId = vs.Id.Value,   // ElementId.Value — Revit 2024 (long)
                    Name           = vs.Name,
                    ScheduleType   = ClassifySchedule(vs)
                })
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────────
        // Schedule data reading
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads all column metadata and row data for the given schedule.
        /// </summary>
        /// <param name="scheduleId">
        /// The <c>ElementId.Value</c> captured from <see cref="ScheduleListItem.ViewScheduleId"/>.
        /// </param>
        /// <returns>
        /// A tuple containing the schedule name, ordered column models, and row models.
        /// Returns an empty row list when the schedule has no matching elements.
        /// </returns>
        public (string scheduleName, List<ScheduleColumnModel> columns, List<ScheduleRowModel> rows)
            ReadScheduleData(long scheduleId)
        {
            var schedule = _doc.GetElement(new ElementId(scheduleId)) as ViewSchedule;
            if (schedule == null)
                throw new ArgumentException($"No ViewSchedule found with id {scheduleId}.");

            var def     = schedule.Definition; // API unverified — check revitapidocs.com/2024/
            var columns = BuildColumns(def);
            var rows    = BuildRows(schedule, def, columns);

            return (schedule.Name, columns, rows);
        }

        // ─────────────────────────────────────────────────────────────────
        // Column building
        // ─────────────────────────────────────────────────────────────────

        private List<ScheduleColumnModel> BuildColumns(ScheduleDefinition def)
        {
            var columns    = new List<ScheduleColumnModel>();
            var fieldOrder = def.GetFieldOrder(); // API unverified — returns IList<ElementId>
            int colIdx     = 0;

            foreach (var fieldId in fieldOrder)
            {
                var field = _doc.GetElement(fieldId) as ScheduleField;
                if (field == null) continue;

                SchedulableField sf;
                try { sf = field.GetSchedulableField(); } // API unverified
                catch (Exception ex)
                {
                    Logger.Log($"[ScheduleReaderService] Skipping field {fieldId.Value}: {ex.Message}");
                    continue;
                }

                var paramId = sf.ParameterId; // API unverified — returns ElementId

                // Determine BuiltInParameter: positive Value means BIP.
                // Revit 2024 uses ElementId.Value (long).  BIP enum values fit in int.
                BuiltInParameter? bip = null;
                if (paramId != null && paramId != ElementId.InvalidElementId && paramId.Value >= 0)
                    bip = (BuiltInParameter)(int)paramId.Value;

                // Formula and Count fields can never be written.
                bool isReadOnly = field.IsReadOnly // API unverified
                    || field.FieldType == ScheduleFieldType.Formula  // API unverified
                    || field.FieldType == ScheduleFieldType.Count;   // API unverified

                // ElementId-typed parameters are read-only in v1 of this tool.
                // StorageType is not known until we read the first element — set Unknown.

                // Column heading: prefer the customised schedule heading; fall back to
                // the schedulable field name if heading is empty.
                string fieldName = string.Empty;
                try   { fieldName = field.ColumnHeading; }  // API unverified
                catch { /* swallow — some field types may not expose a heading */ }
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    try   { fieldName = sf.Name; }  // API unverified
                    catch { fieldName = $"Column{colIdx}"; }
                }

                columns.Add(new ScheduleColumnModel
                {
                    ColumnIndex  = colIdx++,
                    FieldName    = fieldName,
                    ParameterId  = paramId,
                    BuiltInParam = bip,
                    FieldType    = field.FieldType,
                    IsReadOnly   = isReadOnly,
                    StorageType  = StorageType.Unknown,
                    ForgeTypeId  = null
                });
            }

            return columns;
        }

        // ─────────────────────────────────────────────────────────────────
        // Row building — dispatch by schedule type
        // ─────────────────────────────────────────────────────────────────

        private List<ScheduleRowModel> BuildRows(
            ViewSchedule schedule,
            ScheduleDefinition def,
            List<ScheduleColumnModel> columns)
        {
            if (schedule.IsKeySchedule)        // API unverified
                return BuildKeyRows(def, columns);
            if (schedule.IsMaterialTakeoff)    // API unverified
                return BuildMaterialTakeoffRows(def, columns);
            if (schedule.IsNoteBlock)          // API unverified
                return BuildNoteBlockRows(def, columns);

            return BuildStandardRows(def, columns);
        }

        /// <summary>Standard element schedule — one row per model element of the category.</summary>
        private List<ScheduleRowModel> BuildStandardRows(
            ScheduleDefinition def,
            List<ScheduleColumnModel> columns)
        {
            var catId = def.GetCategoryId(); // API unverified — returns ElementId
            var elements = new FilteredElementCollector(_doc)
                .OfCategoryId(catId)          // API unverified
                .WhereElementIsNotElementType()
                .ToList();

            // TODO: Re-apply ScheduleDefinition.GetFilters() as ElementParameterFilter /
            //       ElementCategoryFilter to match exactly what the schedule shows.
            //       Currently returns ALL elements of the category.

            Logger.Log($"[ScheduleReaderService] Standard schedule — {elements.Count} elements in category {catId.Value}.");
            return BuildRowsFromElements(elements, columns, ScheduleType.Standard);
        }

        /// <summary>
        /// Key schedule — rows are <c>ScheduleKeyEntry</c> instances.
        /// </summary>
        /// <remarks>
        /// API unverified: <c>ScheduleKeyEntry</c> class availability in Revit 2024.
        /// If <c>ScheduleKeyEntry</c> does not exist, replace with a query against
        /// the schedule's category using element type filtering.
        /// </remarks>
        private List<ScheduleRowModel> BuildKeyRows(
            ScheduleDefinition def,
            List<ScheduleColumnModel> columns)
        {
            var catId = def.GetCategoryId(); // API unverified

            // API unverified — ScheduleKeyEntry may not be available in Revit 2024.
            // Fallback: treat as standard rows if the class is not found.
            List<Element> elements;
            try
            {
                elements = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ScheduleKeyEntry)) // API unverified
                    .Cast<ScheduleKeyEntry>()           // API unverified
                    .Where(ke => ke.Category?.Id != null
                                 && ke.Category.Id.Equals(catId))
                    .Cast<Element>()
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Log($"[ScheduleReaderService] ScheduleKeyEntry not available ({ex.Message}). Falling back to standard collector.");
                elements = new FilteredElementCollector(_doc)
                    .OfCategoryId(catId)
                    .WhereElementIsNotElementType()
                    .ToList();
            }

            Logger.Log($"[ScheduleReaderService] Key schedule — {elements.Count} key entries.");
            return BuildRowsFromElements(elements, columns, ScheduleType.Key);
        }

        /// <summary>
        /// Material takeoff — same element collector as standard but each element
        /// may appear once per material layer.
        /// </summary>
        /// <remarks>
        /// TODO: Proper per-layer row expansion requires iterating
        /// <c>CompoundStructure</c> layers on each element and matching material
        /// assignments.  The current implementation creates one row per element
        /// (first layer only) and tracks <c>MaterialLayerIndex = 0</c>.
        /// </remarks>
        private List<ScheduleRowModel> BuildMaterialTakeoffRows(
            ScheduleDefinition def,
            List<ScheduleColumnModel> columns)
        {
            var catId = def.GetCategoryId(); // API unverified
            var elements = new FilteredElementCollector(_doc)
                .OfCategoryId(catId)
                .WhereElementIsNotElementType()
                .ToList();

            Logger.Log($"[ScheduleReaderService] Material takeoff — {elements.Count} elements (layer expansion not yet implemented).");

            var rows = new List<ScheduleRowModel>();
            int rowIdx = 0;
            foreach (var element in elements)
            {
                var cells = BuildCellsForElement(element, columns);
                rows.Add(new ScheduleRowModel
                {
                    ElementId          = element.Id.Value,
                    RowIndex           = rowIdx++,
                    Cells              = cells,
                    ScheduleType       = ScheduleType.MaterialTakeoff,
                    MaterialLayerIndex = 0   // TODO: expand per layer
                });
            }

            return rows;
        }

        /// <summary>Note block schedule — rows are <c>AnnotationSymbol</c> elements.</summary>
        private List<ScheduleRowModel> BuildNoteBlockRows(
            ScheduleDefinition def,
            List<ScheduleColumnModel> columns)
        {
            var catId = def.GetCategoryId(); // API unverified
            // AnnotationSymbol instances belong to the detail-item categories.
            // Collect all non-element-type elements of the schedule's category.
            var elements = new FilteredElementCollector(_doc)
                .OfCategoryId(catId)
                .WhereElementIsNotElementType()
                .ToList();

            Logger.Log($"[ScheduleReaderService] Note block schedule — {elements.Count} annotation symbols.");
            return BuildRowsFromElements(elements, columns, ScheduleType.NoteBlock);
        }

        // ─────────────────────────────────────────────────────────────────
        // Shared helpers
        // ─────────────────────────────────────────────────────────────────

        private List<ScheduleRowModel> BuildRowsFromElements(
            IList<Element> elements,
            List<ScheduleColumnModel> columns,
            ScheduleType scheduleType)
        {
            var rows = new List<ScheduleRowModel>(elements.Count);

            for (int rowIdx = 0; rowIdx < elements.Count; rowIdx++)
            {
                var element = elements[rowIdx];
                var cells   = BuildCellsForElement(element, columns);

                rows.Add(new ScheduleRowModel
                {
                    ElementId    = element.Id.Value,
                    RowIndex     = rowIdx,
                    Cells        = cells,
                    ScheduleType = scheduleType
                });
            }

            return rows;
        }

        private Dictionary<int, ScheduleCellModel> BuildCellsForElement(
            Element element,
            List<ScheduleColumnModel> columns)
        {
            var cells = new Dictionary<int, ScheduleCellModel>(columns.Count);

            foreach (var col in columns)
            {
                var cell = ReadCell(element, col);
                cells[col.ColumnIndex] = cell;

                // Back-fill StorageType and ForgeTypeId on the column model from the
                // first element that successfully resolves the parameter.
                if (col.StorageType == StorageType.Unknown
                    && cell.StorageType != StorageType.Unknown)
                {
                    col.StorageType = cell.StorageType;
                    col.ForgeTypeId = cell.ForgeTypeId;
                }
            }

            return cells;
        }

        private ScheduleCellModel ReadCell(Element element, ScheduleColumnModel col)
        {
            // Formula / Count fields have no backing parameter to read.
            if (col.FieldType == ScheduleFieldType.Formula  // API unverified
             || col.FieldType == ScheduleFieldType.Count)   // API unverified
            {
                return new ScheduleCellModel
                {
                    DisplayValue         = "",
                    OriginalDisplayValue = "",
                    IsReadOnly           = true,
                    StorageType          = StorageType.Unknown
                };
            }

            Parameter? param = null;

            // Prefer BuiltInParameter lookup; fall back to ElementId lookup.
            if (col.BuiltInParam.HasValue)
                param = element.get_Parameter(col.BuiltInParam.Value);

            if (param == null
                && col.ParameterId != null
                && col.ParameterId != ElementId.InvalidElementId)
            {
                param = element.get_Parameter(col.ParameterId);
            }

            if (param == null)
            {
                // Parameter not present on this element (e.g. type param on a key row).
                return new ScheduleCellModel
                {
                    DisplayValue         = "",
                    OriginalDisplayValue = "",
                    IsReadOnly           = true,
                    StorageType          = StorageType.Unknown
                };
            }

            bool isReadOnly = col.IsReadOnly || param.IsReadOnly;

            // ElementId parameters are treated as read-only in v1.
            if (param.StorageType == StorageType.ElementId)
                isReadOnly = true;

            ForgeTypeId? forgeTypeId = null;
            try { forgeTypeId = param.Definition.GetDataType(); } // API unverified
            catch { /* not all parameters expose a data type */ }

            string displayValue = FormatDisplayValue(param, forgeTypeId);
            object? rawValue    = ReadRawValue(param);

            return new ScheduleCellModel
            {
                DisplayValue         = displayValue,
                OriginalDisplayValue = displayValue,
                RawValue             = rawValue,
                IsReadOnly           = isReadOnly,
                StorageType          = param.StorageType,
                ForgeTypeId          = forgeTypeId
            };
        }

        private string FormatDisplayValue(Parameter param, ForgeTypeId? forgeTypeId)
        {
            try
            {
                switch (param.StorageType)
                {
                    case StorageType.String:
                        return param.AsString() ?? string.Empty;

                    case StorageType.Integer:
                        return param.AsInteger().ToString();

                    case StorageType.Double:
                        if (forgeTypeId != null)
                        {
                            // UnitFormatUtils.Format — API unverified for Revit 2024 signature.
                            // Expected: Format(Units units, ForgeTypeId specTypeId, double value)
                            return UnitFormatUtils.Format(   // API unverified
                                _doc.GetUnits(),             // API unverified
                                forgeTypeId,
                                param.AsDouble());
                        }
                        return param.AsDouble().ToString("G10");

                    case StorageType.ElementId:
                        var eid = param.AsElementId();
                        if (eid == null || eid == ElementId.InvalidElementId) return string.Empty;
                        var target = _doc.GetElement(eid);
                        return target?.Name ?? eid.Value.ToString();

                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ScheduleReaderService] FormatDisplayValue failed for param '{param.Definition?.Name}': {ex.Message}");
                return string.Empty;
            }
        }

        private static object? ReadRawValue(Parameter param)
        {
            return param.StorageType switch
            {
                StorageType.String    => (object?)param.AsString(),
                StorageType.Integer   => param.AsInteger(),
                StorageType.Double    => param.AsDouble(),
                StorageType.ElementId => param.AsElementId()?.Value,
                _                     => null
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // Classification
        // ─────────────────────────────────────────────────────────────────

        private static ScheduleType ClassifySchedule(ViewSchedule vs)
        {
            if (vs.IsKeySchedule)    return ScheduleType.Key;            // API unverified
            if (vs.IsMaterialTakeoff) return ScheduleType.MaterialTakeoff; // API unverified
            if (vs.IsNoteBlock)      return ScheduleType.NoteBlock;       // API unverified
            return ScheduleType.Standard;
        }
    }
}
