// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Photon.Pun;
using UnityEngine;
using Zetori.Base;
using Zetori.TagManager;

namespace Zetori.History
{
    internal sealed class SessionStats : ObjectComponent
    {
        private const float SpeedWindow = 0.25f;
        private const float TeleportDistance = 5f;
        private const float MaximumPlausibleSpeed = 40f;

        private string room = string.Empty;
        private bool? wasTagged;
        private Vector3? lastPosition;
        private float windowDistance;
        private float windowTime;

        internal static float RoomSeconds { get; private set; }
        internal static int RoomsJoined { get; private set; }
        internal static int TagsMade { get; private set; }
        internal static int TimesTagged { get; private set; }
        internal static float CurrentRun { get; private set; }
        internal static float LongestRun { get; private set; }
        internal static float Distance { get; private set; }
        internal static float TopSpeed { get; private set; }

        internal static void CountTag() => TagsMade++;

        internal static void Reset()
        {
            RoomSeconds = 0f;
            RoomsJoined = 0;
            TagsMade = 0;
            TimesTagged = 0;
            CurrentRun = 0f;
            LongestRun = 0f;
            Distance = 0f;
            TopSpeed = 0f;
        }

        internal void Update()
        {
            try
            {
                float deltaTime = Time.unscaledDeltaTime;
                if (!TrackRoom(deltaTime)) return;
                TrackMovement(deltaTime);
                TrackTags(deltaTime);
            }
            catch (Exception e) { Log.ExceptionOnce("session-stats", e, "Counting session stats failed"); }
        }

        private bool TrackRoom(float deltaTime)
        {
            string current = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : string.Empty;
            if (current != room)
            {
                room = current;
                if (current.Length > 0) RoomsJoined++;
                CurrentRun = 0f;
                lastPosition = null;
                wasTagged = null;
            }
            if (current.Length == 0) return false;
            RoomSeconds += deltaTime;
            return true;
        }

        private void TrackMovement(float deltaTime)
        {
            VRRig? local = GorillaTagger.Instance != null ? GorillaTagger.Instance.offlineVRRig : null;
            if (local == null) return;
            Vector3 position = local.transform.position;
            if (lastPosition is Vector3 previous)
            {
                float moved = Vector3.Distance(previous, position);
                if (moved < TeleportDistance)
                {
                    Distance += moved;
                    windowDistance += moved;
                    windowTime += deltaTime;
                }
            }
            lastPosition = position;
            if (windowTime < SpeedWindow) return;
            float speed = windowDistance / windowTime;
            if (speed <= MaximumPlausibleSpeed) TopSpeed = Mathf.Max(TopSpeed, speed);
            windowDistance = 0f;
            windowTime = 0f;
        }

        private void TrackTags(float deltaTime)
        {
            VRRig? local = GorillaTagger.Instance != null ? GorillaTagger.Instance.offlineVRRig : null;
            if (local == null) return;
            bool tagged = TagManagerControl.IsTagged(local);
            if (tagged && wasTagged == false) BecomeTagged();
            wasTagged = tagged;
            if (!tagged && TagManagerControl.AnyoneTagged) Run(deltaTime);
        }

        private static void BecomeTagged()
        {
            TimesTagged++;
            CurrentRun = 0f;
        }

        private static void Run(float deltaTime)
        {
            CurrentRun += deltaTime;
            LongestRun = Mathf.Max(LongestRun, CurrentRun);
        }
    }
}
