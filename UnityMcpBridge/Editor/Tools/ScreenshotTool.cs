using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
                string view = parameters.ContainsKey("view") ? parameters["view"] as string : "game";
                int width = parameters.ContainsKey("width") ? Convert.ToInt32(parameters["width"]) : 1920;
                int height = parameters.ContainsKey("height") ? Convert.ToInt32(parameters["height"]) : 1080;

                Camera camera = GetCameraForView(view);
                if (camera == null)
                {
                    return new Dictionary<string, object>
                    {
                        { "success", false },
                        { "error", $"Could not find a suitable camera for view '{view}'." }
                    };
                }

                byte[] imageBytes = CaptureCameraView(camera, width, height);

                var imageResult = new Dictionary<string, object>
                {
                    { "image", Convert.ToBase64String(imageBytes) },
                    { "format", "png" }
                };

                var metadata = new Dictionary<string, object>
                {
                    { "view", view },
                    { "play_mode", EditorApplication.isPlaying },
                    { "width", width },
                    { "height", height }
                };

                return new Dictionary<string, object>
                {
                    { "success", true },
                    { "data", imageResult },
                    { "metadata", metadata }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", $"Failed to take screenshot: {e.Message}\n{e.StackTrace}" }
                };
            }
        }

        private static Camera GetCameraForView(string view)
        {
            if (view == "scene")
            {
                SceneView sceneView = SceneView.lastActiveSceneView;
                return sceneView != null ? sceneView.camera : null;
            }
            else // game view
            {
                return Camera.main;
            }
        }

        private static byte[] CaptureCameraView(Camera camera, int width, int height)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            camera.targetTexture = renderTexture;
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);

            camera.Render();

            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(renderTexture);

            byte[] bytes = screenshot.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(screenshot);

            return bytes;
        }
    }
}