// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Cheats.Signals
{
    internal sealed class SyncTiming
    {
        internal const float GapSeconds = 1f;
        internal const float JumpDistance = 3f;
        internal const float BackwardsSeconds = 0.25f;
        internal const float SkewSeconds = 1f;
        private const double WrapSeconds = 1000d;
        private const float FreshSeconds = 1f;
        private const long OthersDuringGap = 3;
        private const float FadeSeconds = 60f;
        private const float TricksToFlag = 4f;

        private readonly TalliedSignal tricks = new TalliedSignal(FadeSeconds, TricksToFlag);
        private double? lastStamp;
        private Vector3 lastPosition;
        private long lastSyncs;
        private float updated = float.NegativeInfinity;

        internal float Offset { get; private set; }
        internal int Gaps { get; private set; }
        internal int Backwards { get; private set; }
        internal int SkewedSeconds { get; private set; }
        internal bool Tricking => tricks.Raised;

        internal bool Fresh(float now) => now - updated <= FreshSeconds;

        internal void ObserveSync(float now, double stamp, double received, Vector3 position, float scale, long syncs, bool teleported)
        {
            Offset = (float)(received - stamp);
            if (lastStamp is double previous && Math.Abs(stamp - previous) < WrapSeconds)
            {
                double step = stamp - previous;
                if (step < -BackwardsSeconds)
                {
                    Backwards++;
                    tricks.Add(now, 1f);
                }
                else if (step >= GapSeconds && syncs - lastSyncs > OthersDuringGap && !teleported && Vector3.Distance(position, lastPosition) >= JumpDistance * scale)
                {
                    Gaps++;
                    tricks.Add(now, 1f);
                }
            }
            lastStamp = stamp;
            lastPosition = position;
            lastSyncs = syncs;
            updated = now;
        }

        internal void ObserveRoomOffset(float now, float roomOffset)
        {
            if (Mathf.Abs(Offset - roomOffset) < SkewSeconds) return;
            SkewedSeconds++;
            tricks.Add(now, 1f);
        }
    }
}
