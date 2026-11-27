// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Photon.Pun;
using UnityEngine;
using Zetori.Base;

namespace Zetori.History
{
    internal sealed class PlayerHistory : ObjectComponent
    {
        private const float ScanInterval = 2f;

        private static readonly HashSet<string> metThisSession = new HashSet<string>();

        private float nextScan;

        internal static int MetThisSession => metThisSession.Count;

        internal static string CurrentRoom => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : string.Empty;

        internal void Update()
        {
            float now = Time.unscaledTime;
            if (now < nextScan) return;
            nextScan = now + ScanInterval;
            try { Scan(); }
            catch (Exception e) { Log.ExceptionOnce("player-history", e, "Counting the players met failed"); }
        }

        private static void Scan()
        {
            if (CurrentRoom.Length == 0) return;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isOfflineVRRig || rig.Creator == null || !rig.Creator.InRoom) continue;
                string userId = rig.Creator.UserId;
                if (!string.IsNullOrEmpty(userId)) metThisSession.Add(userId);
            }
        }
    }
}
