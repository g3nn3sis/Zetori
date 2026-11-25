// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.UI.InGame
{
    internal readonly struct InGameClip
    {
        private const float Tolerance = 0.0005f;

        internal static readonly InGameClip None = new InGameClip(float.NegativeInfinity, float.PositiveInfinity);

        internal readonly float Bottom;
        internal readonly float Top;

        internal InGameClip(float bottom, float top)
        {
            Bottom = bottom;
            Top = top;
        }

        internal bool Contains(float y) => y >= Bottom && y <= Top;

        internal bool Covers(float bottom, float top) => bottom >= Bottom - Tolerance && top <= Top + Tolerance;

        internal bool CoversBox(float centerY, float height) => Covers(centerY - height * 0.5f, centerY + height * 0.5f);

        internal bool Misses(float centerY, float height) => centerY - height * 0.5f >= Top - Tolerance || centerY + height * 0.5f <= Bottom + Tolerance;

        internal bool Trim(float centerY, float height, out float bottom, out float top)
        {
            bottom = Mathf.Max(centerY - height * 0.5f, Bottom);
            top = Mathf.Min(centerY + height * 0.5f, Top);
            return top > bottom;
        }
    }
}
