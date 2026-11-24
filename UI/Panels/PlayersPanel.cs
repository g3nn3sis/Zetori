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

        private sealed class Row
        {
            internal VRRig Rig { get; }
            internal GorillaPlayerScoreboardLine? Line { get; }
            internal Action Open { get; }
            internal Action Draw { get; }

            internal Row(VRRig rig, GorillaPlayerScoreboardLine? line, Action open, Action draw)
            {
                Rig = rig;
                Line = line;
                Open = open;
                Draw = draw;
            }
        }

        private readonly Action<VRRig> openPlayer;
        private readonly ReportMenu report = new ReportMenu();
        private Row[] rows = Array.Empty<Row>();
        private IPanelSurface? drawing;
        private float nextRefresh;

        internal PlayersPanel(Action<VRRig> openPlayer) => this.openPlayer = openPlayer;

        internal void Draw(IPanelSurface panel)
        {
            Refresh();
            if (rows.Length == 0)
            {
                panel.Label("No players.");
                return;
            }
            panel.Label("Tap a name to view the player. Mute only affects what you hear.");
            drawing = panel;
            try
            {
                foreach (Row row in rows)
                {
                    panel.HorizontalArea(row.Draw);
                    report.DrawOptions(panel, row.Line);
                }
            }
            finally { drawing = null; }
        }

        private void DrawRow(Row row)
        {
            IPanelSurface panel = drawing!;
            VRRig rig = row.Rig;
            if (rig == null) return;
            panel.Button(Describe(rig), row.Open);
            MuteToggle.Draw(panel, row.Line);
            report.DrawButton(panel, row.Line);
        }

        private static string Describe(VRRig rig)
        {
            string name = TagManagerControl.NameOf(rig.Creator);
            string tagged = TagManagerControl.IsTagged(rig) ? "  [tagged]" : string.Empty;
            return $"{name}  {Mathf.RoundToInt(NetworkedPlayerManager.GetFPS(rig))} FPS  {Platform.Name(NetworkedPlayerManager.GetPlatform(rig))}{tagged}";
        }

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
            row = new Row(rig, PlayerMute.FindLine(rig.Creator), () => openPlayer(rig), () => DrawRow(row!));
            return row;
        }
    }
}
