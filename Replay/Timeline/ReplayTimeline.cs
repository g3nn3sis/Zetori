// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base.Config;
using Zetori.GameCamera;
using Zetori.Replay.CameraPaths;
using Zetori.Replay.Highlights;
using Zetori.Replay.Playback;
using Zetori.Replay.Voice;
using Zetori.UI;
using UnityEngine;
using System.Globalization;
using UnityEngine.InputSystem;

namespace Zetori.Replay.Timeline
{
    internal sealed class ReplayTimeline : IDisposable
    {
        internal const float Margin = 8f;
        internal const float Height = 48f;
        internal static TimelineEdge Edge => CasterConfig.ReplayTimelineEdge.Value;
        internal static bool DockedTop => Edge == TimelineEdge.Top;
        internal static float Bottom => DockY(Edge) + Height;

        private const float DockDamping = 16f;
        private const float DockSnap = 0.5f;
        private const float Inset = 10f;
        private const float Spacing = 8f;
        private const float ButtonWidth = 56f;
        private const float ButtonHeight = 24f;
        private const float ClockWidth = 96f;
        private const float RailHeight = 6f;
        private const float KnobWidth = 8f;
        private const float KnobHeight = 20f;
        private const float MarkerWidth = 2f;
        private const float MarkerHeight = 8f;
        private const float VoiceHeight = 3f;
        private const float HighlightWidth = 3f;
        private const float HighlightHeight = 12f;
        private const float KeyframeSize = 5f;
        private const float KeyframeGap = 8f;
        private const float HoverRadius = 4f;
        private const float TooltipWidth = 300f;
        private const float TooltipHeight = 20f;
        private const float TooltipGap = 2f;
        private const int FontSize = 12;
        private const int SliceBorder = 10;

        private static readonly Color32 BackgroundColor = new Color32(10, 10, 10, 190);
        private static readonly Color32 ButtonColor = new Color32(20, 5, 8, 210);
        private static readonly Color RailColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color VoiceColor = new Color(0.45f, 0.85f, 1f, 0.9f);
        private static readonly Color TagColor = new Color(1f, 0.33f, 0.05f, 1f);
        private static readonly Color JoinColor = new Color(0.35f, 0.9f, 0.45f, 1f);
        private static readonly Color LeaveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color EnvironmentColor = new Color(0.55f, 0.7f, 1f, 1f);
        private static readonly Color BookmarkColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color HighlightColor = new Color(1f, 0.35f, 0.8f, 1f);
        private static readonly Color KeyframeColor = new Color(0.75f, 0.6f, 1f, 1f);
        private static readonly Color RangeColor = new Color(1f, 1f, 1f, 0.35f);

        private readonly int scrubHint = "ZetoriReplayScrub".GetHashCode();
        private readonly int dragHint = "ZetoriReplayTimelineDrag".GetHashCode();
        private Texture2D? background, button;
        private GUIStyle? backgroundStyle, buttonStyle, clockStyle, tooltipStyle;
        private Color32 builtAccent;
        private bool resumeAfterScrub;
        private float barY = DockY(Edge);
        private float grabOffset;
        private bool dragging;
        private bool inputSawPress;

        internal static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        internal static string FormatPrecise(float seconds)
        {
            float clamped = Mathf.Max(0f, seconds);
            int minutes = Mathf.FloorToInt(clamped / 60f);
            return $"{minutes}:{(clamped - minutes * 60f).ToString("00.0", CultureInfo.InvariantCulture)}";
        }

        internal static string FormatSpeed(float speed) => speed.ToString("0.##", CultureInfo.InvariantCulture) + "x";

        private static float DockY(TimelineEdge edge) =>
            edge == TimelineEdge.Top ? Margin : Mathf.Max(Margin, UiScale.Height - Margin - Height);

        private static TimelineEdge NearestEdge(float y) =>
            Mathf.Abs(y - DockY(TimelineEdge.Top)) <= Mathf.Abs(y - DockY(TimelineEdge.Bottom)) ? TimelineEdge.Top : TimelineEdge.Bottom;

        internal void Tick(float dt)
        {
            if (dragging)
            {
                FollowPress();
                return;
            }
            float target = DockY(Edge);
            float next = Mathf.Lerp(barY, target, 1f - Mathf.Exp(-DockDamping * dt));
            barY = Mathf.Abs(next - target) < DockSnap ? target : next;
        }

        private void FollowPress()
        {
            Mouse? mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.leftButton.isPressed) inputSawPress = true;
            else if (inputSawPress) Release();
        }

