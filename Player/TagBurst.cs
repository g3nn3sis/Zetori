// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Player
{
    internal sealed class TagBurst
    {
        internal const float Window = 0.5f;
        internal const int BurstTags = 3;
        private const int BurstsToFlag = 2;
        private const int LargeBurstTags = 4;

        private readonly Queue<float> recent = new Queue<float>();
        private float lastBurstTag = float.NegativeInfinity;

        internal int Bursts { get; private set; }
        internal int LargestBurst { get; private set; }
        internal bool Bursting => Bursts >= BurstsToFlag || LargestBurst >= LargeBurstTags;

        internal void Observe(float time)
        {
            while (recent.Count > 0 && time - recent.Peek() > Window) recent.Dequeue();
            recent.Enqueue(time);
            if (recent.Count < BurstTags) return;
            if (recent.Peek() > lastBurstTag) Bursts++;
            lastBurstTag = time;
            LargestBurst = Mathf.Max(LargestBurst, recent.Count);
        }
    }
}
