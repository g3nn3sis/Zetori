// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Cheats.Signals
{
    internal sealed class RateBurst
    {
        private readonly int burstSize;
        private readonly int burstsToFlag;
        private readonly Queue<float> recent = new Queue<float>();
        private float lastBurstEvent = float.NegativeInfinity;

        internal RateBurst(float window, int burstSize, int burstsToFlag)
        {
            Window = window;
            this.burstSize = burstSize;
            this.burstsToFlag = burstsToFlag;
        }

        internal float Window { get; }
        internal int Events { get; private set; }
        internal int Bursts { get; private set; }
        internal int Busiest { get; private set; }
        internal bool Bursting => Bursts >= burstsToFlag;

        internal void Observe(float time)
        {
            Events++;
            while (recent.Count > 0 && time - recent.Peek() > Window) recent.Dequeue();
            recent.Enqueue(time);
            Busiest = Mathf.Max(Busiest, recent.Count);
            if (recent.Count < burstSize) return;
            if (recent.Peek() > lastBurstEvent) Bursts++;
            lastBurstEvent = time;
        }
    }
}
