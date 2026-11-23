// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI.Icons
{
    internal sealed class IconShape
    {
        internal IReadOnlyList<IconContour> Contours { get; }
        internal bool Filled { get; }
        internal float StrokeWidth { get; }
        internal bool Stroked => StrokeWidth > 0f;

        internal IconShape(IReadOnlyList<IconContour> contours, bool filled, float strokeWidth)
        {
            Contours = contours;
            Filled = filled;
            StrokeWidth = strokeWidth;
        }
    }
}
