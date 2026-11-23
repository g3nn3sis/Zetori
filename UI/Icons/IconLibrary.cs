// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Text;
using UnityEngine;
using Zetori.Base;

namespace Zetori.UI.Icons
{
    internal static class IconLibrary
    {
        private const string ResourcePrefix = "Zetori.Icons.Panels.";
        private const string ResourceExtension = ".svg";

        private static readonly Dictionary<IconGlyph, IconGraphic?> graphics = new Dictionary<IconGlyph, IconGraphic?>();

        internal static Texture2D? CreateTexture(IconGlyph glyph, int size)
        {
            IconGraphic? graphic = Graphic(glyph);
            if (graphic == null || size <= 0) return null;
            float[] coverage = IconRasterizer.Coverage(graphic, size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                int sourceRow = (size - 1 - y) * size;
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage[sourceRow + x] * 255f));
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"_Zetori Icon {glyph} {size}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        internal static Mesh? CreateMesh(IconGlyph glyph)
        {
            IconGraphic? graphic = Graphic(glyph);
            if (graphic == null) return null;
            IconGeometry geometry = IconTessellator.Tessellate(graphic);
            var vertices = new Vector3[geometry.VertexCount];
            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(geometry.Vertices[i * 2], geometry.Vertices[i * 2 + 1], 0f);
                normals[i] = Vector3.back;
            }
            var mesh = new Mesh
            {
                name = $"_Zetori Icon {glyph}",
                vertices = vertices,
                normals = normals,
                triangles = geometry.Triangles
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static IconGraphic? Graphic(IconGlyph glyph)
        {
            if (graphics.TryGetValue(glyph, out IconGraphic? cached)) return cached;
            IconGraphic? graphic = Load(glyph);
            graphics[glyph] = graphic;
            return graphic;
        }

        private static IconGraphic? Load(IconGlyph glyph)
        {
            string resource = ResourcePrefix + glyph.ToString().ToLowerInvariant() + ResourceExtension;
            byte[]? data = EmbeddedResources.Read(resource);
            if (data == null)
            {
                Log.Error($"Missing embedded icon {resource}.");
                return null;
            }
            try { return SvgReader.Read(Encoding.UTF8.GetString(data)); }
            catch (Exception e)
            {
                Log.Exception(e, $"Reading the icon {resource} failed");
                return null;
            }
        }
    }
}
