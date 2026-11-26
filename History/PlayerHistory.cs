// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using Photon.Pun;
using UnityEngine;
using Zetori.Base;

namespace Zetori.History
{
    internal sealed class PlayerHistory : ObjectComponent
    {
        private const string FileName = "RecentPlayers.txt";
        private const int MaximumPlayers = 50;
        private const float ScanInterval = 2f;
        private const float SaveInterval = 30f;

        private static readonly List<MetPlayer> players = new List<MetPlayer>();
        private static readonly HashSet<string> metThisSession = new HashSet<string>();
        private static bool loaded;
        private static bool dirty;

        private float nextScan;
        private float nextSave;
        private string currentRoom = string.Empty;

        internal static IReadOnlyList<MetPlayer> Players
        {
            get
            {
                EnsureLoaded();
                return players;
            }
        }

        internal static int MetThisSession => metThisSession.Count;

        internal static string CurrentRoom => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : string.Empty;

        internal static void Clear()
        {
            EnsureLoaded();
            players.Clear();
            Write();
        }

        private static string FilePath => Path.Combine(DataFolders.Root, FileName);

        internal void Update()
        {
            float now = Time.unscaledTime;
            if (now >= nextScan)
            {
                nextScan = now + ScanInterval;
                try { Scan(); }
                catch (Exception e) { Log.ExceptionOnce("player-history", e, "Remembering recent players failed"); }
            }
            if (dirty && now >= nextSave)
            {
                nextSave = now + SaveInterval;
                Write();
            }
        }

        private void Scan()
        {
            string room = CurrentRoom;
            if (room != currentRoom)
            {
                currentRoom = room;
                if (room.Length > 0) RoomBook.Remember(room);
                if (dirty) Write();
            }
            if (room.Length == 0) return;
            EnsureLoaded();
            bool privateRoom = !PhotonNetwork.CurrentRoom.IsVisible;
            DateTime seen = DateTime.Now;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isOfflineVRRig || rig.Creator == null || !rig.Creator.InRoom) continue;
                string userId = rig.Creator.UserId;
                if (string.IsNullOrEmpty(userId)) continue;
                metThisSession.Add(userId);
                Note(userId, rig.Creator.NickName ?? string.Empty, rig.playerColor, seen, room, privateRoom);
            }
        }

        private static void Note(string userId, string name, Color color, DateTime seen, string room, bool privateRoom)
        {
            int index = players.FindIndex(player => player.UserId == userId);
            MetPlayer entry;
            if (index >= 0)
            {
                entry = players[index];
                players.RemoveAt(index);
                entry.Name = name;
                entry.Color = color;
                entry.LastSeen = seen;
                entry.Room = room;
                entry.PrivateRoom = privateRoom;
            }
            else entry = new MetPlayer(userId, name, color, seen, room, privateRoom);
            players.Insert(0, entry);
            if (players.Count > MaximumPlayers) players.RemoveRange(MaximumPlayers, players.Count - MaximumPlayers);
            dirty = true;
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                    if (MetPlayer.Parse(line) is MetPlayer player && players.Count < MaximumPlayers) players.Add(player);
            }
            catch (Exception e) { Log.Exception(e, "Loading the recent players failed"); }
        }

        private static void Write()
        {
            dirty = false;
            try
            {
                Directory.CreateDirectory(DataFolders.Root);
                File.WriteAllLines(FilePath, players.Select(player => player.Format()));
            }
            catch (Exception e) { Log.Exception(e, "Saving the recent players failed"); }
        }

        protected override void OnApplicationQuit()
        {
            if (dirty) Write();
            base.OnApplicationQuit();
        }
    }
}
