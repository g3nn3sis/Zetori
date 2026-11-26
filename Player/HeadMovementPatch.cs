// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;

namespace Zetori.Player
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
    internal static class HeadMovementPatch
    {
        static void Postfix(VRRig __instance)
        {
            if (!__instance.isLocal) return;
            GorillaTagger tagger = GorillaTagger.Instance;
            if (tagger == null || tagger.headCollider == null) return;
            __instance.head.rigTarget.rotation = tagger.headCollider.transform.rotation;
        }
    }
}
