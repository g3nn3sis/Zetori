// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Photon.Pun;
using Zetori.Base.Config;
using Zetori.Hud.Overlays;

namespace Zetori.UI.Panels
{
    internal sealed class HostPanel
    {
        private const string Offline = "Not in a room";

        private readonly TagManagerPanel tagManager = new TagManagerPanel();

        internal void Draw(IPanelSurface panel)
        {
            panel.Label(RoomStatus());
            DrawMatch(panel);
            tagManager.Draw(panel);
        }

        internal static string Summary()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return Offline;
            string kind = PhotonNetwork.CurrentRoom.IsVisible ? "Public" : "Private";
            return PhotonNetwork.IsMasterClient ? $"{kind}, you host" : $"{kind} room";
        }

        private static string RoomStatus()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return $"{Offline}. Host tools are for private rooms you run.";
            string kind = PhotonNetwork.CurrentRoom.IsVisible ? "Public room" : "Private room";
            string host = PhotonNetwork.IsMasterClient ? "you are the host" : $"{PhotonNetwork.MasterClient?.NickName ?? "someone else"} is the host";
            return $"{kind}, {host}. The scoreboard shows on your stream; taking control of tags needs a private infection room you host.";
        }

        private static void DrawMatch(IPanelSurface panel)
        {
            Scoreboard.enabled = panel.Toggle("Scoreboard", Scoreboard.enabled);
            panel.Label($"{CasterConfig.ScoreboardTeamAName.Value} {CasterConfig.ScoreboardTeamAScore.Value} : {CasterConfig.ScoreboardTeamBScore.Value} {CasterConfig.ScoreboardTeamBName.Value}");
            panel.HorizontalArea(() =>
            {
                panel.Button("A -", () => Scoreboard.AddScore(Scoreboard.Team.A, -1));
                panel.Button("A +", () => Scoreboard.AddScore(Scoreboard.Team.A, 1));
                panel.Button("B -", () => Scoreboard.AddScore(Scoreboard.Team.B, -1));
                panel.Button("B +", () => Scoreboard.AddScore(Scoreboard.Team.B, 1));
            });
            panel.Label($"Timer: {Scoreboard.TimeString}");
            panel.HorizontalArea(() =>
            {
                panel.Button(Scoreboard.Running ? "Pause" : "Start", Scoreboard.ToggleRun);
                panel.Button("Reset", Scoreboard.ResetTimer);
            });
        }
    }
}
