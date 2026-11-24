// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using TMPro;
using UnityEngine;

namespace Zetori.UI.InGame
{
    internal sealed class InGameVisual
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly Vector4 ControlMargin = new Vector4(0.003f, 0f, 0.003f, 0f);
        private static readonly Vector4 LabelMargin = new Vector4(0.002f, 0f, 0.002f, 0f);

        private readonly GameObject quadObject;
        private readonly Transform quadTransform;
        private readonly Material material;
        private readonly TextMeshPro text;
        private readonly Transform textTransform;
        private Color shownColor;
        private InGameTextStyle? shownStyle;
        private bool quadUsed;
        private bool textUsed;

        internal Renderer QuadRenderer { get; }
        internal Renderer TextRenderer { get; }

        internal InGameVisual(Transform parent, Mesh quad, Material material, TMP_FontAsset? font)
        {
            this.material = material;
            shownColor = material.GetColor(BaseColorProperty);
            quadObject = new GameObject("quad");
            quadTransform = quadObject.transform;
            quadTransform.SetParent(parent, false);
            quadObject.AddComponent<MeshFilter>().sharedMesh = quad;
            var quadRenderer = quadObject.AddComponent<MeshRenderer>();
            quadRenderer.sharedMaterial = material;
            QuadRenderer = quadRenderer;
            quadObject.SetActive(false);

            var textObject = new GameObject("text");
            textObject.transform.SetParent(parent, false);
            text = textObject.AddComponent<TextMeshPro>();
            textTransform = text.rectTransform;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = true;
            text.fontSizeMin = InGameMetrics.MinimumFontSize;
            text.color = Color.white;
            if (font != null) text.font = font;
            TextRenderer = textObject.GetComponent<Renderer>();
            textObject.SetActive(false);
        }

        internal void SetFont(TMP_FontAsset font)
        {
            if (text.font != font) text.font = font;
        }

        internal void BeginFrame()
        {
            quadUsed = false;
            textUsed = false;
        }

        internal void EndFrame()
        {
            if (!quadUsed) HideQuad();
            if (!textUsed) HideText();
        }

        internal void ShowQuad(Vector2 center, Vector2 size, float depth, Color color)
        {
            quadUsed = true;
            if (!quadObject.activeSelf) quadObject.SetActive(true);
            var position = new Vector3(center.x, center.y, depth);
            if (quadTransform.localPosition != position) quadTransform.localPosition = position;
            var scale = new Vector3(size.x, size.y, 1f);
            if (quadTransform.localScale != scale) quadTransform.localScale = scale;
            if (color == shownColor) return;
            shownColor = color;
            material.SetColor(BaseColorProperty, color);
        }

        private void HideQuad()
        {
            if (quadObject.activeSelf) quadObject.SetActive(false);
        }

        internal void ShowText(string value, Vector2 center, Vector2 size, float depth, InGameTextStyle style, Color color)
        {
            textUsed = true;
            GameObject textObject = text.gameObject;
            if (!textObject.activeSelf) textObject.SetActive(true);
            ApplyStyle(style);
            var position = new Vector3(center.x, center.y, depth);
            if (textTransform.localPosition != position) textTransform.localPosition = position;
            if (text.rectTransform.sizeDelta != size) text.rectTransform.sizeDelta = size;
            if (!string.Equals(text.text, value, StringComparison.Ordinal)) text.text = value;
            if (text.color != color) text.color = color;
        }

        private void HideText()
        {
            GameObject textObject = text.gameObject;
            if (textObject.activeSelf) textObject.SetActive(false);
        }

        internal void Hide()
        {
            HideQuad();
            HideText();
        }

        private void ApplyStyle(InGameTextStyle style)
        {
            if (shownStyle == style) return;
            shownStyle = style;
            switch (style)
            {
                case InGameTextStyle.Label:
                    text.alignment = TextAlignmentOptions.Left;
                    text.textWrappingMode = TextWrappingModes.Normal;
                    text.fontSizeMax = InGameMetrics.LabelFontSize;
                    text.fontStyle = FontStyles.Normal;
                    text.margin = LabelMargin;
                    break;
                case InGameTextStyle.Block:
                    text.alignment = TextAlignmentOptions.TopLeft;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.fontSizeMax = InGameMetrics.LabelFontSize;
                    text.fontStyle = FontStyles.Normal;
                    text.margin = LabelMargin;
                    break;
                case InGameTextStyle.Title:
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.fontSizeMax = InGameMetrics.TitleFontSize;
                    text.fontStyle = FontStyles.Bold;
                    text.margin = ControlMargin;
                    break;
                default:
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.fontSizeMax = InGameMetrics.ControlFontSize;
                    text.fontStyle = FontStyles.Normal;
                    text.margin = ControlMargin;
                    break;
            }
            text.fontSize = text.fontSizeMax;
        }
    }
}
