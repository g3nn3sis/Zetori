// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Cheats.Signals
{
    internal sealed class HandMotion
    {
        internal const float FastSpeed = 10f;
        private const float SnapAcceleration = 85f;
        private const float OverreachDistance = 1.3f;
        private const float OverreachSpeed = 6f;
        private const float Window = 0.2f;
        private const float MaximumSpan = 0.5f;
        private const float MaximumStep = 0.1f;
        private const float FadeSeconds = 30f;
        private const float FastSecondsToFlag = 1f / 3f;
        private const float SnapsToFlag = 5f / 3f;
        private const float OverreachSecondsToFlag = 1f / 3f;

        private readonly HandTrack left = new HandTrack();
        private readonly HandTrack right = new HandTrack();
        private float? lastTime;
        private float fastScore;
        private float snapScore;
        private float overreachScore;

        internal float TopSpeed { get; private set; }
        internal float FastSeconds { get; private set; }
        internal int Snaps { get; private set; }
        internal float OverreachSeconds { get; private set; }
        internal bool Inhuman { get; private set; }

        internal void Observe(float time, Vector3 head, Vector3 leftHand, Vector3 rightHand)
        {
            float step = lastTime is float previous ? Mathf.Clamp(time - previous, 0f, MaximumStep) : 0f;
            lastTime = time;
            Fade(step);
            (bool leftFast, bool leftOverreaching) = ObserveHand(left, time, head, leftHand);
            (bool rightFast, bool rightOverreaching) = ObserveHand(right, time, head, rightHand);
            if (leftFast || rightFast)
            {
                fastScore += step;
                FastSeconds += step;
            }
            if (leftOverreaching || rightOverreaching)
            {
                overreachScore += step;
                OverreachSeconds += step;
            }
            if (fastScore >= FastSecondsToFlag || snapScore >= SnapsToFlag || overreachScore >= OverreachSecondsToFlag) Inhuman = true;
        }

        private void Fade(float step)
        {
            float fade = Mathf.Exp(-step / FadeSeconds);
            fastScore *= fade;
            snapScore *= fade;
            overreachScore *= fade;
        }

        private (bool Fast, bool Overreaching) ObserveHand(HandTrack track, float time, Vector3 head, Vector3 hand)
        {
            bool measured = track.Observe(time, hand, Window, MaximumSpan, out Vector3 velocity, out float acceleration);
            if (track.StartsSnap(acceleration >= SnapAcceleration))
            {
                snapScore += 1f;
                Snaps++;
            }
            if (!measured) return (false, false);
            float speed = velocity.magnitude;
            TopSpeed = Mathf.Max(TopSpeed, speed);
            return (speed >= FastSpeed, IsOverreaching(hand - head, velocity));
        }

        private static bool IsOverreaching(Vector3 reach, Vector3 velocity)
        {
            float distance = reach.magnitude;
            return distance >= OverreachDistance && Vector3.Dot(velocity, reach) / distance >= OverreachSpeed;
        }
    }
}
