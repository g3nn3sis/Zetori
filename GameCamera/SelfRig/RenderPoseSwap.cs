// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.SelfRig
{
    internal sealed class RenderPoseSwap
    {
        private const int PartCount = 10;

        private readonly Transform?[] parts = new Transform?[PartCount];
        private readonly Vector3[] localPositions = new Vector3[PartCount];
        private readonly Quaternion[] localRotations = new Quaternion[PartCount];
        private Transform? body;
        private Vector3 bodyPosition;
        private Quaternion bodyRotation;

        internal void Apply(VRRig rig, GorillaIK ik, InterpolatedRig pose)
        {
            Save(rig, ik);
            rig.transform.SetPositionAndRotation(pose.BodyPosition, pose.BodyRotation);
            rig.head.rigTarget.SetLocalPositionAndRotation(pose.Head.position, pose.Head.rotation);
            rig.leftHand.rigTarget.SetLocalPositionAndRotation(pose.LeftHand.position, pose.LeftHand.rotation);
            rig.rightHand.rigTarget.SetLocalPositionAndRotation(pose.RightHand.position, pose.RightHand.rotation);
            RigArmSolver.Solve(ik);
        }

        internal void Restore()
        {
            if (body == null) return;
            for (int i = 0; i < PartCount; i++)
            {
                Transform? part = parts[i];
                if (part != null) part.SetLocalPositionAndRotation(localPositions[i], localRotations[i]);
                parts[i] = null;
            }
            body.SetPositionAndRotation(bodyPosition, bodyRotation);
            body = null;
        }

        private void Save(VRRig rig, GorillaIK ik)
        {
            body = rig.transform;
            body.GetPositionAndRotation(out bodyPosition, out bodyRotation);
            Remember(0, rig.head.rigTarget);
            Remember(1, rig.leftHand.rigTarget);
            Remember(2, rig.rightHand.rigTarget);
            Remember(3, ik.headBone);
            Remember(4, ik.leftUpperArm);
            Remember(5, ik.leftLowerArm);
            Remember(6, ik.leftHand);
            Remember(7, ik.rightUpperArm);
            Remember(8, ik.rightLowerArm);
            Remember(9, ik.rightHand);
        }

        private void Remember(int index, Transform part)
        {
            parts[index] = part;
            part.GetLocalPositionAndRotation(out localPositions[index], out localRotations[index]);
        }
    }
}
