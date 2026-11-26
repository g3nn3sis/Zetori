// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using Zetori.History;

namespace Zetori.UI.Panels
{
    internal static class StatsPanel
    {
        private const float MetresPerKilometre = 1000f;

        internal static void Draw(IPanelSurface panel)
        {
            panel.Label($"Time in rooms: {DisplayFormat.Clock(SessionStats.RoomSeconds)}");
            panel.Label($"Rooms joined: {SessionStats.RoomsJoined}");
            panel.Label($"Players met: {PlayerHistory.MetThisSession}");
            panel.Label($"Tags made: {SessionStats.TagsMade}");
            panel.Label($"Times tagged: {SessionStats.TimesTagged}");
            panel.Label($"Current run: {DisplayFormat.Clock(SessionStats.CurrentRun)}");
            panel.Label($"Longest run: {DisplayFormat.Clock(SessionStats.LongestRun)}");
            panel.Label($"Distance: {DistanceText(SessionStats.Distance)}");
            panel.Label($"Top speed: {DisplayFormat.Decimal(SessionStats.TopSpeed)} m/s");
            panel.Label("Counted since the game started. A run is time spent untagged while someone is tagged.");
            panel.Button("Reset", SessionStats.Reset);
        }

        internal static string Summary() => $"{SessionStats.TagsMade} tags, best run {DisplayFormat.Clock(SessionStats.LongestRun)}";

        private static string DistanceText(float metres) => metres >= MetresPerKilometre
            ? $"{(metres / MetresPerKilometre).ToString("0.00", CultureInfo.InvariantCulture)} km"
            : $"{metres.ToString("0", CultureInfo.InvariantCulture)} m";
    }
}
