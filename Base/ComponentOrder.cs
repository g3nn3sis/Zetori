// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base.Config;
using Zetori.Cheats;
using Zetori.Cheats.Signals;
using Zetori.DiscordRichPresence;
using Zetori.GameCamera;
using Zetori.GameCamera.Photo;
using Zetori.GameCamera.Webs;
using Zetori.History;
using Zetori.Hud.Overlays;
using Zetori.NameTags;
using Zetori.Player;
using Zetori.Player.Movement;
using Zetori.Player.Voice;
using Zetori.Replay;
using Zetori.Replay.Render;
using Zetori.Shared.Assets;
using Zetori.TagManager;
using Zetori.UI;

namespace Zetori.Base
{
    internal static class ComponentOrder
    {
        private static readonly Type[] Sequence =
        {
            typeof(CoreUI),
            typeof(WebMarkers),
            typeof(ReplayRender),
            typeof(ReplaySession),
            typeof(CheatSignals),
            typeof(FlaggedPlayers),
            typeof(GorillaController),
            typeof(LocalPlayerManager),
            typeof(MicrophoneDeviceWatcher),
            typeof(NetworkedPlayerManager),
            typeof(PlayerHistory),
            typeof(PlayerVolume),
            typeof(RigTeleport),
            typeof(SessionStats),
            typeof(ZetoriUsers),
            typeof(AssetManager),
            typeof(CheatAlertFeed),
            typeof(CheatAlerts),
            typeof(ClosestLava),
            typeof(HeadsetAlertToast),
            typeof(KillFeed),
            typeof(Leaderboard),
            typeof(MiniMap),
            typeof(NameTag),
            typeof(Scoreboard),
            typeof(TagManagerControl),
            typeof(CameraManager),
            typeof(PhotoCapture),
            typeof(DiscordRPCManager),
            typeof(DiscordRPCStateManager),
            typeof(CasterConfigSaver)
        };

        internal static int Of(Type component)
        {
            int index = Array.IndexOf(Sequence, component);
            return index < 0 ? Sequence.Length : index;
        }
    }
}
