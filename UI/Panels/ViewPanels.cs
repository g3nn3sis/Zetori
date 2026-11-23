// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.Game;
using Zetori.GameCamera;
using Zetori.GameCamera.Modes;
using Zetori.GameCamera.Vr;
using Zetori.Player;
using UnityEngine;

namespace Zetori.UI.Panels
{
    internal static class ViewPanels
    {
        internal static void DrawCamera(IPanelSurface panel)
        {
            panel.Label($"Mode: {CameraManager.mode}");
            panel.HorizontalArea(() =>
            {
                panel.Button("Default", () => CameraManager.mode = CameraMode.Default, CameraManager.mode != CameraMode.Default);
                panel.Button("Spectator", () => CameraManager.mode = CameraMode.Spectator, CameraManager.mode != CameraMode.Spectator);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Freecam", () => CameraManager.mode = CameraMode.Freecam, CameraManager.mode != CameraMode.Freecam);
                panel.Button("Web", () => CameraManager.mode = CameraMode.Web, CameraManager.mode != CameraMode.Web);
            });
            panel.Button("Camera", () => CameraManager.mode = CameraMode.Camera, CameraManager.mode != CameraMode.Camera && VrCameraRig.HeadsetActive);
            DrawLens(panel);
            DrawFollow(panel);
            DrawOrbit(panel);
            DrawSmoothing(panel);
        }

        private static void DrawLens(IPanelSurface panel)
        {
            CameraManager.fieldOfView = panel.Slider("Field Of View", CameraManager.fieldOfView, 10f, 130f, "F0", "°");
            panel.HorizontalArea(() =>
            {
                panel.Button("FOV 60", () => CameraManager.fieldOfView = 60f);
                panel.Button("FOV 90", () => CameraManager.fieldOfView = 90f);
                panel.Button("FOV 110", () => CameraManager.fieldOfView = 110f);
            });
            CameraManager.nearClipPlane = panel.Slider("Near Clip", CameraManager.nearClipPlane, 0.01f, 0.99f);
        }

        private static void DrawFollow(IPanelSurface panel)
        {
            CasterConfig.HeadDistance.Value = panel.Slider("Head Distance", CasterConfig.HeadDistance.Value, 0f, 0.99f);
            panel.HorizontalArea(() =>
            {
                panel.Button("First Person", () => CasterConfig.HeadDistance.Value = 0f);
                panel.Button("Orbit", () => CasterConfig.HeadDistance.Value = Mathf.Max(0.5f, CasterConfig.HeadDistance.Value));
            });
        }

        private static void DrawOrbit(IPanelSurface panel)
        {
            CasterConfig.OrbitSpeed.Value = panel.Slider("Orbit Speed", CasterConfig.OrbitSpeed.Value, 10f, 600f, "F0", "°/s");
            CasterConfig.OrbitAutoRotate.Value = panel.Toggle("Auto Orbit", CasterConfig.OrbitAutoRotate.Value);
            panel.Button("Reset Orbit", SpectatorCamera.ResetOrbit);
        }

        private static void DrawSmoothing(IPanelSurface panel)
        {
            CasterConfig.PositionSmoothing.Value = panel.Slider("Position Smoothing", CasterConfig.PositionSmoothing.Value, 0f, CasterConfig.MaximumPositionSmoothing);
            CasterConfig.RotationSmoothing.Value = panel.Slider("Rotation Smoothing", CasterConfig.RotationSmoothing.Value, 0f, CasterConfig.MaximumRotationSmoothing);
        }

        internal static void DrawWasd(IPanelSurface panel)
        {
            if (CameraManager.mode == CameraMode.Freecam) panel.Label("WASD is off in Freecam mode");
            else GorillaController.wasd = panel.Toggle("WASD", GorillaController.wasd);
            GorillaController.mouseClick = panel.Toggle("Mouse Click", GorillaController.mouseClick);
            DrawFlightSpeeds(panel);
        }

        internal static void DrawFreecam(IPanelSurface panel)
        {
            if (CameraManager.ActiveMode != CameraMode.Freecam) panel.Label("Camera is not in Freecam mode");
            CasterConfig.FreecamMovementSpeed.Value = panel.Slider("Movement Speed", CasterConfig.FreecamMovementSpeed.Value, 1f, 50f, "F1", " m/s");
            CasterConfig.FreecamRotationSensitivity.Value = panel.Slider("Rotation Sensitivity", CasterConfig.FreecamRotationSensitivity.Value, 0.05f, 2f);
            panel.Button("Teleport Here", TeleportToFreecam, CanTeleportToFreecam);
        }

        private static bool CanTeleportToFreecam => CameraManager.ActiveMode == CameraMode.Freecam && FreecamCamera.Active && RigTeleport.Allowed;

        private static void TeleportToFreecam() => RigTeleport.HeadTo(FreecamCamera.Position, FreecamCamera.Rotation);

        internal static void DrawMiniMap(IPanelSurface panel)
        {
            MiniMap.enabled = panel.Toggle("MiniMap", MiniMap.enabled);
            MiniMap.draggable = panel.Toggle("Draggable", MiniMap.draggable);
            PositionPicker.Draw(panel, HudSlot.MiniMap);
            MiniMap.miniMapSize = Mathf.Round(panel.Slider("Size", MiniMap.miniMapSize, 100f, 520f, "F0", " px"));
            MiniMap.cameraHeight = panel.Slider("Camera Height", MiniMap.cameraHeight, 1f, 50f, "F0", " m");
            panel.Button("Reset Position", MiniMap.ResetPosition);
        }

        private static void DrawFlightSpeeds(IPanelSurface panel)
        {
            GorillaController.MovementSpeed = panel.Slider("Movement Speed", GorillaController.MovementSpeed, 5f, 35f, "F1", " m/s");
            GorillaController.MouseSensitivity = panel.Slider("Mouse Sensitivity", GorillaController.MouseSensitivity, 0.05f, 2f);
        }
    }
}
