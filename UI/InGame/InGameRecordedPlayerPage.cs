// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using System.Text;
using UnityEngine;
using Zetori.Player;
using Zetori.Replay;

namespace Zetori.UI.InGame
{
    internal sealed class InGameRecordedPlayerPage : IDisposable
    {
        private const float ViewHeight = 0.18f;
        private const float DetailsInterval = 0.25f;
        private const string DefaultTitle = "Player";
        private const string Ended = "The replay has ended.";
        private const string Absent = "This player is not in the replay at this moment.";
        private const string Note = "Recorded player. Ping, cheat signals, volume, mute and report are for live players only.";
        private const string SpectateLabel = "Spectate";
        private const string SpectatingLabel = "Spectating";
        private const string Unknown = "Unknown";
        private const string Yes = "Yes";
        private const string No = "No";

        private readonly InGameSurface surface;
        private readonly InGameMirror mirror = new InGameMirror();
        private readonly StringBuilder builder = new StringBuilder();
        private readonly Action viewRow;
        private readonly Action spectate;
        private ReplayPlayer? replay;
        private ReplayPuppet? puppet;
        private Texture? view;
        private string details = string.Empty;
        private float nextDetails;

        internal InGameRecordedPlayerPage(InGameSurface surface)
        {
            this.surface = surface;
            viewRow = DrawViewRow;
            spectate = Spectate;
        }

        internal string Title => puppet != null ? puppet.Actor.Name : DefaultTitle;

        internal Color TitleColor => puppet != null ? InGameDetails.Legible(puppet.Actor.Color) : Color.white;

        internal void Open(ReplayPlayer source, ReplayPuppet target)
        {
            replay = source;
            puppet = target;
            nextDetails = 0f;
        }

        internal void Draw()
        {
            if (replay == null || puppet == null || ReplaySession.Player != replay)
            {
                replay = null;
                puppet = null;
                Conceal();
                surface.Label(Ended);
                return;
            }
            if (!puppet.Visible)
            {
                Conceal();
                surface.Label(Absent);
                return;
            }
            float now = Time.unscaledTime;
            if (now >= nextDetails)
            {
                nextDetails = now + DetailsInterval;
                details = Describe(replay, puppet);
            }
            view = mirror.Follow(puppet.Body, puppet.Body.localScale.x);
            surface.HorizontalArea(viewRow);
            bool spectating = replay.IsSpectating(puppet);
            surface.Button(spectating ? SpectatingLabel : SpectateLabel, spectate, !spectating);
            surface.Label(Note);
        }

        internal void Conceal() => mirror.Conceal();

        private void DrawViewRow()
        {
            surface.Image(view!, ViewHeight);
            surface.Block(details, ViewHeight);
        }

        private void Spectate()
        {
            if (replay == null || puppet == null) return;
            for (int i = 0; i < replay.Puppets.Count; i++)
            {
                if (replay.Puppets[i] != puppet) continue;
                replay.Spectate(i);
                return;
            }
        }

        private string Describe(ReplayPlayer source, ReplayPuppet target)
        {
            ReplayActor actor = target.Actor;
            ActorFrame frame = target.Current;
            string nameHex = ColorUtility.ToHtmlStringRGB(InGameDetails.Legible(actor.Color));
            builder.Clear();
            AddDetail("Name", $"<noparse>{actor.Name}</noparse>", nameHex);
            AddDetail("Platform", PlatformText(actor.Platform));
            AddDetail("FPS", frame.Fps > 0 ? frame.Fps.ToString(CultureInfo.InvariantCulture) : Unknown);
            AddDetail("Color", InGameDetails.ColorCode(actor.Color), nameHex);
            AddDetail("Status", TagText(frame.Tag));
            AddDetail("Distance", DistanceText(source, target));
            AddDetail("In Replay", SpanText(actor));
            AddDetail("Recorder", actor.IsLocal ? Yes : No);
            return builder.ToString();
        }

        private void AddDetail(string label, string value, string hex = InGameDetails.ValueColor) => InGameDetails.Append(builder, label, value, hex);

        private static string PlatformText(int platform)
        {
            string name = Platform.Name(platform);
            return name.Length > 0 ? name : Unknown;
        }

        private static string TagText(ReplayTagState state) => state switch
        {
            ReplayTagState.Lava => "Tagged",
            ReplayTagState.Rock => "Tagged (rock)",
            _ => "Not tagged"
        };

        private static string DistanceText(ReplayPlayer source, ReplayPuppet target)
        {
            if (target.Actor.IsLocal) return "Recorder";
            foreach (ReplayPuppet other in source.Puppets)
                if (other.Actor.IsLocal && other.Visible)
                    return $"{Vector3.Distance(other.Body.position, target.Body.position).ToString("0.0", CultureInfo.InvariantCulture)} m";
            return Unknown;
        }

        private static string SpanText(ReplayActor actor)
        {
            if (actor.Frames.Count == 0) return Unknown;
            return $"{InGameDetails.Clock(actor.Frames[0].Time)} to {InGameDetails.Clock(actor.Frames[actor.Frames.Count - 1].Time)}";
        }

        public void Dispose() => mirror.Dispose();
    }
}
