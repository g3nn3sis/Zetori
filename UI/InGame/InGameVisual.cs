// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using TMPro;
using UnityEngine;

namespace Zetori.UI.InGame
{
    internal sealed class InGameVisual
    {
        private const int NoProbe = -1;

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
        private InGameClip clip = InGameClip.None;
        private int keptFirstLine;
        private int keptLastLine;
        private int probeMaterial;
        private int probeVertex = NoProbe;
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

        internal void BeginFrame(Transform parent, InGameClip frameClip)
        {
            quadUsed = false;
            textUsed = false;
            clip = frameClip;
            if (quadTransform.parent == parent) return;
            quadTransform.SetParent(parent, false);
            textTransform.SetParent(parent, false);
        }

        internal void EndFrame()
        {
            if (!quadUsed) HideQuad();
            if (!textUsed) HideText();
        }

        internal void ShowQuad(Vector2 center, Vector2 size, float depth, Color color)
        {
            if (!clip.Trim(center.y, size.y, out float bottom, out float top)) return;
            quadUsed = true;
            if (!quadObject.activeSelf) quadObject.SetActive(true);
            var position = new Vector3(center.x, (bottom + top) * 0.5f, depth);
            if (quadTransform.localPosition != position) quadTransform.localPosition = position;
            var scale = new Vector3(size.x, top - bottom, 1f);
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
            if (clip.Misses(center.y, size.y)) return;
            textUsed = true;
            GameObject textObject = text.gameObject;
            if (!textObject.activeSelf) textObject.SetActive(true);
            ApplyStyle(style);
            var position = new Vector3(center.x, center.y, depth);
            if (textTransform.localPosition != position) textTransform.localPosition = position;
            if (text.rectTransform.sizeDelta != size) text.rectTransform.sizeDelta = size;
            if (!string.Equals(text.text, value, StringComparison.Ordinal)) text.text = value;
            if (text.color != color) text.color = color;
            if (clip.CoversBox(center.y, size.y)) RestoreLines();
            else ClipLines(center.y);
        }

        private void RestoreLines()
        {
            if (probeVertex == NoProbe) return;
            probeVertex = NoProbe;
            text.ForceMeshUpdate();
        }

        private void ClipLines(float centerY)
        {
            if (text.havePropertiesChanged)
            {
                text.ForceMeshUpdate();
                probeVertex = NoProbe;
            }
            TMP_TextInfo info = text.textInfo;
            FindShownLines(info, centerY, out int first, out int last);
            if (probeVertex != NoProbe && first == keptFirstLine && last == keptLastLine && ProbeHidden(info)) return;
            if (probeVertex != NoProbe) text.ForceMeshUpdate();
            keptFirstLine = first;
            keptLastLine = last;
            HideLinesOutside(text.textInfo, first, last);
        }

        private void FindShownLines(TMP_TextInfo info, float centerY, out int first, out int last)
        {
            first = info.lineCount;
            last = -1;
            for (int i = 0; i < info.lineCount; i++)
            {
                TMP_LineInfo line = info.lineInfo[i];
                if (!clip.Covers(centerY + line.descender, centerY + line.ascender)) continue;
                if (last < 0) first = i;
                last = i;
            }
        }

        private void HideLinesOutside(TMP_TextInfo info, int first, int last)
        {
            probeVertex = NoProbe;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible || (character.lineNumber >= first && character.lineNumber <= last)) continue;
                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                int start = character.vertexIndex;
                for (int corner = 1; corner < 4; corner++) vertices[start + corner] = vertices[start];
                probeMaterial = character.materialReferenceIndex;
                probeVertex = start;
            }
            if (probeVertex != NoProbe) text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private bool ProbeHidden(TMP_TextInfo info)
        {
            if (probeMaterial >= info.meshInfo.Length) return false;
            Vector3[] vertices = info.meshInfo[probeMaterial].vertices;
            return probeVertex + 2 < vertices.Length && vertices[probeVertex] == vertices[probeVertex + 2];
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
                    Configure(TextAlignmentOptions.Left, TextWrappingModes.Normal, InGameMetrics.LabelFontSize, FontStyles.Normal, LabelMargin);
                    break;
                case InGameTextStyle.Block:
                    Configure(TextAlignmentOptions.TopLeft, TextWrappingModes.NoWrap, InGameMetrics.LabelFontSize, FontStyles.Normal, LabelMargin);
                    break;
                case InGameTextStyle.Title:
                    Configure(TextAlignmentOptions.Center, TextWrappingModes.NoWrap, InGameMetrics.TitleFontSize, FontStyles.Bold, ControlMargin);
                    break;
                case InGameTextStyle.Brand:
                    Configure(TextAlignmentOptions.Left, TextWrappingModes.NoWrap, InGameMetrics.BrandFontSize, FontStyles.Bold, LabelMargin);
                    break;
                case InGameTextStyle.Heading:
                    Configure(TextAlignmentOptions.Left, TextWrappingModes.NoWrap, InGameMetrics.TitleFontSize, FontStyles.Bold, LabelMargin);
                    break;
                case InGameTextStyle.Caption:
                    Configure(TextAlignmentOptions.Left, TextWrappingModes.NoWrap, InGameMetrics.CaptionFontSize, FontStyles.Normal, LabelMargin);
                    break;
                default:
                    Configure(TextAlignmentOptions.Center, TextWrappingModes.NoWrap, InGameMetrics.ControlFontSize, FontStyles.Normal, ControlMargin);
                    break;
            }
        }

        private void Configure(TextAlignmentOptions alignment, TextWrappingModes wrapping, float fontSize, FontStyles fontStyle, Vector4 margin)
        {
            text.alignment = alignment;
            text.textWrappingMode = wrapping;
            text.fontSizeMax = fontSize;
            text.fontStyle = fontStyle;
            text.margin = margin;
            text.fontSize = fontSize;
        }
    }
}
