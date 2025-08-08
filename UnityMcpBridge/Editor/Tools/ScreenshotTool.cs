#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ScreenshotTool
    {
        public static object HandleCommand(JObject parameters)
        {
            return TakeScreenshot(parameters);
        }
        public static object TakeScreenshot(JObject parameters)
        {
            string view = parameters["view"]?.ToString()?.ToLower();
            int? width = parameters["width"]?.ToObject<int?>();
            int? height = parameters["height"]?.ToObject<int?>();
            int? max_size = parameters["max_size"]?.ToObject<int?>();
            string save_to_path = parameters["save_to_path"]?.ToString();
            bool compress = parameters["compress"]?.ToObject<bool>() ?? true;
            string format = parameters["format"]?.ToString()?.ToLower() ?? "png";

            try
            {
                byte[] imageBytes = null;
                int capturedWidth = 0;
                int capturedHeight = 0;
                string capturedView = view ?? (EditorApplication.isPlaying ? "game" : "scene");

                Camera cam;
                if (capturedView == "scene")
                {
                    var sv = SceneView.lastActiveSceneView;
                    if (sv == null || sv.camera == null) return new { success = false, error = "No active Scene View found (SceneView.camera is null)." };
                    cam = sv.camera;
                    capturedWidth = Mathf.Max(1, cam.pixelWidth);
                    capturedHeight = Mathf.Max(1, cam.pixelHeight);
                }
                else
                {
                    cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
                    if (cam == null) return new { success = false, error = "No camera found for Game View." };
                    var size = GetGameViewSize();
                    capturedWidth = size.x;
                    capturedHeight = size.y;
                }

                // Fallbacks for invalid dimensions
                if (capturedWidth <= 0 || capturedHeight <= 0)
                {
                    // Try Screen size first, then sensible defaults
                    capturedWidth = Screen.width > 0 ? Screen.width : 1280;
                    capturedHeight = Screen.height > 0 ? Screen.height : 720;
                }

                // Apply width/height if specified
                if (width.HasValue || height.HasValue)
                {
                    if (width.HasValue && !height.HasValue) height = (int)(capturedHeight * (width.Value / (float)Mathf.Max(1, capturedWidth)));
                    else if (!width.HasValue && height.HasValue) width = (int)(capturedWidth * (height.Value / (float)Mathf.Max(1, capturedHeight)));
                    capturedWidth = Mathf.Max(1, width ?? capturedWidth);
                    capturedHeight = Mathf.Max(1, height ?? capturedHeight);
                }

                // Capture
                imageBytes = CaptureCamera(cam, capturedWidth, capturedHeight, format);

                // Apply max_size
                if (max_size.HasValue && (capturedWidth > max_size.Value || capturedHeight > max_size.Value))
                {
                    float scale = Mathf.Min(max_size.Value / (float)capturedWidth, max_size.Value / (float)capturedHeight);
                    int newWidth = (int)(capturedWidth * scale);
                    int newHeight = (int)(capturedHeight * scale);
                    // Would need to resize imageBytes here, but Unity doesn't have built-in resize, skip for now or implement
                }

                // Save if requested
                if (!string.IsNullOrEmpty(save_to_path))
                {
                    File.WriteAllBytes(save_to_path, imageBytes);
                    AssetDatabase.Refresh();
                }

                // Compress if requested (for PNG it's already compressed, for JPEG can adjust quality)
                // For now, assume PNG is compressed

                string base64 = System.Convert.ToBase64String(imageBytes);

                return new { success = true, data = new { imageData = base64, view = capturedView, width = capturedWidth, height = capturedHeight, isPlayMode = EditorApplication.isPlaying } };
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
        }

        private static byte[] CaptureCamera(Camera cam, int width, int height, string format)
        {
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes;
            if (format == "jpg" || format == "jpeg")
                bytes = tex.EncodeToJPG();
            else
                bytes = tex.EncodeToPNG();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);

            return bytes;
        }

        private static Vector2Int GetGameViewSize()
        {
            try
            {
                System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
                System.Type U = Res.GetType();
                int width = (int)U.GetField("width").GetValue(Res);
                int height = (int)U.GetField("height").GetValue(Res);
                if (width <= 0 || height <= 0)
                {
                    width = Screen.width > 0 ? Screen.width : 1280;
                    height = Screen.height > 0 ? Screen.height : 720;
                }
                return new Vector2Int(width, height);
            }
            catch
            {
                int w = Screen.width > 0 ? Screen.width : 1280;
                int h = Screen.height > 0 ? Screen.height : 720;
                return new Vector2Int(w, h);
            }
        }
    }
}
#endif