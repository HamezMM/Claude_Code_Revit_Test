// PDG GENERATED: 2026-03-01 | Revit 2024 | Verified: revitapidocs.com/2024/
namespace PDG.Revit.AutomationTools.Helpers
{
    // PDG SHARED LIBRARY CANDIDATE: UnitConversionHelper has no AutomationTools-specific logic.
    // Promote to shared/PDG.Revit.Shared/Helpers/UnitConversionHelper.cs before next release.
    // Both AutomationTools and FireRatingLines will then reference a single source of truth.
    // Do not commit a duplicate if this already exists in the shared library.
    /// <summary>
    /// Utility methods for converting between user-facing units and
    /// Revit's internal unit system (decimal feet).
    /// Candidate for promotion to PDG.Revit.Shared.
    /// </summary>
    public static class UnitConversionHelper
    {
        private const double MmPerFoot = 304.8;

        /// <summary>Converts millimetres to Revit internal units (feet).</summary>
        public static double MmToFeet(double millimetres) => millimetres / MmPerFoot;

        /// <summary>Converts Revit internal units (feet) to millimetres.</summary>
        public static double FeetToMm(double feet) => feet * MmPerFoot;
    }
}
