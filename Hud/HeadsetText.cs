// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zetori.Hud
{
    internal sealed class HeadsetText : IDisposable
    {
        private const string BackdropName = "Backdrop";

        private static readonly int GuiZTestMode = Shader.PropertyToID("unity_GUIZTestMode");

        private readonly List<TextMeshPro> rows = new List<TextMeshPro>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<SpriteRenderer?> backdrops = new List<SpriteRenderer?>();
        private readonly string rootName;
        private readonly string rowName;
        private readonly Vector2 rowSize;
        private readonly float rowPitch;
        private readonly float fontSize;

        private Transform? root;
        private TMP_FontAsset? appliedFont;
        private Material? overlay;
        private Material? backdropMaterial;

        internal HeadsetText(string rootName, string rowName, Vector2 rowSize, float rowPitch, float fontSize)
        {
            this.rootName = rootName;
            this.rowName = rowName;
            this.rowSize = rowSize;
            this.rowPitch = rowPitch;
            this.fontSize = fontSize;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        internal Camera? Headset { get; set; }

        internal void Hide()
        {
            if (root != null && root.gameObject.activeSelf) root.gameObject.SetActive(false);
        }

        internal void Show()
        {
            if (root != null && !root.gameObject.activeSelf) root.gameObject.SetActive(true);
        }

        internal Transform RootUnder(Transform head)
        {
            if (root == null)
            {
                rows.Clear();
                renderers.Clear();
                backdrops.Clear();
                root = new GameObject(rootName).transform;
            }
            if (root.parent != head)
            {
                root.SetParent(head, false);
                root.localRotation = Quaternion.identity;
            }
            return root;
        }

        internal TextMeshPro RowAt(int index)
        {
            while (rows.Count <= index) rows.Add(CreateRow(rows.Count));
            TextMeshPro row = rows[index];
            if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
            return row;
        }

        internal void HideRowsFrom(int index)
        {
            for (int i = index; i < rows.Count; i++)
                if (rows[i].gameObject.activeSelf) rows[i].gameObject.SetActive(false);
        }

        internal SpriteRenderer BackdropAt(int index)
        {
            TextMeshPro row = RowAt(index);
            while (backdrops.Count <= index) backdrops.Add(null);
            SpriteRenderer? backdrop = backdrops[index];
            if (backdrop == null)
            {
                backdrop = CreateBackdrop(row.transform);
                backdrops[index] = backdrop;
            }
            if (!backdrop.gameObject.activeSelf) backdrop.gameObject.SetActive(true);
            return backdrop;
        }

        internal void HideBackdropsFrom(int index)
        {
            for (int i = index; i < backdrops.Count; i++)
            {
                SpriteRenderer? backdrop = backdrops[i];
                if (backdrop != null && backdrop.gameObject.activeSelf) backdrop.gameObject.SetActive(false);
            }
        }

        internal void ApplyFont() => ApplyFont(HudAssets.CurrentFont);

        internal void ApplyFont(TMP_FontAsset? font)
        {
            if (font == null || font == appliedFont) return;
            appliedFont = font;
            if (overlay != null) UnityEngine.Object.Destroy(overlay);
            overlay = new Material(font.material);
            overlay.SetFloat(ShaderUtilities.ShaderTag_ZTestMode, (float)CompareFunction.Always);
            overlay.renderQueue = (int)RenderQueue.Overlay;
            foreach (TextMeshPro row in rows) Style(row);
        }

        private TextMeshPro CreateRow(int index)
        {
            var rowObject = new GameObject($"{rowName}{index}");
            rowObject.transform.SetParent(root, false);
            rowObject.transform.localPosition = new Vector3(0f, -index * rowPitch, 0f);
            var row = rowObject.AddComponent<TextMeshPro>();
            row.rectTransform.sizeDelta = rowSize;
            row.fontSize = fontSize;
            row.alignment = TextAlignmentOptions.Midline;
            row.textWrappingMode = TextWrappingModes.NoWrap;
            row.overflowMode = TextOverflowModes.Overflow;
            row.color = Color.white;
            Style(row);
            renderers.Add(rowObject.GetComponent<Renderer>());
            return row;
        }

        private SpriteRenderer CreateBackdrop(Transform row)
        {
            var backdropObject = new GameObject(BackdropName);
            backdropObject.transform.SetParent(row, false);
            var backdrop = backdropObject.AddComponent<SpriteRenderer>();
            backdrop.drawMode = SpriteDrawMode.Sliced;
            backdrop.sharedMaterial = BackdropMaterial();
            renderers.Add(backdrop);
            return backdrop;
        }

        private Material BackdropMaterial()
        {
            if (backdropMaterial == null)
            {
                backdropMaterial = new Material(Canvas.GetDefaultCanvasMaterial());
                backdropMaterial.SetFloat(GuiZTestMode, (float)CompareFunction.Always);
                backdropMaterial.renderQueue = (int)RenderQueue.Overlay - 1;
            }
            return backdropMaterial;
        }

        private void Style(TextMeshPro row)
        {
            if (appliedFont == null || overlay == null) return;
            row.font = appliedFont;
            row.fontSharedMaterial = overlay;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            bool hidden = camera != Headset;
            foreach (Renderer renderer in renderers)
                if (renderer != null && renderer.forceRenderingOff != hidden) renderer.forceRenderingOff = hidden;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
            if (overlay != null) UnityEngine.Object.Destroy(overlay);
            if (backdropMaterial != null) UnityEngine.Object.Destroy(backdropMaterial);
        }
    }
}
