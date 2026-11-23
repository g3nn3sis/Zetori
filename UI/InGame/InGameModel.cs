// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Zetori.Base;
using Zetori.Game;

namespace Zetori.UI.InGame
{
    internal sealed class InGameModel : IDisposable
    {
        private const string ModelName = "In-Game Panel";
        private const string MeshResource = "Zetori.Models.in_game_panel.zmdl";
        private const float HandleOffset = 0.262f;
        private const float HandleHalfLength = 0.105f;
        private const float HandleReach = 0.075f;
        private const float MinimumScale = 0.001f;

        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly Color BodyColor = new Color(0.12f, 0.12f, 0.14f);
        private static readonly Color FaceColor = new Color(0.03f, 0.03f, 0.04f);
        private static readonly Color HandleColor = new Color(0.2f, 0.22f, 0.26f);

        private readonly GameObject root;
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private Material rimMaterial = null!;
        private Color32? shownAccent;
        private bool hiddenFromView;

        internal Transform Root => root.transform;
        internal Transform Content { get; }
        internal Mesh Quad { get; }
        internal bool Shown => root.activeSelf;

        private InGameModel()
        {
            root = new GameObject("_Zetori In-Game Panel");
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.SetActive(false);
            var content = new GameObject("content");
            content.transform.SetParent(root.transform, false);
            Content = content.transform;
            Quad = CreateQuad();
            meshes.Add(Quad);
        }

        internal static InGameModel Create()
        {
            var model = new InGameModel();
            try
            {
                model.Build();
                return model;
            }
            catch
            {
                model.Dispose();
                throw;
            }
        }

        internal Material CreateMaterial(Color color)
        {
            var material = new Material(UberShader.ReferenceMaterial);
            material.SetColor(BaseColorProperty, color);
            materials.Add(material);
            return material;
        }

        internal void Register(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.forceRenderingOff = hiddenFromView;
            renderers.Add(renderer);
        }

        internal void SetHiddenFromView(bool hidden)
        {
            if (hidden == hiddenFromView) return;
            hiddenFromView = hidden;
            foreach (Renderer renderer in renderers)
                if (renderer != null) renderer.forceRenderingOff = hidden;
        }

        internal void SetAccent(Color32 accent)
        {
            if (shownAccent is Color32 shown && shown.r == accent.r && shown.g == accent.g && shown.b == accent.b) return;
            shownAccent = accent;
            rimMaterial.SetColor(BaseColorProperty, accent);
        }

        internal void SetScale(float scale)
        {
            Vector3 size = Vector3.one * Mathf.Max(MinimumScale, scale);
            if (root.transform.localScale != size) root.transform.localScale = size;
        }

        internal void Show()
        {
            if (!root.activeSelf) root.SetActive(true);
        }

        internal void Hide()
        {
            if (root.activeSelf) root.SetActive(false);
        }

        internal bool WithinGrabReach(Vector3 hand)
        {
            Vector3 local = root.transform.InverseTransformPoint(hand);
            float along = Mathf.Max(0f, Mathf.Abs(local.y) - HandleHalfLength);
            float across = Mathf.Abs(local.x) - HandleOffset;
            return across * across + along * along + local.z * local.z <= HandleReach * HandleReach;
        }

        private void Build()
        {
            byte[] data = EmbeddedResources.Read(MeshResource) ?? throw new FileNotFoundException("The in-game panel model is missing.", MeshResource);
            Dictionary<string, ModelPart> parts = ModelFile.Read(data, ModelName);
            foreach (ModelPart part in parts.Values) meshes.Add(part.Mesh);
            Material body = CreateMaterial(BodyColor);
            Material handle = CreateMaterial(HandleColor);
            rimMaterial = CreateMaterial(ColorSpace.Current.Color);
            Place(parts["body"], "body", body);
            Place(parts["face"], "face", CreateMaterial(FaceColor));
            Place(parts["rim"], "rim", rimMaterial);
            Place(parts["handle_left"], "handle_left", handle);
            Place(parts["handle_right"], "handle_right", handle);
        }

        private void Place(ModelPart part, string name, Material material)
        {
            var partObject = new GameObject(name);
            partObject.transform.SetParent(root.transform, false);
            partObject.transform.localPosition = part.Pivot;
            partObject.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
            var renderer = partObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            Register(renderer);
        }

        private static Mesh CreateQuad()
        {
            var mesh = new Mesh
            {
                name = "_Zetori In-Game Panel Quad",
                vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) },
                normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 3, 2, 0, 2, 1 }
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Dispose()
        {
            foreach (Material material in materials)
                if (material != null) UnityEngine.Object.Destroy(material);
            materials.Clear();
            foreach (Mesh mesh in meshes)
                if (mesh != null) UnityEngine.Object.Destroy(mesh);
            meshes.Clear();
            renderers.Clear();
            if (root != null) UnityEngine.Object.Destroy(root);
        }
    }
}
