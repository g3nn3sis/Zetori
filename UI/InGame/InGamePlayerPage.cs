// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using System.Text;
using UnityEngine;
using Zetori.Game;
using Zetori.Player;
using Zetori.UI.Panels;

namespace Zetori.UI.InGame
{
    internal sealed class InGamePlayerPage : IDisposable
    {
        private const float ViewHeight = 0.18f;
        private const float DetailsInterval = 0.25f;
        private const float LineLookupInterval = 1f;
        private const float TalkingLoudness = 0.05f;
        private const float MinimumNameLuminance = 0.3f;
        private const float NameLift = 0.45f;
        private const int ColorCodeSteps = 9;
        private const int NoActor = -1;
        private const string DefaultTitle = "Player";
        private const string LeftRoom = "This player is no longer in the room.";
        private const string SpectateLabel = "Spectate";
        private const string SpectatingLabel = "Spectating";
        private const string ValueColumn = "<pos=38%>";
        private const string ValueColor = "FFFFFF";
        private const string Unknown = "Unknown";
        private const string Yes = "Yes";
        private const string No = "No";

        private readonly InGameSurface surface;
        private readonly InGameMirror mirror = new InGameMirror();
        private readonly ReportMenu report = new ReportMenu();
        private readonly StringBuilder builder = new StringBuilder();
        private readonly Action viewRow;
        private readonly Action actionRow;
        private readonly Action spectate;
        private VRRig? rig;
        private RigContainer? container;
        private GorillaPlayerScoreboardLine? line;
        private Texture? view;
        private int actorNumber = NoActor;
        private string details = string.Empty;
        private float nextDetails;
        private float nextLineLookup;

        internal InGamePlayerPage(InGameSurface surface)
        {
            this.surface = surface;
            viewRow = DrawViewRow;
            actionRow = DrawActionRow;
            spectate = Spectate;
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
            if (!Present)
            {
                Conceal();
                surface.Label(LeftRoom);
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
            }
            view = mirror.Follow(current);
            surface.HorizontalArea(viewRow);
            surface.HorizontalArea(actionRow);
            report.DrawOptions(surface, line);
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
            MuteToggle.Draw(surface, line);
            report.DrawButton(surface, line);
        }

        private void Spectate()
        {
            if (rig != null) NetworkedPlayerManager.Spectate(rig);
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
            AddDetail("Color", $"{ColorCode(color.r)} {ColorCode(color.g)} {ColorCode(color.b)}", nameHex);
            AddDetail("Status", TagManagerControl.IsTagged(target) ? "Tagged" : "Not tagged");
            AddDetail("Speed", $"{Decimal(target.LatestVelocity().magnitude)} m/s");
            AddDetail("Distance", DistanceText(target));
            AddDetail("Voice", VoiceText(target));
            AddDetail("In Room", DurationText(Time.time - player.JoinedTime));
            AddDetail("Zetori", ZetoriUsers.IsUser(target) ? Yes : No);
            AddDetail("Host", player.IsMasterClient ? Yes : No);
            return builder.ToString();
        }

        private void AddDetail(string label, string value, string hex = ValueColor)
        {
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(label).Append(ValueColumn).Append("<color=#").Append(hex).Append('>').Append(value).Append("</color>");
        }

        private static string PlatformText(VRRig target)
        {
            string name = Platform.Name(NetworkedPlayerManager.GetPlatform(target));
            return name.Length > 0 ? name : Unknown;
        }

        private string FrameRateText(VRRig target)
        {
            int frameRate = Mathf.RoundToInt(NetworkedPlayerManager.GetFPS(target));
            int refreshRate = container != null ? container.PlayerStats.TargetFPS : 0;
            return refreshRate > 0 ? $"{frameRate} of {refreshRate} Hz" : frameRate.ToString(CultureInfo.InvariantCulture);
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

        private static string DurationText(float seconds)
        {
            if (seconds <= 0f) return Unknown;
            var span = TimeSpan.FromSeconds(seconds);
            return span.TotalHours >= 1d
                ? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
                : span.ToString(@"m\:ss", CultureInfo.InvariantCulture);
        }

        private static int ColorCode(float channel) => Mathf.RoundToInt(Mathf.Clamp01(channel) * ColorCodeSteps);

        private static string Decimal(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);

        private static Color NameColor(VRRig target)
        {
            Color color = target.playerColor;
            color.a = 1f;
            float luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
            return luminance < MinimumNameLuminance ? Color.Lerp(color, Color.white, NameLift) : color;
        }

        public void Dispose() => mirror.Dispose();
    }
}
