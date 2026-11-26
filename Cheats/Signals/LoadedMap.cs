// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using UnityEngine.SceneManagement;
using Zetori.Base;

namespace Zetori.Cheats.Signals
{
    internal static class LoadedMap
    {
        private const float NearbyRadius = 30f;

        internal static bool Around(VRRig rig, int layers) =>
            !rig.IsInDisplacementZone
            && ZoneLoaded(rig)
            && Physics.CheckSphere(rig.transform.position, NearbyRadius, layers, QueryTriggerInteraction.Ignore);

        private static bool ZoneLoaded(VRRig rig)
        {
            ZoneManagement? zones = ZoneManagement.instance;
            ZoneEntityBSP? entity = rig.zoneEntity;
            if (zones == null || entity == null) return true;
            GTZone zone = entity.currentZone;
            if (zone == GTZone.none) return true;
            try
            {
                if (!zones.IsZoneActive(zone)) return false;
                string? scene = zones.GetSceneNameForZone(zone);
                return string.IsNullOrEmpty(scene) || SceneManager.GetSceneByName(scene).isLoaded;
            }
            catch (Exception e)
            {
                Log.ExceptionOnce("cheat-signals-zone", e, "Checking whether a player's zone is loaded failed");
                return false;
            }
        }
    }
}
