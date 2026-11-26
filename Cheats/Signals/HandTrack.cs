// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Cheats.Signals
{
    internal sealed class HandTrack
    {
        private const int Capacity = 48;

        private readonly float[] times = new float[Capacity];
        private readonly Vector3[] positions = new Vector3[Capacity];
        private readonly Vector3?[] velocities = new Vector3?[Capacity];
        private int newest = -1;
        private int count;
        private bool snapping;

        internal bool Observe(float time, Vector3 position, float window, float maximumSpan, out Vector3 velocity, out float acceleration)
        {
            Vector3? measured = VelocityAt(time, position, window, maximumSpan);
            acceleration = measured is Vector3 current && EarlierVelocity(time, window, maximumSpan) is (float earlierTime, Vector3 earlier)
                ? (current - earlier).magnitude / (time - earlierTime)
                : 0f;
            Push(time, position, measured);
            velocity = measured ?? Vector3.zero;
            return measured.HasValue;
        }

        internal bool StartsSnap(bool snap)
        {
            bool starts = snap && !snapping;
            snapping = snap;
            return starts;
        }

        private Vector3? VelocityAt(float time, Vector3 position, float window, float maximumSpan)
        {
            for (int age = 0; age < count; age++)
            {
                int index = IndexOf(age);
                float span = time - times[index];
                if (span < window) continue;
                return span > maximumSpan ? null : (position - positions[index]) / span;
            }
            return null;
        }

        private (float Time, Vector3 Velocity)? EarlierVelocity(float time, float window, float maximumSpan)
        {
            for (int age = 0; age < count; age++)
            {
                int index = IndexOf(age);
                float span = time - times[index];
                if (span < window) continue;
                if (span > maximumSpan) return null;
                if (velocities[index] is Vector3 velocity) return (times[index], velocity);
            }
            return null;
        }

        private void Push(float time, Vector3 position, Vector3? velocity)
        {
            newest = (newest + 1) % Capacity;
            times[newest] = time;
            positions[newest] = position;
            velocities[newest] = velocity;
            count = Mathf.Min(count + 1, Capacity);
        }

        private int IndexOf(int age) => (newest - age + Capacity) % Capacity;
    }
}
