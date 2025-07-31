using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Data;
using UnityMcpBridge.Editor.Helpers;
using UnityMcpBridge.Editor.Models;

namespace UnityMcpBridge.Editor.Windows
{
    public class UnityMcpEditorWindow : EditorWindow
    {
        private bool isUnityBridgeRunning = false;
        private Vector2 scrollPosition;
        private string pythonServerInstallationStatus = "Not Installed";
        private Color pythonServerInstallationStatusColor = Color.red;
        private readonly McpClients mcpClients = new();
        private bool showPortSettings = false;
        private string unityPortInput;
        private string mcpPortInput;
        private int _selectedTab = 0;

        [MenuItem("Window/Unity MCP")]
        public static void ShowWindow()
        {
            GetWindow<UnityMcpEditorWindow>("MCP Editor");
        }

        private void OnEnable()
        {
            UpdatePythonServerInstallationStatus();

            isUnityBridgeRunning = UnityMcpBridge.IsRunning;
            foreach (McpClient mcpClient in mcpClients.clients)
            {
                CheckMcpConfiguration(mcpClient);
            }
            
            // Initialize port input fields with current values
            unityPortInput = McpSettings.Instance.UnityPort.ToString();
            mcpPortInput = McpSettings.Instance.McpPort.ToString();
        }

