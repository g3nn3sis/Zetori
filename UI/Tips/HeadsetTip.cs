// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using TMPro;
using UnityEngine;
using Zetori.Base;
using Zetori.Base.Config;
using Zetori.GameCamera;
using Zetori.GameCamera.Vr;
using Zetori.Hud;
using Zetori.Replay;

namespace Zetori.UI.Tips
{
    internal sealed class HeadsetTip : ObjectComponent
    {
        private const string PanelLine = "Double-press X on your left controller to open Zetori";
        private const string TabletLine = "Double-press A on your right controller for the camera tablet";
        private const float Distance = 0.8f;
        private const float Height = -0.08f;
        private const float RowPitch = 0.04f;
        private const float FontSize = 0.2f;
        private const float FirstDelay = 4f;
        private const float ShownSeconds = 12f;
        private const float RepeatSeconds = 90f;
        private const float FadeSeconds = 0.6f;

        private static readonly Vector2 RowSize = new Vector2(1.2f, RowPitch);
        private static readonly Vector3 Position = new Vector3(0f, Height, Distance);

        private HeadsetText? text;
        private float eligibleSince = float.NegativeInfinity;

        protected override void Awake()
        {
            base.Awake();
            text = new HeadsetText($"_{PluginInfo.Name} Headset Tip", "Tip", RowSize, RowPitch, FontSize);
        }

        internal void LateUpdate()
        {
            try { Show(); }
            catch (Exception e) { Log.ExceptionOnce("headset-tip", e, "Showing the headset tip failed"); }
        }

        private void Show()
        {
            if (text == null) return;
            GTPlayer? player = GTPlayer.Instance;
            Camera? headset = player != null ? player.mainCamera : null;
            text.Headset = headset;
            float now = Time.unscaledTime;
            if (headset == null || !Eligible)
            {
                eligibleSince = float.NegativeInfinity;
                text.Hide();
                return;
            }
            if (float.IsNegativeInfinity(eligibleSince)) eligibleSince = now;
            float opacity = Opacity(now - eligibleSince - FirstDelay);
            if (opacity <= 0f)
            {
                text.Hide();
                return;
            }
            Transform root = text.RootUnder(headset.transform);
            if (root.localPosition != Position) root.localPosition = Position;
            text.ApplyFont();
            int count = 0;
            SetRow(text, count++, PanelLine, opacity);
            if (CameraManager.ActiveMode == CameraMode.Camera) SetRow(text, count++, TabletLine, opacity);
            text.HideRowsFrom(count);
            text.Show();
        }

        private static bool Eligible =>
            CasterConfig.ShowTips.Value && VrCameraRig.HeadsetActive && !ReplaySession.IsReplaying && !TipsSeen.InGamePanelOpened;

        private static void SetRow(HeadsetText text, int index, string line, float opacity)
        {
            TextMeshPro row = text.RowAt(index);
            if (row.text != line) row.text = line;
            row.alpha = opacity;
        }

        private static float Opacity(float elapsed)
        {
            if (elapsed < 0f) return 0f;
            float phase = elapsed % RepeatSeconds;
            if (phase >= ShownSeconds) return 0f;
            return Mathf.Clamp01(Mathf.Min(phase, ShownSeconds - phase) / FadeSeconds);
        }

        protected override void OnDestroy()
        {
            text?.Dispose();
            text = null;
            base.OnDestroy();
        }
    }
}
