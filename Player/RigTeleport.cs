// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Collections;
using Zetori.Base;
using Zetori.Replay;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.XR;

namespace Zetori.Player
{
    internal sealed class RigTeleport : ObjectComponent
    {
        private const int MaxAttempts = 10;
        private const float LandingTolerance = 0.25f;

        private static RigTeleport? instance;

        private Coroutine? settling;
        private GTPlayer? ignoringSurfaces;
        private LayerMask savedLocomotionLayers;

        internal static bool Allowed => instance != null && !ReplaySession.IsReplaying;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        internal static void HeadTo(Vector3 position, Quaternion facing)
        {
            if (Allowed) instance!.Begin(position, facing);
        }

        private void Begin(Vector3 head, Quaternion facing)
        {
            if (settling != null) StopCoroutine(settling);
            settling = StartCoroutine(Settle(head, facing));
        }

        private IEnumerator Settle(Vector3 head, Quaternion facing)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                yield return null;
                GTPlayer? player = GTPlayer.Instance;
                GorillaTagger? tagger = GorillaTagger.Instance;
                if (!Allowed || player == null || tagger == null || !Ready(player, tagger)) break;
                if (attempt > 0 && Landed(player, head)) break;
                IgnoreSurfaces(player);
                if (!TryTeleport(player, tagger, head, facing)) break;
            }
            RestoreSurfaces();
            settling = null;
        }

        private void IgnoreSurfaces(GTPlayer player)
        {
            if (ignoringSurfaces == player) return;
            RestoreSurfaces();
            savedLocomotionLayers = player.locomotionEnabledLayers;
            player.locomotionEnabledLayers = 0;
            ignoringSurfaces = player;
        }

        private void RestoreSurfaces()
        {
            if (ignoringSurfaces != null) ignoringSurfaces.locomotionEnabledLayers = savedLocomotionLayers;
            ignoringSurfaces = null;
        }

        private static bool Ready(GTPlayer player, GorillaTagger tagger) =>
            player.mainCamera != null && tagger.offlineVRRig != null && tagger.headCollider != null;

        private static bool Landed(GTPlayer player, Vector3 head) =>
            (player.mainCamera.transform.position - head).sqrMagnitude <= LandingTolerance * LandingTolerance;

        private static bool TryTeleport(GTPlayer player, GorillaTagger tagger, Vector3 head, Quaternion facing)
        {
            try
            {
                Face(player, tagger, facing);
                player.TeleportTo(head, player.transform.rotation, false, true);
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Teleporting the player failed");
                return false;
            }
        }

        private static void Face(GTPlayer player, GorillaTagger tagger, Quaternion facing)
        {
            if (XRSettings.isDeviceActive) player.Turn(Mathf.DeltaAngle(player.mainCamera.transform.eulerAngles.y, facing.eulerAngles.y));
            else tagger.headCollider.transform.rotation = facing;
        }

        protected override void OnDestroy()
        {
            RestoreSurfaces();
            if (instance == this) instance = null;
            base.OnDestroy();
        }
    }
}
