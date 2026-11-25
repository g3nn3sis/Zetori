// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Player
{
    internal sealed class MessageRate
    {
        private const float FadeSeconds = 30f;
        private const float SpamSecondsToFlag = 2.5f;

        private float score;

        internal int TopRate { get; private set; }
        internal int SpamSeconds { get; private set; }
        internal bool Spamming { get; private set; }

        internal void Observe(int rate, float spamRate)
        {
            score *= Mathf.Exp(-1f / FadeSeconds);
            TopRate = Mathf.Max(TopRate, rate);
            if (rate < spamRate) return;
            score += 1f;
            SpamSeconds++;
            if (score >= SpamSecondsToFlag) Spamming = true;
        }
    }
}
