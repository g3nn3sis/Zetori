// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using Zetori.Base.Config;
using Zetori.GameCamera;
using Zetori.GameCamera.Vr;
using Zetori.Shared;
using Zetori.TagManager;

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
            DrawFilming(panel);
            DrawSpots(panel);
        }

        private static void DrawFilming(IPanelSurface panel)
        {
            VRRig? subject = VrCameraDirector.Subject;
            panel.Label($"Filming: {(subject != null ? TagManagerControl.NameOf(subject.Creator) : "Nobody")}");
            panel.HorizontalArea(() =>
            {
                panel.Button("Film Me", FilmLocal, !VrCameraDirector.IsFilming(LocalRig));
                panel.Button("Stop Filming", VrCameraDirector.StopFilming, subject != null);
            });
            panel.Label("A Hand shot tablet you let go of turns to keep the filmed player in frame. Film other players from their player page.");
        }

        private static void DrawSpots(IPanelSurface panel)
        {
            panel.Button("Save Spot", SaveSpot, VrCameraDirector.TabletPose.HasValue);
            IReadOnlyList<(string Name, Pose Pose)> spots = CameraSpots.All;
            if (spots.Count == 0)
            {
                panel.Label("No saved spots. Save Spot remembers where the tablet is; tap a spot to put the tablet back there.");
                return;
            }
            for (int i = 0; i < spots.Count; i++)
            {
                int index = i;
                (string name, Pose pose) = spots[i];
                panel.HorizontalArea(() =>
                {
                    panel.Button(name, () => VrCameraDirector.PlaceTablet(pose));
                    panel.Button("x", () => CameraSpots.Remove(index));
                });
            }
        }

        private static VRRig? LocalRig => GorillaTagger.Instance != null ? GorillaTagger.Instance.offlineVRRig : null;

        private static void FilmLocal()
        {
            VRRig? local = LocalRig;
            if (local != null) VrCameraDirector.Film(local);
        }

        private static void SaveSpot()
        {
            if (VrCameraDirector.TabletPose is Pose pose) CameraSpots.Add(pose);
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
