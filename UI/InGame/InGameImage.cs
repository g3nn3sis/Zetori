// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.UI.InGame
{
    internal sealed class InGameImage
    {
        private const string TextureKeyword = "_USE_TEXTURE";

        private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");

        private readonly GameObject imageObject;
        private readonly Transform imageTransform;
        private readonly Material material;
        private Texture? shownTexture;
        private bool used;

        internal Renderer Renderer { get; }

        internal InGameImage(Transform parent, Mesh quad, Material material)
        {
            this.material = material;
            material.EnableKeyword(TextureKeyword);
            imageObject = new GameObject("image");
            imageTransform = imageObject.transform;
            imageTransform.SetParent(parent, false);
            imageObject.AddComponent<MeshFilter>().sharedMesh = quad;
            var renderer = imageObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            Renderer = renderer;
            imageObject.SetActive(false);
        }

        internal void BeginFrame() => used = false;

        internal void EndFrame()
        {
            if (!used) Hide();
        }

        internal void Show(Texture texture, Vector2 center, Vector2 bounds)
        {
            used = true;
            if (!imageObject.activeSelf) imageObject.SetActive(true);
            if (shownTexture != texture)
            {
                shownTexture = texture;
                material.SetTexture(BaseMapProperty, texture);
            }
            var position = new Vector3(center.x, center.y, InGameMetrics.QuadDepth);
            if (imageTransform.localPosition != position) imageTransform.localPosition = position;
            Vector2 size = Fit(texture, bounds);
            var scale = new Vector3(size.x, size.y, 1f);
            if (imageTransform.localScale != scale) imageTransform.localScale = scale;
        }

        internal void Hide()
        {
            if (imageObject.activeSelf) imageObject.SetActive(false);
        }

        private static Vector2 Fit(Texture texture, Vector2 bounds)
        {
            float aspect = texture.width / (float)Mathf.Max(1, texture.height);
            return bounds.x > bounds.y * aspect
                ? new Vector2(bounds.y * aspect, bounds.y)
                : new Vector2(bounds.x, bounds.x / aspect);
        }
    }
}
