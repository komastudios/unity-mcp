using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMcpBridge.Editor.Models
{
    [Serializable]
    public class McpSettings
    {
        private const string SETTINGS_FILE_NAME = "McpSettings.json";
        private const string SETTINGS_FOLDER = "ProjectSettings";
        
        [SerializeField]
        private int unityPort = 6400;
        
        [SerializeField]
        private int mcpPort = 6500;
        
        public int UnityPort
        {
            get => unityPort;
            set
            {
                if (unityPort != value)
                {
                    unityPort = value;
                    Save();
                }
            }
        }
        
        public int McpPort
        {
            get => mcpPort;
            set
            {
                if (mcpPort != value)
                {
                    mcpPort = value;
                    Save();
                }
            }
        }
        
        private static McpSettings instance;
        
        public static McpSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Load();
                }
                return instance;
            }
        }
        
        private static string SettingsPath => Path.Combine(SETTINGS_FOLDER, SETTINGS_FILE_NAME);
        
        private static McpSettings Load()
        {
            if (!Directory.Exists(SETTINGS_FOLDER))
            {
                Directory.CreateDirectory(SETTINGS_FOLDER);
            }
            
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<McpSettings>(json) ?? new McpSettings();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load MCP settings: {e.Message}");
                    return new McpSettings();
                }
            }
            
            return new McpSettings();
        }
        
        public void Save()
        {
            try
            {
                if (!Directory.Exists(SETTINGS_FOLDER))
                {
                    Directory.CreateDirectory(SETTINGS_FOLDER);
                }
                
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save MCP settings: {e.Message}");
            }
        }
        
        public void ResetToDefaults()
        {
            unityPort = 6400;
            mcpPort = 6500;
            Save();
        }
    }
}