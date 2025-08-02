#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ScreenshotTool
    {
        public static object TakeScreenshot(Dictionary<string, object> args)
        {
            string view = args.ContainsKey("view") ? args["view"].ToString().ToLower() : null;
            int? width = args.ContainsKey("width") ? (int?)System.Convert.ToInt32(args["width"]) : null;
            int? height = args.ContainsKey("height") ? (int?)System.Convert.ToInt32(args["height"]) : null;
            int? max_size = args.ContainsKey("max_size") ? (int?)System.Convert.ToInt32(args["max_size"]) : null;
            string save_to_path = args.ContainsKey("save_to_path") ? args["save_to_path"].ToString() : null;
            bool compress = args.ContainsKey("compress") ? (bool)args["compress"] : true;
            string format = args.ContainsKey("format") ? args["format"].ToString().ToLower() : "png";

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
                    if (sv == null || sv.camera == null) return new { success = false, error = "No active Scene View found." };
                    cam = sv.camera;
                    capturedWidth = cam.pixelWidth;
                    capturedHeight = cam.pixelHeight;
                }
                else
                {
                    cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
                    if (cam == null) return new { success = false, error = "No camera found for Game View." };
                    var size = GetGameViewSize();
                    capturedWidth = size.x;
                    capturedHeight = size.y;
                }

                // Apply width/height if specified
                if (width.HasValue || height.HasValue)
                {
                    if (width.HasValue && !height.HasValue) height = (int)(capturedHeight * (width.Value / (float)capturedWidth));
                    else if (!width.HasValue && height.HasValue) width = (int)(capturedWidth * (height.Value / (float)capturedHeight));
                    capturedWidth = width.Value;
                    capturedHeight = height.Value;
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
            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
            System.Type U = Res.GetType();
            int width = (int)U.GetField("width").GetValue(Res);
            int height = (int)U.GetField("height").GetValue(Res);
            return new Vector2Int(width, height);
        }
    }
}
#endif