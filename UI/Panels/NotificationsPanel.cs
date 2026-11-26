// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using Zetori.Base.Config;
using Zetori.Cheats;
using Zetori.Notifications;

namespace Zetori.UI.Panels
{
    internal static class NotificationsPanel
    {
        private static readonly Action showTest = CheatAlerts.ShowTest;
        private static readonly Action resetAll = ResetAll;
        private static IPanelSurface? drawing;

        internal static void Draw(IPanelSurface panel)
        {
            DrawPlacement(panel);
            DrawBackground(panel);
            CasterConfig.NotificationFont.Value = FontPicker.Draw(panel, CasterConfig.NotificationFont.Value);
            CasterConfig.NotificationTextRed.Value = panel.Slider("Text Red", CasterConfig.NotificationTextRed.Value, 0f, 1f);
            CasterConfig.NotificationTextGreen.Value = panel.Slider("Text Green", CasterConfig.NotificationTextGreen.Value, 0f, 1f);
            CasterConfig.NotificationTextBlue.Value = panel.Slider("Text Blue", CasterConfig.NotificationTextBlue.Value, 0f, 1f);
            panel.ColorSwatch(NotificationStyle.TextColor);
            drawing = panel;
            try { panel.HorizontalArea(DrawActions); }
            finally { drawing = null; }
        }

        private static void DrawPlacement(IPanelSurface panel)
        {
            CasterConfig.NotificationY.Value = panel.Slider("Height", CasterConfig.NotificationY.Value, -40f, 40f, "F0", "cm");
            CasterConfig.NotificationX.Value = panel.Slider("Sideways", CasterConfig.NotificationX.Value, -40f, 40f, "F0", "cm");
            CasterConfig.NotificationScale.Value = panel.Slider("Size", CasterConfig.NotificationScale.Value, 0.5f, 2f, "F2", "x");
        }

        private static void DrawBackground(IPanelSurface panel)
        {
            bool background = panel.Toggle("Background", CasterConfig.NotificationBackground.Value);
            CasterConfig.NotificationBackground.Value = background;
            CasterConfig.NotificationRoundedCorners.Value = panel.Toggle("Rounded Corners", CasterConfig.NotificationRoundedCorners.Value, interactable: background);
            bool custom = panel.Toggle("Theme", CasterConfig.NotificationTheme.Value == NotificationTheme.Custom, "Custom Color", "UI Color", background);
            CasterConfig.NotificationTheme.Value = custom ? NotificationTheme.Custom : NotificationTheme.Interface;
            if (!custom) return;
            CasterConfig.NotificationRed.Value = panel.Slider("Red", CasterConfig.NotificationRed.Value, 0f, 1f);
            CasterConfig.NotificationGreen.Value = panel.Slider("Green", CasterConfig.NotificationGreen.Value, 0f, 1f);
            CasterConfig.NotificationBlue.Value = panel.Slider("Blue", CasterConfig.NotificationBlue.Value, 0f, 1f);
            panel.ColorSwatch(NotificationStyle.CustomColor);
        }

        private static void DrawActions()
        {
            drawing!.Button("Show Test", showTest, CasterConfig.CheatAlerts.Value && CasterConfig.CheatAlertsInHeadset.Value);
            drawing.Button("Reset", resetAll);
        }

        private static void ResetAll()
        {
            Reset(CasterConfig.NotificationY);
            Reset(CasterConfig.NotificationX);
            Reset(CasterConfig.NotificationScale);
            Reset(CasterConfig.NotificationBackground);
            Reset(CasterConfig.NotificationRoundedCorners);
            Reset(CasterConfig.NotificationTheme);
            Reset(CasterConfig.NotificationRed);
            Reset(CasterConfig.NotificationGreen);
            Reset(CasterConfig.NotificationBlue);
            Reset(CasterConfig.NotificationFont);
            Reset(CasterConfig.NotificationTextRed);
            Reset(CasterConfig.NotificationTextGreen);
            Reset(CasterConfig.NotificationTextBlue);
        }

        private static void Reset<T>(ConfigEntry<T> entry) => entry.Value = (T)entry.DefaultValue;
    }
}
