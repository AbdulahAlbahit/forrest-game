using UnityEngine;
using System.Diagnostics;

namespace ForrestGame.Utils
{
    /// <summary>
    /// Professional logging utility. 
    /// Strips standard logs from production builds to improve performance.
    /// Warnings and Errors are kept in builds for analytics and crash reporting.
    /// </summary>
    public static class GameLogger
    {
        // [Conditional] attribute completely removes the method call from the compiled code 
        // if the ENABLE_LOGS symbol is not defined (e.g., in Release builds).
        // For development, we rely on UNITY_EDITOR or Development Build flags.

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message, Object context = null)
        {
            UnityEngine.Debug.Log($"[LOG] {message}", context);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message, Object context = null)
        {
            UnityEngine.Debug.LogWarning($"[WARNING] {message}", context);
        }

        // Errors should usually remain in builds for crash analytics
        public static void LogError(string message, Object context = null)
        {
            UnityEngine.Debug.LogError($"[ERROR] {message}", context);
        }
    }
}
