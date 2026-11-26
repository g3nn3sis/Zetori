// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Cheats.Signals
{
    internal sealed class TalliedSignal
    {
        private readonly float fadeSeconds;
        private readonly float toFlag;
        private float score;
        private float updated = float.NaN;

        internal TalliedSignal(float fadeSeconds, float toFlag)
        {
            this.fadeSeconds = fadeSeconds;
            this.toFlag = toFlag;
        }

        internal float Total { get; private set; }
        internal bool Raised { get; private set; }

        internal void Add(float now, float amount)
        {
            score = ScoreAt(now) + amount;
            updated = now;
            Total += amount;
            if (score >= toFlag) Raised = true;
        }

        private float ScoreAt(float now) =>
            float.IsNaN(updated) ? 0f : score * Mathf.Exp(-Mathf.Max(0f, now - updated) / fadeSeconds);
    }
}
