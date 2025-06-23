using UnityEngine;
using UnityMcpBridge.Editor.Models;

namespace UnityMcpBridge.Editor.Helpers
{
    /// <summary>
    /// Centralized logging utility for Unity MCP Bridge that respects the debug logging setting.
    /// </summary>
    public static class McpLogger
    {
        /// <summary>
        /// Logs important status messages that should always be shown.
        /// Use this for key events like server start/stop, connections, etc.
        /// </summary>
        public static void LogInfo(string message)
        {
            Debug.Log($"[MCP] {message}");
        }
        
        /// <summary>
        /// Logs a message only if debug logging is enabled.
        /// Use this for verbose/debug messages.
        /// </summary>
        public static void Log(string message)
        {
            if (McpSettings.Instance.DebugLogging)
            {
                Debug.Log($"[MCP] {message}");
            }
        }
        
        /// <summary>
        /// Logs a warning only if debug logging is enabled.
        /// </summary>
        public static void LogWarning(string message)
        {
            if (McpSettings.Instance.DebugLogging)
            {
                Debug.LogWarning($"[MCP] {message}");
            }
        }
        
        /// <summary>
        /// Always logs errors regardless of debug logging setting.
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError($"[MCP] {message}");
        }
        
        /// <summary>
        /// Logs a message with a specific context/category only if debug logging is enabled.
        /// </summary>
        public static void LogWithContext(string context, string message)
        {
            if (McpSettings.Instance.DebugLogging)
            {
                Debug.Log($"[MCP:{context}] {message}");
            }
        }
    }
}