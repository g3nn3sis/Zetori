// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

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
                BitPackUtils.UnpackWorldPosFromNetwork(BitPackUtils.PackWorldPosForNetwork(body.position)),
                Quantize(body.rotation),
                Quantize(rig.head.rigTarget.localRotation),
                QuantizeHand(rig.leftHand.rigTarget),
                QuantizeHand(rig.rightHand.rigTarget));
        }

        private static Quaternion Quantize(Quaternion rotation) =>
            BitPackUtils.UnpackQuaternionFromNetwork(BitPackUtils.PackQuaternionForNetwork(rotation));

        private static Pose QuantizeHand(Transform hand)
        {
            hand.GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);
            BitPackUtils.UnpackHandPosRotFromNetwork(BitPackUtils.PackHandPosRotForNetwork(localPosition, localRotation), out Vector3 position, out Quaternion rotation);
            return new Pose(position, rotation);
        }
    }
}
