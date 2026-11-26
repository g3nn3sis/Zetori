// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using Zetori.Base;

namespace Zetori.Cheats.Signals
{
    [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.HandleTagBroadcast), typeof(NetPlayer), typeof(NetPlayer))]
    internal static class TagBroadcastPatch
    {
        static void Postfix(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
        {
            if (taggedPlayer == null || taggingPlayer == null) return;
            try { CheatSignals.ObserveTag(taggedPlayer, taggingPlayer); }
            catch (Exception e) { Log.ExceptionOnce("tag-broadcast", e, "Reading a tag failed"); }
        }
    }
}
