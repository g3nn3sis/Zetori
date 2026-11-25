// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using System.Text;
using UnityEngine;
using Zetori.Game;
using Zetori.GameCamera.Vr;
using Zetori.Player;
using Zetori.Replay;
using Zetori.UI.Panels;

namespace Zetori.UI.InGame
{
    internal sealed class InGamePlayerPage : IDisposable
    {
        private const float ViewHeight = 0.18f;
        private const float DetailsInterval = 0.25f;
        private const float LineLookupInterval = 1f;
        private const float TalkingLoudness = 0.05f;
        private const float Percent = 100f;
        private const int NoActor = -1;
        private const string DefaultTitle = "Player";
        private const string LeftRoom = "This player is no longer in the room.";
        private const string HiddenByReplay = "Live players are hidden while a replay plays. Open Players to see who is in the replay.";
        private const string SpectateLabel = "Spectate";
        private const string SpectatingLabel = "Spectating";
        private const string FilmLabel = "Film";
        private const string FilmingLabel = "Filming";
        private const string Unknown = "Unknown";
        private const string Yes = "Yes";
        private const string No = "No";

        private static readonly float SignalsHeight = InGameMetrics.BlockHeight(InGameSignalReport.Lines);

        private readonly InGameSurface surface;
        private readonly InGameMirror mirror = new InGameMirror();
        private readonly InGameReportMenu report;
        private readonly StringBuilder builder = new StringBuilder();
        private readonly StringBuilder signalBuilder = new StringBuilder();
        private readonly Action viewRow;
        private readonly Action actionRow;
        private readonly Action spectate;
        private readonly Action film;
        private VRRig? rig;
        private RigContainer? container;
        private GorillaPlayerScoreboardLine? line;
        private Texture? view;
        private int actorNumber = NoActor;
        private string details = string.Empty;
        private string signalHeading = string.Empty;
        private string signals = string.Empty;
        private float nextDetails;
        private float nextLineLookup;

        internal InGamePlayerPage(InGameSurface surface)
        {
            this.surface = surface;
            report = new InGameReportMenu(surface);
            viewRow = DrawViewRow;
            actionRow = DrawActionRow;
            spectate = Spectate;
            film = Film;
        }

        internal string Title => Present ? TagManagerControl.NameOf(rig!.Creator) : DefaultTitle;

        internal Color TitleColor => Present ? NameColor(rig!) : Color.white;

        private bool Present => rig != null && rig.Creator != null && rig.Creator.InRoom && rig.Creator.ActorNumber == actorNumber;

        internal void Open(VRRig target)
        {
            rig = target;
            actorNumber = target.Creator != null ? target.Creator.ActorNumber : NoActor;
            container = target.GetComponent<RigContainer>();
            line = null;
            nextDetails = 0f;
            nextLineLookup = 0f;
        }

        internal void Draw()
        {
            if (ReplaySession.IsReplaying || !Present)
            {
                Conceal();
                surface.Label(ReplaySession.IsReplaying ? HiddenByReplay : LeftRoom);
                return;
            }
            VRRig current = rig!;
            float now = Time.unscaledTime;
            if (now >= nextLineLookup)
            {
                nextLineLookup = now + LineLookupInterval;
                line = PlayerMute.FindLine(current.Creator);
            }
            if (now >= nextDetails)
            {
                nextDetails = now + DetailsInterval;
                details = Describe(current);
                DescribeSignals(current);
            }
            view = mirror.Follow(current.transform, current.scaleFactor);
            surface.HorizontalArea(viewRow);
            if (!current.isOfflineVRRig) DrawSignals();
            surface.HorizontalArea(actionRow);
            if (!current.isOfflineVRRig) DrawVolume(current);
        }

        private void DescribeSignals(VRRig target)
        {
            SignalRecord? record = CheatSignals.For(target);
            signalHeading = InGameSignalReport.Heading(record);
            signals = record != null ? InGameSignalReport.Describe(record, signalBuilder) : string.Empty;
        }

        private void DrawSignals()
        {
            surface.Label(signalHeading);
            if (signals.Length > 0) surface.Block(signals, SignalsHeight);
            surface.Label(InGameSignalReport.CaveatText);
        }

