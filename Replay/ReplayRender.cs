// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Zetori.Replay
{
    internal sealed class ReplayRender : ObjectComponent
    {
        private const string ObjectName = "_Zetori Render Camera";
        private const string AudioName = "audio.wav";
        private const int DepthBits = 24;
        private const int MaximumPendingFrames = 4;
        private const int FramesPerCapture = 2;
        private const float TimeTolerance = 0.0001f;
        private const int NoFrame = -1;

        private static readonly List<Task> pending = new List<Task>();

        private static ReplayPlayer? replay;
        private static Camera? capturing;
        private static GameObject? cameraObject;
        private static RenderTexture? texture;
        private static Texture2D? readback;
        private static string folder = string.Empty;
        private static float start;
        private static float frameDuration;
        private static int next;
        private static int renderedFrame = NoFrame;
        private static float renderedAt;
        private static int previousCaptureFramerate;

        internal static bool Active => replay != null;
        internal static int FramesDone => next;
        internal static int FramesTotal { get; private set; }
        internal static string? LastFolder { get; private set; }

        internal static bool Renders(Camera camera) => capturing != null && camera == capturing;

        internal static (int Width, int Height) Size(RenderResolution resolution) => resolution switch
        {
            RenderResolution.P1080 => (1920, 1080),
            RenderResolution.P1440 => (2560, 1440),
            _ => (3840, 2160)
        };

        internal static void Start(ReplayPlayer player)
        {
            if (Active) return;
            CameraWrapper? output = CameraManager.camera;
            if (output == null || !output.IsAlive)
            {
                Log.Warning("There is no camera to render the replay with yet.");
                return;
            }
            int frameRate = CasterConfig.ReplayRenderFrameRate.Value;
            start = player.RangeStart;
            frameDuration = 1f / frameRate;
            FramesTotal = Mathf.FloorToInt((player.RangeEnd - start) * frameRate) + 1;
            next = 0;
            renderedFrame = NoFrame;
            folder = UniqueFolder(player.Name);
            Directory.CreateDirectory(folder);
            float end = player.RangeEnd;
            string audioPath = Path.Combine(folder, AudioName);
            ReplayRecording recording = player.Recording;
            pending.Add(Task.Run(() => RenderAudio.Write(audioPath, recording, start, end)));
            BuildCamera(output, Size(CasterConfig.ReplayRenderResolution.Value));
            replay = player;
            player.Rendering = true;
            player.Playing = true;
            player.Seek(start);
            previousCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = frameRate * FramesPerCapture;
            Log.Info($"Rendering {FramesTotal} frames of replay '{player.Name}'.");
        }

        internal static void Cancel()
        {
            if (!Active) return;
            Log.Info("Rendering the replay was cancelled.");
            Finish();
        }

        private static void BuildCamera(CameraWrapper output, (int Width, int Height) size)
        {
            texture = new RenderTexture(size.Width, size.Height, DepthBits, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = ObjectName };
            readback = new Texture2D(size.Width, size.Height, TextureFormat.RGB24, false);
            cameraObject = new GameObject(ObjectName);
            DontDestroyOnLoad(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(output.Camera);
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.targetTexture = texture;
            camera.ResetAspect();
            camera.enabled = true;
            capturing = camera;
        }

        protected override void Awake()
        {
            base.Awake();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        internal void LateUpdate()
        {
            ObservePending();
            if (replay == null) return;
            try { Advance(replay); }
            catch (Exception e)
            {
                Log.ExceptionOnce("replay-render", e, "Rendering the replay failed");
                Finish();
            }
        }

        private static void Advance(ReplayPlayer player)
        {
            if (ReplaySession.Player != player)
            {
                Cancel();
                return;
            }
            if (next >= FramesTotal)
            {
                if (pending.Count > 0) return;
                LastFolder = Path.GetFileName(folder);
                Log.Info($"Rendered {FramesTotal} frames to '{folder}'.");
                Finish();
                return;
            }
            if (renderedFrame != Time.frameCount - 1 || Mathf.Abs(renderedAt - TimeOf(next)) > TimeTolerance) return;
            if (pending.Count >= MaximumPendingFrames) return;
            Capture(next);
            next++;
            if (next < FramesTotal) player.Seek(TimeOf(next));
        }

        private static float TimeOf(int frame) => start + frame * frameDuration;

        private static void Capture(int frame)
        {
            if (texture == null || readback == null) return;
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = texture;
                readback.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            }
            finally { RenderTexture.active = previous; }
            byte[] pixels = readback.GetRawTextureData();
            GraphicsFormat format = readback.graphicsFormat;
            uint width = (uint)readback.width, height = (uint)readback.height;
            string path = Path.Combine(folder, $"frame_{frame:000000}.png");
            pending.Add(Task.Run(() => File.WriteAllBytes(path, ImageConversion.EncodeArrayToPNG(pixels, format, width, height))));
        }

        private static void ObservePending()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                Task task = pending[i];
                if (!task.IsCompleted) continue;
                if (task.IsFaulted) Log.Exception(task.Exception!.GetBaseException(), "Writing a rendered replay file failed");
                pending.RemoveAt(i);
            }
        }

        private static string UniqueFolder(string replayName)
        {
            string stamp = $"{replayName} {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture)}";
            string name = stamp;
            for (int copy = 2; Directory.Exists(Path.Combine(DataFolders.Renders, name)); copy++) name = $"{stamp} ({copy})";
            return Path.Combine(DataFolders.Renders, name);
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (capturing == null || camera != capturing) return;
            CameraWrapper? output = CameraManager.camera;
            if (output == null || !output.IsAlive) return;
            capturing.transform.SetPositionAndRotation(output.Transform.position, output.Transform.rotation);
            capturing.fieldOfView = output.Camera.fieldOfView;
            capturing.nearClipPlane = output.Camera.nearClipPlane;
            capturing.cullingMask = output.Camera.cullingMask;
        }

        private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (capturing == null || camera != capturing || replay == null) return;
            renderedFrame = Time.frameCount;
            renderedAt = replay.PosedAt;
        }

        private static void Finish()
        {
            Time.captureFramerate = previousCaptureFramerate;
            if (replay != null)
            {
                replay.Rendering = false;
                replay.Playing = false;
            }
            replay = null;
            Release();
        }

        private static void Release()
        {
            if (capturing != null) capturing.targetTexture = null;
            capturing = null;
            if (cameraObject != null) Destroy(cameraObject);
            cameraObject = null;
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
            texture = null;
            if (readback != null) Destroy(readback);
            readback = null;
        }

        protected override void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            if (Active) Finish();
            base.OnDestroy();
        }
    }
}
