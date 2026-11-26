// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Cheats.Signals
{
    internal sealed class HeadSpin
    {
        internal const float Window = 0.5f;
        internal const float SpinDegrees = 540f;
        private const float MaximumStep = 0.1f;
        private const float FadeSeconds = 30f;
        private const float SpinSecondsToFlag = 1f;

        private readonly Queue<(float Time, float Degrees)> turns = new Queue<(float Time, float Degrees)>();
        private readonly TalliedSignal spinning = new TalliedSignal(FadeSeconds, SpinSecondsToFlag);
        private Quaternion? last;
        private float lastTime;
        private float turned;

        internal float FastestTurn { get; private set; }
        internal float SpinSeconds => spinning.Total;
        internal bool Spinning => spinning.Raised;

        internal void Observe(float time, Quaternion rotation)
        {
            float step = time - lastTime;
            if (last is Quaternion previous && step > 0f && step <= MaximumStep) Turn(time, Quaternion.Angle(previous, rotation), step);
            else Clear();
            last = rotation;
            lastTime = time;
        }

        private void Turn(float time, float degrees, float step)
        {
            turns.Enqueue((time, degrees));
            turned += degrees;
            while (turns.Count > 0 && time - turns.Peek().Time >= Window) turned -= turns.Dequeue().Degrees;
            FastestTurn = Mathf.Max(FastestTurn, turned / Window);
            if (turned >= SpinDegrees) spinning.Add(time, step);
        }

        private void Clear()
        {
            turns.Clear();
            turned = 0f;
        }
    }
}
