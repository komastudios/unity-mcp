using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityMcpBridge.Editor.Tools;
using UnityMcpBridge.Tools;

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Registry for all MCP command handlers (Refactored Version)
    /// </summary>
    public static class CommandRegistry
    {
        // Maps command names (matching those called from Python via ctx.bridge.unity_editor.HandlerName)
        // to the corresponding static HandleCommand method in the appropriate tool class.
        private static readonly Dictionary<string, Func<JObject, object>> _handlers = new()
        {
            { "HandleManageScript", (JObject @params) => ManageScript.HandleCommand(@params) },
            { "HandleManageScene", (JObject @params) => ManageScene.HandleCommand(@params) },
            { "HandleManageEditor", (JObject @params) => ManageEditor.HandleCommand(@params) },
            { "HandleManageGameObject", (JObject @params) => ManageGameObject.HandleCommand(@params) },
            { "HandleManageAsset", (JObject @params) => ManageAsset.HandleCommand(@params) },
            { "HandleManageAnimation", (JObject @params) => ManageAnimation.HandleCommand(@params) },
            { "HandleManageAudio", (JObject @params) => ManageAudio.HandleCommand(@params) },
            { "HandleManageInput", (JObject @params) => ManageInput.HandleCommand(@params) },
            { "HandleManageLighting", (JObject @params) => ManageLighting.HandleCommand(@params) },
            { "HandleManageParticles", (JObject @params) => ManageParticles.HandleCommand(@params) },
            { "HandleManagePhysics", (JObject @params) => ManagePhysics.HandleCommand(@params) },
            { "HandleManageTerrain", (JObject @params) => ManageTerrain.HandleCommand(@params) },
            { "HandleManageUI", (JObject @params) => ManageUI.HandleCommand(@params) },
            { "HandleManageAI", (JObject @params) => ManageAI.HandleAICommand(@params) },
            { "HandleManageNetworking", (JObject @params) => ManageNetworking.HandleNetworkingCommand(@params) },
            { "HandleManageBuild", (JObject @params) => ManageBuild.HandleCommand(@params) },
            { "HandleManagePerformance", (JObject @params) => ManagePerformance.HandleCommand(@params) },
            { "HandleReadConsole", (JObject @params) => ReadConsole.HandleCommand(@params) },
            { "HandleExecuteMenuItem", (JObject @params) => ExecuteMenuItem.HandleCommand(@params) },
            { "HandleScreenshotTool", (JObject @params) => ScreenshotTool.TakeScreenshot(@params) },
            { "HandleTriggerDomainReload", (JObject @params) => TriggerDomainReload.HandleCommand(@params) },
        };

        /// <summary>
        /// Gets a command handler by name.
        /// </summary>
        /// <param name="commandName">Name of the command handler (e.g., "HandleManageAsset").</param>
        /// <returns>The command handler function if found, null otherwise.</returns>
        public static Func<JObject, object> GetHandler(string commandName)
        {
            // Use case-insensitive comparison for flexibility, although Python side should be consistent
            return _handlers.TryGetValue(commandName, out var handler) ? handler : null;
            // Consider adding logging here if a handler is not found
            /*
            if (_handlers.TryGetValue(commandName, out var handler)) {
                return handler;
            } else {
                UnityEngine.Debug.LogError($\"[CommandRegistry] No handler found for command: {commandName}\");
                return null;
            }
            */
        }
    }
}

