using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ScreenshotTool
    {
        public static Dictionary<string, object> TakeScreenshot(Dictionary<string, object> parameters)
        {
            try
            {
                string view = parameters.ContainsKey("view") ? parameters["view"].ToString() : null;
                string saveToPath = null;
                if (parameters.ContainsKey("save_to_path"))
                    saveToPath = parameters["save_to_path"].ToString();
                else if (parameters.ContainsKey("savePath"))
                    saveToPath = parameters["savePath"].ToString();

                string format = parameters.ContainsKey("format") ? parameters["format"].ToString().ToLower() : "png";

                if (string.IsNullOrEmpty(view))
                {
                    view = EditorApplication.isPlaying ? "game" : "scene";
                }

                if (view.Equals("scene", StringComparison.OrdinalIgnoreCase))
                {
                    var sceneView = SceneView.lastActiveSceneView;
                    if (sceneView == null)
                    {
                        throw new Exception("No active Scene View found to capture.");
                    }
                    sceneView.Focus();
                }

                string capturePath;
                if (saveToPath != null)
                {
                    capturePath = saveToPath.StartsWith("Assets") 
                        ? Path.Combine(Directory.GetCurrentDirectory(), saveToPath) 
                        : saveToPath;
                    Directory.CreateDirectory(Path.GetDirectoryName(capturePath));
                }
                else
                {
                    capturePath = Path.Combine(Path.GetTempPath(), $"screenshot_{Path.GetRandomFileName()}.{format}");
                }

                ScreenCapture.CaptureScreenshot(capturePath);

                // Wait for the file to be created
                int timeoutMs = 5000;
                int intervalMs = 100;
                int elapsedMs = 0;
                while (!File.Exists(capturePath) && elapsedMs < timeoutMs)
                {
                    Thread.Sleep(intervalMs);
                    elapsedMs += intervalMs;
                }

                if (!File.Exists(capturePath))
                {
                    throw new Exception($"Screenshot file was not created at '{capturePath}' within {timeoutMs}ms.");
                }

                if (saveToPath != null)
                {
                    if(saveToPath.StartsWith("Assets")) {
                        AssetDatabase.Refresh();
                    }
                    // If saving to a file, report success, the path, and dummy image data
                    Texture2D placeholder = new Texture2D(1, 1);
                    placeholder.SetPixel(0, 0, Color.black);
                    placeholder.Apply();
                    byte[] placeholderBytes = placeholder.EncodeToPNG();
                    UnityEngine.Object.DestroyImmediate(placeholder);

                    var savedResponseData = new Dictionary<string, object>
                    {
                        { "savedPath", saveToPath },
                        { "imageData", Convert.ToBase64String(placeholderBytes) }
                    };
                    return new Dictionary<string, object>
                    {
                        { "success", true },
                        { "data", savedResponseData }
                    };
                }
                
                // If not saving to a file, read the bytes and return them
                byte[] imageBytes = File.ReadAllBytes(capturePath);
                File.Delete(capturePath); // Delete temp file

                var responseData = new Dictionary<string, object>
                {
                    { "imageData", Convert.ToBase64String(imageBytes) },
                    { "view", view },
                    { "isPlayMode", EditorApplication.isPlaying },
                    { "width", Screen.width },
                    { "height", Screen.height },
                    { "format", format }
                };

                return new Dictionary<string, object>
                {
                    { "success", true },
                    { "data", responseData }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", $"Failed to take screenshot: {e.Message}" }
                };
            }
        }
    }
}