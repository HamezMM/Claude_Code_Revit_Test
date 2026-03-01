// PDG TEMPORARY: PDGLogger stub — promote to shared/PDG.Revit.Shared/Logging/PDGLogger.cs
// before next shared library release. Do not ship this stub in production.

namespace PDG.Revit.FireRatingLines
{
    internal static class PDGLogger
    {
        public static void Warning(string message)
        {
            System.Diagnostics.Trace.TraceWarning(message);
        }

        public static void Info(string message)
        {
            System.Diagnostics.Trace.TraceInformation(message);
        }
    }
}
