// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Replay.Render
{
    internal readonly struct VideoFormat
    {
        internal VideoFormat(int width, int height, int frameRate)
        {
            Width = width;
            Height = height;
            FrameRate = frameRate;
        }

        internal int Width { get; }
        internal int Height { get; }
        internal int FrameRate { get; }
    }
}
