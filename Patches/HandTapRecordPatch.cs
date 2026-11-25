// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using Zetori.Base;
using Zetori.Replay;

namespace Zetori.Patches
{
    [HarmonyPatch(typeof(VRRig), TargetMethod)]
    internal static class HandTapRecordPatch
    {
        private const string TargetMethod = "SetHandEffectData";

        static bool Prepare()
        {
            if (AccessTools.Method(typeof(VRRig), TargetMethod) != null && HandEffectReader.Available) return true;
            Log.WarningOnce("replay-hand-tap-unavailable", "Hand taps cannot be recorded in this game version; replays will have no hand-tap sounds.");
            return false;
        }

        static void Postfix(VRRig __instance, object effectContext, int audioClipIndex, bool isLeftHand)
        {
            if (!ReplaySession.IsCapturing) return;
            try
            {
                if (HandEffectReader.TryReadSound(effectContext, out float volume))
                    ReplaySession.RecordHandTap(__instance, isLeftHand, audioClipIndex, volume);
            }
            catch (Exception e) { Log.ExceptionOnce("replay-hand-tap", e, "Recording a hand tap failed"); }
        }
    }
}
