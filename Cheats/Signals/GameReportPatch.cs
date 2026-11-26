// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using Zetori.Base;

namespace Zetori.Cheats.Signals
{
    [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.SendReport))]
    internal static class GameReportPatch
    {
        static void Prefix(string susReason, string susId)
        {
            try { CheatSignals.ObserveReport(susId, susReason); }
            catch (Exception e) { Log.ExceptionOnce("game-report", e, "Reading the game's cheat report failed"); }
        }
    }
}
