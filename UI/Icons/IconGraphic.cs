// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI.Icons
{
    internal sealed class IconGraphic
    {
        internal float Left { get; }
        internal float Top { get; }
        internal float Width { get; }
        internal float Height { get; }
        internal IReadOnlyList<IconShape> Shapes { get; }
        internal float Extent => Math.Max(Width, Height);
        internal float CenterX => Left + Width * 0.5f;
        internal float CenterY => Top + Height * 0.5f;

        internal IconGraphic(float left, float top, float width, float height, IReadOnlyList<IconShape> shapes)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            Shapes = shapes;
        }
    }
}
