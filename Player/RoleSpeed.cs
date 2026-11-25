// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using UnityEngine;
using Zetori.Base;

namespace Zetori.Player
{
    internal static class RoleSpeed
    {
        private const float ScaleTolerance = 0.05f;

        private static readonly Func<GorillaTagManager, int, float>? InfectedLimit = Limit("InterpolatedInfectedJumpSpeed");
        private static readonly Func<GorillaTagManager, int, float>? SurvivorLimit = Limit("InterpolatedNoobJumpSpeed");

        internal static float? LimitOf(VRRig rig)
        {
            if (!(GorillaGameManager.instance is GorillaTagManager manager) || manager.GetType() != typeof(GorillaTagManager)) return null;
            if (Mathf.Abs(rig.scaleFactor - 1f) > ScaleTolerance) return null;
            bool tagged = manager.LocalIsTagged(rig.Creator);
            if (manager.isCurrentlyTag) return tagged ? manager.fastJumpLimit : manager.slowJumpLimit;
            Func<GorillaTagManager, int, float>? limit = tagged ? InfectedLimit : SurvivorLimit;
            return limit != null ? limit(manager, manager.currentInfected.Count) : manager.fastJumpLimit;
        }

        private static Func<GorillaTagManager, int, float>? Limit(string method)
        {
            if (AccessTools.Method(typeof(GorillaTagManager), method, new[] { typeof(int) }) is System.Reflection.MethodInfo info)
                return AccessTools.MethodDelegate<Func<GorillaTagManager, int, float>>(info);
            Log.Warning($"The game's {method} is missing in this game version; the speed signal allows every player the fastest tagger speed.");
            return null;
        }
    }
}
