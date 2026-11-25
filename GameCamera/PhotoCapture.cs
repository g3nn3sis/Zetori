// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Zetori.Base;
using Zetori.GameCamera.Vr;
using Zetori.Player;
using Zetori.Replay;

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

        private static readonly Color GreenScreen = new Color32(0, 177, 64, 255);
        private static readonly Color BlueScreen = new Color32(0, 71, 187, 255);

        private sealed class Shot : IDisposable
        {
            private readonly GameObject holder;

            internal Camera Camera { get; }
            internal RenderTexture Texture { get; }
            internal bool Rendered { get; set; }

            internal Shot(Camera source, Color? background)
            {
                Texture = new RenderTexture(Width, Height, DepthBits, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = ObjectName };
                holder = new GameObject(ObjectName);
                DontDestroyOnLoad(holder);
                Camera = holder.AddComponent<Camera>();
                Camera.CopyFrom(source);
                Camera.stereoTargetEye = StereoTargetEyeMask.None;
                Camera.targetTexture = Texture;
                Camera.ResetAspect();
                if (background is Color color)
                {
                    Camera.clearFlags = CameraClearFlags.SolidColor;
                    Camera.backgroundColor = color;
                }
                Camera.enabled = true;
            }

            public void Dispose()
            {
                if (Camera != null) Camera.targetTexture = null;
                if (holder != null) Destroy(holder);
                if (Texture == null) return;
                Texture.Release();
                Destroy(Texture);
            }
        }

        private static readonly List<Shot> shots = new List<Shot>();
        private static float countdownEnds = NotCounting;
        private static PlayerIsolation? isolation;
        private static bool transparent;
        private static Task? saving;
        private static string? savingName;

        private int shotFrame;
        private int lastTick;

        internal static bool CountingDown => countdownEnds >= 0f;
        internal static int SecondsLeft => CountingDown ? Mathf.CeilToInt(Mathf.Max(0f, countdownEnds - Time.unscaledTime)) : 0;
        internal static bool Busy => CountingDown || shots.Count > 0 || saving != null;
        internal static string? LastSaved { get; private set; }

        internal static void Take()
        {
            if (Busy) return;
            countdownEnds = Time.unscaledTime + CasterConfig.PhotoTimer.Value;
        }

        internal static void Cancel() => countdownEnds = NotCounting;

        internal static bool Renders(Camera camera) => shots.Exists(shot => shot.Camera == camera);

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
                if (shots.Count > 0) Develop();
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
            PhotoBackground background = CasterConfig.PhotoBackground.Value;
            transparent = false;
            if (background == PhotoBackground.Scene || !TryIsolate(output, background)) shots.Add(new Shot(output.Camera, null));
            shotFrame = Time.frameCount;
            Vibrate(ShutterStrength, ShutterSeconds);
        }

        private static bool TryIsolate(CameraWrapper output, PhotoBackground background)
        {
            Transform? target = CutoutTarget();
            if (target == null)
            {
                Log.Warning("There is no spectated player to cut out, so the photo shows the whole scene.");
                return false;
            }
            Shot[] taken = background == PhotoBackground.Transparent
                ? new[] { new Shot(output.Camera, Color.black), new Shot(output.Camera, Color.white) }
                : new[] { new Shot(output.Camera, background == PhotoBackground.Green ? GreenScreen : BlueScreen) };
            isolation = PlayerIsolation.TryCreate(target, taken.Select(shot => shot.Camera).ToArray());
            if (isolation == null)
            {
                foreach (Shot shot in taken) shot.Dispose();
                Log.Warning("The player could not be cut out, so the photo shows the whole scene.");
                return false;
            }
            shots.AddRange(taken);
            transparent = background == PhotoBackground.Transparent;
            return true;
        }

        private static Transform? CutoutTarget()
        {
            if (ReplaySession.Player is ReplayPlayer replay) return replay.Spectated?.Body;
            VRRig? rig = NetworkedPlayerManager.selectedPlayer;
            return rig != null ? rig.transform : null;
        }

        private void Develop()
        {
            bool rendered = shots.TrueForAll(shot => shot.Rendered);
            if (!rendered && Time.frameCount - shotFrame < MaximumWaitFrames) return;
            if (!rendered) Log.Warning("The photo camera did not render, so no photo was saved.");
            else if (transparent) SaveCutout(Read(shots[0].Texture), Read(shots[1].Texture));
            else
            {
                byte[] png = Encode(shots[0].Texture);
                Save(() => png);
            }
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

        private static byte[] Read(RenderTexture source)
        {
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = source;
                image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                return image.GetRawTextureData();
            }
            finally
            {
                RenderTexture.active = previous;
                Destroy(image);
            }
        }

        private static void SaveCutout(byte[] overBlack, byte[] overWhite) =>
            Save(() => ImageConversion.EncodeArrayToPNG(CutoutMatte.Extract(overBlack, overWhite), GraphicsFormat.R8G8B8A8_SRGB, Width, Height));

        private static void Save(Func<byte[]> encode)
        {
            Directory.CreateDirectory(DataFolders.Photos);
            string name = UniqueName();
            string path = Path.Combine(DataFolders.Photos, name + Extension);
            savingName = name;
            saving = Task.Run(() => File.WriteAllBytes(path, encode()));
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
            foreach (Shot shot in shots)
                if (shot.Camera == camera) shot.Rendered = true;
        }

        private static void Release()
        {
            isolation?.Dispose();
            isolation = null;
            foreach (Shot shot in shots) shot.Dispose();
            shots.Clear();
        }

        protected override void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            Release();
            base.OnDestroy();
        }
    }
}
