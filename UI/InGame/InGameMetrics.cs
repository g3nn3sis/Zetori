// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.UI.InGame
{
    internal static class InGameMetrics
    {
        internal const float FaceHalfWidth = 0.217f;
        internal const float FaceHalfHeight = 0.167f;
        internal const float FaceDepth = -0.0102f;
        internal const float Left = -0.206f;
        internal const float Right = 0.206f;
        internal const float Top = 0.156f;
        internal const float Bottom = -0.156f;

        internal const float ButtonHeight = 0.025f;
        internal const float TitleHeight = 0.025f;
        internal const float SliderHeight = 0.022f;
        internal const float SwatchHeight = 0.02f;
        internal const float RowGap = 0.0028f;
        internal const float ColumnGap = 0.0035f;
        internal const float RegionGap = 0.006f;
        internal const float ScrollbarWidth = 0.011f;
        internal const float ScrollbarGap = 0.005f;
        internal const float MinimumThumbHeight = 0.02f;

        internal const float QuadDepth = -0.0112f;
        internal const float FillDepth = -0.0117f;
        internal const float KnobDepth = -0.0122f;
        internal const float TextDepth = -0.0128f;

        internal const float ControlFontSize = 0.12f;
        internal const float LabelFontSize = 0.1f;
        internal const float TitleFontSize = 0.13f;
        internal const float MinimumFontSize = 0.055f;

        private const float LabelLineHeight = 0.0145f;
        private const float LabelPadding = 0.003f;
        private const float LabelCharacterWidth = 0.0052f;
        private const int MaximumLabelLines = 4;

        internal static float BodyContentWidth => Right - ScrollbarWidth - ScrollbarGap - Left;

        internal static float LabelHeight(string text, float width)
        {
            int perLine = Mathf.Max(1, Mathf.FloorToInt(width / LabelCharacterWidth));
            int lines = Mathf.Clamp((text.Length + perLine - 1) / perLine, 1, MaximumLabelLines);
            return lines * LabelLineHeight + LabelPadding;
        }
    }
}
