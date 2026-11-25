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

        private static readonly RenderResolution[] ResolutionChoices = { RenderResolution.P1080, RenderResolution.P1440, RenderResolution.P2160 };

        private Vector2 puppetScroll;
        private Vector2 highlightScroll;
        private Vector2 keyframeScroll;
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
            if (ReplayRender.Active)
            {
                DrawRendering(panel);
                return;
            }
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
                panel.Button("Path", () => replay.Camera = CameraMode.Path, replay.Camera != CameraMode.Path && !replay.Path.IsEmpty);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous", () => replay.SpectateNext(-1));
                panel.Button("Next", () => replay.SpectateNext(1));
            });
            replay.RigLerp = panel.Slider("Rig Lerp", replay.RigLerp, ReplayPlayer.MinRigLerp, ReplayPlayer.MaxRigLerp);
            DrawHighlights(panel, replay);
            DrawPath(panel, replay);
            DrawRender(panel, replay);
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

        private void DrawPath(IPanelSurface panel, ReplayPlayer replay)
        {
            IReadOnlyList<CameraKeyframe> keyframes = replay.Path.Keyframes;
            panel.HorizontalArea(() =>
            {
                panel.Button("Add Keyframe", replay.AddKeyframe);
                panel.Button("Clear Path", replay.ClearPath, keyframes.Count > 0);
            });
            if (keyframes.Count == 0) return;
            keyframeScroll = panel.ScrollArea(keyframeScroll, Panel.ListHeight, keyframes.Count, () =>
            {
                for (int i = 0; i < keyframes.Count; i++)
                {
                    int index = i;
                    float time = keyframes[i].Time;
                    panel.HorizontalArea(() =>
                    {
                        panel.Button($"Keyframe {index + 1} at {ReplayTimeline.FormatPrecise(time)}", () => replay.Seek(time));
                        panel.Button("x", () => replay.RemoveKeyframe(index));
                    });
                }
            });
        }

        private static void DrawRendering(IPanelSurface panel)
        {
            panel.Label(RenderStatus());
            panel.Label("Your own player runs slower until it finishes.");
            panel.Button("Cancel Render", ReplayRender.Cancel);
        }

        private static string RenderStatus()
        {
            if (ReplayRender.Preparing) return "Preparing the render";
            if (ReplayRender.Finishing) return "Finishing the video";
            return $"Rendering frame {ReplayRender.FramesDone} of {ReplayRender.FramesTotal}";
        }

        private static void DrawRender(IPanelSurface panel, ReplayPlayer replay)
        {
            panel.Label($"Render {ReplayTimeline.FormatPrecise(replay.RangeStart)} to {ReplayTimeline.FormatPrecise(replay.RangeEnd)}");
            panel.HorizontalArea(() =>
            {
                panel.Button("Set Start", replay.MarkRangeStart);
                panel.Button("Set End", replay.MarkRangeEnd);
                panel.Button("Whole Replay", replay.ResetRange, replay.HasRange);
            });
            panel.HorizontalArea(() =>
            {
                foreach (RenderResolution resolution in ResolutionChoices)
                    panel.Button(ResolutionLabel(resolution), () => CasterConfig.ReplayRenderResolution.Value = resolution, CasterConfig.ReplayRenderResolution.Value != resolution);
            });
            panel.HorizontalArea(() =>
            {
                foreach (int rate in CasterConfig.ReplayRenderFrameRates)
                    panel.Button($"{rate} fps", () => CasterConfig.ReplayRenderFrameRate.Value = rate, CasterConfig.ReplayRenderFrameRate.Value != rate);
            });
            panel.Button("Render Video", () => ReplayRender.Start(replay), replay.RangeEnd > replay.RangeStart);
            if (ReplayRender.LastFile is string file) panel.Label($"Rendered to: {file}");
        }

        private static string ResolutionLabel(RenderResolution resolution) => resolution switch
        {
            RenderResolution.P1080 => "1080p",
            RenderResolution.P1440 => "1440p",
            _ => "4K"
        };

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
            CameraMode.Path => $"Path: flying through {replay.Path.Keyframes.Count} keyframes",
            _ => $"Spectating: {replay.Spectated?.Actor.Name ?? "nobody"}"
        };
    }
}
