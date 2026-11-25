// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using Zetori.Base;
using Zetori.GameCamera;
using Zetori.Replay;

namespace Zetori.UI.Panels
{
    internal static class CapturePanel
    {
        private static readonly (PhotoBackground Background, string Label)[] Backgrounds =
        {
            (PhotoBackground.Scene, "Scene"),
            (PhotoBackground.Transparent, "Cutout"),
            (PhotoBackground.Green, "Green"),
            (PhotoBackground.Blue, "Blue")
        };

        internal static void Draw(IPanelSurface panel)
        {
            DrawInstantReplay(panel);
            DrawRecording(panel);
            DrawPhoto(panel);
            panel.Label($"Keys: {CasterConfig.ClipKey.Value} clip, {CasterConfig.MarkerKey.Value} marker, {CasterConfig.PhotoKey.Value} photo");
        }

        internal static string Summary()
        {
            if (ReplaySession.IsRecording) return $"REC {ReplayTimeline.Format(ReplaySession.RecordedSeconds)}";
            if (PhotoCapture.CountingDown) return $"Photo in {PhotoCapture.SecondsLeft}";
            if (ReplaySession.IsBuffering) return $"Instant replay {ReplayTimeline.Format(ReplaySession.BufferedSeconds)}";
            return "Clip, record, photo";
        }

        private static void DrawInstantReplay(IPanelSurface panel)
        {
            CasterConfig.InstantReplay.Value = panel.Toggle("Instant Replay", CasterConfig.InstantReplay.Value);
            float length = panel.Slider("Clip Length", CasterConfig.ClipLength.Value, CasterConfig.MinimumClipLength, CasterConfig.MaximumClipLength, "F0", " s");
            CasterConfig.ClipLength.Value = Mathf.Round(length);
            panel.Button($"Clip Last {Mathf.RoundToInt(ReplaySession.ClipSeconds)} s", ReplaySession.SaveClip, ReplaySession.IsCapturing);
            panel.Label(ClipStatus());
        }

        private static string ClipStatus()
        {
            if (ReplaySession.IsReplaying) return "Clips are not kept while a replay plays.";
            if (ReplaySession.IsRecording) return "Clips come from the recording in progress.";
            if (ReplaySession.IsBuffering) return $"Holding {ReplayTimeline.Format(ReplaySession.BufferedSeconds)} of {ReplayTimeline.Format(ReplaySession.ClipSeconds)}.";
            return "Turn on Instant Replay or start recording to clip.";
        }

        private static void DrawRecording(IPanelSurface panel)
        {
            if (ReplaySession.IsRecording)
            {
                panel.Label($"Recording {ReplayTimeline.Format(ReplaySession.RecordedSeconds)} / {ReplayTimeline.Format(ReplaySession.MaxSeconds)}");
                panel.HorizontalArea(() =>
                {
                    panel.Button("Stop Recording", ReplaySession.StopRecording);
                    panel.Button("Add Marker", ReplaySession.AddMarker);
                });
            }
            else
            {
                panel.HorizontalArea(() =>
                {
                    panel.Button("Record", ReplaySession.StartRecording, !ReplaySession.IsReplaying && !ReplaySession.IsLoading);
                    panel.Button("Add Marker", ReplaySession.AddMarker, ReplaySession.IsBuffering);
                });
            }
            if (ReplaySession.LastMarker > 0) panel.Label($"Last marker: {ReplaySession.LastMarker}");
            if (ReplaySession.IsSaving) panel.Label("Saving replay...");
            else if (ReplaySession.LastSaved is string saved) panel.Label($"Saved replay: {saved}");
        }

        private static void DrawPhoto(IPanelSurface panel)
        {
            if (PhotoCapture.CountingDown) panel.Button($"Cancel Photo ({PhotoCapture.SecondsLeft})", PhotoCapture.Cancel);
            else panel.Button("Take Photo", PhotoCapture.Take, !PhotoCapture.Busy);
            float timer = panel.Slider("Photo Timer", CasterConfig.PhotoTimer.Value, 0f, CasterConfig.MaximumPhotoTimer, "F0", " s");
            CasterConfig.PhotoTimer.Value = Mathf.Round(timer);
            panel.HorizontalArea(() =>
            {
                foreach ((PhotoBackground background, string label) in Backgrounds)
                    panel.Button(label, () => CasterConfig.PhotoBackground.Value = background, CasterConfig.PhotoBackground.Value != background);
            });
            if (CasterConfig.PhotoBackground.Value != PhotoBackground.Scene) panel.Label("Only the spectated player is in the photo.");
            CasterConfig.PhotoThirdsGrid.Value = panel.Toggle("Thirds Grid", CasterConfig.PhotoThirdsGrid.Value);
            if (PhotoCapture.LastSaved is string photo) panel.Label($"Saved photo: {photo}");
        }
    }
}
