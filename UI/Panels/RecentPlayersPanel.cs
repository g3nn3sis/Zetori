// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaNetworking;
using UnityEngine;
using Zetori.Player;

namespace Zetori.UI.Panels
{
    internal sealed class RecentPlayersPanel
    {
        private Vector2 scroll;

        internal void Draw(IPanelSurface panel)
        {
            IReadOnlyList<MetPlayer> players = PlayerHistory.Players;
            if (players.Count == 0)
            {
                panel.Label("Players you share a room with are listed here, newest first.");
                return;
            }
            panel.Label("Newest first. Tap a player who is here to spectate them; Join goes to the room they were last seen in.");
            scroll = panel.ScrollArea(scroll, Panel.ListHeight, players.Count, () =>
            {
                foreach (MetPlayer player in players) DrawRow(panel, player);
            });
            panel.Button("Clear List", PlayerHistory.Clear);
        }

        private static void DrawRow(IPanelSurface panel, MetPlayer player)
        {
            panel.HorizontalArea(() =>
            {
                VRRig? rig = RigOf(player.UserId);
                panel.Button(Describe(player, rig != null), rig != null ? () => NetworkedPlayerManager.Spectate(rig) : null);
                panel.Button("Join", () => LocalPlayerManager.AttemptRoomJoin(player.Room, JoinType.Solo), CanJoin(player));
            });
        }

        private static string Describe(MetPlayer player, bool here)
        {
            string where = here ? "here now" : $"{Ago(player.LastSeen)} in {StreamerMode.RoomCode(player.Room)}{(player.PrivateRoom ? string.Empty : " (public)")}";
            return $"{player.Name}  {where}";
        }

        private static bool CanJoin(MetPlayer player) =>
            player.Room.Length > 0 && !string.Equals(player.Room, PlayerHistory.CurrentRoom, StringComparison.OrdinalIgnoreCase);

        private static VRRig? RigOf(string userId)
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
                if (rig != null && !rig.isOfflineVRRig && rig.Creator != null && rig.Creator.InRoom && rig.Creator.UserId == userId) return rig;
            return null;
        }

        private static string Ago(DateTime seen)
        {
            TimeSpan age = DateTime.Now - seen;
            if (age.TotalMinutes < 1d) return "just now";
            if (age.TotalHours < 1d) return $"{(int)age.TotalMinutes}m ago";
            if (age.TotalDays < 1d) return $"{(int)age.TotalHours}h ago";
            return $"{(int)age.TotalDays}d ago";
        }
    }
}
