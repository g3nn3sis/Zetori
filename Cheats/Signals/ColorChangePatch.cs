// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using Zetori.Base;

namespace Zetori.Cheats.Signals
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.InitializeNoobMaterial))]
    internal static class ColorChangePatch
    {
        static void Prefix(PhotonMessageInfoWrapped info)
        {
            try { CheatSignals.ObserveColorChange(info.senderID); }
            catch (Exception e) { Log.ExceptionOnce("color-change-signal", e, "Reading a colour change failed"); }
        }
    }
}
