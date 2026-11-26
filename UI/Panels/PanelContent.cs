// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine.InputSystem;
using Zetori.Base.Config;

namespace Zetori.UI.Panels
{
    internal sealed class PanelContent
    {
        private readonly MainPanel main;
        private readonly SetupPanels setup;
        private readonly WorldPanels world = new WorldPanels();
        private readonly ReplayPanel replay = new ReplayPanel();
        private readonly KeybindPanel keybinds = new KeybindPanel();
        private readonly WebPanel web = new WebPanel();
        private readonly TagManagerPanel tagManager = new TagManagerPanel();
        private readonly RecentPlayersPanel recentPlayers = new RecentPlayersPanel();
        private Key menuTipKey;
        private string? menuTip;

        internal PanelContent(PanelOrchestrator owner)
        {
            main = new MainPanel(owner);
            setup = new SetupPanels(owner);
        }

        internal Action<IPanelSurface> For(PanelId id) => id switch
        {
            PanelId.Main => main.Draw,
            PanelId.Replay => replay.Draw,
            PanelId.Camera => ViewPanels.DrawCamera,
            PanelId.Web => web.Draw,
            PanelId.Wasd => ViewPanels.DrawWasd,
            PanelId.FreecamCamera => ViewPanels.DrawFreecam,
            PanelId.MiniMap => ViewPanels.DrawMiniMap,
            PanelId.Nametag => OverlayPanels.DrawNameTags,
            PanelId.Leaderboard => OverlayPanels.DrawLeaderboard,
            PanelId.Scoreboard => OverlayPanels.DrawScoreboard,
            PanelId.ClosestLava => OverlayPanels.DrawClosestLava,
            PanelId.KillFeed => OverlayPanels.DrawKillFeed,
            PanelId.Time => WorldPanels.DrawTime,
            PanelId.MapLoader => world.DrawMapLoader,
            PanelId.TagManager => tagManager.Draw,
            PanelId.Player => SetupPanels.DrawPlayer,
            PanelId.PlayerSettings => SetupPanels.DrawSettings,
            PanelId.Theme => setup.DrawUiControl,
            PanelId.Presets => setup.DrawPresets,
            PanelId.Keybinds => keybinds.Draw,
            PanelId.Capture => CapturePanel.Draw,
            PanelId.RecentPlayers => recentPlayers.Draw,
            PanelId.Stats => StatsPanel.Draw,
            PanelId.CheatAlerts => CheatAlertsPanel.Draw,
            _ => DrawNothing
        };

        internal Action<IPanelSurface> Described(PanelId id) => WithTip(PanelCatalog.Description(id), For(id));

        internal Action<IPanelSurface> DescribedOnDesktop(PanelId id) => id == PanelId.Main ? WithTip(DesktopMainTip, For(id)) : Described(id);

        internal static Action<IPanelSurface> WithTip(string tip, Action<IPanelSurface> draw) => WithTip(() => tip, draw);

        private static Action<IPanelSurface> WithTip(Func<string> tip, Action<IPanelSurface> draw) => panel =>
        {
            if (CasterConfig.ShowTips.Value) panel.Label(tip());
            draw(panel);
        };

        private string DesktopMainTip()
        {
            Key key = CasterConfig.MenuKey.Value;
            if (menuTip == null || key != menuTipKey)
            {
                menuTipKey = key;
                menuTip = $"{PanelCatalog.Description(PanelId.Main)} {key} shows or hides these panels; click a title to open one. In VR, double-press X on your left controller for the in-game panel.";
            }
            return menuTip;
        }

        private static void DrawNothing(IPanelSurface panel) { }
    }
}
