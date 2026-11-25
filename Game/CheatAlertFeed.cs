// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Photon.Pun;
using TMPro;
using UnityEngine;
using Zetori.Replay;

namespace Zetori.Game
{
    internal sealed class CheatAlertFeed : HudOverlay
    {
        private const float CentreDrop = 150f;
        private const float RowSpacing = 6f;
        private const float FallbackRowHeight = 48f;

        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);

        private sealed class Row
        {
            internal GameObject Go = null!;
            internal RectTransform Rt = null!;
            internal TMP_Text Value = null!;
            internal CanvasGroup Group = null!;
            internal string? Text;
        }

        private readonly List<Row> rows = new List<Row>();

        private RectTransform? template;
        private RectTransform? feed;
        private float rowHeight = FallbackRowHeight;

        protected override bool Active =>
            CheatAlerts.Enabled && CheatAlerts.Active.Count > 0 && PhotonNetwork.InRoom && !ReplaySession.IsReplaying;

        protected override GameObject? Prefab => HudAssets.ClosestLavaPrefab;

        protected override bool Bind(GameObject canvas)
        {
            template = canvas.transform.Find("ClosestLava") as RectTransform;
            TMP_Text? probe = template != null ? template.Find("Value")?.GetComponent<TMP_Text>() : null;
            if (template == null || probe == null) return false;

            rowHeight = template.rect.height > 1f ? template.rect.height : FallbackRowHeight;
            template.gameObject.SetActive(false);

            var feedObject = new GameObject("Alerts", typeof(RectTransform));
            feed = (RectTransform)feedObject.transform;
            feed.SetParent(canvas.transform, false);
            feed.anchorMin = Centre;
            feed.anchorMax = Centre;
            feed.pivot = TopCentre;
            feed.sizeDelta = Vector2.zero;
            feed.anchoredPosition = new Vector2(0f, -CentreDrop);
            return true;
        }

        protected override void Refresh()
        {
            ApplyFontIfChanged();
            IReadOnlyList<CheatAlert> alerts = CheatAlerts.Active;
            float now = Time.unscaledTime;
            for (int i = 0; i < alerts.Count; i++)
            {
                Row row = RowAt(i);
                SetAnchoredPosition(row.Rt, new Vector2(0f, -i * (rowHeight + RowSpacing)));
                ShowText(row, alerts[i].Text);
                float opacity = alerts[i].Opacity(now);
                if (row.Group.alpha != opacity) row.Group.alpha = opacity;
            }
            for (int i = alerts.Count; i < rows.Count; i++)
                if (rows[i].Go.activeSelf) rows[i].Go.SetActive(false);
        }

        private static void ShowText(Row row, string text)
        {
            if (row.Text == text) return;
            row.Text = text;
            row.Value.text = text;
        }

        private Row RowAt(int index)
        {
            while (rows.Count <= index) rows.Add(CreateRow());
            Row row = rows[index];
            if (!row.Go.activeSelf) row.Go.SetActive(true);
            return row;
        }

        private Row CreateRow()
        {
            GameObject go = Instantiate(template!.gameObject, feed);
            go.name = $"Alert{rows.Count}";
            var rt = (RectTransform)go.transform;
            rt.anchorMin = TopCentre;
            rt.anchorMax = TopCentre;
            rt.pivot = TopCentre;
            TMP_Text value = go.transform.Find("Value").GetComponent<TMP_Text>();
            value.alignment = TextAlignmentOptions.Midline;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.overflowMode = TextOverflowModes.Overflow;
            value.color = Color.white;
            InvalidateFont();
            return new Row
            {
                Go = go,
                Rt = rt,
                Value = value,
                Group = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>()
            };
        }
    }
}
