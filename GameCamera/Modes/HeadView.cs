// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using UnityEngine;

namespace Zetori.GameCamera.Modes
{
    internal static class HeadView
    {
        internal static void Follow(CameraWrapper camera, VRRig target) => Follow(camera, Eyes(target));

        internal static void Follow(CameraWrapper camera, Pose eyes) => camera.SetPose(eyes.position, eyes.rotation);

        internal static Pose Eyes(VRRig target)
        {
            GorillaTagger? tagger = GorillaTagger.Instance;
            if (tagger != null && target == tagger.offlineVRRig && GTPlayer.Instance != null)
                return new Pose(GTPlayer.Instance.headCollider.transform.position, target.headMesh.transform.rotation);
            return BodyEyes(target.transform, target.headBodyOffset, target.headMesh.transform.rotation);
        }

        internal static Pose BodyEyes(Transform body, Vector3 headBodyOffset, Quaternion headRotation)
        {
            Vector3 eyes = body.position + headBodyOffset + body.up * 0.09f + body.forward * 0.06f;
            return new Pose(eyes, headRotation);
        }
    }
}
