// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Player.Moderation
{
    internal static class PlayerMute
    {
        internal static GorillaPlayerScoreboardLine? FindLine(NetPlayer? player)
        {
            if (player == null || player.IsLocal) return null;
            foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
                if (line != null && line.linePlayer == player && line.muteButton != null) return line;
            return null;
        }

        internal static bool IsMuted(GorillaPlayerScoreboardLine? line) => line != null && line.muteButton != null && line.muteButton.isOn;

        internal static void Set(GorillaPlayerScoreboardLine line, bool muted)
        {
            NetPlayer player = line.linePlayer;
            foreach (GorillaPlayerScoreboardLine other in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (other == null || other.linePlayer != player || other.muteButton == null) continue;
                other.muteButton.isOn = muted;
                other.muteButton.UpdateColor();
            }
            line.PressButton(muted, GorillaPlayerLineButton.ButtonType.Mute);
        }
    }
}
