// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Cheats.Signals
{
    internal sealed class RigTrail
    {
        private const int Capacity = 40;

        private readonly float[] times = new float[Capacity];
        private readonly Vector3[] bodies = new Vector3[Capacity];
        private readonly Vector3[] heads = new Vector3[Capacity];
        private readonly Vector3[] leftHands = new Vector3[Capacity];
        private readonly Vector3[] rightHands = new Vector3[Capacity];
        private int newest = -1;
        private int count;

        internal void Add(float time, Vector3 body, Vector3 head, Vector3 leftHand, Vector3 rightHand)
        {
            newest = (newest + 1) % Capacity;
            times[newest] = time;
            bodies[newest] = body;
            heads[newest] = head;
            leftHands[newest] = leftHand;
            rightHands[newest] = rightHand;
            count = Mathf.Min(count + 1, Capacity);
        }

        internal float ClosestHandTo(RigTrail target, float since, float skew)
        {
            float closest = float.PositiveInfinity;
            for (int age = 0; age < count; age++)
            {
                int hand = IndexOf(age);
                if (times[hand] < since) break;
                for (int targetAge = 0; targetAge < target.count; targetAge++)
                {
                    int body = target.IndexOf(targetAge);
                    float gap = target.times[body] - times[hand];
                    if (gap > skew) continue;
                    if (gap < -skew) break;
                    closest = Mathf.Min(closest, HandDistance(leftHands[hand], target, body), HandDistance(rightHands[hand], target, body));
                }
            }
            return closest;
        }

        private static float HandDistance(Vector3 hand, RigTrail target, int index) =>
            Mathf.Min(Vector3.Distance(hand, target.bodies[index]), Vector3.Distance(hand, target.heads[index]));

        private int IndexOf(int age) => (newest - age + Capacity) % Capacity;
    }
}
