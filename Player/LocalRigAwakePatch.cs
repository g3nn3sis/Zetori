// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;

namespace Zetori.Player
{
    [HarmonyPatch(typeof(VRRig), "Awake")]
    internal static class LocalRigAwakePatch
    {
        private const string LocalRigName = "Local Gorilla Player(Clone)";

        static bool Prefix(VRRig __instance) => __instance.gameObject.name != LocalRigName;
    }
}
