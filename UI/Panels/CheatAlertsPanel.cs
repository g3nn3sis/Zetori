// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using Zetori.Base.Config;
using Zetori.Cheats;
using Zetori.Cheats.Signals;

namespace Zetori.UI.Panels
{
    internal static class CheatAlertsPanel
    {
        internal static void Draw(IPanelSurface panel)
        {
            CasterConfig.CheatAlerts.Value = panel.Toggle("Cheat Alerts", CasterConfig.CheatAlerts.Value);
            CasterConfig.CheatAlertsOnScreen.Value = panel.Toggle("On Screen", CasterConfig.CheatAlertsOnScreen.Value);
            CasterConfig.CheatAlertsScale.Value = panel.Slider("Screen Scale", CasterConfig.CheatAlertsScale.Value, 0.5f, 2f, "F2", "x");
            CasterConfig.CheatAlertsOffsetX.Value = panel.Slider("Screen X", CasterConfig.CheatAlertsOffsetX.Value, -800f, 800f, "F0", "px");
            CasterConfig.CheatAlertsOffsetY.Value = panel.Slider("Screen Y", CasterConfig.CheatAlertsOffsetY.Value, -500f, 500f, "F0", "px");
            CasterConfig.CheatAlertsInHeadset.Value = panel.Toggle("In Headset", CasterConfig.CheatAlertsInHeadset.Value);
            CasterConfig.CheatAlertsHeadsetScale.Value = panel.Slider("Headset Scale", CasterConfig.CheatAlertsHeadsetScale.Value, 0.5f, 2f, "F2", "x");
            CasterConfig.CheatAlertsHeadsetX.Value = panel.Slider("Headset X", CasterConfig.CheatAlertsHeadsetX.Value, -40f, 40f, "F0", "cm");
            CasterConfig.CheatAlertsHeadsetY.Value = panel.Slider("Headset Y", CasterConfig.CheatAlertsHeadsetY.Value, -40f, 40f, "F0", "cm");
            panel.HorizontalArea(() =>
            {
                panel.Button("Show Test Alert", CheatAlerts.ShowTest, CasterConfig.CheatAlerts.Value);
                panel.Button("Reset Placement", ResetPlacement);
            });
            CasterConfig.CheatAlertsReasons.Value = panel.Toggle("Report Reasons", CasterConfig.CheatAlertsReasons.Value);
            CasterConfig.CheatAlertGameReports.Value = panel.Toggle("Game Reports", CasterConfig.CheatAlertGameReports.Value);
            CasterConfig.CheatAlertModData.Value = panel.Toggle("Mod Data", CasterConfig.CheatAlertModData.Value);
            CasterConfig.CheatAlertOddNames.Value = panel.Toggle("Odd Names", CasterConfig.CheatAlertOddNames.Value);
            CasterConfig.CheatAlertQuestTweaks.Value = panel.Toggle("Quest Tweaks (ADB)", CasterConfig.CheatAlertQuestTweaks.Value);
            CasterConfig.CheatAlertCosmetics.Value = panel.Toggle("Unowned Cosmetics", CasterConfig.CheatAlertCosmetics.Value);
            CasterConfig.CheatAlertSpeed.Value = panel.Toggle("Too Fast", CasterConfig.CheatAlertSpeed.Value);
            CasterConfig.CheatAlertTeleports.Value = panel.Toggle("Teleporting", CasterConfig.CheatAlertTeleports.Value);
            CasterConfig.CheatAlertArmReach.Value = panel.Toggle("Long Arms", CasterConfig.CheatAlertArmReach.Value);
            CasterConfig.CheatAlertTagReach.Value = panel.Toggle("Far Tags", CasterConfig.CheatAlertTagReach.Value);
            CasterConfig.CheatAlertHandMotion.Value = panel.Toggle("Inhuman Hands", CasterConfig.CheatAlertHandMotion.Value);
            CasterConfig.CheatAlertNetworkSpam.Value = panel.Toggle("Network Spam", CasterConfig.CheatAlertNetworkSpam.Value);
            CasterConfig.CheatAlertLowGravity.Value = panel.Toggle("Low Gravity", CasterConfig.CheatAlertLowGravity.Value);
            CasterConfig.CheatAlertTagBursts.Value = panel.Toggle("Tag Bursts", CasterConfig.CheatAlertTagBursts.Value);
            CasterConfig.CheatAlertThroughWalls.Value = panel.Toggle("Through Walls", CasterConfig.CheatAlertThroughWalls.Value);
            CasterConfig.CheatAlertUnderMap.Value = panel.Toggle("Under The Map", CasterConfig.CheatAlertUnderMap.Value);
            CasterConfig.CheatAlertHeadSpin.Value = panel.Toggle("Spinning Head", CasterConfig.CheatAlertHeadSpin.Value);
            CasterConfig.CheatAlertIdentitySpam.Value = panel.Toggle("Colour/Name Spam", CasterConfig.CheatAlertIdentitySpam.Value);
            CasterConfig.CheatAlertProjectileSpam.Value = panel.Toggle("Projectile Spam", CasterConfig.CheatAlertProjectileSpam.Value);
            CasterConfig.CheatAlertMasterTheft.Value = panel.Toggle("Took Master", CasterConfig.CheatAlertMasterTheft.Value);
            CasterConfig.CheatAlertPropertyTampering.Value = panel.Toggle("Property Tampering", CasterConfig.CheatAlertPropertyTampering.Value);
            CasterConfig.CheatAlertLagTricks.Value = panel.Toggle("Lag Tricks", CasterConfig.CheatAlertLagTricks.Value);
            CasterConfig.CheatAlertFlaggedBefore.Value = panel.Toggle("Flagged Before", CasterConfig.CheatAlertFlaggedBefore.Value);
            panel.Button("Forget Flagged Players", FlaggedPlayers.Clear, FlaggedPlayers.Count > 0);
        }

        private static void ResetPlacement()
        {
            Reset(CasterConfig.CheatAlertsScale);
            Reset(CasterConfig.CheatAlertsOffsetX);
            Reset(CasterConfig.CheatAlertsOffsetY);
            Reset(CasterConfig.CheatAlertsHeadsetScale);
            Reset(CasterConfig.CheatAlertsHeadsetX);
            Reset(CasterConfig.CheatAlertsHeadsetY);
        }

        private static void Reset(ConfigEntry<float> entry) => entry.Value = (float)entry.DefaultValue;
    }
}
