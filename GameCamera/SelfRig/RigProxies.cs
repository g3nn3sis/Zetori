// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.SelfRig
{
    internal sealed class RigProxies : IDisposable
    {
        private const float ScanInterval = 1f;

        private readonly List<SkinnedProxy> proxies = new List<SkinnedProxy>();
        private readonly List<SkinnedMeshRenderer> found = new List<SkinnedMeshRenderer>();
        private readonly List<Material> materials = new List<Material>();
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private VRRig? scanned;
        private float nextScan;

        internal void Show(VRRig rig)
        {
            if (rig != scanned || Time.unscaledTime >= nextScan) Scan(rig);
            foreach (SkinnedProxy proxy in proxies) proxy.Show(materials, block);
        }

        internal void Hide()
        {
            foreach (SkinnedProxy proxy in proxies) proxy.Hide();
        }

        private void Scan(VRRig rig)
        {
            scanned = rig;
            nextScan = Time.unscaledTime + ScanInterval;
            rig.GetComponentsInChildren(false, found);
            for (int i = proxies.Count - 1; i >= 0; i--)
            {
                if (proxies[i].Alive && found.Contains(proxies[i].Source)) continue;
                proxies[i].Dispose();
                proxies.RemoveAt(i);
            }
            foreach (SkinnedMeshRenderer renderer in found)
                if (!HasProxy(renderer)) proxies.Add(new SkinnedProxy(renderer));
        }

        private bool HasProxy(SkinnedMeshRenderer renderer)
        {
            foreach (SkinnedProxy proxy in proxies)
                if (proxy.Source == renderer) return true;
            return false;
        }

        public void Dispose()
        {
            foreach (SkinnedProxy proxy in proxies) proxy.Dispose();
            proxies.Clear();
            scanned = null;
        }
    }
}
