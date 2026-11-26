// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Text;
using UnityEngine;
using Zetori.Player;

namespace Zetori.UI.InGame
{
    internal static class InGameDetails
    {
        internal const string ValueColor = "FFFFFF";
        internal const string Unknown = "Unknown";
        private const string ValueColumn = "<pos=38%>";
        private const float MinimumNameLuminance = 0.3f;
        private const float NameLift = 0.45f;
        private const int ColorCodeSteps = 9;

        internal static void Append(StringBuilder builder, string label, string value, string hex = ValueColor)
        {
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(label).Append(ValueColumn).Append("<color=#").Append(hex).Append('>').Append(value).Append("</color>");
        }

        internal static Color Legible(Color color)
        {
            color.a = 1f;
            float luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
            return luminance < MinimumNameLuminance ? Color.Lerp(color, Color.white, NameLift) : color;
        }

        internal static string ColorCode(Color color) => $"{CodeOf(color.r)} {CodeOf(color.g)} {CodeOf(color.b)}";

        internal static string PlatformText(int platform)
        {
            string name = Platform.Name(platform);
            return name.Length > 0 ? name : Unknown;
        }

        private static int CodeOf(float channel) => Mathf.RoundToInt(Mathf.Clamp01(channel) * ColorCodeSteps);
    }
}
