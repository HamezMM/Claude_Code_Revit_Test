// PDG GENERATED: 2026-03-01 | Revit 2024

namespace PDG.Revit.FireRatingLines.Models
{
    /// <summary>
    /// Canonical fire rating names used for line style lookup and auto-creation.
    /// Wall types must have their "Fire Rating" type parameter set to one of these
    /// exact strings for the Fire Rating Lines tool to annotate them.
    /// </summary>
    public static class FireRatingStandards
    {
        /// <summary>
        /// Ordered list of standard fire rating display names.
        /// The tool ensures a line style with each of these names exists in the document
        /// before drawing annotation lines, creating any that are missing.
        /// </summary>
        public static readonly string[] StandardRatings = new[]
        {
            "45 MIN",
            "1 HR",
            "1.5 HR",
            "2 HR",
            "3 HR",
            "4 HR"
        };
    }
}
