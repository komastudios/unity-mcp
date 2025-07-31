using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;
using UnityMcpBridge.Editor.Models;
using UnityMcpBridge.Editor.Tools;

namespace UnityMcpBridge.Editor
{
    [InitializeOnLoad]
    public class ClientAction
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
    }

    public class ClientState
    {
        public string EndPoint { get; set; }
        public string CurrentCommand { get; set; }
        public List<ClientAction> LastActions { get; } = new List<ClientAction>();
        public bool IsExpanded { get; set; } = false;
        public Vector2 ActionScrollPosition; // Add this

        public void AddAction(string action)
        {
            LastActions.Add(new ClientAction { Timestamp = DateTime.Now, Action = action });
            if (LastActions.Count > 10)
            {
                LastActions.RemoveAt(0);
            }
        }
    }

    public static partial class UnityMcpBridge
    {
        private static TcpListener listener;
        private static bool isRunning = false;
        private static readonly object lockObj = new();
        private static Dictionary<
            string,
            (string commandJson, TaskCompletionSource<string> tcs, ClientState clientState)
        > commandQueue = new();
        private static int UnityPort => McpSettings.Instance.UnityPort;
        public static readonly List<ClientState> ConnectedClients = new();

        public static bool IsRunning => isRunning;

        public static bool FolderExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (path.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string fullPath = Path.Combine(
                Application.dataPath,
                path.StartsWith("Assets/") ? path[7..] : path
            );
            return Directory.Exists(fullPath);
        }

        static UnityMcpBridge()
        {
            Start();
            EditorApplication.quitting += Stop;
        }

        public static void Start()
        {
            Stop();

            try
            {
                ServerInstaller.EnsureServerInstalled();
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to ensure UnityMcpServer is installed: {ex.Message}");
            }

            if (isRunning)
            {
                return;
            }

            try
            {
                // TODO: Add TLS encryption for secure communication between Python MCP server and Unity C# bridge
                // Currently using unencrypted TCP listener which exposes commands and data on localhost:6400
                // Consider using SslStream with self-signed certificates for local security
                // This is important since the port accepts commands that can modify Unity project files
                listener = new TcpListener(IPAddress.Loopback, UnityPort);
                listener.Start();
                isRunning = true;
                McpLogger.LogInfo($"UnityMcpBridge started on port {UnityPort}.");
                McpLogger.LogInfo($"Listening on: {IPAddress.Loopback}:{UnityPort}");
                McpLogger.Log($"Waiting for MCP server connections...");
                // Assuming ListenerLoop and ProcessCommands are defined elsewhere
                Task.Run(ListenerLoop);
                EditorApplication.update += ProcessCommands;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    McpLogger.LogError(
                        $"Port {UnityPort} is already in use. Ensure no other instances are running or change the port."
                    );
                }
                else
                {
                    McpLogger.LogError($"Failed to start TCP listener: {ex.Message}");
                }
            }
        }

        public static void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            try
            {
                listener?.Stop();
                listener = null;
                isRunning = false;
                EditorApplication.update -= ProcessCommands;
                McpLogger.LogInfo("UnityMcpBridge stopped.");
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error stopping UnityMcpBridge: {ex.Message}");
            }
        }

        private static async Task ListenerLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    McpLogger.LogInfo($"Accepted connection from: {client.Client.RemoteEndPoint}");
                    
                    // Enable basic socket keepalive
                    client.Client.SetSocketOption(
                        SocketOptionLevel.Socket,
                        SocketOptionName.KeepAlive,
                        true
                    );

                    // Set longer receive timeout to prevent quick disconnections
                    client.ReceiveTimeout = 60000; // 60 seconds

                    // Fire and forget each client connection
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        McpLogger.LogError($"Listener error: {ex.Message}");
                    }
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            var clientState = new ClientState
            {
                EndPoint = client.Client.RemoteEndPoint.ToString(),
                CurrentCommand = "Idle"
            };
            clientState.AddAction("Connected");
            ConnectedClients.Add(clientState);

            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[8192];
                    while (isRunning)
                    {
                        try
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                            {
                                clientState.AddAction("Disconnected");
                                break; // Client disconnected
                            }

                            string commandText = System.Text.Encoding.UTF8.GetString(
                                buffer,
                                0,
                                bytesRead
                            );
                            try
                            {
                                var commandJson = JObject.Parse(commandText);
                                string action = commandJson["action"]?.ToString();
                                string tool = commandJson["tool_name"]?.ToString();
                                string type = commandJson["type"]?.ToString();

                                string actionLog;
                                if (!string.IsNullOrEmpty(tool))
                                {
                                    actionLog = $"Tool: {tool}";
                                }
                                else if (!string.IsNullOrEmpty(action))
                                {
                                    actionLog = $"Action: {action}";
                                }
                                else if (!string.IsNullOrEmpty(type))
                                {
                                    actionLog = $"Command: {type}";
                                }
                                else
                                {
                                    actionLog = "unknown_action";
                                }
                                clientState.AddAction(actionLog);
                            }
                            catch (JsonReaderException)
                            {
                                clientState.AddAction($"Received: {commandText.Trim()}");
                            }
                            string commandId = Guid.NewGuid().ToString();
                            TaskCompletionSource<string> tcs = new();

                            // Special handling for ping command to avoid JSON parsing
                            if (commandText.Trim() == "ping")
                            {
                                // Direct response to ping without going through JSON parsing
                                byte[] pingResponseBytes = System.Text.Encoding.UTF8.GetBytes(
                                    /*lang=json,strict*/
                                    "{\"status\":\"success\",\"result\":{\"message\":\"pong\"}}"
                                );
                                await stream.WriteAsync(pingResponseBytes, 0, pingResponseBytes.Length);
                                continue;
                            }

                            clientState.CurrentCommand = commandText;
                            lock (lockObj)
                            {
                                commandQueue[commandId] = (commandText, tcs, clientState);
                            }


                            string response = await tcs.Task;
                            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                        }
                        catch (IOException ex)
                        {
                            McpLogger.LogWarning($"Client disconnected: {ex.Message}");
                            break; // Exit loop on disconnection
                        }
                        catch (Exception ex)
                        {
                            McpLogger.LogError($"Error handling client: {ex.Message}");
                            break; // Exit loop on error
                        }
                    }
                }
            }
            finally
            {
                ConnectedClients.Remove(clientState);
            }
        }

        private static void ProcessCommands()
        {
            if (commandQueue.Count == 0)
            {
                return;
            }

            var commandsToProcess = new Dictionary<string, (string, TaskCompletionSource<string>, ClientState)>();
            lock (lockObj)
            {
                commandsToProcess = commandQueue;
                commandQueue = new Dictionary<string, (string, TaskCompletionSource<string>, ClientState)>();
            }

            foreach (var command in commandsToProcess)
            {
                string responseJson;
                try
                {
                    var commandData = JsonConvert.DeserializeObject<Command>(
                        command.Value.Item1
                    );
                    var result = new JObject();

                    switch (commandData.type)
                    {
                        case "manage_editor":
                            result = JObject.FromObject(
                                ManageEditor.HandleCommand(commandData.@params)
                            );
                            break;
                        case "manage_scene":
                            result = JObject.FromObject(
                                ManageScene.HandleCommand(commandData.@params)
                            );
                            break;
                        case "manage_gameobject":
                            result = JObject.FromObject(
                                ManageGameObject.HandleCommand(commandData.@params)
                            );
                            break;
                        case "manage_asset":
                            result = JObject.FromObject(
                                ManageAsset.HandleCommand(commandData.@params)
                            );
                            break;
                        case "manage_script":
                            result = JObject.FromObject(
                                ManageScript.HandleCommand(commandData.@params)
                            );
                            break;
                        case "read_console":
                            result = JObject.FromObject(
                                ReadConsole.HandleCommand(commandData.@params)
                            );
                            break;
                        case "execute_menu_item":
                            result = JObject.FromObject(
                                ExecuteMenuItem.HandleCommand(commandData.@params)
                            );
                            break;
                        case "take_screenshot":
                            result = JObject.FromObject(
                                ScreenshotTool.TakeScreenshot(commandData.@params.ToObject<Dictionary<string, object>>())                            
                            );
                            break;
                        case "trigger_domain_reload":
                            result = JObject.FromObject(
                                TriggerDomainReload.HandleCommand(commandData.@params)
                            );
                            break;
                        default:
                            result = new JObject
                            {
                                { "success", false },
                                {
                                    "error",
                                    $"Unknown or unsupported command type: {commandData.type}"
                                }
                            };
                            break;
                    }

                    responseJson = JsonConvert.SerializeObject(
                        new { status = "success", result },
                        Formatting.Indented
                    );
                }
                catch (Exception ex)
                {
                    responseJson = JsonConvert.SerializeObject(
                        new
                        {
                            status = "error",
                            error = $"Failed to process command: {ex.Message}",
                            stackTrace = ex.StackTrace
                        },
                        Formatting.Indented
                    );
                }

                command.Value.Item2.SetResult(responseJson);
                command.Value.Item3.CurrentCommand = "Idle";
            }
        }
    }
}