        private void DrawVolume(VRRig target)
        {
            float volume = PlayerVolume.Of(target);
            float chosen = surface.Slider("Volume", volume * Percent, PlayerVolume.Minimum * Percent, PlayerVolume.Maximum * Percent, "F0", "%") / Percent;
            if (!Mathf.Approximately(chosen, volume)) PlayerVolume.Set(target, chosen);
        }

        internal void Conceal() => mirror.Conceal();

        private void DrawViewRow()
        {
            surface.Image(view!, ViewHeight);
            surface.Block(details, ViewHeight);
        }

        private void DrawActionRow()
        {
            bool spectating = NetworkedPlayerManager.selectedPlayer == rig;
            surface.Button(spectating ? SpectatingLabel : SpectateLabel, spectate, !spectating);
            surface.Button(VrCameraDirector.IsFilming(rig) ? FilmingLabel : FilmLabel, film);
            MuteToggle.Draw(surface, line);
            report.Draw(line);
        }

        private void Spectate()
        {
            if (rig != null) NetworkedPlayerManager.Spectate(rig);
        }

        private void Film()
        {
            if (rig == null) return;
            if (VrCameraDirector.IsFilming(rig)) VrCameraDirector.StopFilming();
            else VrCameraDirector.Film(rig);
        }

        private string Describe(VRRig target)
        {
            NetPlayer player = target.Creator;
            Color color = target.playerColor;
            string nameHex = ColorUtility.ToHtmlStringRGB(NameColor(target));
            builder.Clear();
            AddDetail("Name", $"<noparse>{TagManagerControl.NameOf(player)}</noparse>", nameHex);
            AddDetail("Platform", PlatformText(target));
            AddDetail("FPS", FrameRateText(target));
            AddDetail("Ping", PingText());
            AddDetail("Color", InGameDetails.ColorCode(color), nameHex);
            AddDetail("Status", TagManagerControl.IsTagged(target) ? "Tagged" : "Not tagged");
            AddDetail("Speed", $"{Decimal(target.LatestVelocity().magnitude)} m/s");
            AddDetail("Distance", DistanceText(target));
            AddDetail("Voice", VoiceText(target));
            AddDetail("In Room", DurationText(Time.time - player.JoinedTime));
            AddDetail("Zetori", ZetoriUsers.IsUser(target) ? Yes : No);
            AddDetail("Host", player.IsMasterClient ? Yes : No);
            return builder.ToString();
        }

        private void AddDetail(string label, string value, string hex = InGameDetails.ValueColor) => InGameDetails.Append(builder, label, value, hex);

        private static string PlatformText(VRRig target)
        {
            string name = Platform.Name(NetworkedPlayerManager.GetPlatform(target));
            return name.Length > 0 ? name : Unknown;
        }

        private string FrameRateText(VRRig target)
        {
            int frameRate = Mathf.RoundToInt(NetworkedPlayerManager.GetFPS(target));
            int refreshRate = container != null ? container.PlayerStats.TargetFPS : 0;
            if (refreshRate > 0) return $"{frameRate} of {refreshRate} Hz";
            return refreshRate < 0 ? $"{frameRate}, uncapped" : frameRate.ToString(CultureInfo.InvariantCulture);
        }

        private string PingText()
        {
            int ping = container != null ? container.PlayerStats.Ping : 0;
            return ping > 0 ? $"{ping} ms" : Unknown;
        }

        private static string DistanceText(VRRig target)
        {
            if (target.isOfflineVRRig) return "You";
            VRRig? local = GorillaTagger.Instance != null ? GorillaTagger.Instance.offlineVRRig : null;
            return local != null ? $"{Decimal(Vector3.Distance(local.transform.position, target.transform.position))} m" : Unknown;
        }

        private string VoiceText(VRRig target)
        {
            if (line != null && PlayerMute.IsMuted(line)) return "Muted";
            return target.SpeakingLoudness > TalkingLoudness ? "Talking" : "Quiet";
        }

        private static string DurationText(float seconds) => seconds > 0f ? InGameDetails.Clock(seconds) : Unknown;

        private static string Decimal(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);

        private static Color NameColor(VRRig target) => InGameDetails.Legible(target.playerColor);

        public void Dispose() => mirror.Dispose();
    }
}
