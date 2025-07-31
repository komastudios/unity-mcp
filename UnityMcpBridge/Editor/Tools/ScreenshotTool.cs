#if UNITY_EDITOR 
 using UnityEditor; 
 using UnityEngine; 
 using System.IO; 
 using System.Reflection; 
 
 public static class CaptureViews 
 { 
     // ===== MENU ===== 
     [MenuItem("Tools/Capture/Game View (content) %#g")] // Shift+Ctrl/Cmd+G 
     private static void CaptureGameView() 
     { 
         var path = AskPath("GameView"); 
         if (string.IsNullOrEmpty(path)) return; 
 
         // Easiest (includes overlay UI) when playing: 
         if (EditorApplication.isPlaying) 
         { 
             ScreenCapture.CaptureScreenshot(path, 1); // writes at end of frame 
             Debug.Log($"Queued Game View screenshot to: {path}"); 
             return; 
         } 
 
         // Off-screen render from a camera when not playing (no overlay UI) 
         var cam = Camera.main ?? (Camera.allCamerasCount > 0 ? Camera.allCameras[0] : Object.FindObjectOfType<Camera>()); 
         if (!cam) 
         { 
             Debug.LogError("No Camera found to render the Game View."); 
             return; 
         } 
 
         var size = GetGameViewSizeOrScreen(); 
         SaveCameraToPng(cam, size.x, size.y, path); 
     } 
 
     [MenuItem("Tools/Capture/Scene View (content) %#s")] // Shift+Ctrl/Cmd+S 
     private static void CaptureSceneView() 
     { 
         var sv = SceneView.lastActiveSceneView; 
         if (sv == null || sv.camera == null) 
         { 
             Debug.LogError("No Scene View is open/active."); 
             return; 
         } 
 
         var path = AskPath("SceneView"); 
         if (string.IsNullOrEmpty(path)) return; 
 
         // Use the scene view's camera size (already accounts for HiDPI) 
         int w = sv.camera.pixelWidth; 
         int h = sv.camera.pixelHeight; 
 
         SaveCameraToPng(sv.camera, w, h, path); 
     } 
 
     // ===== CORE ===== 
     private static void SaveCameraToPng(Camera cam, int width, int height, string path) 
     { 
         var prevRT = cam.targetTexture; 
         var prevActive = RenderTexture.active; 
 
         var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); 
         var tex = new Texture2D(width, height, TextureFormat.RGBA32, false); 
 
         try 
         { 
             cam.targetTexture = rt; 
             cam.Render(); 
 
             RenderTexture.active = rt; 
             tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); 
             tex.Apply(); 
 
             File.WriteAllBytes(path, tex.EncodeToPNG()); 
             AssetDatabase.Refresh(); 
             Debug.Log($"Saved {width}x{height} PNG → {path}"); 
         } 
         finally 
         { 
             cam.targetTexture = prevRT; 
             RenderTexture.active = prevActive; 
             Object.DestroyImmediate(rt); 
             Object.DestroyImmediate(tex); 
         } 
     } 
 
     /// Try to get the exact Game View size (Editor-only, via reflection). Falls back to Screen.width/height. 
     private static Vector2Int GetGameViewSizeOrScreen() 
     { 
         var editorAsm = typeof(Editor).Assembly; 
         var gameViewType = editorAsm.GetType("UnityEditor.GameView"); 
         var gameView = EditorWindow.GetWindow(gameViewType); 
 
         var sizeProp = gameViewType.GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Instance); 
         if (sizeProp != null) 
         { 
             var sizeObj = sizeProp.GetValue(gameView, null); 
             if (sizeObj != null) 
             { 
                 var w = (int)sizeObj.GetType().GetProperty("width").GetValue(sizeObj); 
                 var h = (int)sizeObj.GetType().GetProperty("height").GetValue(sizeObj); 
                 return new Vector2Int(w, h); 
             } 
         } 
 
         return new Vector2Int(Screen.width, Screen.height); 
     } 
 
     private static string AskPath(string prefix) 
     { 
         var name = $"{prefix}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png"; 
         return EditorUtility.SaveFilePanel("Save Screenshot", Application.dataPath, name, "png"); 
     } 
 } 
 #endif