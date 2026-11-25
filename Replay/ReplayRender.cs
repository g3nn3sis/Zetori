// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zetori.Replay
{
    internal sealed class ReplayRender : ObjectComponent
    {
        private const string ObjectName = "_Zetori Render Camera";
        private const string VideoExtension = ".mp4";
        private const string AudioExtension = ".wav";
        private const string TemporaryAudioPrefix = "zetori-render-";
        private const int DepthBits = 24;
        private const int MaximumPendingFrames = 4;
        private const int FramesPerCapture = 2;
        private const float TimeTolerance = 0.0001f;
        private const int NoFrame = -1;

        private static ReplayPlayer? replay;
        private static Camera? capturing;
        private static GameObject? cameraObject;
        private static RenderTexture? texture;
        private static Texture2D? readback;
        private static Task<VideoEncoder>? preparing;
        private static VideoEncoder? encoder;
        private static Task? finishing;
        private static string output = string.Empty;
        private static string audioPath = string.Empty;
        private static float start;
        private static float frameDuration;
        private static int next;
        private static int renderedFrame = NoFrame;
        private static float renderedAt;
        private static int previousCaptureFramerate;

        internal static bool Active => replay != null;
        internal static bool Preparing => preparing != null;
        internal static bool Finishing => finishing != null;
        internal static int FramesDone => next;
        internal static int FramesTotal { get; private set; }
        internal static string? LastFile { get; private set; }

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
            CameraWrapper? camera = CameraManager.camera;
            if (camera == null || !camera.IsAlive)
            {
                Log.Warning("There is no camera to render the replay with yet.");
                return;
            }
            int frameRate = CasterConfig.ReplayRenderFrameRate.Value;
            (int Width, int Height) size = Size(CasterConfig.ReplayRenderResolution.Value);
            try
            {
                BuildCamera(camera, size);
            }
            catch (Exception e)
            {
                Release();
                Log.Exception(e, "Couldn't create the render camera");
                return;
            }
            start = player.RangeStart;
            frameDuration = 1f / frameRate;
            FramesTotal = Mathf.FloorToInt((player.RangeEnd - start) * frameRate) + 1;
            next = 0;
            renderedFrame = NoFrame;
            output = ReserveFile(player.Name);
            audioPath = Path.Combine(Path.GetTempPath(), $"{TemporaryAudioPrefix}{Guid.NewGuid():N}{AudioExtension}");
            preparing = Prepare(DataFolders.Tools, new VideoFormat(size.Width, size.Height, frameRate), player.Recording, start, player.RangeEnd, audioPath, output);
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
            Abandon();
            Finish();
        }

        private static Task<VideoEncoder> Prepare(string tools, VideoFormat format, ReplayRecording recording, float from, float to, string audio, string video)
        {
            return Task.Run(() =>
            {
                RenderAudio.Write(audio, recording, from, to);
                return VideoEncoder.Start(BundledFFmpeg.Extract(tools), format, audio, video);
            });
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
            if (replay == null) return;
            try { Advance(replay); }
            catch (Exception e)
            {
                Log.ExceptionOnce("replay-render", e, "Rendering the replay failed");
                Abandon();
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
            if (encoder == null) TakeEncoder();
            if (encoder == null) return;
            if (finishing != null)
            {
                if (finishing.IsCompleted) Complete(finishing, encoder);
                return;
            }
            if (encoder.Faulted || next >= FramesTotal)
            {
                finishing = encoder.Finish();
                return;
            }
            if (renderedFrame != Time.frameCount - 1 || Mathf.Abs(renderedAt - TimeOf(next)) > TimeTolerance) return;
            if (encoder.Pending >= MaximumPendingFrames) return;
            Capture(encoder);
            next++;
            if (next < FramesTotal) player.Seek(TimeOf(next));
        }

        private static void TakeEncoder()
        {
            if (preparing == null || !preparing.IsCompleted) return;
            Task<VideoEncoder> prepared = preparing;
            preparing = null;
            if (prepared.IsFaulted)
            {
                Log.Exception(prepared.Exception!.GetBaseException(), "Couldn't start the video encoder");
                DeleteQuietly(audioPath);
                DeleteQuietly(output);
                Finish();
                return;
            }
            encoder = prepared.Result;
        }

        private static void Complete(Task finished, VideoEncoder done)
        {
            if (finished.IsFaulted) Log.Exception(finished.Exception!.GetBaseException(), "Encoding the replay video failed");
            else
            {
                LastFile = Path.GetFileName(output);
                Log.Info($"Rendered {FramesTotal} frames to '{output}'.");
            }
            try { done.Dispose(); }
            finally
            {
                DeleteQuietly(audioPath);
                Finish();
            }
        }

        private static float TimeOf(int frame) => start + frame * frameDuration;

        private static void Capture(VideoEncoder target)
        {
            if (texture == null || readback == null) return;
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = texture;
                readback.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            }
            finally { RenderTexture.active = previous; }
            byte[] frame = target.RentFrame();
            readback.GetRawTextureData<byte>().CopyTo(frame);
            target.Write(frame);
        }

        private static void Abandon()
        {
            string audio = audioPath;
            string video = output;
            try
            {
                if (encoder != null) AbandonEncoder(encoder, finishing, audio, video);
                else preparing?.ContinueWith(prepared => AbandonPrepared(prepared, audio, video), TaskScheduler.Default);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Stopping the video encoder failed");
            }
        }

        private static void AbandonEncoder(VideoEncoder abandoned, Task? finished, string audio, string video)
        {
            try
            {
                if (!abandoned.Abort()) Log.Warning($"Couldn't delete the cancelled render '{video}'.");
            }
            finally
            {
                if (finished == null) DisposeEncoder(abandoned, audio);
                else finished.ContinueWith(task => ReleaseAfterFinish(task, abandoned, audio), TaskScheduler.Default);
            }
        }

        private static void ReleaseAfterFinish(Task finished, VideoEncoder abandoned, string audio)
        {
            if (finished.Exception != null) Log.Debug($"The cancelled render's encoder stopped with: {finished.Exception.GetBaseException().Message}");
            DisposeEncoder(abandoned, audio);
        }

        private static void DisposeEncoder(VideoEncoder abandoned, string audio)
        {
            try { abandoned.Dispose(); }
            finally { DeleteQuietly(audio); }
        }

        private static void AbandonPrepared(Task<VideoEncoder> prepared, string audio, string video)
        {
            if (prepared.Status == TaskStatus.RanToCompletion) DisposeEncoder(prepared.Result, audio);
            else
            {
                if (prepared.Exception != null) Log.Debug($"The cancelled render's encoder did not start: {prepared.Exception.GetBaseException().Message}");
                DeleteQuietly(audio);
                DeleteQuietly(video);
            }
        }

        private static void DeleteQuietly(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException e)
            {
                Log.Warning($"Couldn't delete '{path}': {e.Message}");
            }
            catch (UnauthorizedAccessException e)
            {
                Log.Warning($"Couldn't delete '{path}': {e.Message}");
            }
        }

        private static string ReserveFile(string replayName)
        {
            Directory.CreateDirectory(DataFolders.Renders);
            string stamp = $"{replayName} {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture)}";
            string name = stamp;
            for (int copy = 2; File.Exists(Path.Combine(DataFolders.Renders, name + VideoExtension)); copy++) name = $"{stamp} ({copy})";
            string path = Path.Combine(DataFolders.Renders, name + VideoExtension);
            File.Create(path).Dispose();
            return path;
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
            preparing = null;
            encoder = null;
            finishing = null;
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
            Cancel();
            base.OnDestroy();
        }
    }
}
