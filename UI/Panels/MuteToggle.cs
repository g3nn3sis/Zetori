// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Player.Moderation;

namespace Zetori.UI.Panels
{
    internal static class MuteToggle
    {
        private const string Label = "Mute";

        internal static void Draw(IPanelSurface panel, GorillaPlayerScoreboardLine? line)
        {
            if (line == null)
            {
                panel.Toggle(Label, false, interactable: false);
                return;
            }
            bool muted = PlayerMute.IsMuted(line);
            if (panel.Toggle(Label, muted) != muted) PlayerMute.Set(line, !muted);
        }
    }
}
