// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera;
using Zetori.Replay;
using Zetori.UI;
using DiscordRPC;
using GorillaGameModes;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Zetori.DiscordRichPresence
{
    internal sealed class DiscordRPCStateManager : ObjectComponent
    {
        private const float RefreshInterval = 1f;
        private const float MinimumSendInterval = 4f;
        private const string LargeImageKey = "large";

        private static readonly string LargeImageText = $"{PluginInfo.Name} v{PluginInfo.Version}";
        private static readonly Button[] Links = { new Button { Label = $"Get {PluginInfo.Name}", Url = PluginInfo.Website } };
        private static int republishPending = 1;

        private string? sentDetails;
        private string? sentState;
        private string? sentPartyId;
        private int sentPartySize;
        private int sentPartyMax;
        private float lastSent = float.NegativeInfinity;
        private float nextRefresh;
        private bool cleared;
        private string? currentRoomName;
        private string? currentPartyId;

        internal static void RequestRepublish() => Interlocked.Exchange(ref republishPending, 1);

        private void Update()
        {
            DiscordRPCWrapper? client = DiscordRPCManager.Client;
            if (client == null) return;
            if (!CasterConfig.DiscordPresence.Value)
            {
                ClearPresence(client);
                return;
            }
            cleared = false;
            float now = Time.unscaledTime;
            if (now < nextRefresh && Volatile.Read(ref republishPending) == 0) return;
            nextRefresh = now + RefreshInterval;
            PublishPresence(client, now);
        }

        private void ClearPresence(DiscordRPCWrapper client)
        {
            if (cleared || !client.IsConnected) return;
            client.ClearPresence();
            cleared = true;
            sentDetails = null;
            sentState = null;
            sentPartyId = null;
        }

        private void PublishPresence(DiscordRPCWrapper client, float now)
        {
            bool loading = GorillaTagger.Instance == null;
            Room? room = !loading && PhotonNetwork.InRoom && !ReplaySession.IsReplaying ? PhotonNetwork.CurrentRoom : null;
            string details = loading ? "Loading Gorilla Tag" : Details();
            string state = loading ? LargeImageText : State(room);
            string? partyId = PartyIdFor(room);
            int partySize = room?.PlayerCount ?? 0;
            int partyMax = room?.MaxPlayers ?? 0;

            bool changed = details != sentDetails || state != sentState || partyId != sentPartyId || partySize != sentPartySize || partyMax != sentPartyMax;
            bool republish = Interlocked.Exchange(ref republishPending, 0) == 1;
            if (!republish && (!changed || now - lastSent < MinimumSendInterval)) return;

            sentDetails = details;
            sentState = state;
            sentPartyId = partyId;
            sentPartySize = partySize;
            sentPartyMax = partyMax;
            lastSent = now;
            client.UpdatePresence(presence =>
            {
                presence.Details = details;
                presence.State = state;
                presence.Assets ??= new Assets();
                presence.Assets.LargeImageKey = LargeImageKey;
                presence.Assets.LargeImageText = LargeImageText;
                presence.Party = partyId != null ? new Party { ID = partyId, Size = partySize, Max = partyMax } : null!;
                presence.Buttons = Links;
            });
        }

        private string? PartyIdFor(Room? room)
        {
            string? roomName = room?.Name;
            if (roomName == currentRoomName) return currentPartyId;
            currentRoomName = roomName;
            currentPartyId = roomName != null ? Guid.NewGuid().ToString("N") : null;
            return currentPartyId;
        }

        private static string Details()
        {
            string? map = MapName();
            string activity = map != null ? $"{ActivityName()} in {map}" : ActivityName();
            return ReplaySession.IsRecording ? $"{activity} · Recording" : activity;
        }

        private static string ActivityName()
        {
            if (ReplaySession.IsReplaying) return "Watching a replay";
            return CameraManager.mode switch
            {
                CameraMode.Spectator => "Spectating",
                CameraMode.Freecam => "Flying the free camera",
                CameraMode.Web => "Casting with a camera web",
                CameraMode.Camera => "Filming with the VR camera",
                _ => "Playing"
            };
        }

        private static string State(Room? room)
        {
            ReplayPlayer? replay = ReplaySession.Player;
            if (replay != null) return $"{ReplayTimeline.Format(replay.Recording.Duration)} recording";
            if (room == null) return "Not in a lobby";
            string visibility = room.IsVisible ? "Public lobby" : "Private room";
            string? mode = GameModeName();
            return mode != null ? $"{mode} · {visibility}" : visibility;
        }

        private static string? MapName()
        {
            ZoneManagement? zones = ZoneManagement.instance;
            if (zones == null || zones.activeZones.Count == 0) return null;
            return ZoneName(zones.activeZones[0]);
        }

        private static string? ZoneName(GTZone zone) => zone switch
        {
            GTZone.none => null,
            GTZone.canyon => "Canyons",
            GTZone.mountain => "Mountains",
            GTZone.skyJungle => "Clouds",
            GTZone.cave => "Caves",
            GTZone.cityWithSkyJungle or GTZone.cityNoBuildings => "City",
            GTZone.forestWithCity => "Forest",
            GTZone.customMaps => "Virtual Stump",
            GTZone.monkeBlocks or GTZone.monkeBlocksShared => "Monke Blocks",
            GTZone.hoverboard => "Hoverpark",
            GTZone.ghostReactor or GTZone.ghostReactorTunnel or GTZone.ghostReactorDrill => "Ghost Reactor",
            _ => DisplayNames.Humanize(zone.ToString())
        };

        private static string? GameModeName()
        {
            GorillaGameManager? manager = GorillaGameManager.instance;
            return manager != null ? GameModeName(manager.GameType()) : null;
        }

        private static string? GameModeName(GameModeType mode) => mode switch
        {
            GameModeType.None or GameModeType.Count => null,
            GameModeType.HuntDown => "Hunt",
            GameModeType.FreezeTag => "Freeze Tag",
            GameModeType.PropHunt => "Prop Hunt",
            GameModeType.InfectionCompetitive => "Competitive Infection",
            GameModeType.SuperInfect => "Super Infection",
            GameModeType.SuperCasual => "Super Casual",
            _ => DisplayNames.Humanize(mode.ToString())
        };
    }
}
