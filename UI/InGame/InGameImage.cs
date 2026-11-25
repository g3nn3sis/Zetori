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
        private readonly Mesh mesh;
        private readonly Vector3[] vertices = new Vector3[4];
        private readonly Vector2[] uvs = new Vector2[4];
        private Texture? shownTexture;
        private InGameClip clip = InGameClip.None;
        private float shownLower = -0.5f;
        private float shownUpper = 0.5f;
        private bool used;

        internal Renderer Renderer { get; }

        internal InGameImage(Transform parent, Mesh quad, Material material)
        {
            this.material = material;
            mesh = quad;
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

        internal void BeginFrame(Transform parent, InGameClip frameClip)
        {
            used = false;
            clip = frameClip;
            if (imageTransform.parent != parent) imageTransform.SetParent(parent, false);
        }

        internal void EndFrame()
        {
            if (!used) Hide();
        }

        internal void Show(Texture texture, Vector2 center, Vector2 bounds, float depth)
        {
            Vector2 size = Fit(texture, bounds);
            if (!clip.Trim(center.y, size.y, out float bottom, out float top)) return;
            used = true;
            if (!imageObject.activeSelf) imageObject.SetActive(true);
            if (shownTexture != texture)
            {
                shownTexture = texture;
                material.SetTexture(BaseMapProperty, texture);
            }
            var position = new Vector3(center.x, center.y, depth);
            if (imageTransform.localPosition != position) imageTransform.localPosition = position;
            Crop((bottom - center.y) / size.y, (top - center.y) / size.y);
            var scale = new Vector3(size.x, size.y, 1f);
            if (imageTransform.localScale != scale) imageTransform.localScale = scale;
        }

        private void Crop(float lower, float upper)
        {
            if (Mathf.Approximately(lower, shownLower) && Mathf.Approximately(upper, shownUpper)) return;
            shownLower = lower;
            shownUpper = upper;
            vertices[0] = new Vector3(-0.5f, lower, 0f);
            vertices[1] = new Vector3(0.5f, lower, 0f);
            vertices[2] = new Vector3(0.5f, upper, 0f);
            vertices[3] = new Vector3(-0.5f, upper, 0f);
            uvs[0] = new Vector2(0f, lower + 0.5f);
            uvs[1] = new Vector2(1f, lower + 0.5f);
            uvs[2] = new Vector2(1f, upper + 0.5f);
            uvs[3] = new Vector2(0f, upper + 0.5f);
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
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
