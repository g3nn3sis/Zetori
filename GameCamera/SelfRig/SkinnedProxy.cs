// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.SelfRig
{
    internal sealed class SkinnedProxy : IDisposable
    {
        private readonly GameObject root;
        private readonly MeshRenderer renderer;
        private readonly Mesh mesh;
        private bool showing;

        internal SkinnedMeshRenderer Source { get; }
        internal bool Alive => Source != null;

        internal SkinnedProxy(SkinnedMeshRenderer source)
        {
            Source = source;
            root = new GameObject($"_Zetori Self Rig {source.name}");
            UnityEngine.Object.DontDestroyOnLoad(root);
            mesh = new Mesh { name = root.name };
            mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = root.AddComponent<MeshRenderer>();
            renderer.forceRenderingOff = true;
        }

        private bool SourceDrawn => Source.enabled && Source.gameObject.activeInHierarchy && !Source.forceRenderingOff && Source.sharedMesh != null;

        internal void Show(List<Material> materials, MaterialPropertyBlock block)
        {
            if (!Alive || !SourceDrawn) return;
            Source.BakeMesh(mesh, true);
            mesh.RecalculateBounds();
            Transform from = Source.transform;
            root.transform.SetPositionAndRotation(from.position, from.rotation);
            root.layer = Source.gameObject.layer;
            CopyLook(materials, block);
            renderer.forceRenderingOff = false;
            Source.forceRenderingOff = true;
            showing = true;
        }

        internal void Hide()
        {
            if (!showing) return;
            showing = false;
            renderer.forceRenderingOff = true;
            if (Alive) Source.forceRenderingOff = false;
        }

        private void CopyLook(List<Material> materials, MaterialPropertyBlock block)
        {
            Source.GetSharedMaterials(materials);
            renderer.SetSharedMaterials(materials);
            Source.GetPropertyBlock(block);
            renderer.SetPropertyBlock(block);
            renderer.shadowCastingMode = Source.shadowCastingMode;
            renderer.receiveShadows = Source.receiveShadows;
            renderer.renderingLayerMask = Source.renderingLayerMask;
        }

        public void Dispose()
        {
            Hide();
            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(mesh);
        }
    }
}
