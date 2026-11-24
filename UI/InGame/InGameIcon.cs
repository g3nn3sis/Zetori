// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.UI.InGame
{
    internal sealed class InGameIcon
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        private readonly GameObject iconObject;
        private readonly Transform iconTransform;
        private readonly MeshFilter filter;
        private readonly Material material;
        private Color shownColor;
        private bool used;

        internal Renderer Renderer { get; }

        internal InGameIcon(Transform parent, Material material)
        {
            this.material = material;
            shownColor = material.GetColor(BaseColorProperty);
            iconObject = new GameObject("icon");
            iconTransform = iconObject.transform;
            iconTransform.SetParent(parent, false);
            filter = iconObject.AddComponent<MeshFilter>();
            var renderer = iconObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            Renderer = renderer;
            iconObject.SetActive(false);
        }

        internal void BeginFrame(Transform parent)
        {
            used = false;
            if (iconTransform.parent != parent) iconTransform.SetParent(parent, false);
        }

        internal void EndFrame()
        {
            if (!used) Hide();
        }

        internal void Show(Mesh mesh, Vector2 center, float size, float depth, Color color)
        {
            used = true;
            if (!iconObject.activeSelf) iconObject.SetActive(true);
            if (filter.sharedMesh != mesh) filter.sharedMesh = mesh;
            var position = new Vector3(center.x, center.y, depth);
            if (iconTransform.localPosition != position) iconTransform.localPosition = position;
            var scale = new Vector3(size, size, 1f);
            if (iconTransform.localScale != scale) iconTransform.localScale = scale;
            if (color == shownColor) return;
            shownColor = color;
            material.SetColor(BaseColorProperty, color);
        }

        internal void Hide()
        {
            if (iconObject.activeSelf) iconObject.SetActive(false);
        }
    }
}
