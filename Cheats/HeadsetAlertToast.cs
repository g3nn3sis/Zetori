// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using TMPro;
using UnityEngine;
using Zetori.Base;
using Zetori.Base.Config;
using Zetori.GameCamera.Vr;
using Zetori.Hud;
using Zetori.Notifications;
using Zetori.Replay;

namespace Zetori.Cheats
{
    internal sealed class HeadsetAlertToast : ObjectComponent
    {
        private const float Distance = 0.8f;
        private const float RowPitch = 0.04f;
        private const float FontSize = 0.2f;
        private const float CentimetresToMetres = 0.01f;
        private const float BackdropPaddingX = 0.012f;
        private const float BackdropPaddingY = 0.006f;
        private const float BackdropGap = 0.004f;

        private static readonly Vector2 RowSize = new Vector2(1.2f, RowPitch);

        private HeadsetText? text;

        protected override void Awake()
        {
            base.Awake();
            text = new HeadsetText($"_{PluginInfo.Name} Headset Alerts", "Alert", RowSize, RowPitch, FontSize);
        }

        internal void LateUpdate()
        {
            try { Show(); }
            catch (Exception e) { Log.ExceptionOnce("headset-alerts", e, "Showing cheat alerts in the headset failed"); }
        }

        private void Show()
        {
            if (text == null) return;
            GTPlayer? player = GTPlayer.Instance;
            Camera? headset = player != null ? player.mainCamera : null;
            text.Headset = headset;
            IReadOnlyList<CheatAlert> alerts = CheatAlerts.Active;
            if (headset == null || alerts.Count == 0 || !CheatAlerts.Enabled || !CasterConfig.CheatAlertsInHeadset.Value || !VrCameraRig.HeadsetActive || ReplaySession.IsReplaying)
            {
                text.Hide();
                return;
            }
            Place(text.RootUnder(headset.transform));
            text.ApplyFont(NotificationStyle.Font);
            float now = Time.unscaledTime;
            bool backdrops = NotificationStyle.HasBackground;
            Color textColor = NotificationStyle.TextColor;
            for (int i = 0; i < alerts.Count; i++)
            {
                TextMeshPro row = text.RowAt(i);
                if (row.text != alerts[i].Text) row.text = alerts[i].Text;
                textColor.a = alerts[i].Opacity(now);
                if (row.color != textColor) row.color = textColor;
                if (backdrops) Frame(text.BackdropAt(i), row, textColor.a);
            }
            text.HideRowsFrom(alerts.Count);
            text.HideBackdropsFrom(backdrops ? alerts.Count : 0);
            text.Show();
        }

        private static void Frame(SpriteRenderer backdrop, TextMeshPro row, float opacity)
        {
            Sprite sprite = NotificationStyle.BackgroundSprite;
            if (backdrop.sprite != sprite) backdrop.sprite = sprite;
            var size = new Vector2(row.preferredWidth + BackdropPaddingX * 2f, Mathf.Min(row.preferredHeight + BackdropPaddingY * 2f, RowPitch - BackdropGap));
            if (backdrop.size != size) backdrop.size = size;
            Color color = NotificationStyle.BackgroundColor;
            color.a *= opacity;
            if (backdrop.color != color) backdrop.color = color;
        }

        private static void Place(Transform target)
        {
            var position = new Vector3(CasterConfig.NotificationX.Value * CentimetresToMetres, CasterConfig.NotificationY.Value * CentimetresToMetres, Distance);
            if (target.localPosition != position) target.localPosition = position;
            Vector3 size = Vector3.one * CasterConfig.NotificationScale.Value;
            if (target.localScale != size) target.localScale = size;
        }

        protected override void OnDestroy()
        {
            text?.Dispose();
            text = null;
            base.OnDestroy();
        }
    }
}
