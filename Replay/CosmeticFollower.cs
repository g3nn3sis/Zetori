// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaExtensions;
using UnityEngine;

namespace Zetori.Replay
{
    internal readonly struct CosmeticFollower
    {
        private const int NoBone = 0;

        private readonly Transform follower;
        private readonly Transform? source;
        private readonly int bone;
        private readonly Vector3 offset;
        private readonly Quaternion turn;

        private CosmeticFollower(Transform follower, Transform? source, int bone, Vector3 offset, Quaternion turn)
        {
            this.follower = follower;
            this.source = source;
            this.bone = bone;
            this.offset = offset;
            this.turn = turn;
        }

        internal static CosmeticFollower[] Of(Transform root)
        {
            var found = new List<CosmeticFollower>();
            foreach (GTPosRotConstraints component in root.GetComponentsInChildren<GTPosRotConstraints>(true))
                if (Registers(component, root) && TryRead(component, root, out CosmeticFollower[] links)) found.AddRange(links);
            return found.ToArray();
        }

        internal Transform? FollowerIn(Transform original, Transform copy) =>
            follower != null ? CosmeticNodes.Counterpart(original, copy, follower) : null;

        internal Transform? SourceIn(Transform original, Transform copy, Func<int, Transform?> gameBone)
        {
            if (bone != NoBone) return gameBone(bone);
            return source != null ? CosmeticNodes.Counterpart(original, copy, source) : null;
        }

        internal void Snap(Transform target, Transform anchor)
        {
            target.SetPositionAndRotation(anchor.position + anchor.TransformVector(offset), anchor.rotation * turn);
            target.SetParent(anchor, true);
        }

        private static bool Registers(GTPosRotConstraints component, Transform root)
        {
            if (component == null || !component.enabled || component.constraints == null) return false;
            for (Transform? current = component.transform; current != null && current != root; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }

        private static bool TryRead(GTPosRotConstraints component, Transform root, out CosmeticFollower[] links)
        {
            links = new CosmeticFollower[component.constraints.Length];
            for (int i = 0; i < links.Length; i++)
            {
                GorillaPosRotConstraint constraint = component.constraints[i];
                int bone = (int)constraint.sourceGorillaBone;
                Transform? source = bone == NoBone ? InternalSource(component.transform, constraint) : null;
                if (!Within(constraint.follower, root) || (bone == NoBone && !Within(source, root))) return false;
                links[i] = new CosmeticFollower(constraint.follower, source, bone, constraint.positionOffset, Turn(constraint.rotationOffset));
            }
            return true;
        }

        private static Transform? InternalSource(Transform owner, GorillaPosRotConstraint constraint)
        {
            if (constraint.source != null) return constraint.source;
            if (string.IsNullOrEmpty(constraint.sourceRelativePath)) return null;
            return owner.TryFindByPath(constraint.sourceRelativePath, out Transform found) ? found : null;
        }

        private static bool Within(Transform? node, Transform root) => node != null && node.IsChildOf(root);

        private static Quaternion Turn(Quaternion rotation) =>
            Mathf.Approximately(rotation.x, 0f) && Mathf.Approximately(rotation.y, 0f) && Mathf.Approximately(rotation.z, 0f) && Mathf.Approximately(rotation.w, 0f)
                ? Quaternion.identity
                : rotation;
    }
}
