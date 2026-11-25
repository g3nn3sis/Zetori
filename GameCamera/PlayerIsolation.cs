// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Game;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zetori.GameCamera
{
    internal sealed class PlayerIsolation : IDisposable
    {
        private const int FirstUserLayer = 8;
        private const int LastLayer = 31;

        private readonly HashSet<Camera> cameras;
        private readonly Renderer[] renderers;
        private readonly int[] layers;
        private readonly int layer;
        private bool isolated;

        private PlayerIsolation(Renderer[] renderers, int layer, Camera[] cameras)
        {
            this.renderers = renderers;
            this.layer = layer;
            this.cameras = new HashSet<Camera>(cameras);
            layers = new int[renderers.Length];
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        internal static PlayerIsolation? TryCreate(Transform target, Camera[] cameras)
        {
            if (!TryFindFreeLayer(out int layer)) return null;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>().Where(renderer => !NameTag.IsTagPart(renderer.transform)).ToArray();
            if (renderers.Length == 0) return null;
            foreach (Camera camera in cameras) camera.cullingMask = 1 << layer;
            return new PlayerIsolation(renderers, layer, cameras);
        }

        private static bool TryFindFreeLayer(out int layer)
        {
            for (layer = LastLayer; layer >= FirstUserLayer; layer--)
                if (string.IsNullOrEmpty(LayerMask.LayerToName(layer))) return true;
            return false;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (isolated || !cameras.Contains(camera)) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                GameObject part = renderers[i].gameObject;
                layers[i] = part.layer;
                part.layer = layer;
            }
            isolated = true;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (cameras.Contains(camera)) Restore();
        }

        private void Restore()
        {
            if (!isolated) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].gameObject.layer = layers[i];
            isolated = false;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            Restore();
        }
    }
}
