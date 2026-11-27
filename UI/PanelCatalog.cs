// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.UI.Icons;

namespace Zetori.UI
{
    internal static class PanelCatalog
    {
        internal static readonly PanelGroup[] Groups =
        {
            new PanelGroup("Room", PanelId.Main, PanelId.Replay, PanelId.Capture),
            new PanelGroup("Camera", PanelId.Camera, PanelId.FreecamCamera, PanelId.Web, PanelId.Wasd),
            new PanelGroup("Overlays", PanelId.Nametag, PanelId.Leaderboard, PanelId.Scoreboard, PanelId.KillFeed, PanelId.ClosestLava, PanelId.MiniMap, PanelId.CheatAlerts),
            new PanelGroup("Match & World", PanelId.TagManager, PanelId.Time, PanelId.MapLoader),
            new PanelGroup("Setup", PanelId.Player, PanelId.PlayerSettings, PanelId.Keybinds, PanelId.Theme, PanelId.Presets)
        };

        internal static readonly PanelId[][] Columns = Array.ConvertAll(Groups, group => group.Panels);

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
            PanelId.Player => "Your Player",
            PanelId.PlayerSettings => "General",
            PanelId.Theme => "Interface",
            PanelId.Presets => "Presets",
            PanelId.Keybinds => "Keybinds",
            PanelId.Capture => "Capture",
            PanelId.Stats => "Session Stats",
            PanelId.CheatAlerts => "Cheat Alerts",
            _ => id.ToString()
        };

        internal static string Description(PanelId id) => id switch
        {
            PanelId.Main => "Join a room by code or a public room, leave, and keep saved room codes.",
            PanelId.Replay => "Record the match and play back saved replays.",
            PanelId.Capture => "Clip the last moments, record, and take 4K photos.",
            PanelId.Camera => "Camera mode, field of view, orbit distance and smoothing.",
            PanelId.FreecamCamera => "Freecam speed and sensitivity, and Teleport Here.",
            PanelId.Web => "Fixed camera spots that turn to watch the best action.",
            PanelId.Wasd => "Fly your own player with WASD and the mouse.",
            PanelId.Nametag => "Names above players, with FPS and platform icons.",
            PanelId.Leaderboard => "On-screen player list with the number key that spectates each.",
            PanelId.Scoreboard => "Two-team scores and a match timer on screen.",
            PanelId.KillFeed => "Announces each player the moment they are tagged.",
            PanelId.ClosestLava => "How far the spectated player is from the nearest tagger.",
            PanelId.MiniMap => "Top-down map in a corner of the screen.",
            PanelId.CheatAlerts => "Warns you when a player shows signs of cheating.",
            PanelId.TagManager => "Run the tag game yourself in a private room you are master of.",
            PanelId.Time => "Time of day and rain.",
            PanelId.MapLoader => "Load any map.",
            PanelId.Player => "Your name, colour and microphone mode.",
            PanelId.PlayerSettings => "Camera audio, Discord, hidden room codes, frame rate, AFK kick and arm exaggeration.",
            PanelId.Keybinds => "Change any key Zetori uses.",
            PanelId.Theme => "Panel size, snapping and colour theme.",
            PanelId.Presets => "Save whole setups and pick which one loads at startup.",
            PanelId.Stats => "Your time in rooms, tags, runs and distance since the game started.",
            _ => string.Empty
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
            PanelId.Stats => IconGlyph.Chart,
            PanelId.CheatAlerts => IconGlyph.Shield,
            _ => IconGlyph.Zetori
        };

        internal static string PositionKey(PanelId id) => id == PanelId.Wasd ? "Freecam" : id.ToString();
    }
}