        internal void Draw(ReplayPlayer replay)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !replay.Rendering;
            try { DrawBar(replay); }
            finally { GUI.enabled = previousEnabled; }
        }

        private void DrawBar(ReplayPlayer replay)
        {
            EnsureStyles(UI.ColorSpace.Current.Color);
            var bar = new Rect(Margin, barY, UiScale.Width - Margin * 2f, Height);
            GUI.Box(bar, GUIContent.none, backgroundStyle);

            float buttonY = bar.y + (Height - ButtonHeight) * 0.5f;
            float x = bar.x + Inset;
            if (GUI.Button(new Rect(x, buttonY, ButtonWidth, ButtonHeight), replay.Playing ? "Pause" : "Play", buttonStyle)) replay.TogglePlaying();
            x += ButtonWidth + Spacing;
            GUI.Label(new Rect(x, bar.y, ClockWidth, Height), $"{Format(replay.Cursor)} / {Format(replay.Recording.Duration)}", clockStyle);
            x += ClockWidth + Spacing;
            if (GUI.Button(new Rect(x, buttonY, ButtonWidth, ButtonHeight), FormatSpeed(replay.Speed), buttonStyle)) replay.CycleSpeed();
            x += ButtonWidth + Spacing;

            float stopX = bar.xMax - Inset - ButtonWidth;
            if (GUI.Button(new Rect(stopX, buttonY, ButtonWidth, ButtonHeight), "Stop", buttonStyle))
            {
                ReplaySession.ExitReplay();
                return;
            }

            Rect track = Rect.MinMaxRect(x + KnobWidth * 0.5f, bar.y, stopX - Spacing - KnobWidth * 0.5f, bar.yMax);
            bool hasTrack = track.width > 0f;
            if (hasTrack) Scrub(track, replay);
            Drag(bar);
            if (!hasTrack) return;
            DrawTrack(track, replay);
            DrawHover(track, bar, replay);
        }

        private void Drag(Rect bar)
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID(dragHint, FocusType.Passive, bar);
            if (dragging && GUIUtility.hotControl != id) Release();
            else if (!dragging && GUIUtility.hotControl == id) GUIUtility.hotControl = 0;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown when GUI.enabled && e.button == 0 && bar.Contains(e.mousePosition):
                    GUIUtility.hotControl = id;
                    dragging = true;
                    inputSawPress = false;
                    grabOffset = e.mousePosition.y - barY;
                    e.Use();
                    break;
                case EventType.MouseDrag when dragging:
                    barY = Mathf.Clamp(e.mousePosition.y - grabOffset, DockY(TimelineEdge.Top), DockY(TimelineEdge.Bottom));
                    e.Use();
                    break;
                case EventType.MouseUp when dragging:
                    GUIUtility.hotControl = 0;
                    Release();
                    e.Use();
                    break;
            }
        }

        private void Release()
        {
            dragging = false;
            CasterConfig.ReplayTimelineEdge.Value = NearestEdge(barY);
        }

        private void Scrub(Rect track, ReplayPlayer replay)
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID(scrubHint, FocusType.Passive, track);
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown when e.button == 0 && track.Contains(e.mousePosition):
                    GUIUtility.hotControl = id;
                    resumeAfterScrub = replay.Playing;
                    replay.Playing = false;
                    replay.Seek(TimeAt(track, e.mousePosition.x, replay));
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    replay.Seek(TimeAt(track, e.mousePosition.x, replay));
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    replay.Playing = resumeAfterScrub;
                    e.Use();
                    break;
            }
        }

        private static void DrawTrack(Rect track, ReplayPlayer replay)
        {
            if (Event.current.type != EventType.Repaint) return;
            ReplayRecording recording = replay.Recording;
            var rail = new Rect(track.x, track.center.y - RailHeight * 0.5f, track.width, RailHeight);
            Fill(rail, RailColor);
            float progress = Progress(replay.Cursor, recording);
            Fill(new Rect(rail.x, rail.y, rail.width * progress, rail.height), UI.ColorSpace.Current.Color);
            if (replay.HasRange)
            {
                float rangeStart = rail.x + rail.width * Progress(replay.RangeStart, recording);
                float rangeEnd = rail.x + rail.width * Progress(replay.RangeEnd, recording);
                Fill(new Rect(rangeStart, rail.y - 1f, Mathf.Max(1f, rangeEnd - rangeStart), rail.height + 2f), RangeColor);
            }

            ReplayPuppet? spectated = replay.Camera == CameraMode.Spectator ? replay.Spectated : null;
            if (spectated != null)
            {
                foreach (VoiceSegment segment in spectated.Actor.Voice.Segments)
                {
                    float start = rail.x + rail.width * Progress(segment.Start, recording);
                    float end = rail.x + rail.width * Progress(segment.End, recording);
                    Fill(new Rect(start, rail.yMax + 3f, Mathf.Max(1f, end - start), VoiceHeight), VoiceColor);
                }
            }

            foreach (ReplayEvent replayEvent in recording.Events)
            {
                if (!Marked(replayEvent)) continue;
                float markerX = rail.x + rail.width * Progress(replayEvent.Time, recording);
                Fill(new Rect(markerX - MarkerWidth * 0.5f, rail.y - MarkerHeight - 2f, MarkerWidth, MarkerHeight), MarkerColor(replayEvent.Kind));
            }

            foreach (ReplayHighlight highlight in replay.Highlights)
            {
                float highlightX = rail.x + rail.width * Progress(highlight.Time, recording);
                Fill(new Rect(highlightX - HighlightWidth * 0.5f, rail.y - HighlightHeight - 2f, HighlightWidth, HighlightHeight), HighlightColor);
            }

            foreach (CameraKeyframe keyframe in replay.Path.Keyframes)
            {
                float keyframeX = rail.x + rail.width * Progress(keyframe.Time, recording);
                Fill(new Rect(keyframeX - KeyframeSize * 0.5f, rail.yMax + KeyframeGap, KeyframeSize, KeyframeSize), KeyframeColor);
            }

            float knobX = rail.x + rail.width * progress;
            Fill(new Rect(knobX - KnobWidth * 0.5f, track.center.y - KnobHeight * 0.5f, KnobWidth, KnobHeight), Color.white);
        }

        private void DrawHover(Rect track, Rect bar, ReplayPlayer replay)
        {
            Vector2 mouse = Event.current.mousePosition;
            if (dragging || !track.Contains(mouse)) return;
            string text = HoveredMark(track, mouse.x, replay) ?? Format(TimeAt(track, mouse.x, replay));
            float x = Mathf.Clamp(mouse.x - TooltipWidth * 0.5f, bar.x, bar.xMax - TooltipWidth);
            float y = NearestEdge(bar.y) == TimelineEdge.Top ? bar.yMax + TooltipGap : bar.y - TooltipGap - TooltipHeight;
            GUI.Label(new Rect(x, y, TooltipWidth, TooltipHeight), text, tooltipStyle);
        }

        private static string? HoveredMark(Rect track, float mouseX, ReplayPlayer replay)
        {
            ReplayRecording recording = replay.Recording;
            foreach (ReplayHighlight highlight in replay.Highlights)
                if (Near(track, mouseX, highlight.Time, recording)) return $"{Format(highlight.Time)}  {recording.Describe(highlight)}";
            foreach (ReplayEvent replayEvent in recording.Events)
                if (Marked(replayEvent) && Near(track, mouseX, replayEvent.Time, recording)) return $"{Format(replayEvent.Time)}  {recording.Describe(replayEvent)}";
            return null;
        }

        private static bool Near(Rect track, float mouseX, float time, ReplayRecording recording) =>
            Mathf.Abs(track.x + track.width * Progress(time, recording) - mouseX) <= HoverRadius;

        private static bool Marked(ReplayEvent replayEvent) => replayEvent.Kind != ReplayEventKind.Signal;

        private static float TimeAt(Rect track, float x, ReplayPlayer replay) =>
            Mathf.Clamp01((x - track.x) / track.width) * replay.Recording.Duration;

        private static float Progress(float time, ReplayRecording recording) =>
            recording.Duration > 0f ? Mathf.Clamp01(time / recording.Duration) : 0f;

        private static Color MarkerColor(ReplayEventKind kind) => kind switch
        {
            ReplayEventKind.Tagged => TagColor,
            ReplayEventKind.Joined => JoinColor,
            ReplayEventKind.Left => LeaveColor,
            ReplayEventKind.Marker => BookmarkColor,
            _ => EnvironmentColor
        };

        private static void Fill(Rect area, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * previous.a);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles(Color32 accent)
        {
            if (background != null && builtAccent.Equals(accent) && backgroundStyle != null) return;
            DestroyTextures();
            builtAccent = accent;
            background = Textures.CreateTexture(64, 32, BackgroundColor, accent, 2, SliceBorder);
            button = Textures.CreateTexture(64, (int)ButtonHeight, ButtonColor, accent, 1, 4);
            var slice = new RectOffset(SliceBorder, SliceBorder, SliceBorder, SliceBorder);
            backgroundStyle = new GUIStyle { border = slice };
            backgroundStyle.normal.background = background;
            buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = FontSize, border = new RectOffset(5, 5, 5, 5) };
            buttonStyle.normal.background = buttonStyle.hover.background = buttonStyle.active.background = button;
            buttonStyle.normal.textColor = buttonStyle.hover.textColor = buttonStyle.active.textColor = Color.white;
            clockStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = FontSize, fontStyle = FontStyle.Bold };
            clockStyle.normal.textColor = Color.white;
            tooltipStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = FontSize - 1, clipping = TextClipping.Clip };
            tooltipStyle.normal.textColor = Color.white;
        }

        private void DestroyTextures()
        {
            if (background != null) UnityEngine.Object.Destroy(background);
            if (button != null) UnityEngine.Object.Destroy(button);
            background = button = null;
        }

        public void Dispose() => DestroyTextures();
    }
}
