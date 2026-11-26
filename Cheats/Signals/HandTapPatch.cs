// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using HarmonyLib;
using Zetori.Base;

namespace Zetori.Cheats.Signals
{
    [HarmonyPatch(typeof(VRRig), TargetMethod)]
    internal static class HandTapPatch
    {
        private const string TargetMethod = "SetHandEffectData";

        static bool Prepare()
        {
            if (AccessTools.Method(typeof(VRRig), TargetMethod) != null) return true;
            Log.WarningOnce("hand-tap-signal-unavailable", "Hand taps cannot be read in this game version; the fake taps signal is off.");
            return false;
        }

        static void Postfix(VRRig __instance, bool isLeftHand, StiltID stiltID)
        {
            try { CheatSignals.ObserveHandTap(__instance, isLeftHand, stiltID != StiltID.None); }
            catch (Exception e) { Log.ExceptionOnce("hand-tap-signal", e, "Reading a player's hand tap failed"); }
        }
    }
}
