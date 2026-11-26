// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Gameplay;
using GorillaTag.Gravity;
using GorillaTagScripts.Builder;
using HarmonyLib;
using UnityEngine;
using Zetori.Base;

namespace Zetori.Cheats.Signals
{
    internal static class MovementAssists
    {
        private const float RefreshInterval = 2f;

        private static readonly Type[] WorldAssists =
        {
            typeof(BasicGravityZone), typeof(GravityOverrideVolume), typeof(RadialGravity), typeof(StickControlledGravity),
            typeof(ForceVolume), typeof(RecyclerForceVolume), typeof(GliderWindVolume), typeof(GliderHoldable),
            typeof(GorillaZipline), typeof(CustomMapsGorillaZipline), typeof(BarrelCannon), typeof(BuilderSpeedBooster),
            typeof(MagicCauldron), typeof(AprilFoolsGravityFX)
        };

        private static readonly GameModeType[] AssistedModes = { GameModeType.SuperInfect, GameModeType.SuperCasual, GameModeType.Custom };

        private static readonly AccessTools.FieldRef<GTPlayer, float>?[] Levitations =
        {
            FieldOf("halloweenLevitationStrength"),
            FieldOf("halloweenLevitationBonusStrength")
        };

        private static float nextRefresh;
        private static int nextAssist;
        private static bool assistFound;
        private static bool worldAssisted;

        internal static float SurfaceBoost { get; private set; } = 1f;

        internal static void Refresh(float now)
        {
            if (now < nextRefresh) return;
            if (nextAssist < WorldAssists.Length)
            {
                if (Present(WorldAssists[nextAssist++]))
                {
                    assistFound = true;
                    worldAssisted = true;
                }
                return;
            }
            worldAssisted = assistFound || Levitating();
            SurfaceBoost = StrongestSurfaceBoost();
            assistFound = false;
            nextAssist = 0;
            nextRefresh = now + RefreshInterval;
        }

        internal static bool Assists(VRRig rig) =>
            worldAssisted
            || ModeAssisted()
            || rig.IsFrozen
            || rig.grabbedRopeIndex != -1
            || (rig.hoverboardVisual != null && rig.hoverboardVisual.IsHeld)
            || Linked(rig.leftHandLink)
            || Linked(rig.rightHandLink);

        private static bool Linked(TakeMyHand_HandLink? link) => link != null && link.IsLinkActive();

        private static bool ModeAssisted()
        {
            GorillaGameManager? manager = GorillaGameManager.instance;
            return manager != null && AssistedModes.Contains(manager.GameType());
        }

        private static bool Present(Type type) =>
            UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OfType<Component>()
                .Any(component => component is Behaviour behaviour ? behaviour.isActiveAndEnabled : component.gameObject.activeInHierarchy);

        private static bool Levitating()
        {
            GTPlayer? player = GTPlayer.Instance;
            if (player == null) return false;
            foreach (AccessTools.FieldRef<GTPlayer, float>? field in Levitations)
                if (field != null && field(player) > 0f) return true;
            return false;
        }

        private static float StrongestSurfaceBoost()
        {
            float strongest = 1f;
            foreach (GorillaSurfaceOverride surface in UnityEngine.Object.FindObjectsByType<GorillaSurfaceOverride>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (surface.isActiveAndEnabled) strongest = Mathf.Max(strongest, surface.extraVelMaxMultiplier);
            return strongest;
        }

        private static AccessTools.FieldRef<GTPlayer, float>? FieldOf(string name)
        {
            if (AccessTools.Field(typeof(GTPlayer), name) != null) return AccessTools.FieldRefAccess<GTPlayer, float>(name);
            Log.Warning($"The game's {name} is missing in this game version; Halloween levitation cannot pause the gravity signal.");
            return null;
        }
    }
}
