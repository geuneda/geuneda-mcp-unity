using System;
using System.Threading.Tasks;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for capturing screenshots of the Unity Game View or Scene View
    /// </summary>
    public class CaptureScreenshotTool : McpToolBase
    {
        public CaptureScreenshotTool()
        {
            Name = "capture_screenshot";
            Description = "Captures a screenshot of the Unity Game View or Scene View and returns it as a base64-encoded PNG image";
            IsAsync = true;
        }

        /// <summary>
        /// Executes the CaptureScreenshot tool asynchronously on the main thread.
        /// </summary>
        /// <param name="parameters">Tool parameters including viewType, width, height, superSize.</param>
        /// <param name="tcs">TaskCompletionSource to set the result or exception.</param>
        public override async void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            try
            {
                string viewType = parameters?["viewType"]?.ToObject<string>() ?? "game";
                int width = parameters?["width"]?.ToObject<int>() ?? 0;
                int height = parameters?["height"]?.ToObject<int>() ?? 0;
                int superSize = parameters?["superSize"]?.ToObject<int>() ?? 1;

                if (superSize < 1) superSize = 1;

                McpLogger.LogInfo($"Executing CaptureScreenshotTool: viewType={viewType}, width={width}, height={height}, superSize={superSize}");

                string base64;
                int capturedWidth;
                int capturedHeight;

                if (viewType.Equals("scene", StringComparison.OrdinalIgnoreCase))
                {
                    (base64, capturedWidth, capturedHeight) = CaptureSceneView(width, height);
                }
                else
                {
                    // Wait one frame for the Game View to render
                    await Task.Yield();
                    (base64, capturedWidth, capturedHeight) = CaptureGameView(superSize);
                }

                var result = new JObject
                {
                    ["success"] = true,
                    ["type"] = "image",
                    ["imageBase64"] = base64,
                    ["mimeType"] = "image/png",
                    ["message"] = $"Captured {viewType} view screenshot ({capturedWidth}x{capturedHeight})"
                };

                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"CaptureScreenshotTool failed: {ex.Message}");
                tcs.TrySetResult(McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to capture screenshot: {ex.Message}",
                    "screenshot_error"
                ));
            }
        }

        private (string base64, int width, int height) CaptureGameView(int superSize)
        {
            var texture = ScreenCapture.CaptureScreenshotAsTexture(superSize);
            if (texture == null)
            {
                throw new InvalidOperationException("Failed to capture Game View screenshot. Ensure the Game View is visible.");
            }

            try
            {
                int w = texture.width;
                int h = texture.height;
                byte[] pngData = texture.EncodeToPNG();
                string base64 = Convert.ToBase64String(pngData);
                return (base64, w, h);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private (string base64, int width, int height) CaptureSceneView(int width, int height)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                throw new InvalidOperationException("No active Scene View found. Please open a Scene View window.");
            }

            var camera = sceneView.camera;
            int w = width > 0 ? width : (int)sceneView.position.width;
            int h = height > 0 ? height : (int)sceneView.position.height;

            var rt = new RenderTexture(w, h, 24);
            camera.targetTexture = rt;
            camera.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;
            camera.targetTexture = null;

            try
            {
                byte[] pngData = tex.EncodeToPNG();
                string base64 = Convert.ToBase64String(pngData);
                return (base64, w, h);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
