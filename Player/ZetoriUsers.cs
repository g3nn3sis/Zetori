// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using ExitGames.Client.Photon;
using Photon.Pun;

namespace Zetori.Player
{
    internal sealed class ZetoriUsers : ObjectComponent
    {
        private const string PropertyKey = "zetori";
        private const float AnnounceInterval = 3f;

        internal void Start() => InvokeRepeating(nameof(Announce), 0f, AnnounceInterval);

        internal static bool IsUser(VRRig rig)
        {
            Photon.Realtime.Room? room = PhotonNetwork.CurrentRoom;
            if (room == null || rig.Creator == null) return false;
            Photon.Realtime.Player? player = room.GetPlayer(rig.Creator.ActorNumber);
            return player != null && player.CustomProperties.ContainsKey(PropertyKey);
        }

        private void Announce()
        {
            try
            {
                Photon.Realtime.Player? local = PhotonNetwork.LocalPlayer;
                if (local == null || IsAnnounced(local)) return;
                local.SetCustomProperties(new Hashtable { { PropertyKey, PluginInfo.Version } });
            }
            catch (Exception e) { Log.ExceptionOnce("zetori-announce", e, "Announcing Zetori to the room failed"); }
        }

        private static bool IsAnnounced(Photon.Realtime.Player local) =>
            local.CustomProperties.TryGetValue(PropertyKey, out object announced) && announced is string version && version == PluginInfo.Version;
    }
}
