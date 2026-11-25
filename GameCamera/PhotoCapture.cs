// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Zetori.Base;
using Zetori.GameCamera.Vr;

namespace Zetori.GameCamera
{
    internal sealed class PhotoCapture : ObjectComponent
    {
        private const string ObjectName = "_Zetori Photo Camera";
        private const string Extension = ".png";
        private const int Width = 3840;
        private const int Height = 2160;
        private const int DepthBits = 24;
        private const int MaximumWaitFrames = 5;
        private const float NotCounting = -1f;
        private const float TickStrength = 0.1f;
        private const float TickSeconds = 0.03f;
        private const float ShutterStrength = 0.35f;
        private const float ShutterSeconds = 0.08f;

        private static float countdownEnds = NotCounting;
        private static Camera? shooting;
        private static Task? saving;
        private static string? savingName;

        private RenderTexture? texture;
        private GameObject? cameraObject;
        private int shotFrame;
        private bool rendered;
        private int lastTick;

        internal static bool CountingDown => countdownEnds >= 0f;
        internal static int SecondsLeft => CountingDown ? Mathf.CeilToInt(Mathf.Max(0f, countdownEnds - Time.unscaledTime)) : 0;
        internal static bool Busy => CountingDown || shooting != null || saving != null;
        internal static string? LastSaved { get; private set; }

        internal static void Take()
        {
            if (Busy) return;
            countdownEnds = Time.unscaledTime + CasterConfig.PhotoTimer.Value;
        }

        internal static void Cancel() => countdownEnds = NotCounting;

        internal static bool Renders(Camera camera) => shooting != null && camera == shooting;

        protected override void Awake()
        {
            base.Awake();
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        internal void Update()
        {
            if (CasterConfig.KeyPressed(CasterConfig.PhotoKey)) Take();
        }

        internal void LateUpdate()
        {
            PollSaving();
            try
            {
                if (shooting != null) Develop();
                else if (CountingDown) CountDown();
            }
            catch (Exception e)
            {
                Log.ExceptionOnce("photo", e, "Taking the photo failed");
                Cancel();
                Release();
            }
        }

        private void CountDown()
        {
            int left = SecondsLeft;
            if (left != lastTick)
            {
                lastTick = left;
                if (left > 0) Vibrate(TickStrength, TickSeconds);
            }
            if (Time.unscaledTime < countdownEnds) return;
            Cancel();
            Shoot();
        }

        private void Shoot()
        {
            CameraWrapper? output = CameraManager.camera;
            if (output == null || !output.IsAlive)
            {
                Log.Warning("There is no camera to take a photo with yet.");
                return;
            }
            texture = new RenderTexture(Width, Height, DepthBits, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = ObjectName };
            cameraObject = new GameObject(ObjectName);
            DontDestroyOnLoad(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(output.Camera);
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.targetTexture = texture;
            camera.ResetAspect();
            camera.enabled = true;
            shooting = camera;
            shotFrame = Time.frameCount;
            rendered = false;
            Vibrate(ShutterStrength, ShutterSeconds);
        }

        private void Develop()
        {
            if (!rendered && Time.frameCount - shotFrame < MaximumWaitFrames) return;
            if (rendered && texture != null) Save(Encode(texture));
            else Log.Warning("The photo camera did not render, so no photo was saved.");
            Release();
        }

        private static byte[] Encode(RenderTexture source)
        {
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = source;
                image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                image.Apply(false);
                return image.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previous;
                Destroy(image);
            }
        }

        private static void Save(byte[] png)
        {
            Directory.CreateDirectory(DataFolders.Photos);
            string name = UniqueName();
            string path = Path.Combine(DataFolders.Photos, name + Extension);
            savingName = name;
            saving = Task.Run(() => File.WriteAllBytes(path, png));
        }

        private static string UniqueName()
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);
            string name = stamp;
            for (int copy = 2; File.Exists(Path.Combine(DataFolders.Photos, name + Extension)); copy++) name = $"{stamp} ({copy})";
            return name;
        }

        private static void PollSaving()
        {
            if (saving == null || !saving.IsCompleted) return;
            if (saving.IsFaulted) Log.Exception(saving.Exception!.GetBaseException(), $"Saving photo '{savingName}' failed");
            else
            {
                Log.Info($"Saved photo '{savingName}'.");
                LastSaved = savingName;
            }
            saving = null;
            savingName = null;
        }

        private static void Vibrate(float strength, float seconds)
        {
            GorillaTagger? tagger = GorillaTagger.Instance;
            if (tagger != null && VrCameraRig.HeadsetActive) tagger.StartVibration(false, strength, seconds);
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (shooting != null && camera == shooting) rendered = true;
        }

        private void Release()
        {
            if (shooting != null) shooting.targetTexture = null;
            shooting = null;
            if (cameraObject != null) Destroy(cameraObject);
            cameraObject = null;
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
            texture = null;
        }

        protected override void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            Release();
            base.OnDestroy();
        }
    }
}
