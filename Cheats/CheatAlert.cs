// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Cheats
{
    internal readonly struct CheatAlert
    {
        private const float Lifetime = 6f;
        private const float FadeIn = 0.2f;
        private const float FadeOut = 0.8f;

        internal CheatAlert(string text, float born)
        {
            Text = text;
            Born = born;
        }

        internal string Text { get; }

        internal float Born { get; }

        internal bool Expired(float now) => now - Born >= Lifetime;

        internal float Opacity(float now)
        {
            float age = now - Born;
            return Mathf.Min(Mathf.Clamp01(age / FadeIn), Mathf.Clamp01((Lifetime - age) / FadeOut));
        }
    }
}
