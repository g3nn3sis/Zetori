// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.History;
using Zetori.Player;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

namespace Zetori.UI.Panels
{
    internal sealed class MainPanel
    {
        private readonly PanelOrchestrator owner;
        private string roomToJoin = string.Empty;
        private Vector2 savedScroll;

        internal MainPanel(PanelOrchestrator owner) => this.owner = owner;

        internal void Draw(IPanelSurface panel)
        {
            panel.Label(RoomStatus());
            panel.Label("Room Code");
            roomToJoin = StreamerMode.HidesRoomCodes
                ? panel.MaskedField(roomToJoin, LocalPlayerManager.MaxNameLength, LocalPlayerManager.NormalizeName)
                : panel.TextField(roomToJoin, LocalPlayerManager.MaxNameLength, LocalPlayerManager.NormalizeName);
            if (PhotonNetwork.InRoom) panel.Button("Leave Room", LocalPlayerManager.LeaveRoom);
            else panel.Button("Join Room", () => Join(roomToJoin), roomToJoin.Length > 0);
            panel.Button("Join Public", () => LocalPlayerManager.AttemptPublicRoomJoin(JoinType.Solo, true), !PhotonNetwork.InRoom);
            DrawSavedRooms(panel);
            panel.Button("Reset Layout", owner.ResetLayout);
        }

        private void DrawSavedRooms(IPanelSurface panel)
        {
            string current = PlayerHistory.CurrentRoom;
            string candidate = current.Length > 0 ? current : roomToJoin;
            panel.Button(current.Length > 0 ? "Save This Room" : "Save Code", () => RoomBook.Save(candidate), candidate.Length > 0 && !RoomBook.IsSaved(candidate));
            IReadOnlyList<string> saved = RoomBook.Saved;
            if (saved.Count == 0) return;
            savedScroll = panel.ScrollArea(savedScroll, Panel.ListHeight, saved.Count, () =>
            {
                for (int i = 0; i < saved.Count; i++) DrawSavedRoom(panel, saved[i], i, current);
            });
        }

        private static void DrawSavedRoom(IPanelSurface panel, string code, int index, string current)
        {
            string label = StreamerMode.HidesRoomCodes ? $"Saved Room {index + 1}" : code;
            panel.HorizontalArea(() =>
            {
                panel.Button(label, () => Join(code), !string.Equals(code, current, StringComparison.OrdinalIgnoreCase));
                panel.Button("x", () => RoomBook.Remove(code));
            });
        }

        private static void Join(string code) => LocalPlayerManager.AttemptRoomJoin(code, JoinType.Solo);

        private static string RoomStatus()
        {
            var room = PhotonNetwork.CurrentRoom;
            return PhotonNetwork.InRoom && room != null
                ? $"In room: {StreamerMode.RoomCode(room.Name)} ({room.PlayerCount}/{room.MaxPlayers})"
                : "Not in a room";
        }
    }
}
