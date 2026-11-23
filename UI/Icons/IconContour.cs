// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI.Icons
{
    internal sealed class IconContour
    {
        internal float[] Points { get; }
        internal bool Closed { get; }
        internal int Count => Points.Length / 2;

        internal IconContour(float[] points, bool closed)
        {
            Points = points;
            Closed = closed;
        }

        internal float X(int index) => Points[index * 2];

        internal float Y(int index) => Points[index * 2 + 1];
    }
}
