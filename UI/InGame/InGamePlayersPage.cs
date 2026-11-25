// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using Zetori.Game;
using Zetori.Player;
using Zetori.Replay;
using Zetori.UI.Panels;

namespace Zetori.UI.InGame
{
    internal sealed class InGamePlayersPage
    {
        private const float RefreshInterval = 0.5f;
        private const string SpectateLabel = "Spectate";
        private const string SpectatingLabel = "Spectating";

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

        private sealed class RecordedRow
        {
            internal ReplayPuppet Puppet { get; }
            internal Action Open { get; }
            internal Action Spectate { get; }
            internal Action Draw { get; }

            internal RecordedRow(ReplayPuppet puppet, Action open, Action spectate, Action draw)
            {
                Puppet = puppet;
                Open = open;
                Spectate = spectate;
                Draw = draw;
            }
        }

        private readonly InGameSurface surface;
        private readonly Action<VRRig> openPlayer;
        private readonly Action<ReplayPlayer, ReplayPuppet> openRecorded;
        private readonly InGameReportMenu report;
        private Row[] rows = Array.Empty<Row>();
        private RecordedRow[] recordedRows = Array.Empty<RecordedRow>();
        private ReplayPlayer? recordedFor;
        private float nextRefresh;

        internal InGamePlayersPage(InGameSurface surface, Action<VRRig> openPlayer, Action<ReplayPlayer, ReplayPuppet> openRecorded)
        {
            this.surface = surface;
            this.openPlayer = openPlayer;
            this.openRecorded = openRecorded;
            report = new InGameReportMenu(surface);
        }

        internal static int RecordedCount(ReplayPlayer replay) => replay.Puppets.Count(puppet => puppet.Visible);

        internal void Draw()
        {
            if (ReplaySession.Player is ReplayPlayer replay)
            {
                DrawRecorded(replay);
                return;
            }
            ForgetRecorded();
            Refresh();
            if (rows.Length == 0)
            {
                surface.Label("No players.");
                return;
            }
            surface.Label("Tap a name to view the player. Mute only affects what you hear.");
            foreach (Row row in rows) surface.HorizontalArea(row.Draw);
        }

        private void DrawRow(Row row)
        {
            VRRig rig = row.Rig;
            if (rig == null) return;
            surface.Button(Describe(rig), row.Open);
            MuteToggle.Draw(surface, row.Line);
            report.Draw(row.Line);
        }

        private static string Describe(VRRig rig)
        {
            string name = TagManagerControl.NameOf(rig.Creator);
            string tagged = TagManagerControl.IsTagged(rig) ? "  [tagged]" : string.Empty;
            return $"{name}  {Mathf.RoundToInt(NetworkedPlayerManager.GetFPS(rig))} FPS  {Platform.Name(NetworkedPlayerManager.GetPlatform(rig))}{tagged}{InGameSignalReport.RowSuffix(CheatSignals.For(rig))}";
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

        private void DrawRecorded(ReplayPlayer replay)
        {
            if (replay != recordedFor) BuildRecordedRows(replay);
            if (RecordedCount(replay) == 0)
            {
                surface.Label("No players at this moment of the replay.");
                return;
            }
            surface.Label("Players in the replay. Tap a name to view the player.");
            foreach (RecordedRow row in recordedRows)
                if (row.Puppet.Visible) surface.HorizontalArea(row.Draw);
        }

        private void DrawRecordedRow(ReplayPlayer replay, RecordedRow row)
        {
            bool spectating = replay.IsSpectating(row.Puppet);
            surface.Button(DescribeRecorded(row.Puppet), row.Open);
            surface.Button(spectating ? SpectatingLabel : SpectateLabel, row.Spectate, !spectating);
        }

        private static string DescribeRecorded(ReplayPuppet puppet)
        {
            ReplayActor actor = puppet.Actor;
            string tagged = puppet.Current.Tag != ReplayTagState.None ? "  [tagged]" : string.Empty;
            string recorder = actor.IsLocal ? "  [recorder]" : string.Empty;
            return $"{actor.Name}  {puppet.Current.Fps} FPS  {Platform.Name(actor.Platform)}{tagged}{recorder}";
        }

        private void ForgetRecorded()
        {
            if (recordedFor == null) return;
            recordedFor = null;
            recordedRows = Array.Empty<RecordedRow>();
        }

        private void BuildRecordedRows(ReplayPlayer replay)
        {
            recordedFor = replay;
            recordedRows = Enumerable.Range(0, replay.Puppets.Count)
                .OrderBy(index => replay.Puppets[index].Actor.IsLocal ? 0 : 1)
                .ThenBy(index => replay.Puppets[index].Actor.Name, StringComparer.OrdinalIgnoreCase)
                .Select(index => CreateRecordedRow(replay, index))
                .ToArray();
        }

        private RecordedRow CreateRecordedRow(ReplayPlayer replay, int index)
        {
            ReplayPuppet puppet = replay.Puppets[index];
            RecordedRow? row = null;
            row = new RecordedRow(puppet, () => openRecorded(replay, puppet), () => replay.Spectate(index), () => DrawRecordedRow(replay, row!));
            return row;
        }
    }
}
