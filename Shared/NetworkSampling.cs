// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Photon.Pun;
using UnityEngine;

namespace Zetori.Shared
{
    internal static class NetworkSampling
    {
        private const float FallbackSendRate = 10f;

        internal static float SendInterval => 1f / (PhotonNetwork.SerializationRate > 0 ? PhotonNetwork.SerializationRate : FallbackSendRate);

        internal static Vector3 WorldPosition(Vector3 position) =>
            BitPackUtils.UnpackWorldPosFromNetwork(BitPackUtils.PackWorldPosForNetwork(position));

        internal static Quaternion Rotation(Quaternion rotation) =>
            BitPackUtils.UnpackQuaternionFromNetwork(BitPackUtils.PackQuaternionForNetwork(rotation));

        internal static Pose Hand(Vector3 localPosition, Quaternion localRotation)
        {
            BitPackUtils.UnpackHandPosRotFromNetwork(BitPackUtils.PackHandPosRotForNetwork(localPosition, localRotation), out Vector3 position, out Quaternion rotation);
            return new Pose(position, rotation);
        }
    }
}
