// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;

namespace Zetori.Player
{
    [HarmonyPatch(typeof(VRRigJobManager), "DeregisterVRRig")]
    internal static class LocalRigDeregisterPatch
    {
        static bool Prefix(VRRig rig) => rig == null || !rig.isLocal;
    }
}
