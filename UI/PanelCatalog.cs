// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.UI.Icons;

namespace Zetori.UI
{
    internal static class PanelCatalog
    {
        internal static readonly PanelId[][] Columns =
        {
            new[] { PanelId.Main, PanelId.RecentPlayers, PanelId.Wasd, PanelId.FreecamCamera, PanelId.Replay },
            new[] { PanelId.Camera, PanelId.Capture, PanelId.Web, PanelId.MiniMap },
            new[] { PanelId.Nametag, PanelId.Leaderboard, PanelId.ClosestLava, PanelId.KillFeed, PanelId.CheatAlerts },
            new[] { PanelId.Scoreboard, PanelId.Time, PanelId.MapLoader, PanelId.TagManager, PanelId.Stats },
            new[] { PanelId.Player, PanelId.PlayerSettings, PanelId.Keybinds, PanelId.Theme, PanelId.Presets }
        };

        internal static string Title(PanelId id) => id switch
        {
            PanelId.Main => "Zetori",
            PanelId.Wasd => "WASD",
            PanelId.FreecamCamera => "Freecam",
            PanelId.Replay => "Replay",
            PanelId.Camera => "Camera",
            PanelId.Web => "Camera Web",
            PanelId.MiniMap => "MiniMap",
            PanelId.Nametag => "NameTags",
            PanelId.Leaderboard => "Leaderboard",
            PanelId.Scoreboard => "Scoreboard",
            PanelId.ClosestLava => "Closest Lava",
            PanelId.KillFeed => "Kill Feed",
            PanelId.Time => "Time",
            PanelId.MapLoader => "Maps",
            PanelId.TagManager => "Tag Manager",
            PanelId.Player => "Player",
            PanelId.PlayerSettings => "Settings",
            PanelId.Theme => "UI Control",
            PanelId.Presets => "Presets",
            PanelId.Keybinds => "Keybinds",
            PanelId.Capture => "Capture",
            PanelId.RecentPlayers => "Recent Players",
            PanelId.Stats => "Session Stats",
            PanelId.CheatAlerts => "Cheat Alerts",
            _ => id.ToString()
        };

        internal static IconGlyph Icon(PanelId id) => id switch
        {
            PanelId.Main => IconGlyph.Zetori,
            PanelId.Wasd => IconGlyph.Keycap,
            PanelId.FreecamCamera => IconGlyph.Move,
            PanelId.Replay => IconGlyph.Replay,
            PanelId.Camera => IconGlyph.Camera,
            PanelId.Web => IconGlyph.Web,
            PanelId.MiniMap => IconGlyph.Map,
            PanelId.Nametag => IconGlyph.Badge,
            PanelId.Leaderboard => IconGlyph.Trophy,
            PanelId.Scoreboard => IconGlyph.Scoreboard,
            PanelId.ClosestLava => IconGlyph.Flame,
            PanelId.KillFeed => IconGlyph.Crosshair,
            PanelId.Time => IconGlyph.Clock,
            PanelId.MapLoader => IconGlyph.Mountains,
            PanelId.TagManager => IconGlyph.Flag,
            PanelId.Player => IconGlyph.Person,
            PanelId.PlayerSettings => IconGlyph.Gear,
            PanelId.Theme => IconGlyph.Sliders,
            PanelId.Presets => IconGlyph.Layers,
            PanelId.Keybinds => IconGlyph.Keyboard,
            PanelId.Capture => IconGlyph.Record,
            PanelId.RecentPlayers => IconGlyph.History,
            PanelId.Stats => IconGlyph.Chart,
            PanelId.CheatAlerts => IconGlyph.Shield,
            _ => IconGlyph.Zetori
        };

        internal static string PositionKey(PanelId id) => id == PanelId.Wasd ? "Freecam" : id.ToString();
    }
}
