// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using Zetori.Player;

namespace Zetori.UI.Panels
{
    internal static class StatsPanel
    {
        private const float MetresPerKilometre = 1000f;

        internal static void Draw(IPanelSurface panel)
        {
            panel.Label($"Time in rooms: {Duration(SessionStats.RoomSeconds)}");
            panel.Label($"Rooms joined: {SessionStats.RoomsJoined}");
            panel.Label($"Players met: {PlayerHistory.MetThisSession}");
            panel.Label($"Tags made: {SessionStats.TagsMade}");
            panel.Label($"Times tagged: {SessionStats.TimesTagged}");
            panel.Label($"Current run: {Duration(SessionStats.CurrentRun)}");
            panel.Label($"Longest run: {Duration(SessionStats.LongestRun)}");
            panel.Label($"Distance: {DistanceText(SessionStats.Distance)}");
            panel.Label($"Top speed: {SessionStats.TopSpeed.ToString("0.0", CultureInfo.InvariantCulture)} m/s");
            panel.Label("Counted since the game started. A run is time spent untagged while someone is tagged.");
            panel.Button("Reset", SessionStats.Reset);
        }

        internal static string Summary() => $"{SessionStats.TagsMade} tags, best run {Duration(SessionStats.LongestRun)}";

        private static string Duration(float seconds)
        {
            var span = TimeSpan.FromSeconds(Math.Max(0f, seconds));
            return span.TotalHours >= 1d
                ? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
                : span.ToString(@"m\:ss", CultureInfo.InvariantCulture);
        }

        private static string DistanceText(float metres) => metres >= MetresPerKilometre
            ? $"{(metres / MetresPerKilometre).ToString("0.00", CultureInfo.InvariantCulture)} km"
            : $"{metres.ToString("0", CultureInfo.InvariantCulture)} m";
    }
}
