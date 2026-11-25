// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;
using Zetori.Base;

namespace Zetori.Player
{
    internal sealed class FlaggedPlayers : ObjectComponent
    {
        private const string FileName = "FlaggedPlayers.txt";
        private const int MaximumPlayers = 500;
        private const float SaveInterval = 30f;

        private static readonly List<FlaggedPlayer> players = new List<FlaggedPlayer>();
        private static bool loaded;
        private static bool dirty;

        private float nextSave;

        internal static int Count
        {
            get
            {
                EnsureLoaded();
                return players.Count;
            }
        }

        private static string FilePath => Path.Combine(DataFolders.Root, FileName);

        internal static FlaggedPlayer? Find(string userId)
        {
            EnsureLoaded();
            return players.Find(player => player.UserId == userId);
        }

        internal static void Remember(string userId, string name, SignalKind signals)
        {
            if (string.IsNullOrEmpty(userId) || signals == SignalKind.None) return;
            EnsureLoaded();
            int index = players.FindIndex(player => player.UserId == userId);
            FlaggedPlayer entry;
            if (index >= 0)
            {
                entry = players[index];
                if (index == 0 && entry.Name == name && (entry.Signals | signals) == entry.Signals) return;
                players.RemoveAt(index);
                entry.Name = name;
                entry.Signals |= signals;
                entry.LastFlagged = DateTime.Now;
            }
            else entry = new FlaggedPlayer(userId, name, signals, DateTime.Now);
            players.Insert(0, entry);
            if (players.Count > MaximumPlayers) players.RemoveRange(MaximumPlayers, players.Count - MaximumPlayers);
            dirty = true;
        }

        internal static void Clear()
        {
            EnsureLoaded();
            players.Clear();
            Write();
        }

        internal void Update()
        {
            float now = Time.unscaledTime;
            if (!dirty || now < nextSave) return;
            nextSave = now + SaveInterval;
            Write();
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                    if (FlaggedPlayer.Parse(line) is FlaggedPlayer player && players.Count < MaximumPlayers) players.Add(player);
            }
            catch (Exception e) { Log.Exception(e, "Loading the flagged players failed"); }
        }

        private static void Write()
        {
            dirty = false;
            try
            {
                Directory.CreateDirectory(DataFolders.Root);
                File.WriteAllLines(FilePath, players.Select(player => player.Format()));
            }
            catch (Exception e) { Log.Exception(e, "Saving the flagged players failed"); }
        }

        protected override void OnApplicationQuit()
        {
            if (dirty) Write();
            base.OnApplicationQuit();
        }
    }
}
