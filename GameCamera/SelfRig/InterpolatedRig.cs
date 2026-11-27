// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.SelfRig
{
    internal sealed class InterpolatedRig
    {
        private const float BodyPositionLerpScale = 0.66f;
        private const float TeleportSnapDistance = 1f;

        private RigNetworkSample target;
        private bool snapPending;

        internal bool Ready { get; private set; }
        internal Vector3 BodyPosition { get; private set; }
        internal Quaternion BodyRotation { get; private set; }
        internal Pose Head { get; private set; }
        internal Pose LeftHand { get; private set; }
        internal Pose RightHand { get; private set; }

        internal void Receive(RigNetworkSample sample, bool teleported)
        {
            bool jumped = teleported && Vector3.Distance(sample.BodyPosition, target.BodyPosition) > TeleportSnapDistance;
            if (!Ready || jumped) snapPending = true;
            target = sample;
            Ready = true;
        }

        internal void Advance(float lerp, Vector3 headBodyOffset, float scale)
        {
            if (!Ready) return;
            bool snap = snapPending;
            snapPending = false;
            Quaternion shownRotation = snap ? target.BodyRotation : BodyRotation;
            BodyPosition = snap ? target.BodyPosition : Vector3.Lerp(BodyPosition, target.BodyPosition, lerp * BodyPositionLerpScale);
            BodyRotation = snap ? target.BodyRotation : Quaternion.Lerp(BodyRotation, target.BodyRotation, lerp);
            float limbLerp = snap ? 1f : lerp;
            Head = Approach(Head, new Pose(shownRotation * -headBodyOffset * scale, target.HeadRotation), limbLerp);
            LeftHand = Approach(LeftHand, target.LeftHand, limbLerp);
            RightHand = Approach(RightHand, target.RightHand, limbLerp);
        }

        internal void Reset()
        {
            Ready = false;
            snapPending = false;
        }

        private static Pose Approach(Pose current, Pose goal, float lerp) =>
            new Pose(Vector3.Lerp(current.position, goal.position, lerp), Quaternion.Lerp(current.rotation, goal.rotation, lerp));
    }
}
