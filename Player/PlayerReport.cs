// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Player
{
    internal static class PlayerReport
    {
        internal static bool CanReport(GorillaPlayerScoreboardLine? line) =>
            line != null && line.reportButton != null && line.linePlayer != null && !line.linePlayer.IsLocal;

        internal static bool HasReportedAny(GorillaPlayerScoreboardLine line)
        {
            NetPlayer player = line.linePlayer;
            foreach (GorillaPlayerScoreboardLine other in GorillaScoreboardTotalUpdater.allScoreboardLines)
                if (other != null && other.linePlayer == player && (other.reportedHateSpeech || other.reportedCheating || other.reportedToxicity)) return true;
            return false;
        }

        internal static bool HasReported(GorillaPlayerScoreboardLine line, GorillaPlayerLineButton.ButtonType reason)
        {
            NetPlayer player = line.linePlayer;
            foreach (GorillaPlayerScoreboardLine other in GorillaScoreboardTotalUpdater.allScoreboardLines)
                if (other != null && other.linePlayer == player && Reported(other, reason)) return true;
            return false;
        }

        internal static void Send(GorillaPlayerScoreboardLine line, GorillaPlayerLineButton.ButtonType reason)
        {
            if (!CanReport(line) || HasReported(line, reason)) return;
            line.PressButton(false, reason);
            Share(line);
        }

        private static bool Reported(GorillaPlayerScoreboardLine line, GorillaPlayerLineButton.ButtonType reason) => reason switch
        {
            GorillaPlayerLineButton.ButtonType.HateSpeech => line.reportedHateSpeech,
            GorillaPlayerLineButton.ButtonType.Cheating => line.reportedCheating,
            GorillaPlayerLineButton.ButtonType.Toxicity => line.reportedToxicity,
            _ => false
        };

        private static void Share(GorillaPlayerScoreboardLine source)
        {
            NetPlayer player = source.linePlayer;
            foreach (GorillaPlayerScoreboardLine other in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (other == null || other == source || other.linePlayer != player || other.reportButton == null) continue;
                other.reportedHateSpeech |= source.reportedHateSpeech;
                other.reportedCheating |= source.reportedCheating;
                other.reportedToxicity |= source.reportedToxicity;
                other.reportButton.isOn = true;
                other.reportButton.UpdateColor();
            }
        }
    }
}
