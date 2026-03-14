// PDG.Revit.ScheduleEditor | Revit 2024 | net48
// API NOTE: Lines marked "// API unverified" must be confirmed at
// https://www.revitapidocs.com/2024/ before compiling.
using Autodesk.Revit.DB;
using PDG.Revit.ScheduleEditor.Models;
using PDG.Revit.ScheduleEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PDG.Revit.ScheduleEditor.Services
{
    /// <summary>
    /// Writes a batch of pending cell edits back to Revit in a single
    /// <c>Transaction</c> named "Schedule Editor — Apply Changes".
    /// Implements <see cref="IFailuresPreprocessor"/> to suppress duplicate-mark
    /// and other expected warnings, collecting them for post-apply display.
    /// <para>
    /// All Revit API access is confined to this class.
    /// </para>
    /// </summary>
    public sealed class ScheduleWriterService : IFailuresPreprocessor
    {
        private readonly Document _doc;

        /// <summary>Warnings collected by the <see cref="IFailuresPreprocessor"/> during the transaction.</summary>
        private readonly List<string> _transactionWarnings = new();

        /// <summary>Initialises the service for the active project document.</summary>
        /// <param name="doc">The Revit project document.</param>
        public ScheduleWriterService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Opens a single <c>Transaction</c>, applies every edit in <paramref name="edits"/>,
        /// and commits.  On commit failure the transaction is rolled back and the
        /// exception message is returned as a warning.
        /// </summary>
        /// <param name="edits">The list of dirty cells to write.</param>
        /// <returns>A <see cref="WriteResult"/> summarising the outcome.</returns>
        public WriteResult ApplyEdits(IReadOnlyList<ScheduleCellEdit> edits)
        {
            if (edits == null || edits.Count == 0)
                return new WriteResult(0, 0, Array.Empty<string>());

            _transactionWarnings.Clear();

            int success = 0;
            int skip    = 0;
            var appWarnings = new List<string>();

            using var trans = new Transaction(_doc, "Schedule Editor — Apply Changes");

            // Wire up the failures preprocessor so Revit warnings are captured
            // rather than displayed as modal dialogs.
            var failureOptions = trans.GetFailureHandlingOptions();
            failureOptions.SetFailuresPreprocessor(this);
            trans.SetFailureHandlingOptions(failureOptions);

            trans.Start();

            try
            {
                foreach (var edit in edits)
                {
                    if (TryApplyEdit(edit, appWarnings))
                        success++;
                    else
                        skip++;
                }

                trans.Commit();

                // Merge warnings suppressed during the transaction.
                appWarnings.AddRange(_transactionWarnings);

                Logger.Log($"[ScheduleWriterService] Applied {success} edits, skipped {skip}.");
                return new WriteResult(success, skip, appWarnings);
            }
            catch (ObjectDisposedException)
            {
                SafeRollback(trans);
                var msg = "The schedule or its elements were deleted while the editor was open. " +
                          "Please close and reopen the Schedule Editor.";
                Logger.Log($"[ScheduleWriterService] ObjectDisposedException during Apply: {msg}");
                return new WriteResult(0, edits.Count, new[] { msg });
            }
            catch (Exception ex)
            {
                SafeRollback(trans);
                Logger.Log($"[ScheduleWriterService] Transaction failed", ex);
                return new WriteResult(0, edits.Count,
                    new[] { $"Transaction commit failed — all changes were rolled back. {ex.Message}" });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // IFailuresPreprocessor
        // ─────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor) // API unverified
        {
            var messages = failuresAccessor.GetFailureMessages(); // API unverified

            foreach (var msg in messages)
            {
                var severity    = msg.GetSeverity();      // API unverified
                var description = msg.GetDescriptionText(); // API unverified

                if (severity == FailureSeverity.Warning) // API unverified
                {
                    // Suppress the warning — it will be surfaced in the UI status bar.
                    _transactionWarnings.Add($"[Warning] {description}");
                    failuresAccessor.DeleteWarning(msg); // API unverified
                }
                else
                {
                    // Errors cannot be suppressed here; record them for the result.
                    _transactionWarnings.Add($"[Error] {description}");
                }
            }

            return FailureProcessingResult.Continue; // API unverified
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private bool TryApplyEdit(ScheduleCellEdit edit, List<string> warnings)
        {
            try
            {
                var element = _doc.GetElement(new ElementId(edit.ElementId));
                if (element == null)
                {
                    warnings.Add($"Element {edit.ElementId} not found — skipped.");
                    return false;
                }

                Parameter? param = null;

                if (edit.BuiltInParam.HasValue)
                    param = element.get_Parameter(edit.BuiltInParam.Value);

                if (param == null
                    && edit.ParameterId != null
                    && edit.ParameterId != ElementId.InvalidElementId)
                {
                    param = element.Parameters.Cast<Parameter>()
                        .FirstOrDefault(p => p.Id.Equals(edit.ParameterId));
                }

                if (param == null)
                {
                    warnings.Add($"Parameter not found on element {edit.ElementId} (column {edit.ColumnIndex}) — skipped.");
                    return false;
                }

                if (param.IsReadOnly)
                {
                    // Likely a worksharing checkout issue or a genuinely read-only parameter.
                    var hint = "Parameter is read-only";
                    if (_doc.IsWorkshared) // API unverified
                        hint += " — element may not be checked out";
                    warnings.Add($"{hint}: element {edit.ElementId}, column {edit.ColumnIndex} — skipped.");
                    return false;
                }

                return SetValue(param, edit, warnings);
            }
            catch (ObjectDisposedException)
            {
                throw; // Let the outer handler surface the full message.
            }
            catch (Exception ex)
            {
                warnings.Add($"Unexpected error writing element {edit.ElementId}: {ex.Message}");
                Logger.Log($"[ScheduleWriterService] Error on element {edit.ElementId}", ex);
                return false;
            }
        }

        private bool SetValue(Parameter param, ScheduleCellEdit edit, List<string> warnings)
        {
            switch (edit.StorageType)
            {
                case StorageType.String:
                    param.Set(edit.NewDisplayValue);
                    return true;

                case StorageType.Integer:
                    if (int.TryParse(edit.NewDisplayValue, NumberStyles.Integer,
                            CultureInfo.CurrentCulture, out int intVal)
                     || int.TryParse(edit.NewDisplayValue, NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out intVal))
                    {
                        param.Set(intVal);
                        return true;
                    }
                    warnings.Add($"Invalid integer '{edit.NewDisplayValue}' " +
                                 $"on element {edit.ElementId}, column {edit.ColumnIndex} — skipped.");
                    return false;

                case StorageType.Double:
                    return SetDoubleValue(param, edit, warnings);

                case StorageType.ElementId:
                    // ElementId parameters are read-only in v1; should never reach here.
                    warnings.Add($"ElementId parameter write is not supported in v1 " +
                                 $"(element {edit.ElementId}, column {edit.ColumnIndex}) — skipped.");
                    return false;

                default:
                    warnings.Add($"Unsupported StorageType '{edit.StorageType}' " +
                                 $"on element {edit.ElementId} — skipped.");
                    return false;
            }
        }

        private bool SetDoubleValue(Parameter param, ScheduleCellEdit edit, List<string> warnings)
        {
            // Parse the display string.  Accept both the current UI culture and
            // the invariant culture so users can type "3.5" or "3,5" depending on locale.
            if (!TryParseDouble(edit.NewDisplayValue, out double displayVal))
            {
                warnings.Add($"Invalid numeric value '{edit.NewDisplayValue}' " +
                             $"on element {edit.ElementId}, column {edit.ColumnIndex} — skipped.");
                return false;
            }

            try
            {
                double internalVal = displayVal;

                if (edit.ForgeTypeId != null)
                {
                    // Convert from the document's current display unit to Revit's internal unit.
                    // FormatOptions.GetUnitTypeId() — API unverified for Revit 2024.
                    var formatOptions  = _doc.GetUnits().GetFormatOptions(edit.ForgeTypeId); // API unverified
                    var displayUnitId  = formatOptions.GetUnitTypeId(); // API unverified — returns ForgeTypeId
                    internalVal = UnitUtils.ConvertToInternalUnits(displayVal, displayUnitId); // API unverified
                }

                param.Set(internalVal);
                return true;
            }
            catch (Exception ex)
            {
                warnings.Add($"Unit conversion failed for '{edit.NewDisplayValue}' " +
                             $"on element {edit.ElementId}: {ex.Message} — skipped.");
                return false;
            }
        }

        private static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture,  out value)
                || double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static void SafeRollback(Transaction trans)
        {
            try
            {
                if (trans.GetStatus() == TransactionStatus.Started)
                    trans.RollBack();
            }
            catch (Exception ex)
            {
                Logger.Log("[ScheduleWriterService] Rollback failed", ex);
            }
        }
    }
}