        private void UpdatePythonServerInstallationStatus()
        {
            string serverPath = ServerInstaller.GetServerPath();

            if (File.Exists(Path.Combine(serverPath, "server.py")))
            {
                string installedVersion = ServerInstaller.GetInstalledVersion();
                string latestVersion = ServerInstaller.GetLatestVersion();

                if (ServerInstaller.IsNewerVersion(latestVersion, installedVersion))
                {
                    pythonServerInstallationStatus = "Newer Version Available";
                    pythonServerInstallationStatusColor = UnityMcpStyles.Yellow;
                }
                else
                {
                    pythonServerInstallationStatus = "Up to Date";
                    pythonServerInstallationStatusColor = UnityMcpStyles.Green;
                }
            }
            else
            {
                pythonServerInstallationStatus = "Not Installed";
                pythonServerInstallationStatusColor = UnityMcpStyles.Red;
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawTitle();
            DrawServerStatusSection();
            DrawBridgeSection();
            DrawConfigurationSections();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTitle()
        {
            EditorGUILayout.LabelField("MCP Editor", UnityMcpStyles.TitleLabel);
        }

        private void DrawServerStatusSection()
        {
            EditorGUILayout.BeginVertical(UnityMcpStyles.Box);
            EditorGUILayout.LabelField("Python Server Status", UnityMcpStyles.HeaderLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            Rect installStatusRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            UnityMcpStyles.DrawStatusDot(installStatusRect, pythonServerInstallationStatusColor);
            EditorGUILayout.LabelField("      " + pythonServerInstallationStatus);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Unity Port: {McpSettings.Instance.UnityPort}");
            EditorGUILayout.LabelField($"MCP Port: {McpSettings.Instance.McpPort}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            bool newDebugLogging = EditorGUILayout.Toggle("Debug Logging", McpSettings.Instance.DebugLogging);
            if (newDebugLogging != McpSettings.Instance.DebugLogging)
            {
                McpSettings.Instance.DebugLogging = newDebugLogging;
                McpLogger.LogInfo($"Debug logging {(newDebugLogging ? "enabled" : "disabled")}");
            }

            showPortSettings = EditorGUILayout.Foldout(showPortSettings, "Port Settings", true);
            if (showPortSettings)
            {
                DrawPortSettings();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Your MCP client (e.g. Cursor or Claude Desktop) will start the server automatically when you start it.",
                MessageType.Info
            );
            EditorGUILayout.EndVertical();
        }

        private void DrawPortSettings()
        {
            EditorGUI.indentLevel++;

            // Unity Port
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unity Port:", GUILayout.Width(80));
            unityPortInput = EditorGUILayout.TextField(unityPortInput, GUILayout.Width(60));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(unityPortInput, out int newPort) && newPort > 0 && newPort <= 65535)
                {
                    if (isUnityBridgeRunning)
                    {
                        EditorUtility.DisplayDialog("Port Change", "Please stop the Unity Bridge before changing ports.", "OK");
                    }
                    else
                    {
                        McpSettings.Instance.UnityPort = newPort;
                        McpLogger.LogInfo($"Unity port changed to {newPort}");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Port", "Please enter a valid port number (1-65535).", "OK");
                    unityPortInput = McpSettings.Instance.UnityPort.ToString();
                }
            }
            EditorGUILayout.EndHorizontal();

            // MCP Port
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MCP Port:", GUILayout.Width(80));
            mcpPortInput = EditorGUILayout.TextField(mcpPortInput, GUILayout.Width(60));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(mcpPortInput, out int newPort) && newPort > 0 && newPort <= 65535)
                {
                    McpSettings.Instance.McpPort = newPort;
                    McpLogger.LogInfo($"MCP port changed to {newPort}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Port", "Please enter a valid port number (1-65535).", "OK");
                    mcpPortInput = McpSettings.Instance.McpPort.ToString();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (isUnityBridgeRunning)
                {
                    EditorUtility.DisplayDialog("Port Change", "Please stop the Unity Bridge before resetting ports.", "OK");
                }
                else
                {
                    McpSettings.Instance.ResetToDefaults();
                    unityPortInput = McpSettings.Instance.UnityPort.ToString();
                    mcpPortInput = McpSettings.Instance.McpPort.ToString();
                    McpLogger.LogInfo("Ports reset to defaults (Unity: 6400, MCP: 6500)");
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawBridgeSection()
        {
            EditorGUILayout.BeginVertical(UnityMcpStyles.Box);
            EditorGUILayout.LabelField("Unity MCP Bridge", UnityMcpStyles.HeaderLabel);

            EditorGUILayout.BeginHorizontal();
            Rect bridgeStatusRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            Color bridgeStatusColor = isUnityBridgeRunning ? UnityMcpStyles.Green : UnityMcpStyles.Red;
            UnityMcpStyles.DrawStatusDot(bridgeStatusRect, bridgeStatusColor);
            EditorGUILayout.LabelField($"      Status: {(isUnityBridgeRunning ? "Running" : "Stopped")}", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"Port: {McpSettings.Instance.UnityPort}", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(isUnityBridgeRunning ? "Stop" : "Start", UnityMcpStyles.SmallButton, GUILayout.Width(80)))
            {
                ToggleUnityBridge();
            }

            using (new EditorGUI.DisabledScope(!isUnityBridgeRunning))
            {
                if (GUILayout.Button("Test", UnityMcpStyles.SmallButton, GUILayout.Width(60)))
                {
                    TestTcpConnection();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (isUnityBridgeRunning)
            {
                EditorGUILayout.Space(5);
                DrawConnectionsSection();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConnectionsSection()
        {
            EditorGUILayout.LabelField("Active Connections", UnityMcpStyles.BoldLabel);

            EditorGUILayout.BeginVertical(UnityMcpStyles.Box);
            if (UnityMcpBridge.ConnectedClients.Count > 0)
            {
                foreach (var client in UnityMcpBridge.ConnectedClients)
                {
                    EditorGUILayout.LabelField($"- {client.EndPoint} - {client.CurrentCommand}", UnityMcpStyles.WrappedLabel);
                    client.IsExpanded = EditorGUILayout.Foldout(client.IsExpanded, "Action History", true);
                    if (client.IsExpanded)
                    {
                        EditorGUI.indentLevel++;
                        client.ActionScrollPosition = EditorGUILayout.BeginScrollView(client.ActionScrollPosition, GUILayout.Height(120));
                        foreach (var action in client.LastActions)
                        {
                            EditorGUILayout.SelectableLabel($"{action.Timestamp:HH:mm:ss} - {action.Action}", GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        }
                        EditorGUILayout.EndScrollView();
                        EditorGUI.indentLevel--;
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No active connections.");
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawConfigurationSections()
        {
            EditorGUILayout.BeginVertical(UnityMcpStyles.Box);
            EditorGUILayout.LabelField("MCP Client Configurations", UnityMcpStyles.HeaderLabel);
            EditorGUILayout.Space();

            if (mcpClients.clients.Count == 0)
            {
                EditorGUILayout.HelpBox("No MCP client configurations found.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            string[] tabNames = mcpClients.clients.ConvertAll(c => c.name).ToArray();
            _selectedTab = GUILayout.Toolbar(_selectedTab, tabNames);

            if (_selectedTab >= 0 && _selectedTab < mcpClients.clients.Count)
            {
                DrawConfigurationSection(mcpClients.clients[_selectedTab]);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConfigurationSection(McpClient mcpClient)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            Rect statusRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            Color statusColor = UnityMcpStyles.GetStatusColor(mcpClient.status);
            UnityMcpStyles.DrawStatusDot(statusRect, statusColor);
            EditorGUILayout.LabelField("      " + mcpClient.configStatus, UnityMcpStyles.WrappedLabel);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Copy MCP JSON", UnityMcpStyles.MutedButton))
            {
                CopyMcpJsonToClipboard(mcpClient);
            }

            if (GUILayout.Button($"Auto Configure {mcpClient.name}", UnityMcpStyles.Button))
            {
                ConfigureMcpClient(mcpClient);
            }

            if (GUILayout.Button("Manual Setup", UnityMcpStyles.MutedButton))
            {
                string configPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? mcpClient.windowsConfigPath
                    : mcpClient.linuxConfigPath;
                ShowManualInstructionsWindow(configPath, mcpClient);
            }

            EditorGUILayout.EndVertical();
        }

        private void CopyMcpJsonToClipboard(McpClient mcpClient)
        {
            string configPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? mcpClient.windowsConfigPath
                : mcpClient.linuxConfigPath;

            string fullPath = Path.GetFullPath(configPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));

            if (File.Exists(fullPath))
            {
                string jsonContent = File.ReadAllText(fullPath);
                EditorGUIUtility.systemCopyBuffer = jsonContent;
                ShowNotification(new GUIContent("MCP JSON copied to clipboard!"));
            }
            else
            {
                ShowNotification(new GUIContent("MCP JSON file not found."));
            }
        }

        private void ToggleUnityBridge()
        {
            if (isUnityBridgeRunning)
            {
                UnityMcpBridge.Stop();
                isUnityBridgeRunning = false;
            }
            else
            {
                UnityMcpBridge.Start();
                isUnityBridgeRunning = true;
            }
        }

        private string WriteToConfig(string pythonDir, string configPath)
        {
            // Create configuration object for unityMCP
            // Build arguments list
            var argsList = new List<string>
            {
                "--directory", pythonDir, 
                "run", "server.py",
                "--unity-port", McpSettings.Instance.UnityPort.ToString(),
                "--mcp-port", McpSettings.Instance.McpPort.ToString()
            };
            
            // Add debug flag if enabled
            if (McpSettings.Instance.DebugLogging)
            {
                argsList.Add("--debug");
            }
            
            McpConfigServer unityMCPConfig = new()
            {
                command = "uv",
                args = argsList.ToArray(),
            };

            JsonSerializerSettings jsonSettings = new() { Formatting = Formatting.Indented };

            // Read existing config if it exists
            string existingJson = "{}";
            if (File.Exists(configPath))
            {
                try
                {
                    existingJson = File.ReadAllText(configPath);
                }
                catch (Exception e)
                {
                    McpLogger.LogWarning($"Error reading existing config: {e.Message}.");
                }
            }

            // Parse the existing JSON while preserving all properties
            dynamic existingConfig = JsonConvert.DeserializeObject(existingJson);
            existingConfig ??= new Newtonsoft.Json.Linq.JObject();

            // Ensure mcpServers object exists
            if (existingConfig.mcpServers == null)
            {
                existingConfig.mcpServers = new Newtonsoft.Json.Linq.JObject();
            }

            // Add/update unityMCP while preserving other servers
            existingConfig.mcpServers.unityMCP =
                JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JToken>(
                    JsonConvert.SerializeObject(unityMCPConfig)
                );

            // Write the merged configuration back to file
            string mergedJson = JsonConvert.SerializeObject(existingConfig, jsonSettings);
            File.WriteAllText(configPath, mergedJson);

            return "Configured successfully";
        }

        private void ShowManualConfigurationInstructions(string configPath, McpClient mcpClient)
        {
            mcpClient.SetStatus(McpStatus.Error, "Manual configuration required");

            ShowManualInstructionsWindow(configPath, mcpClient);
        }

        // New method to show manual instructions without changing status
        private void ShowManualInstructionsWindow(string configPath, McpClient mcpClient)
        {
            // Get the Python directory path using Package Manager API
            string pythonDir = FindPackagePythonDirectory();

            // Build arguments list for manual config
            var manualArgsList = new List<string>
            {
                "--directory", pythonDir, 
                "run", "server.py",
                "--unity-port", McpSettings.Instance.UnityPort.ToString(),
                "--mcp-port", McpSettings.Instance.McpPort.ToString()
            };
            
            // Add debug flag if enabled
            if (McpSettings.Instance.DebugLogging)
            {
                manualArgsList.Add("--debug");
            }
            
            // Create the manual configuration message
            McpConfig jsonConfig = new()
            {
                mcpServers = new McpConfigServers
                {
                    unityMCP = new McpConfigServer
                    {
                        command = "uv",
                        args = manualArgsList.ToArray(),
                    },
                },
            };

            JsonSerializerSettings jsonSettings = new() { Formatting = Formatting.Indented };
            string manualConfigJson = JsonConvert.SerializeObject(jsonConfig, jsonSettings);

            ManualConfigEditorWindow.ShowWindow(configPath, manualConfigJson, mcpClient);
        }

        private string FindPackagePythonDirectory()
        {
            string pythonDir = ServerInstaller.GetServerPath();

            try
            {
                // Try to find the package using Package Manager API
                UnityEditor.PackageManager.Requests.ListRequest request =
                    UnityEditor.PackageManager.Client.List();
                while (!request.IsCompleted) { } // Wait for the request to complete

                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo package in request.Result)
                    {
                        if (package.name == "com.justinpbarnett.unity-mcp")
                        {
                            string packagePath = package.resolvedPath;
                            string potentialPythonDir = Path.Combine(packagePath, "Python");

                            if (
                                Directory.Exists(potentialPythonDir)
                                && File.Exists(Path.Combine(potentialPythonDir, "server.py"))
                            )
                            {
                                return potentialPythonDir;
                            }
                        }
                    }
                }
                else if (request.Error != null)
                {
                    McpLogger.LogError("Failed to list packages: " + request.Error.message);
                }

                // If not found via Package Manager, try manual approaches
                // First check for local installation
                string[] possibleDirs =
                {
                    Path.GetFullPath(Path.Combine(Application.dataPath, "unity-mcp", "Python")),
                };

                foreach (string dir in possibleDirs)
                {
                    if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "server.py")))
                    {
                        return dir;
                    }
                }

                // If still not found, return the placeholder path
                McpLogger.LogWarning("Could not find Python directory, using placeholder path");
            }
            catch (Exception e)
            {
                McpLogger.LogError($"Error finding package path: {e.Message}");
            }

            return pythonDir;
        }

        private string ConfigureMcpClient(McpClient mcpClient)
        {
            try
            {
                // Determine the config file path based on OS
                string configPath;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    configPath = mcpClient.windowsConfigPath;
                }
                else if (
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                )
                {
                    configPath = mcpClient.linuxConfigPath;
                }
                else
                {
                    return "Unsupported OS";
                }

                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));

                // Find the server.py file location
                string pythonDir = ServerInstaller.GetServerPath();

                if (pythonDir == null || !File.Exists(Path.Combine(pythonDir, "server.py")))
                {
                    ShowManualInstructionsWindow(configPath, mcpClient);
                    return "Manual Configuration Required";
                }

                string result = WriteToConfig(pythonDir, configPath);

                // Update the client status after successful configuration
                if (result == "Configured successfully")
                {
                    mcpClient.SetStatus(McpStatus.Configured);
                }

                return result;
            }
            catch (Exception e)
            {
                // Determine the config file path based on OS for error message
                string configPath = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    configPath = mcpClient.windowsConfigPath;
                }
                else if (
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                )
                {
                    configPath = mcpClient.linuxConfigPath;
                }

                ShowManualInstructionsWindow(configPath, mcpClient);
                McpLogger.LogError(
                    $"Failed to configure {mcpClient.name}: {e.Message}\n{e.StackTrace}"
                );
                return $"Failed to configure {mcpClient.name}";
            }
        }

        private void ShowCursorManualConfigurationInstructions(
            string configPath,
            McpClient mcpClient
        )
        {
            mcpClient.SetStatus(McpStatus.Error, "Manual configuration required");

            // Get the Python directory path using Package Manager API
            string pythonDir = FindPackagePythonDirectory();

            // Build arguments list for manual config
            var manualArgsList = new List<string>
            {
                "--directory", pythonDir, 
                "run", "server.py",
                "--unity-port", McpSettings.Instance.UnityPort.ToString(),
                "--mcp-port", McpSettings.Instance.McpPort.ToString()
            };
            
            // Add debug flag if enabled
            if (McpSettings.Instance.DebugLogging)
            {
                manualArgsList.Add("--debug");
            }
            
            // Create the manual configuration message
            McpConfig jsonConfig = new()
            {
                mcpServers = new McpConfigServers
                {
                    unityMCP = new McpConfigServer
                    {
                        command = "uv",
                        args = manualArgsList.ToArray(),
                    },
                },
            };

            JsonSerializerSettings jsonSettings = new() { Formatting = Formatting.Indented };
            string manualConfigJson = JsonConvert.SerializeObject(jsonConfig, jsonSettings);

            ManualConfigEditorWindow.ShowWindow(configPath, manualConfigJson, mcpClient);
        }

        private void CheckMcpConfiguration(McpClient mcpClient)
        {
            try
            {
                string configPath;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    configPath = mcpClient.windowsConfigPath;
                }
                else if (
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                )
                {
                    configPath = mcpClient.linuxConfigPath;
                }
                else
                {
                    mcpClient.SetStatus(McpStatus.UnsupportedOS);
                    return;
                }

                if (!File.Exists(configPath))
                {
                    mcpClient.SetStatus(McpStatus.NotConfigured);
                    return;
                }

                string configJson = File.ReadAllText(configPath);
                McpConfig config = JsonConvert.DeserializeObject<McpConfig>(configJson);

                if (config?.mcpServers?.unityMCP != null)
                {
                    string pythonDir = ServerInstaller.GetServerPath();
                    if (
                        pythonDir != null
                        && Array.Exists(
                            config.mcpServers.unityMCP.args,
                            arg => arg.Contains(pythonDir, StringComparison.Ordinal)
                        )
                    )
                    {
                        mcpClient.SetStatus(McpStatus.Configured);
                    }
                    else
                    {
                        mcpClient.SetStatus(McpStatus.IncorrectPath);
                    }
                }
                else
                {
                    mcpClient.SetStatus(McpStatus.MissingConfig);
                }
            }
            catch (Exception e)
            {
                mcpClient.SetStatus(McpStatus.Error, e.Message);
            }
        }
        
        private void TestTcpConnection()
        {
            McpLogger.Log($"Testing TCP connection to localhost:{McpSettings.Instance.UnityPort}...");
            
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Try to connect to ourselves
                    client.Connect(IPAddress.Loopback, McpSettings.Instance.UnityPort);
                    McpLogger.LogInfo($"Successfully connected to Unity Bridge on port {McpSettings.Instance.UnityPort}");
                    
                    // Send a test ping
                    NetworkStream stream = client.GetStream();
                    byte[] pingData = Encoding.UTF8.GetBytes("ping");
                    stream.Write(pingData, 0, pingData.Length);
                    McpLogger.Log("Sent ping command");
                    
                    // Read response
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        McpLogger.Log($"Received response: {response}");
                    }
                    else
                    {
                        McpLogger.LogWarning("No response received");
                    }
                }
            }
            catch (SocketException ex)
            {
                McpLogger.LogError($"Socket error during connection test: {ex.Message}");
                McpLogger.LogError($"Error code: {ex.SocketErrorCode}");
                
                if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    McpLogger.LogError("Connection refused - Unity Bridge may not be listening properly");
                }
                else if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    McpLogger.LogError("Port already in use by another process");
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error during connection test: {ex.Message}");
                McpLogger.LogError($"Exception type: {ex.GetType().Name}");
            }
        }
    }
}
