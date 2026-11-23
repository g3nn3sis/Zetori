// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using Zetori.Game;
using Zetori.Player;

namespace Zetori.UI.Panels
{
    internal sealed class PlayersPanel
    {
        private const float RefreshInterval = 0.5f;
        private const string MuteLabel = "Mute";

        private sealed class Row
        {
            internal VRRig Rig { get; }
            internal GorillaPlayerScoreboardLine? Line { get; }
            internal Action Spectate { get; }
            internal Action Draw { get; }

            internal Row(VRRig rig, GorillaPlayerScoreboardLine? line, Action spectate, Action draw)
            {
                Rig = rig;
                Line = line;
                Spectate = spectate;
                Draw = draw;
            }
        }

        private Row[] rows = Array.Empty<Row>();
        private IPanelSurface? drawing;
        private float nextRefresh;

        internal void Draw(IPanelSurface panel)
        {
            Refresh();
            if (rows.Length == 0)
            {
                panel.Label("No players.");
                return;
            }
            panel.Label("Tap a name to spectate. Mute only affects what you hear.");
            drawing = panel;
            try
            {
                foreach (Row row in rows) panel.HorizontalArea(row.Draw);
            }
            finally { drawing = null; }
        }

        private void DrawRow(Row row)
        {
            IPanelSurface panel = drawing!;
            VRRig rig = row.Rig;
            if (rig == null) return;
            panel.Button(Describe(rig), row.Spectate, NetworkedPlayerManager.selectedPlayer != rig);
            GorillaPlayerScoreboardLine? line = row.Line;
            if (line == null)
            {
                panel.Toggle(MuteLabel, false, interactable: false);
                return;
            }
            bool muted = PlayerMute.IsMuted(line);
            if (panel.Toggle(MuteLabel, muted) != muted) PlayerMute.Set(line, !muted);
        }

        private static string Describe(VRRig rig)
        {
            string name = TagManagerControl.NameOf(rig.Creator);
            string tagged = TagManagerControl.IsTagged(rig) ? "  [tagged]" : string.Empty;
            return $"{name}  {Mathf.RoundToInt(NetworkedPlayerManager.GetFPS(rig))} FPS  {PlatformName(rig)}{tagged}";
        }

        private static string PlatformName(VRRig rig) => NetworkedPlayerManager.GetPlatform(rig) switch
        {
            Platform.Steam => "Steam",
            Platform.Meta => "Meta",
            _ => string.Empty
        };

        private void Refresh()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            rows = VRRigCache.ActiveRigs
                .Where(rig => rig != null && rig.Creator != null && rig.Creator.InRoom)
                .OrderBy(rig => rig.isOfflineVRRig ? 0 : 1)
                .ThenBy(rig => TagManagerControl.NameOf(rig.Creator), StringComparer.OrdinalIgnoreCase)
                .Select(CreateRow)
                .ToArray();
        }

        private Row CreateRow(VRRig rig)
        {
            Row? row = null;
            row = new Row(rig, PlayerMute.FindLine(rig.Creator), () => NetworkedPlayerManager.Spectate(rig), () => DrawRow(row!));
            return row;
        }
    }
}
