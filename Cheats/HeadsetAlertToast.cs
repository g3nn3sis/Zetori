// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
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

        private readonly List<TextMeshPro> rows = new List<TextMeshPro>();
        private readonly List<Renderer> renderers = new List<Renderer>();

        private Transform? root;
        private Camera? headset;
        private TMP_FontAsset? appliedFont;
        private Material? overlay;

        protected override void Awake()
        {
            base.Awake();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        internal void LateUpdate()
        {
            try { Show(); }
            catch (Exception e) { Log.ExceptionOnce("headset-alerts", e, "Showing cheat alerts in the headset failed"); }
        }

        private void Show()
        {
            GTPlayer? player = GTPlayer.Instance;
            headset = player != null ? player.mainCamera : null;
            IReadOnlyList<CheatAlert> alerts = CheatAlerts.Active;
            if (headset == null || alerts.Count == 0 || !CheatAlerts.Enabled || !CasterConfig.CheatAlertsInHeadset.Value || !VrCameraRig.HeadsetActive || ReplaySession.IsReplaying)
            {
                Hide();
                return;
            }
            Transform parent = RootUnder(headset.transform);
            Place(parent);
            ApplyFont();
            float now = Time.unscaledTime;
            for (int i = 0; i < alerts.Count; i++)
            {
                TextMeshPro row = RowAt(i, parent);
                if (row.text != alerts[i].Text) row.text = alerts[i].Text;
                row.alpha = alerts[i].Opacity(now);
            }
            for (int i = alerts.Count; i < rows.Count; i++)
                if (rows[i].gameObject.activeSelf) rows[i].gameObject.SetActive(false);
            if (!parent.gameObject.activeSelf) parent.gameObject.SetActive(true);
        }

        private void Hide()
        {
            if (root != null && root.gameObject.activeSelf) root.gameObject.SetActive(false);
        }

        private Transform RootUnder(Transform head)
        {
            if (root == null)
            {
                rows.Clear();
                renderers.Clear();
                root = new GameObject($"_{PluginInfo.Name} Headset Alerts").transform;
            }
            if (root.parent != head)
            {
                root.SetParent(head, false);
                root.localRotation = Quaternion.identity;
            }
            return root;
        }

        private static void Place(Transform target)
        {
            var position = new Vector3(CasterConfig.CheatAlertsHeadsetX.Value * CentimetresToMetres, CasterConfig.CheatAlertsHeadsetY.Value * CentimetresToMetres, Distance);
            if (target.localPosition != position) target.localPosition = position;
            Vector3 size = Vector3.one * CasterConfig.CheatAlertsHeadsetScale.Value;
            if (target.localScale != size) target.localScale = size;
        }

        private TextMeshPro RowAt(int index, Transform parent)
        {
            while (rows.Count <= index) rows.Add(CreateRow(parent, rows.Count));
            TextMeshPro row = rows[index];
            if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
            return row;
        }

        private TextMeshPro CreateRow(Transform parent, int index)
        {
            var rowObject = new GameObject($"Alert{index}");
            rowObject.transform.SetParent(parent, false);
            rowObject.transform.localPosition = new Vector3(0f, -index * RowPitch, 0f);
            var row = rowObject.AddComponent<TextMeshPro>();
            row.rectTransform.sizeDelta = RowSize;
            row.fontSize = FontSize;
            row.alignment = TextAlignmentOptions.Midline;
            row.textWrappingMode = TextWrappingModes.NoWrap;
            row.overflowMode = TextOverflowModes.Overflow;
            row.color = Color.white;
            Style(row);
            renderers.Add(rowObject.GetComponent<Renderer>());
            return row;
        }

        private void ApplyFont()
        {
            TMP_FontAsset? font = HudAssets.CurrentFont;
            if (font == null || font == appliedFont) return;
            appliedFont = font;
            if (overlay != null) Destroy(overlay);
            overlay = new Material(font.material);
            overlay.SetFloat(ShaderUtilities.ShaderTag_ZTestMode, (float)CompareFunction.Always);
            overlay.renderQueue = (int)RenderQueue.Overlay;
            foreach (TextMeshPro row in rows) Style(row);
        }

        private void Style(TextMeshPro row)
        {
            if (appliedFont == null || overlay == null) return;
            row.font = appliedFont;
            row.fontSharedMaterial = overlay;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            bool hidden = camera != headset;
            foreach (Renderer renderer in renderers)
                if (renderer != null && renderer.forceRenderingOff != hidden) renderer.forceRenderingOff = hidden;
        }

        protected override void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (root != null) Destroy(root.gameObject);
            if (overlay != null) Destroy(overlay);
            base.OnDestroy();
        }
    }
}
