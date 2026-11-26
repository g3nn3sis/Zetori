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
using Zetori.Replay;

namespace Zetori.Cheats
{
    internal sealed class HeadsetAlertToast : ObjectComponent
    {
        private const float Distance = 0.8f;
        private const float RowPitch = 0.04f;
        private const float FontSize = 0.2f;
        private const float CentimetresToMetres = 0.01f;

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
            text.ApplyFont();
            float now = Time.unscaledTime;
            for (int i = 0; i < alerts.Count; i++)
            {
                TextMeshPro row = text.RowAt(i);
                if (row.text != alerts[i].Text) row.text = alerts[i].Text;
                row.alpha = alerts[i].Opacity(now);
            }
            text.HideRowsFrom(alerts.Count);
            text.Show();
        }

        private static void Place(Transform target)
        {
            var position = new Vector3(CasterConfig.CheatAlertsHeadsetX.Value * CentimetresToMetres, CasterConfig.CheatAlertsHeadsetY.Value * CentimetresToMetres, Distance);
            if (target.localPosition != position) target.localPosition = position;
            Vector3 size = Vector3.one * CasterConfig.CheatAlertsHeadsetScale.Value;
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
