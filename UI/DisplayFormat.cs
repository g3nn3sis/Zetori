// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;

namespace Zetori.UI
{
    internal static class DisplayFormat
    {
        internal static string Clock(float seconds)
        {
            var span = TimeSpan.FromSeconds(Math.Max(0f, seconds));
            return span.TotalHours >= 1d
                ? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
                : span.ToString(@"m\:ss", CultureInfo.InvariantCulture);
        }

        internal static string Decimal(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
