// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using Zetori.Base;
using Zetori.Player;

namespace Zetori.Patches
{
    [HarmonyPatch(typeof(VRRig), "SerializeReadShared")]
    internal static class RigSyncPatch
    {
        private const int TeleportedFlag = 0x100;

        static void Postfix(VRRig __instance, InputStruct data)
        {
            try { CheatSignals.ObserveSync(__instance, (data.packedFields & TeleportedFlag) != 0, data.serverTimeStamp, data.isGroundedHand || data.isGroundedButt); }
            catch (Exception e) { Log.ExceptionOnce("rig-sync", e, "Reading a player's position failed"); }
        }
    }
}
