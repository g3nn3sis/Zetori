// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base.Config;

namespace Zetori.UI.Panels
{
    internal sealed class InGameSettingsPanel
    {
        private readonly Action recenter;
        private readonly Action close;
        private readonly Action placementRow;
        private IPanelSurface? drawing;

        internal InGameSettingsPanel(Action recenter, Action close)
        {
            this.recenter = recenter;
            this.close = close;
            placementRow = DrawPlacementRow;
        }

        internal void Draw(IPanelSurface panel)
        {
            CasterConfig.InGamePanelSize.Value = panel.Slider("Panel Size", CasterConfig.InGamePanelSize.Value, CasterConfig.MinimumInGamePanelSize, CasterConfig.MaximumInGamePanelSize, "F2", "x");
            CasterConfig.InGamePanelHaptics.Value = panel.Toggle("Haptics", CasterConfig.InGamePanelHaptics.Value);
            CasterConfig.InGamePanelOnStream.Value = panel.Toggle("Show On Stream", CasterConfig.InGamePanelOnStream.Value);
            CasterConfig.HideRoomCodes.Value = panel.Toggle("Hide Room Codes", CasterConfig.HideRoomCodes.Value);
            CasterConfig.ShowTips.Value = panel.Toggle("Tips", CasterConfig.ShowTips.Value);
            drawing = panel;
            try { panel.HorizontalArea(placementRow); }
            finally { drawing = null; }
            panel.Label("Double press left primary to open or close this panel. Grip a side handle to move it; it stays where you leave it. Hold a button to do what right-clicking it does on desktop.");
        }

        private void DrawPlacementRow()
        {
            drawing!.Button("Bring Here", recenter);
            drawing.Button("Close", close);
        }
    }
}
