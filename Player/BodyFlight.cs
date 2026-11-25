// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Player
{
    internal sealed class BodyFlight
    {
        internal const float Gravity = 9.81f;
        internal const float LowPull = 0.4f;
        private const float Span = 0.6f;
        private const float MinimumPart = Span / 3f;
        private const float MaximumGap = 0.3f;
        private const float MaximumStep = 0.1f;
        private const float MaximumFallSpeed = 10f;
        private const float FadeSeconds = 30f;
        private const float FloatSecondsToFlag = 1.5f;

        private readonly List<(double Time, float Height)> samples = new List<(double Time, float Height)>();
        private double? lastTime;
        private float score;

        internal float FloatSeconds { get; private set; }
        internal float WeakestPull { get; private set; } = float.PositiveInfinity;
        internal bool Floating { get; private set; }

        internal void Observe(double time, float height, bool free, float scale)
        {
            float step = lastTime is double previous ? (float)(time - previous) : 0f;
            lastTime = time;
            score *= Mathf.Exp(-Mathf.Clamp(step, 0f, MaximumStep) / FadeSeconds);
            if (!free || step <= 0f || step > MaximumGap) samples.Clear();
            if (!free) return;
            samples.Add((time, height));
            Trim(time);
            if (!TryMeasure(scale, out float pull, out float fallSpeed)) return;
            WeakestPull = Mathf.Min(WeakestPull, pull);
            if (pull >= LowPull || fallSpeed >= MaximumFallSpeed * scale) return;
            float counted = Mathf.Min(step, MaximumStep);
            score += counted;
            FloatSeconds += counted;
            if (score >= FloatSecondsToFlag) Floating = true;
        }

        private void Trim(double now)
        {
            int stale = 0;
            while (stale < samples.Count - 1 && now - samples[stale + 1].Time >= Span) stale++;
            if (stale > 0) samples.RemoveRange(0, stale);
        }

        private bool TryMeasure(float scale, out float pull, out float fallSpeed)
        {
            pull = 0f;
            fallSpeed = 0f;
            (double startTime, float startHeight) = samples[0];
            (double endTime, float endHeight) = samples[samples.Count - 1];
            if (endTime - startTime < Span) return false;
            (double middleTime, float middleHeight) = Middle((startTime + endTime) * 0.5);
            float first = (float)(middleTime - startTime);
            float second = (float)(endTime - middleTime);
            if (first < MinimumPart || second < MinimumPart) return false;
            float rising = (middleHeight - startHeight) / first;
            float falling = (endHeight - middleHeight) / second;
            float acceleration = (falling - rising) / ((first + second) * 0.5f);
            pull = -acceleration / (Gravity * scale);
            fallSpeed = -falling;
            return true;
        }

        private (double Time, float Height) Middle(double target)
        {
            (double Time, float Height) closest = samples[0];
            foreach ((double Time, float Height) sample in samples)
                if (Math.Abs(sample.Time - target) < Math.Abs(closest.Time - target)) closest = sample;
            return closest;
        }
    }
}
