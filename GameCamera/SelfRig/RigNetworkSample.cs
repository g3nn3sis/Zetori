// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Shared;
using UnityEngine;

namespace Zetori.GameCamera.SelfRig
{
    internal readonly struct RigNetworkSample
    {
        internal readonly Vector3 BodyPosition;
        internal readonly Quaternion BodyRotation;
        internal readonly Quaternion HeadRotation;
        internal readonly Pose LeftHand;
        internal readonly Pose RightHand;

        private RigNetworkSample(Vector3 bodyPosition, Quaternion bodyRotation, Quaternion headRotation, Pose leftHand, Pose rightHand)
        {
            BodyPosition = bodyPosition;
            BodyRotation = bodyRotation;
            HeadRotation = headRotation;
            LeftHand = leftHand;
            RightHand = rightHand;
        }

        internal static RigNetworkSample Capture(VRRig rig)
        {
            Transform body = rig.transform;
            return new RigNetworkSample(
                NetworkSampling.WorldPosition(body.position),
                NetworkSampling.Rotation(body.rotation),
                NetworkSampling.Rotation(rig.head.rigTarget.localRotation),
                Hand(rig.leftHand.rigTarget),
                Hand(rig.rightHand.rigTarget));
        }

        private static Pose Hand(Transform hand)
        {
            hand.GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);
            return NetworkSampling.Hand(localPosition, localRotation);
        }
    }
}
