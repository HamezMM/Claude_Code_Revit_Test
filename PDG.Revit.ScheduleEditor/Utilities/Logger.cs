// PDG.Revit.ScheduleEditor | Revit 2024 | net48
using System;
using System.Diagnostics;

namespace PDG.Revit.ScheduleEditor.Utilities
{
    /// <summary>
    /// Minimal static logger stub.
    /// Writes to <see cref="Debug"/> output in DEBUG builds and to
    /// <see cref="Trace"/> in all builds.
    /// Replace or extend this with a proper logging framework (e.g. Serilog,
    /// NLog, log4net) as required by the host project.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Writes a timestamped message to the debug/trace output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Log(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss.fff}] [ScheduleEditor] {message}";
            Debug.WriteLine(entry);
            Trace.WriteLine(entry);
        }

        /// <summary>
        /// Writes a timestamped exception message and stack trace to the debug/trace output.
        /// </summary>
        /// <param name="message">Context describing where the exception occurred.</param>
        /// <param name="ex">The exception to log.</param>
        public static void Log(string message, Exception ex)
        {
            Log($"{message} | Exception: {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
        }
    }
}
