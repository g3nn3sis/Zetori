// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera;
using Zetori.GameCamera.Vr;
using Zetori.Replay;
using UnityEngine;

namespace Zetori.UI.Panels
{
    internal sealed class ReplayPanel
    {
        private const int RenameLength = 32;

        private Vector2 puppetScroll;
        private Vector2 highlightScroll;
        private Vector2 savedScroll;
        private string? renaming;
        private string renameText = string.Empty;

        internal void Draw(IPanelSurface panel)
        {
            if (ReplaySession.Player is ReplayPlayer replay)
            {
                DrawPlayback(panel, replay);
                return;
            }
            if (ReplaySession.IsRecording)
            {
                panel.Label($"Recording {ReplayTimeline.Format(ReplaySession.RecordedSeconds)} / {ReplayTimeline.Format(ReplaySession.MaxSeconds)}");
                panel.HorizontalArea(() =>
                {
                    panel.Button("Stop Recording", ReplaySession.StopRecording);
                    panel.Button("Add Marker", ReplaySession.AddMarker);
                });
                return;
            }
            panel.Button("Record", ReplaySession.StartRecording, !ReplaySession.IsLoading);
            if (ReplaySession.IsSaving) panel.Label("Saving replay...");
            if (ReplaySession.IsLoading) panel.Label($"Loading {ReplaySession.LoadingName}...");
            DrawSavedReplays(panel);
        }

        private void DrawSavedReplays(IPanelSurface panel)
        {
            string[] names = ReplaySession.SavedReplays;
            if (names.Length == 0)
            {
                panel.Label("No saved replays.");
                return;
            }
            savedScroll = panel.ScrollArea(savedScroll, Panel.ListHeight, names.Length, () =>
            {
                foreach (string name in names)
                {
                    if (name == renaming) DrawRenameRow(panel, name);
                    else DrawReplayRow(panel, name);
                }
            });
        }

        private void DrawReplayRow(IPanelSurface panel, string name)
        {
            panel.HorizontalArea(() =>
            {
                panel.Button(name, () => ReplaySession.Play(name), () => BeginRename(name), !ReplaySession.IsLoading);
                panel.Button("x", () => ReplaySession.Delete(name));
            });
        }

        private void DrawRenameRow(IPanelSurface panel, string name)
        {
            panel.HorizontalArea(() =>
            {
                renameText = panel.TextField(renameText, RenameLength, Presets.Sanitize);
                panel.Button("Save", () => CommitRename(name), renameText.Trim().Length > 0);
                panel.Button("x", () => renaming = null);
            });
        }

        private void BeginRename(string name)
        {
            renaming = name;
            renameText = name;
        }

        private void CommitRename(string name)
        {
            if (ReplaySession.Rename(name, renameText.Trim())) renaming = null;
        }

        private void DrawPlayback(IPanelSurface panel, ReplayPlayer replay)
        {
            panel.Label($"{ReplayTimeline.Format(replay.Cursor)} / {ReplayTimeline.Format(replay.Recording.Duration)}");
            panel.HorizontalArea(() =>
            {
                panel.Button(replay.Playing ? "Pause" : "Play", replay.TogglePlaying);
                panel.Button("Stop", ReplaySession.ExitReplay);
            });
            DrawSpeed(panel, replay);
            panel.HorizontalArea(() =>
            {
                panel.Button("Spectate", () => replay.Camera = CameraMode.Spectator, replay.Camera != CameraMode.Spectator);
                panel.Button("FreeCam", () => replay.Camera = CameraMode.Freecam, replay.Camera != CameraMode.Freecam && !VrCameraRig.HeadsetActive);
                panel.Button("Web", () => replay.Camera = CameraMode.Web, replay.Camera != CameraMode.Web);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous", () => replay.SpectateNext(-1));
                panel.Button("Next", () => replay.SpectateNext(1));
            });
            replay.RigLerp = panel.Slider("Rig Lerp", replay.RigLerp, ReplayPlayer.MinRigLerp, ReplayPlayer.MaxRigLerp);
            DrawHighlights(panel, replay);
            panel.Label(CameraStatus(replay));
            IReadOnlyList<ReplayPuppet> puppets = replay.Puppets;
            puppetScroll = panel.ScrollArea(puppetScroll, Panel.ListHeight, puppets.Count, () =>
            {
                for (int i = 0; i < puppets.Count; i++)
                {
                    int index = i;
                    panel.Button(puppets[i].Actor.Name, () => replay.Spectate(index), puppets[i].Visible && !replay.IsSpectating(puppets[i]));
                }
            });
        }

        private void DrawHighlights(IPanelSurface panel, ReplayPlayer replay)
        {
            IReadOnlyList<ReplayHighlight> highlights = replay.Highlights;
            if (highlights.Count == 0)
            {
                panel.Label("No highlights found.");
                return;
            }
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous Highlight", () => replay.JumpToHighlight(-1));
                panel.Button("Next Highlight", () => replay.JumpToHighlight(1));
            });
            highlightScroll = panel.ScrollArea(highlightScroll, Panel.ListHeight, highlights.Count, () =>
            {
                foreach (ReplayHighlight highlight in highlights)
                    panel.Button($"{ReplayTimeline.Format(highlight.Time)} {replay.Recording.Describe(highlight)}", () => replay.JumpTo(highlight));
            });
        }

        private static void DrawSpeed(IPanelSurface panel, ReplayPlayer replay)
        {
            panel.HorizontalArea(() =>
            {
                foreach (float speed in ReplayPlayer.Speeds) panel.Button(ReplayTimeline.FormatSpeed(speed), () => replay.Speed = speed, replay.Speed != speed);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Frame Back", () => replay.StepFrame(-1));
                panel.Button("Frame Forward", () => replay.StepFrame(1));
            });
        }

        private static string CameraStatus(ReplayPlayer replay) => replay.Camera switch
        {
            CameraMode.Freecam => "FreeCam: hold right mouse, WASD to fly",
            CameraMode.Web => "Web: watching from the camera web",
            _ => $"Spectating: {replay.Spectated?.Actor.Name ?? "nobody"}"
        };
    }
}
