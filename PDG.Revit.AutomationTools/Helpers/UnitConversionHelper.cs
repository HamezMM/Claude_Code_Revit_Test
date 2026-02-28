namespace PDG.Revit.AutomationTools.Helpers
{
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
