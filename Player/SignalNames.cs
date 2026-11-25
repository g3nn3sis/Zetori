// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Player
{
    internal static class SignalNames
    {
        internal static readonly (SignalKind Kind, string Name)[] All =
        {
            (SignalKind.GameReport, "Game report"),
            (SignalKind.ModData, "Mod data"),
            (SignalKind.Name, "Odd name"),
            (SignalKind.QuestTweaks, "Quest tweaks"),
            (SignalKind.Cosmetics, "Unowned cosmetics"),
            (SignalKind.Speed, "Too fast"),
            (SignalKind.Teleports, "Teleporting"),
            (SignalKind.Hovering, "Hovering"),
            (SignalKind.ArmReach, "Long arms"),
            (SignalKind.TagReach, "Far tags"),
            (SignalKind.HandMotion, "Inhuman hands"),
            (SignalKind.NetworkSpam, "Network spam"),
            (SignalKind.LowGravity, "Low gravity"),
            (SignalKind.TagBurst, "Tag burst"),
            (SignalKind.FlaggedBefore, "Flagged before")
        };

        internal static string Describe(SignalKind kinds) =>
            string.Join(", ", All.Where(entry => (kinds & entry.Kind) != 0).Select(entry => entry.Name));
    }
}
