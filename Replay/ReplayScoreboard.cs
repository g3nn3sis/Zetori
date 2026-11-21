// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Zetori.Replay
{
    internal static class ReplayScoreboard
    {
        private const string Title = "REPLAY   PLAYERS: ";
        private const string ColumnHeader = "\n  PLAYER     COLOR";
        private const string NameStart = "\n <color=#ffffff><noparse>";
        private const string NameEnd = "</noparse></color>";
        private const int CompactRosterSize = 10;
        private const string ShowFailureKey = "replay-scoreboard";
        private const string ShowFailure = "Showing the replay on the scoreboard failed";
        private static readonly Vector3 CompactScale = new Vector3(1f, 0.5f, 1f);

        private static readonly List<(ReplayPuppet Puppet, int Material)> roster = new List<(ReplayPuppet, int)>();
        private static readonly HashSet<GorillaScoreBoard> drawnBoards = new HashSet<GorillaScoreBoard>();
        private static readonly StringBuilder composer = new StringBuilder(256);
        private static ReplayPlayer? shownReplay;
        private static string boardText = string.Empty;

        internal static bool TryShow(GorillaScoreBoard board)
        {
            if (!ReplaySession.IsReplaying) return false;
            ShowGuarded(board);
            return true;
        }

        internal static void Refresh(ReplayPlayer replay)
        {
            try
            {
                if (!RosterChanged(replay)) return;
                Rebuild(replay);
                foreach (GorillaScoreBoard board in GorillaScoreboardTotalUpdater.allScoreboards) ShowGuarded(board);
            }
            catch (Exception e) { Log.ExceptionOnce(ShowFailureKey, e, ShowFailure); }
        }

        internal static void RestoreLive()
        {
            shownReplay = null;
            roster.Clear();
            boardText = string.Empty;
            if (drawnBoards.Count == 0) return;
            try
            {
                foreach (GorillaScoreBoard board in drawnBoards)
                    if (board != null) ForgetLinePlayers(board);
                if (GorillaScoreboardTotalUpdater.allScoreboards.Count > 0) GorillaScoreboardTotalUpdater.instance.UpdateActiveScoreboards();
            }
            catch (Exception e) { Log.Exception(e, "Restoring the live scoreboard failed"); }
            finally { drawnBoards.Clear(); }
        }

        private static void ForgetLinePlayers(GorillaScoreBoard board)
        {
            foreach (GorillaPlayerScoreboardLine line in board.lines) line.ResetData();
        }

        private static bool RosterChanged(ReplayPlayer replay)
        {
            if (replay != shownReplay) return true;
            int row = 0;
            foreach (ReplayPuppet puppet in replay.Puppets)
            {
                if (!puppet.Visible) continue;
                if (row >= roster.Count || roster[row].Puppet != puppet || roster[row].Material != puppet.Current.Material) return true;
                row++;
            }
            return row != roster.Count;
        }

        private static void Rebuild(ReplayPlayer replay)
        {
            shownReplay = replay;
            roster.Clear();
            foreach (ReplayPuppet puppet in replay.Puppets)
                if (puppet.Visible) roster.Add((puppet, puppet.Current.Material));
            composer.Clear().Append(Title).Append(roster.Count).Append(ColumnHeader);
            foreach ((ReplayPuppet puppet, _) in roster) composer.Append(NameStart).Append(puppet.Actor.Name).Append(NameEnd);
            boardText = composer.ToString();
        }

        private static void ShowGuarded(GorillaScoreBoard board)
        {
            try { Show(board); }
            catch (Exception e) { Log.ExceptionOnce(ShowFailureKey, e, ShowFailure); }
        }

        private static void Show(GorillaScoreBoard board)
        {
            drawnBoards.Add(board);
            Wake(board);
            List<GorillaPlayerScoreboardLine> lines = board.lines;
            for (int row = 0; row < lines.Count; row++)
            {
                bool used = row < roster.Count;
                SetActive(lines[row].gameObject, used);
                if (used) FillLine(board, lines[row], row, roster[row].Puppet, roster[row].Material);
            }
            Arrange(board);
            board.boardText.text = boardText;
            board.buttonText.text = string.Empty;
        }

        private static void Wake(GorillaScoreBoard board)
        {
            if (!board.boardText.enabled) board.SetSleepState(true);
            if (board.notInRoomText != null) SetActive(board.notInRoomText.gameObject, false);
        }

        private static void Arrange(GorillaScoreBoard board)
        {
            board.leftPanel.transform.localScale = Vector3.one;
            board.leftPanel.transform.localPosition = Vector3.zero;
            SetActive(board.rightPanel, false);
            bool compact = roster.Count > CompactRosterSize;
            board.linesParent.transform.localScale = compact ? CompactScale : Vector3.one;
            board.linesParent.transform.localPosition = compact ? new Vector3(0f, board.bigRoomYOffset, 0f) : Vector3.zero;
            board.textsParent.transform.localScale = compact ? CompactScale : Vector3.one;
        }

        private static void FillLine(GorillaScoreBoard board, GorillaPlayerScoreboardLine line, int row, ReplayPuppet puppet, int material)
        {
            SetActive(line.muteButton.gameObject, false);
            SetActive(line.reportButton.gameObject, false);
            SetActive(line.reportButtons, false);
            SetActive(line.roomControlButtons, false);
            line.transform.localPosition = new Vector3(0f, board.startingYValue - board.lineHeight * row, 0f);
            line.playerName.text = puppet.Actor.Name;
            Paint(line.playerSwatch, puppet.Actor.Color, material);
            line.speakerIcon.enabled = false;
        }

        private static void Paint(Image swatch, Color color, int material)
        {
            VRRig? template = GorillaTagger.Instance != null ? GorillaTagger.Instance.offlineVRRig : null;
            if (template == null) return;
            Material[] tagMaterials = template.materialsToChangeTo;
            swatch.material = material > 0 && material < tagMaterials.Length ? tagMaterials[material] : template.scoreboardMaterial;
            swatch.color = color;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target.activeSelf != active) target.SetActive(active);
        }
    }
}
