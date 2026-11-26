// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;

namespace Zetori.Player
{
    [HarmonyPatch(typeof(VRRig), "OnDisable")]
    internal static class LocalRigDisablePatch
    {
        static bool Prefix(VRRig __instance) => !__instance.isLocal;
    }
}
