// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera;
using Zetori.GameCamera.Vr;

namespace Zetori.UI.Panels
{
    internal static class VrCameraPanel
    {
        private const float MinimumFieldOfView = 30f;
        private const float MaximumFieldOfView = 130f;

        internal static void Draw(IPanelSurface panel)
        {
            bool streaming = CameraManager.mode == CameraMode.Camera;
            if (panel.Toggle("Stream From Camera", streaming) != streaming) CameraManager.mode = streaming ? CameraMode.Default : CameraMode.Camera;
            panel.Label($"Shot: {DisplayNames.Humanize(CasterConfig.VrMode.Value.ToString())}");
            panel.HorizontalArea(() =>
            {
                ShotButton(panel, "First", VrCameraMode.FirstPerson);
                ShotButton(panel, "Third", VrCameraMode.ThirdPerson);
            });
            panel.HorizontalArea(() =>
            {
                ShotButton(panel, "Follow", VrCameraMode.Follower);
                ShotButton(panel, "Hand", VrCameraMode.Handheld);
            });
            CasterConfig.VrFirstPersonCosmetics.Value = panel.Toggle("First Person Cosmetics", CasterConfig.VrFirstPersonCosmetics.Value);
            CameraManager.fieldOfView = panel.Slider("Field Of View", CameraManager.fieldOfView, MinimumFieldOfView, MaximumFieldOfView, "F0", "°");
            panel.Label("Double press right primary to bring out the camera tablet.");
        }

        internal static string Summary()
        {
            string shot = DisplayNames.Humanize(CasterConfig.VrMode.Value.ToString());
            return CameraManager.mode == CameraMode.Camera ? $"Live: {shot}" : shot;
        }

        private static void ShotButton(IPanelSurface panel, string label, VrCameraMode mode) =>
            panel.Button(label, () => CasterConfig.VrMode.Value = mode, CasterConfig.VrMode.Value != mode);
    }
}
