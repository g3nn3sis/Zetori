// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.SelfRig
{
    internal static class RigArmSolver
    {
        private const float MinimumReach = 0.001f;

        private static readonly Vector3 UpperArmOffset = new Vector3(0f, 0.1454885f, -0.02598158f);
        private static readonly Vector3 ForearmOffset = new Vector3(0f, 0.4061671f, 0f);
        private static readonly Vector3 HandOffset = new Vector3(0f, 0.3816895f, 0f);
        private static readonly Vector3 BodyPivot = new Vector3(0f, 0.011406422f, 1.6582015f);
        private static readonly Vector3 LeftShoulderPosition = new Vector3(-0.018300775f, -0.04206751f, 0.08612572f);
        private static readonly Vector3 RightShoulderPosition = new Vector3(0.018300813f, -0.042066876f, 0.08613044f);
        private static readonly Quaternion LeftShoulderRotation = new Quaternion(-0.59150106f, 0.3665933f, 0.20795153f, 0.68738055f);
        private static readonly Quaternion RightShoulderRotation = new Quaternion(-0.591501f, -0.3665933f, -0.20795153f, 0.6873807f);

        internal static void Solve(GorillaIK ik)
        {
            bool updatedIk = ik.usingUpdatedIK;
            Quaternion bodyRotation = ik.lerpBodyRot;
            var left = new Arm(ik.initialUpperLeft, ik.initialLowerLeft, LeftShoulderPosition, LeftShoulderRotation, ik.leftUpperArm, ik.leftLowerArm, ik.leftHand, ik.targetLeft);
            var right = new Arm(ik.initialUpperRight, ik.initialLowerRight, RightShoulderPosition, RightShoulderRotation, ik.rightUpperArm, ik.rightLowerArm, ik.rightHand, ik.targetRight);
            SolveArm(left, ik.GetShoulderLocalTargetPos_Left(updatedIk), ik.lerpLeftElbowDirection, updatedIk, bodyRotation);
            SolveArm(right, ik.GetShoulderLocalTargetPos_Right(updatedIk), ik.lerpRightElbowDirection, updatedIk, bodyRotation);
            ik.headBone.rotation = ik.targetHead.rotation;
        }

        private static void SolveArm(Arm arm, Vector3 target, Vector3 elbowDirection, bool updatedIk, Quaternion bodyRotation)
        {
            Vector3 shoulder = UpperArmOffset;
            Quaternion restLowerFrame = arm.InitialUpper * arm.InitialLower;
            Vector3 elbow = shoulder + arm.InitialUpper * ForearmOffset;
            Vector3 wrist = elbow + restLowerFrame * HandOffset;
            float upperLength = (shoulder - elbow).magnitude;
            float lowerLength = (elbow - wrist).magnitude;
            float maximumReach = upperLength + lowerLength - MinimumReach;
            Vector3 restReach = (wrist - shoulder).normalized;
            Vector3 restUpper = (elbow - shoulder).normalized;
            Vector3 restLower = (wrist - elbow).normalized;
            Vector3 toTarget = (target - shoulder).normalized;
            float reach = Mathf.Clamp((target - shoulder).magnitude, MinimumReach, maximumReach);

            float restShoulderAngle = AngleBetween(restReach, restUpper);
            float restElbowAngle = AngleBetween(-restUpper, restLower);
            float swingAngle = AngleBetween(restReach, toTarget);
            float shoulderAngle = LawOfCosines(lowerLength, upperLength, reach);
            float elbowAngle = LawOfCosines(reach, upperLength, lowerLength);

            Vector3 bendAxis = Vector3.Cross(restReach, restUpper).normalized;
            Vector3 swingAxis = Vector3.Cross(restReach, toTarget).normalized;
            Quaternion shoulderBend = Quaternion.AngleAxis((shoulderAngle - restShoulderAngle) * Mathf.Rad2Deg, Quaternion.Inverse(arm.InitialUpper) * bendAxis);
            Quaternion elbowBend = Quaternion.AngleAxis((elbowAngle - restElbowAngle) * Mathf.Rad2Deg, Quaternion.Inverse(restLowerFrame) * bendAxis);
            Quaternion swing = Quaternion.AngleAxis(swingAngle * Mathf.Rad2Deg, Quaternion.Inverse(arm.InitialUpper) * swingAxis);
            Quaternion upperRotation = arm.InitialUpper * swing * shoulderBend;
            Quaternion lowerRotation = arm.InitialLower * elbowBend;

            Quaternion shoulderFrame = bodyRotation * arm.ShoulderRotation;
            Quaternion upperFrame = shoulderFrame * upperRotation;
            Quaternion lowerFrame = upperFrame * lowerRotation;
            Vector3 handPosition = BodyPivot + bodyRotation * arm.ShoulderPosition + shoulderFrame * UpperArmOffset + upperFrame * ForearmOffset + lowerFrame * HandOffset;
            if (updatedIk) upperRotation = TwistTowardElbow(upperRotation, toTarget, elbowDirection);

            arm.Upper.localRotation = upperRotation;
            arm.Lower.localRotation = lowerRotation;
            arm.Hand.rotation = arm.Target.rotation;
            arm.Hand.localPosition = handPosition;
        }

        private static Quaternion TwistTowardElbow(Quaternion upperRotation, Vector3 toTarget, Vector3 elbowDirection)
        {
            Vector3 forearm = (upperRotation * ForearmOffset).normalized;
            Vector3 desiredPlane = Vector3.Cross(toTarget, elbowDirection.normalized).normalized;
            Vector3 currentPlane = Vector3.Cross(toTarget, forearm).normalized;
            return Quaternion.AngleAxis(Vector3.SignedAngle(currentPlane, desiredPlane, toTarget), toTarget) * upperRotation;
        }

        private static float AngleBetween(Vector3 from, Vector3 to) => Mathf.Acos(Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f));

        private static float LawOfCosines(float opposite, float adjacentA, float adjacentB) =>
            Mathf.Acos(Mathf.Clamp((opposite * opposite - adjacentA * adjacentA - adjacentB * adjacentB) / (-2f * adjacentA * adjacentB), -1f, 1f));

        private readonly struct Arm
        {
            internal readonly Quaternion InitialUpper;
            internal readonly Quaternion InitialLower;
            internal readonly Vector3 ShoulderPosition;
            internal readonly Quaternion ShoulderRotation;
            internal readonly Transform Upper;
            internal readonly Transform Lower;
            internal readonly Transform Hand;
            internal readonly Transform Target;

            internal Arm(Quaternion initialUpper, Quaternion initialLower, Vector3 shoulderPosition, Quaternion shoulderRotation, Transform upper, Transform lower, Transform hand, Transform target)
            {
                InitialUpper = initialUpper;
                InitialLower = initialLower;
                ShoulderPosition = shoulderPosition;
                ShoulderRotation = shoulderRotation;
                Upper = upper;
                Lower = lower;
                Hand = hand;
                Target = target;
            }
        }
    }
}
