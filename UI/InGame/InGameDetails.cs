// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Text;

namespace Zetori.UI.InGame
{
    internal static class InGameDetails
    {
        internal const string ValueColor = "FFFFFF";
        private const string ValueColumn = "<pos=38%>";

        internal static void Append(StringBuilder builder, string label, string value, string hex = ValueColor)
        {
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(label).Append(ValueColumn).Append("<color=#").Append(hex).Append('>').Append(value).Append("</color>");
        }
    }
}
