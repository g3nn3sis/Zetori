// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Reflection;
using HarmonyLib;
using Zetori.Base;

namespace Zetori.TagManager
{
    internal static class TagManagerPatches
    {
        [HarmonyPatch]
        private static class FirstTaggedPickPatch
        {
            private static readonly MethodInfo RandomPick = SymbolExtensions.GetMethodInfo(() => UnityEngine.Random.Range(0, 0));
            private static readonly MethodInfo LocalFirstPick = SymbolExtensions.GetMethodInfo(() => TagManagerControl.PickFirstTagged(0, 0));

            static IEnumerable<MethodBase> TargetMethods() =>
                new[] { nameof(GorillaTagManager.UpdateState), "InfectionRoundStart", nameof(GorillaTagManager.OnPlayerLeftRoom) }
                    .Select(name => (MethodBase?)AccessTools.DeclaredMethod(typeof(GorillaTagManager), name))
                    .Where(method => method != null)
                    .Select(method => method!);

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                List<CodeInstruction> code = instructions.ToList();
                int replaced = 0;
                foreach (CodeInstruction instruction in code.Where(instruction => instruction.Calls(RandomPick)))
                {
                    instruction.operand = LocalFirstPick;
                    replaced++;
                }
                if (replaced == 0) Log.Warning($"GorillaTagManager.{original.Name} no longer picks a random player, so Me First Each Round does not apply there.");
                return code;
            }
        }

        [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.UpdateState))]
        private static class UpdateStatePatch
        {
            static bool Prefix(GorillaTagManager __instance) => !TagManagerControl.Controls(__instance);
        }

        [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.UpdateInfectionState))]
        private static class UpdateInfectionStatePatch
        {
            static bool Prefix(GorillaTagManager __instance) => !TagManagerControl.Controls(__instance);
        }

        [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.NewVRRig))]
        private static class NewVRRigPatch
        {
            static bool Prefix(GorillaTagManager __instance) => !TagManagerControl.Controls(__instance);
        }

        [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.ReportTag))]
        private static class ReportTagPatch
        {
            static bool Prefix(GorillaTagManager __instance) => !TagManagerControl.BlocksPlayerTags(__instance);
        }

        [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.HitPlayer))]
        private static class HitPlayerPatch
        {
            static bool Prefix(GorillaTagManager __instance) => !TagManagerControl.BlocksPlayerTags(__instance);
        }

        [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.LocalCanTag))]
        private static class LocalCanTagPatch
        {
            static bool Prefix(GorillaTagManager __instance, ref bool __result)
            {
                if (!TagManagerControl.BlocksPlayerTags(__instance)) return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.OnPlayerLeftRoom))]
        private static class OnPlayerLeftRoomPatch
        {
            static bool Prefix(GorillaTagManager __instance, NetPlayer otherPlayer)
            {
                if (!TagManagerControl.HandlesPlayerLeaving(__instance)) return true;
                TagManagerControl.ForgetPlayer(__instance, otherPlayer);
                return false;
            }
        }
    }
}
