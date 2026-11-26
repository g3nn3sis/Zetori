// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Zetori.Base;

namespace Zetori.Cheats.Signals
{
    internal static class PropertyChanges
    {
        private const int RoomTarget = 0;

        private static bool listening;

        internal static void Listen()
        {
            if (listening) return;
            PhotonNetwork.NetworkingClient.EventReceived += Read;
            listening = true;
        }

        internal static void Stop()
        {
            if (!listening) return;
            PhotonNetwork.NetworkingClient.EventReceived -= Read;
            listening = false;
        }

        private static void Read(EventData data)
        {
            if (data.Code != EventCode.PropertiesChanged) return;
            try
            {
                int sender = data.Sender;
                if (sender <= 0 || !(data[ParameterCode.Properties] is Hashtable changed)) return;
                int target = data[ParameterCode.TargetActorNr] is int actor ? actor : RoomTarget;
                bool masterKey = target == RoomTarget && changed.ContainsKey(GamePropertyKey.MasterClientId);
                if (masterKey && changed[GamePropertyKey.MasterClientId] is int claimed && claimed == sender) CheatSignals.ObserveMasterClaim(sender);
                if (IsTampering(sender, target, changed.Count - (masterKey ? 1 : 0))) CheatSignals.ObserveTampering(sender);
            }
            catch (Exception e) { Log.ExceptionOnce("property-changes", e, "Reading a change to the room's properties failed"); }
        }

        private static bool IsTampering(int sender, int target, int changedKeys)
        {
            int master = PhotonNetwork.CurrentRoom?.MasterClientId ?? sender;
            if (sender == master) return false;
            return target == RoomTarget ? changedKeys > 0 : target != sender;
        }
    }
}
