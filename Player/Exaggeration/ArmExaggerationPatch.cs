// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using Zetori.Base;

namespace Zetori.Player.Exaggeration
{
    [HarmonyPatch(typeof(GorillaIKMgr), "CopyInput")]
    internal static class ArmExaggerationPatch
    {
        static void Prefix()
        {
            try { ArmExaggeration.ApplyToSpectatedRig(); }
            catch (Exception e) { Log.ExceptionOnce("arm-exaggeration", e, "Exaggerating the spectated arms failed"); }
        }
    }
}
