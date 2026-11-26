// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using ExitGames.Client.Photon;
using Photon.Pun;
using Zetori.Base;

namespace Zetori.Cheats.Signals
{
    internal static class NetworkTraffic
    {
        private static readonly Dictionary<int, int> counts = new Dictionary<int, int>();
        private static bool listening;

        internal static void Listen()
        {
            if (listening) return;
            PhotonNetwork.NetworkingClient.EventReceived += Count;
            listening = true;
        }

        internal static void Stop()
        {
            if (!listening) return;
            PhotonNetwork.NetworkingClient.EventReceived -= Count;
            listening = false;
        }

        internal static void TakeCounts(Dictionary<int, int> into)
        {
            into.Clear();
            foreach (KeyValuePair<int, int> entry in counts) into[entry.Key] = entry.Value;
            counts.Clear();
        }

        private static void Count(EventData data)
        {
            try
            {
                int sender = data.Sender;
                if (sender > 0) counts[sender] = counts.TryGetValue(sender, out int count) ? count + 1 : 1;
            }
            catch (Exception e) { Log.ExceptionOnce("network-traffic", e, "Counting a player's network messages failed"); }
        }
    }
}
