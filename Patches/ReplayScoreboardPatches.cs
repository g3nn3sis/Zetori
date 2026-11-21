// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using Zetori.Replay;

namespace Zetori.Patches
{
    internal static class ReplayScoreboardPatches
    {
        [HarmonyPatch(typeof(GorillaScoreboardTotalUpdater), nameof(GorillaScoreboardTotalUpdater.UpdateScoreboard))]
        private static class BoardUpdatePatch
        {
            static bool Prefix(GorillaScoreBoard sB) => !ReplayScoreboard.TryShow(sB);
        }

        [HarmonyPatch(typeof(GorillaScoreBoard), nameof(GorillaScoreBoard.RedrawPlayerLines))]
        private static class BoardRedrawPatch
        {
            static bool Prefix(GorillaScoreBoard __instance) => !ReplayScoreboard.TryShow(__instance);
        }

        [HarmonyPatch(typeof(GorillaPlayerScoreboardLine), nameof(GorillaPlayerScoreboardLine.UpdateLine))]
        private static class LineUpdatePatch
        {
            static bool Prefix() => !ReplaySession.IsReplaying;
        }

        [HarmonyPatch(typeof(GorillaPlayerScoreboardLine), nameof(GorillaPlayerScoreboardLine.PressButton))]
        private static class LineButtonPatch
        {
            static bool Prefix() => !ReplaySession.IsReplaying;
        }
    }
}
