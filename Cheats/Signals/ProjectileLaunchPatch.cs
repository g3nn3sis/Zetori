// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Reflection;
using HarmonyLib;
using Zetori.Base;

namespace Zetori.Cheats.Signals
{
    [HarmonyPatch]
    internal static class ProjectileLaunchPatch
    {
        private const string TargetType = "RoomSystem";
        private const string TargetName = "DeserializeLaunchProjectile";

        static bool Prepare()
        {
            if (Target() != null) return true;
            Log.WarningOnce("projectile-signal-unavailable", "Projectile launches cannot be read in this game version; the projectile spam signal is off.");
            return false;
        }

        static MethodBase? TargetMethod() => Target();

        private static MethodInfo? Target() => AccessTools.TypeByName(TargetType) is Type type ? AccessTools.Method(type, TargetName) : null;

        static void Prefix(PhotonMessageInfoWrapped info)
        {
            try { CheatSignals.ObserveProjectile(info.senderID); }
            catch (Exception e) { Log.ExceptionOnce("projectile-signal", e, "Reading a projectile launch failed"); }
        }
    }
}
