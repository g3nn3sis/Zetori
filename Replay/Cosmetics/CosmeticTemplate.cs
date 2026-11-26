// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay.Cosmetics
{
    internal sealed class CosmeticTemplate
    {
        private readonly CosmeticFollower[] followers;

        internal GameObject Prefab { get; }

        internal CosmeticTemplate(GameObject prefab, CosmeticFollower[] followers)
        {
            Prefab = prefab;
            this.followers = followers;
        }

        internal Transform[] Rig(Transform copy, Func<int, Transform?> gameBone)
        {
            if (followers.Length == 0) return Array.Empty<Transform>();
            Transform original = Prefab.transform;
            Transform?[] targets = followers.Select(link => link.FollowerIn(original, copy)).ToArray();
            Transform?[] anchors = followers.Select(link => link.SourceIn(original, copy, gameBone)).ToArray();
            var attached = new List<Transform>(followers.Length);
            for (int i = 0; i < followers.Length; i++)
            {
                Transform? target = targets[i];
                Transform? anchor = anchors[i];
                if (target == null || anchor == null) continue;
                followers[i].Snap(target, anchor);
                attached.Add(target);
            }
            return attached.ToArray();
        }
    }
}
