// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.GameCamera.SelfRig;
using Zetori.Shared;
using UnityEngine;

namespace Zetori.Replay.Playback
{
    internal static class NetworkHold
    {
        internal static bool Applies(ReplayActor actor) => actor.IsLocal && SelfRigLerp.Enabled;

        internal static Pose RawRoot(ReplayActor actor, int index, float blend)
        {
            Pose from = Root(HeldFrame(actor, index));
            Pose to = blend > 0f ? Root(HeldFrame(actor, index + 1)) : from;
            return Blend(from, to, blend);
        }

        internal static Pose RawLimb(ReplayActor actor, int index, float blend, int limb)
        {
            Pose from = Limb(HeldFrame(actor, index), limb);
            Pose to = blend > 0f ? Limb(HeldFrame(actor, index + 1), limb) : from;
            return Blend(from, to, blend);
        }

        private static ActorFrame HeldFrame(ReplayActor actor, int index)
        {
            List<ActorFrame> frames = actor.Frames;
            float interval = NetworkSampling.SendInterval;
            float sentAt = Mathf.Floor(frames[index].Time / interval) * interval;
            int held = index;
            while (held > 0 && frames[held].Time > sentAt) held--;
            return frames[held];
        }

        private static Pose Root(ActorFrame frame) =>
            new Pose(NetworkSampling.WorldPosition(frame.RawPosition), NetworkSampling.Rotation(frame.RawRotation.Unpack()));

        private static Pose Limb(ActorFrame frame, int limb)
        {
            LimbSample sample = frame.Limbs[limb];
            Pose raw = LimbSample.Raw(sample, sample, 0f);
            if (limb == LimbSample.Head) return new Pose(raw.position, NetworkSampling.Rotation(raw.rotation));
            return NetworkSampling.Hand(raw.position, raw.rotation);
        }

        private static Pose Blend(Pose from, Pose to, float blend) =>
            new Pose(Vector3.Lerp(from.position, to.position, blend), Quaternion.Slerp(from.rotation, to.rotation, blend));
    }
}
