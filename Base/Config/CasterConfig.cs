// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using Zetori.GameCamera;
using Zetori.GameCamera.Photo;
using Zetori.GameCamera.Vr;
using Zetori.Hud;
using Zetori.Notifications;
using Zetori.Replay.Render;
using Zetori.Replay.Timeline;
using Zetori.UI;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Zetori.Base.Config
{
    internal static class CasterConfig
    {
        private const float SaveDelay = 1f;

        private static readonly ConfigEntry<string>[] panelPositions = new ConfigEntry<string>[(int)PanelId.Count];
        private static ConfigFile? file;
        private static bool dirty;
        private static float lastChange;

        internal static ConfigEntry<CameraMode> ActiveCameraMode = null!;
        internal static ConfigEntry<VrCameraMode> VrMode = null!;
        internal static ConfigEntry<bool> VrFirstPersonCosmetics = null!;
        internal static ConfigEntry<float> PhotoTimer = null!;
        internal static ConfigEntry<Key> PhotoKey = null!;
        internal static ConfigEntry<PhotoBackground> PhotoBackground = null!;
        internal static ConfigEntry<bool> PhotoThirdsGrid = null!;
        internal static ConfigEntry<RenderResolution> ReplayRenderResolution = null!;
        internal static ConfigEntry<int> ReplayRenderFrameRate = null!;
        internal static ConfigEntry<float> FieldOfView = null!;
        internal static ConfigEntry<float> NearClipPlane = null!;
        internal static ConfigEntry<bool> AudioListener = null!;
        internal static ConfigEntry<float> HeadDistance = null!;
        internal static ConfigEntry<float> PositionSmoothing = null!;
        internal static ConfigEntry<float> RotationSmoothing = null!;
        internal static ConfigEntry<float> OrbitSpeed = null!;
        internal static ConfigEntry<bool> OrbitAutoRotate = null!;
        internal static ConfigEntry<float> BodyLerping = null!;
        internal static ConfigEntry<bool> RigExaggeration = null!;
        internal static ConfigEntry<float> RigExaggerationAmount = null!;
        internal static ConfigEntry<bool> RegisterNewRigs = null!;
        internal static ConfigEntry<bool> Microphone = null!;
        internal static ConfigEntry<Key> PushToTalkKey = null!;
        internal static ConfigEntry<Key> NextPlayerKey = null!;
        internal static ConfigEntry<Key> PreviousPlayerKey = null!;
        internal static ConfigEntry<Key> ReplayPauseKey = null!;
        internal static ConfigEntry<Key> ReplayTimelineKey = null!;
        internal static ConfigEntry<Key> FrameBackKey = null!;
        internal static ConfigEntry<Key> FrameForwardKey = null!;
        internal static ConfigEntry<TimelineEdge> ReplayTimelineEdge = null!;
        internal static ConfigEntry<bool> InstantReplay = null!;
        internal static ConfigEntry<float> ClipLength = null!;
        internal static ConfigEntry<Key> ClipKey = null!;
        internal static ConfigEntry<Key> MarkerKey = null!;
        internal static ConfigEntry<Key> OrbitLeftKey = null!;
        internal static ConfigEntry<Key> OrbitRightKey = null!;
        internal static ConfigEntry<Key> OrbitUpKey = null!;
        internal static ConfigEntry<Key> OrbitDownKey = null!;
        internal static ConfigEntry<Key> OrbitResetKey = null!;

        internal static ConfigEntry<string> WebName = null!;
        internal static ConfigEntry<bool> WebDynamic = null!;
        internal static ConfigEntry<Key> WebNextNodeKey = null!;
        internal static ConfigEntry<Key> WebPreviousNodeKey = null!;
        internal static ConfigEntry<Key> WebPlaceNodeKey = null!;
        internal static ConfigEntry<Key> WebRemoveNodeKey = null!;

        internal static ConfigEntry<bool> Wasd = null!;
        internal static ConfigEntry<bool> MouseClick = null!;
        internal static ConfigEntry<float> MovementSpeed = null!;
        internal static ConfigEntry<float> MouseSpeed = null!;
        internal static ConfigEntry<Key> FlyForwardKey = null!;
        internal static ConfigEntry<Key> FlyBackKey = null!;
        internal static ConfigEntry<Key> FlyLeftKey = null!;
        internal static ConfigEntry<Key> FlyRightKey = null!;
        internal static ConfigEntry<Key> FlyUpKey = null!;
        internal static ConfigEntry<Key> FlyDownKey = null!;
        internal static ConfigEntry<Key> NoClipKey = null!;

        internal static ConfigEntry<float> FreecamMovementSpeed = null!;
        internal static ConfigEntry<float> FreecamRotationSensitivity = null!;

        internal static ConfigEntry<bool> NameTags = null!;
        internal static ConfigEntry<bool> NameTagPlayerColor = null!;
        internal static ConfigEntry<bool> NameTagFps = null!;
        internal static ConfigEntry<bool> NameTagPlatform = null!;
        internal static ConfigEntry<bool> NameTagZetori = null!;
        internal static ConfigEntry<int> NameTagFont = null!;
        internal static ConfigEntry<float> NameTagSize = null!;
        internal static ConfigEntry<bool> MiniMapEnabled = null!;
        internal static ConfigEntry<bool> MiniMapDraggable = null!;
        internal static ConfigEntry<float> MiniMapSize = null!;
        internal static ConfigEntry<float> MiniMapHeight = null!;
        internal static ConfigEntry<float> MiniMapOffsetX = null!;
        internal static ConfigEntry<float> MiniMapOffsetY = null!;
        internal static ConfigEntry<HudPosition> MiniMapPosition = null!;
        internal static ConfigEntry<bool> DiscordPresence = null!;
        internal static ConfigEntry<bool> HideRoomCodes = null!;
        internal static ConfigEntry<bool> CheatAlerts = null!;
        internal static ConfigEntry<bool> CheatAlertsOnScreen = null!;
        internal static ConfigEntry<float> CheatAlertsScale = null!;
        internal static ConfigEntry<float> CheatAlertsOffsetX = null!;
        internal static ConfigEntry<float> CheatAlertsOffsetY = null!;
        internal static ConfigEntry<bool> CheatAlertsInHeadset = null!;
        internal static ConfigEntry<bool> CheatAlertsReasons = null!;
        internal static ConfigEntry<bool> CheatAlertGameReports = null!;
        internal static ConfigEntry<bool> CheatAlertModData = null!;
        internal static ConfigEntry<bool> CheatAlertOddNames = null!;
        internal static ConfigEntry<bool> CheatAlertQuestTweaks = null!;
        internal static ConfigEntry<bool> CheatAlertCosmetics = null!;
        internal static ConfigEntry<bool> CheatAlertSpeed = null!;
        internal static ConfigEntry<bool> CheatAlertTeleports = null!;
        internal static ConfigEntry<bool> CheatAlertArmReach = null!;
        internal static ConfigEntry<bool> CheatAlertTagReach = null!;
        internal static ConfigEntry<bool> CheatAlertHandMotion = null!;
        internal static ConfigEntry<bool> CheatAlertNetworkSpam = null!;
        internal static ConfigEntry<bool> CheatAlertLowGravity = null!;
        internal static ConfigEntry<bool> CheatAlertTagBursts = null!;
        internal static ConfigEntry<bool> CheatAlertFlaggedBefore = null!;
        internal static ConfigEntry<bool> CheatAlertThroughWalls = null!;
        internal static ConfigEntry<bool> CheatAlertUnderMap = null!;
        internal static ConfigEntry<bool> CheatAlertHeadSpin = null!;
        internal static ConfigEntry<bool> CheatAlertIdentitySpam = null!;
        internal static ConfigEntry<bool> CheatAlertProjectileSpam = null!;
        internal static ConfigEntry<bool> CheatAlertMasterTheft = null!;
        internal static ConfigEntry<bool> CheatAlertPropertyTampering = null!;
        internal static ConfigEntry<bool> CheatAlertLagTricks = null!;
        internal static ConfigEntry<float> NotificationScale = null!;
        internal static ConfigEntry<float> NotificationX = null!;
        internal static ConfigEntry<float> NotificationY = null!;
        internal static ConfigEntry<NotificationTheme> NotificationTheme = null!;
        internal static ConfigEntry<float> NotificationRed = null!;
        internal static ConfigEntry<float> NotificationGreen = null!;
        internal static ConfigEntry<float> NotificationBlue = null!;
        internal static ConfigEntry<bool> NotificationBackground = null!;
        internal static ConfigEntry<bool> NotificationRoundedCorners = null!;
        internal static ConfigEntry<int> NotificationFont = null!;
        internal static ConfigEntry<float> NotificationTextRed = null!;
        internal static ConfigEntry<float> NotificationTextGreen = null!;
        internal static ConfigEntry<float> NotificationTextBlue = null!;

        internal static ConfigEntry<int> HudFont = null!;

        internal static ConfigEntry<bool> LeaderboardEnabled = null!;
        internal static ConfigEntry<float> LeaderboardScale = null!;
        internal static ConfigEntry<bool> LeaderboardPlatformIcons = null!;
        internal static ConfigEntry<bool> LeaderboardFps = null!;
        internal static ConfigEntry<float> LeaderboardOffsetX = null!;
        internal static ConfigEntry<float> LeaderboardOffsetY = null!;
        internal static ConfigEntry<HudPosition> LeaderboardPosition = null!;

        internal static ConfigEntry<bool> ClosestLavaEnabled = null!;
        internal static ConfigEntry<float> ClosestLavaScale = null!;
        internal static ConfigEntry<float> ClosestLavaOffsetX = null!;
        internal static ConfigEntry<float> ClosestLavaOffsetY = null!;

        internal static ConfigEntry<bool> ScoreboardEnabled = null!;
        internal static ConfigEntry<float> ScoreboardScale = null!;
        internal static ConfigEntry<float> ScoreboardOffsetX = null!;
        internal static ConfigEntry<float> ScoreboardOffsetY = null!;
        internal static ConfigEntry<HudPosition> ScoreboardPosition = null!;
        internal static ConfigEntry<string> ScoreboardTeamAName = null!;
        internal static ConfigEntry<string> ScoreboardTeamBName = null!;
        internal static ConfigEntry<int> ScoreboardTeamAScore = null!;
        internal static ConfigEntry<int> ScoreboardTeamBScore = null!;
        internal static ConfigEntry<Key> ScoreboardStartKey = null!;
        internal static ConfigEntry<Key> ScoreboardPauseKey = null!;
        internal static ConfigEntry<Key> ScoreboardResetKey = null!;
        internal static ConfigEntry<Key> ScoreboardTeamAUpKey = null!;
        internal static ConfigEntry<Key> ScoreboardTeamADownKey = null!;
        internal static ConfigEntry<Key> ScoreboardTeamBUpKey = null!;
        internal static ConfigEntry<Key> ScoreboardTeamBDownKey = null!;

        internal static ConfigEntry<bool> KillFeedEnabled = null!;
        internal static ConfigEntry<float> KillFeedScale = null!;
        internal static ConfigEntry<float> KillFeedOffsetX = null!;
        internal static ConfigEntry<float> KillFeedOffsetY = null!;
        internal static ConfigEntry<HudPosition> KillFeedPosition = null!;

        internal static ConfigEntry<bool> TagManagerEnabled = null!;
        internal static ConfigEntry<bool> TagManagerRefereeOnly = null!;
        internal static ConfigEntry<bool> TagManagerPlayerTags = null!;
        internal static ConfigEntry<bool> TagManagerLocalFirst = null!;

        internal static ConfigEntry<string> PlayerName = null!;
        internal static ConfigEntry<float> PlayerRed = null!;
        internal static ConfigEntry<float> PlayerGreen = null!;
        internal static ConfigEntry<float> PlayerBlue = null!;

        internal static ConfigEntry<bool> DisableVSync = null!;
        internal static ConfigEntry<bool> UncapFps = null!;
        internal static ConfigEntry<bool> DisableAfkKick = null!;

        internal static ConfigEntry<string> Theme = null!;
        internal const float MinimumUiScale = 0.75f;
        internal const float MaximumUiScale = 1f;
        internal const float MaximumPositionSmoothing = 0.95f;
        internal const float MaximumRotationSmoothing = 0.75f;
        internal const float MinimumNameTagSize = 0.5f;
        internal const float MaximumNameTagSize = 2f;
        internal const float MinimumClipLength = 10f;
        internal const float MaximumClipLength = 120f;
        internal const float MaximumPhotoTimer = 10f;
        internal static readonly int[] ReplayRenderFrameRates = { 30, 60 };

        internal static ConfigEntry<float> UiScale = null!;
        internal static ConfigEntry<bool> SnapPanels = null!;
        internal static ConfigEntry<bool> ShowTips = null!;
        internal static ConfigEntry<Key> MenuKey = null!;
        internal static ConfigEntry<bool> AutoSave = null!;

        internal const float MinimumInGamePanelSize = 0.75f;
        internal const float MaximumInGamePanelSize = 1.5f;

        internal static ConfigEntry<float> InGamePanelSize = null!;
        internal static ConfigEntry<bool> InGamePanelHaptics = null!;
        internal static ConfigEntry<bool> InGamePanelOnStream = null!;

        internal static ConfigEntry<string> IgnoredUpdate = null!;

        internal static void Bind(ConfigFile config)
        {
            file = config;
            config.SaveOnConfigSet = false;

            ActiveCameraMode = config.Bind("Camera", "Mode", CameraMode.Spectator, "Default shows your own first person view, Spectator follows the selected player, Freecam flies the camera on its own (not in VR), Web watches the action from placed nodes, Camera uses the in-game VR camera tablet.");
            VrMode = config.Bind("VR Camera", "Mode", VrCameraMode.Handheld, "What the in-game VR camera shows: FirstPerson, ThirdPerson, Follower or Handheld.");
            VrFirstPersonCosmetics = config.Bind("VR Camera", "First Person Cosmetics", true, "Whether the in-game VR camera shows your own cosmetics in first person.");
            PhotoTimer = Range(config, "Photo", "Timer", 3f, 0f, MaximumPhotoTimer, "Seconds between pressing Take Photo and the photo being taken.");
            PhotoKey = config.Bind("Photo", "Photo Key", Key.F9, "Take a 4K photo from the camera, without the panels and overlays.");
            PhotoBackground = config.Bind("Photo", "Background", GameCamera.Photo.PhotoBackground.Scene, "Scene photographs everything; Transparent, Green and Blue show only the spectated player, on a see-through, green-screen or blue-screen background.");
            PhotoThirdsGrid = config.Bind("Photo", "Thirds Grid", false, "Draw rule-of-thirds lines over the game window to help frame a shot. They are never in photos.");
            ReplayRenderResolution = config.Bind("Render", "Resolution", RenderResolution.P2160, "Size of each frame when rendering a replay: P1080, P1440 or P2160 (4K).");
            ReplayRenderFrameRate = config.Bind("Render", "Frame Rate", 60, new ConfigDescription("Frames per second of replay time when rendering a replay.", new AcceptableValueList<int>(ReplayRenderFrameRates)));
            FieldOfView = Range(config, "Camera", "Field Of View", 100f, 10f, 130f);
            NearClipPlane = Range(config, "Camera", "Near Clip", 0.01f, 0.01f, 0.99f);
            AudioListener = config.Bind("Camera", "Audio Listener", true, "Hear the game from the spectator camera.");
            HeadDistance = Range(config, "Camera", "Head Distance", 0f, 0f, 0.99f, "0 is first person, anything higher orbits the player.");
            PositionSmoothing = Range(config, "Camera", "Position Smoothing", 0.05f, 0f, MaximumPositionSmoothing, "How much the orbit camera lags behind the player. 0 follows instantly.");
            RotationSmoothing = Range(config, "Camera", "Rotation Smoothing", 0f, 0f, MaximumRotationSmoothing, "How much the orbit camera's rotation lags behind. 0 follows instantly.");
            OrbitSpeed = Range(config, "Camera", "Orbit Speed", 150f, 10f, 600f, "Arrow key orbit speed in degrees per second.");
            OrbitAutoRotate = config.Bind("Camera", "Auto Orbit", false, "Slowly rotate the orbit camera around the spectated player with no input. Arrow keys still steer on top.");
            BodyLerping = Range(config, "Camera", "Body Lerping", 1f, 0.1f, 2.5f, "Multiplier on how quickly other players' rigs move to their networked position.");
            RigExaggeration = config.Bind("Camera", "Rig Exaggeration", false, "Make the spectated player's arm movements bigger on your screen only.");
            RigExaggerationAmount = Range(config, "Camera", "Rig Exaggeration Amount", 1.5f, 1f, 3f, "How much the spectated player's arm movements are multiplied.");
            RegisterNewRigs = config.Bind("Camera", "Register New Rigs", true, "Keep the player list updated as players join and leave.");
            Microphone = config.Bind("Camera", "Microphone", false, "On is all chat, off is push to talk.");
            PushToTalkKey = config.Bind("Camera", "Hold To Talk Key", Key.V, "Hold to open the mic to all chat; release returns to the Microphone setting (push to talk by default).");
            NextPlayerKey = config.Bind("Camera", "Next Player Key", Key.RightBracket, "Spectate the next player in the list.");
            PreviousPlayerKey = config.Bind("Camera", "Previous Player Key", Key.LeftBracket, "Spectate the previous player in the list.");
            OrbitLeftKey = config.Bind("Camera", "Orbit Left Key", Key.LeftArrow, "Hold to orbit the spectator camera left.");
            OrbitRightKey = config.Bind("Camera", "Orbit Right Key", Key.RightArrow, "Hold to orbit the spectator camera right.");
            OrbitUpKey = config.Bind("Camera", "Orbit Up Key", Key.UpArrow, "Hold to orbit the spectator camera up.");
            OrbitDownKey = config.Bind("Camera", "Orbit Down Key", Key.DownArrow, "Hold to orbit the spectator camera down.");
            OrbitResetKey = config.Bind("Camera", "Reset Orbit Key", Key.R, "Put the orbit camera back behind the spectated player.");
            ReplayPauseKey = config.Bind("Replay", "Pause Key", Key.P, "Pause and unpause the replay while in replay mode.");
            ReplayTimelineKey = config.Bind("Replay", "Timeline Key", Key.F1, "Show and hide the replay timeline while in replay mode.");
            FrameBackKey = config.Bind("Replay", "Frame Back Key", Key.Comma, "Pause the replay and step back one recorded frame.");
            FrameForwardKey = config.Bind("Replay", "Frame Forward Key", Key.Period, "Pause the replay and step forward one recorded frame.");
            ReplayTimelineEdge = config.Bind("Replay", "Timeline Position", TimelineEdge.Top, "Screen edge the replay timeline docks to: Top or Bottom. Drag the timeline to move it.");
            InstantReplay = config.Bind("Replay", "Instant Replay", false, "Keep the last moments of play in memory so Clip can save them as a replay at any time.");
            ClipLength = Range(config, "Replay", "Clip Length", 30f, MinimumClipLength, MaximumClipLength, "How many seconds Clip saves.");
            ClipKey = config.Bind("Replay", "Clip Key", Key.F8, "Save the last moments as a replay clip.");
            MarkerKey = config.Bind("Replay", "Marker Key", Key.F7, "Drop a numbered marker on the replay timeline while recording or keeping an instant replay.");

            WebName = config.Bind("Web", "Name", string.Empty, "Name of the saved camera web to use, from the Webs folder.");
            WebDynamic = config.Bind("Web", "Dynamic", true, "On picks the node nearest the action by itself, off steps through nodes with the node keys.");
            WebNextNodeKey = config.Bind("Web", "Next Node Key", Key.RightArrow, "Move the web camera to the next node in manual selection.");
            WebPreviousNodeKey = config.Bind("Web", "Previous Node Key", Key.LeftArrow, "Move the web camera to the previous node in manual selection.");
            WebPlaceNodeKey = config.Bind("Web", "Place Node Key", Key.F2, "Add a node where the camera is while editing the web.");
            WebRemoveNodeKey = config.Bind("Web", "Remove Node Key", Key.F3, "Remove the node nearest the camera while editing the web.");

            Wasd = config.Bind("Freecam", "Enabled", false, "Hold right mouse to fly your own player with WASD. Turned off in VR and while the camera is in Freecam mode.");
            MouseClick = config.Bind("Freecam", "Mouse Click", false, "Left click presses whatever is under the cursor with the right hand.");
            MovementSpeed = Range(config, "Freecam", "Movement Speed", 10f, 5f, 35f);
            MouseSpeed = Range(config, "Freecam", "Mouse Sensitivity", 0.3f, 0.05f, 2f);
            FlyForwardKey = config.Bind("Freecam", "Forward Key", Key.W, "Fly forward while holding right mouse.");
            FlyBackKey = config.Bind("Freecam", "Back Key", Key.S, "Fly back while holding right mouse.");
            FlyLeftKey = config.Bind("Freecam", "Left Key", Key.A, "Fly left while holding right mouse.");
            FlyRightKey = config.Bind("Freecam", "Right Key", Key.D, "Fly right while holding right mouse.");
            FlyUpKey = config.Bind("Freecam", "Up Key", Key.Space, "Fly up while holding right mouse.");
            FlyDownKey = config.Bind("Freecam", "Down Key", Key.LeftShift, "Fly down while holding right mouse.");
            NoClipKey = config.Bind("Freecam", "No Clip Key", Key.E, "Hold while flying your own player to pass through walls.");

            FreecamMovementSpeed = Range(config, "Freecam Camera", "Movement Speed", 10f, 1f, 50f, "How fast the camera flies in Freecam mode and while editing a camera web, in metres per second.");
            FreecamRotationSensitivity = Range(config, "Freecam Camera", "Rotation Sensitivity", 0.3f, 0.05f, 2f, "How far the camera turns per unit of mouse movement in Freecam mode and while editing a camera web.");

            NameTags = config.Bind("Nametags", "Enabled", false);
            NameTagPlayerColor = config.Bind("Nametags", "Player Color", false);
            NameTagFps = config.Bind("Nametags", "Player FPS", false);
            NameTagPlatform = config.Bind("Nametags", "Platform Indicator", false);
            NameTagZetori = config.Bind("Nametags", "Zetori Users", false);
            NameTagFont = config.Bind("Nametags", "Font Index", 0, new ConfigDescription("Index into the fonts found in the game.", new AcceptableValueRange<int>(0, 255)));
            NameTagSize = Range(config, "Nametags", "Size", 1f, MinimumNameTagSize, MaximumNameTagSize, "Size multiplier on the nametags above players.");

            MiniMapEnabled = config.Bind("MiniMap", "Enabled", false);
            MiniMapDraggable = config.Bind("MiniMap", "Draggable", false);
            MiniMapSize = Range(config, "MiniMap", "Size", 520f, 100f, 520f, "Width and height at 1080p, scaled with the interface.");
            MiniMapHeight = Range(config, "MiniMap", "Camera Height", 8f, 1f, 50f);
            MiniMapOffsetX = Range(config, "MiniMap", "Offset X", 10f, 0f, 8000f, "Distance from the side edge of its corner at 1080p.");
            MiniMapOffsetY = Range(config, "MiniMap", "Offset Y", 10f, 0f, 8000f, "Distance from the top or bottom edge of its corner at 1080p.");
            MiniMapPosition = config.Bind("MiniMap", "Position", HudPosition.TopRight, "Corner the minimap sits in: BottomLeft, BottomRight or TopRight. Only one overlay can use a corner.");

            DiscordPresence = config.Bind("Discord", "Rich Presence", true, "Show what you are doing on your Discord profile.");
            CheatAlerts = config.Bind("Cheat Alerts", "Enabled", false, "Show a short notice below the centre of the screen and of your headset view when another player raises a cheat or mod signal.");
            CheatAlertsOnScreen = config.Bind("Cheat Alerts", "On Screen", true, "Show the alerts on the game window.");
            CheatAlertsScale = Range(config, "Cheat Alerts", "Scale", 1f, 0.5f, 2f, "Size multiplier of the on-screen alerts on top of automatic resolution scaling.");
            CheatAlertsOffsetX = Range(config, "Cheat Alerts", "Offset X", 0f, -800f, 800f, "Where the top of the on-screen alerts sits left (negative) or right (positive) of the screen centre, in 1080p pixels.");
            CheatAlertsOffsetY = Range(config, "Cheat Alerts", "Offset Y", -150f, -500f, 500f, "Where the top of the on-screen alerts sits below (negative) or above (positive) the screen centre, in 1080p pixels.");
            CheatAlertsInHeadset = config.Bind("Cheat Alerts", "In Headset", true, "With a headset on, also show the alerts in your view, seen only by your headset.");
            CheatAlertsReasons = config.Bind("Cheat Alerts", "Report Reasons", true, "Add the game's reason after a game report.");
            CheatAlertGameReports = config.Bind("Cheat Alerts", "Game Reports", true, "Alert when the game's own cheat checks report a player.");
            CheatAlertModData = config.Bind("Cheat Alerts", "Mod Data", true, "Alert when a player's profile carries mod data.");
            CheatAlertOddNames = config.Bind("Cheat Alerts", "Odd Names", true, "Alert when a player's name is banned, too long or has symbols the game's keyboard lacks.");
            CheatAlertQuestTweaks = config.Bind("Cheat Alerts", "Quest Tweaks", true, "Alert when a Quest player's headset settings were changed with developer tools such as ADB.");
            CheatAlertCosmetics = config.Bind("Cheat Alerts", "Unowned Cosmetics", true, "Alert when a player wears cosmetics they do not own.");
            CheatAlertSpeed = config.Bind("Cheat Alerts", "Too Fast", true, "Alert when a player moves faster than the game allows.");
            CheatAlertTeleports = config.Bind("Cheat Alerts", "Teleporting", true, "Alert when a player jumps across the map.");
            CheatAlertArmReach = config.Bind("Cheat Alerts", "Long Arms", true, "Alert when a player's hands reach further than the game allows, holding on or in the air.");
            CheatAlertTagReach = config.Bind("Cheat Alerts", "Far Tags", true, "Alert when a player tags others from too far away.");
            CheatAlertHandMotion = config.Bind("Cheat Alerts", "Inhuman Hands", true, "Alert when a player's hands move faster, jerk harder or fling further than a real arm can, as a hand prediction cheat makes them.");
            CheatAlertNetworkSpam = config.Bind("Cheat Alerts", "Network Spam", true, "Alert when a player sends network messages many times faster than everyone else in the room, as lag, crash and sound spam cheats do.");
            CheatAlertLowGravity = config.Bind("Cheat Alerts", "Low Gravity", true, "Alert when a player in open air falls much slower than gravity pulls, or rises, as fly and low gravity cheats make them.");
            CheatAlertTagBursts = config.Bind("Cheat Alerts", "Tag Bursts", true, "Alert when a player tags several players in the same half second, as tag all cheats do.");
            CheatAlertFlaggedBefore = config.Bind("Cheat Alerts", "Flagged Before", true, "Alert when a player joins who raised cheat signals in an earlier room.");
            CheatAlertThroughWalls = config.Bind("Cheat Alerts", "Through Walls", true, "Alert when a player passes through solid walls, as noclip cheats let them.");
            CheatAlertUnderMap = config.Bind("Cheat Alerts", "Under The Map", true, "Alert when a player stays below the map's floor.");
            CheatAlertHeadSpin = config.Bind("Cheat Alerts", "Spinning Head", true, "Alert when a player's head turns faster than a person can, as spin cheats make it.");
            CheatAlertIdentitySpam = config.Bind("Cheat Alerts", "Colour Name Spam", true, "Alert when a player changes colour many times a second or keeps changing name, as rainbow and name changer cheats do.");
            CheatAlertProjectileSpam = config.Bind("Cheat Alerts", "Projectile Spam", true, "Alert when a player launches projectiles faster than the game allows.");
            CheatAlertMasterTheft = config.Bind("Cheat Alerts", "Took Master", true, "Alert when a player makes themselves master client, which the game never does.");
            CheatAlertPropertyTampering = config.Bind("Cheat Alerts", "Property Tampering", true, "Alert when a player who is not master client changes the room's or another player's properties.");
            CheatAlertLagTricks = config.Bind("Cheat Alerts", "Lag Tricks", true, "Alert when a player's updates stop and jump while everyone else's keep coming, run backwards in time or arrive far out of step with the room, as lag switches do.");
            NotificationScale = Range(config, "Cheat Alerts", "Headset Scale", 1f, 0.5f, 2f, "Size multiplier of notifications in your headset view.");
            NotificationX = Range(config, "Cheat Alerts", "Headset X", 0f, -40f, 40f, "Where notifications sit left (negative) or right (positive) of the centre of your headset view, in centimetres at 80 cm away.");
            NotificationY = Range(config, "Cheat Alerts", "Headset Y", -17f, -40f, 40f, "Where notifications sit below (negative) or above (positive) the centre of your headset view, in centimetres at 80 cm away.");
            NotificationTheme = config.Bind("Notifications", "Theme", Notifications.NotificationTheme.Interface, "Interface colours notification backgrounds with the Interface panel's theme colour; Custom uses the custom colour.");
            NotificationRed = Range(config, "Notifications", "Custom Red", 0.12f, 0f, 1f, "Red part of the custom notification background colour.");
            NotificationGreen = Range(config, "Notifications", "Custom Green", 0.12f, 0f, 1f, "Green part of the custom notification background colour.");
            NotificationBlue = Range(config, "Notifications", "Custom Blue", 0.12f, 0f, 1f, "Blue part of the custom notification background colour.");
            NotificationBackground = config.Bind("Notifications", "Background", false, "Draw a coloured box behind each notification.");
            NotificationRoundedCorners = config.Bind("Notifications", "Rounded Corners", true, "Round the corners of the box behind each notification.");
            NotificationFont = config.Bind("Notifications", "Font Index", 0, new ConfigDescription("Index into the fonts found in the game, used by notifications in your headset view.", new AcceptableValueRange<int>(0, 255)));
            NotificationTextRed = Range(config, "Notifications", "Text Red", 1f, 0f, 1f, "Red part of the notification text colour.");
            NotificationTextGreen = Range(config, "Notifications", "Text Green", 1f, 0f, 1f, "Green part of the notification text colour.");
            NotificationTextBlue = Range(config, "Notifications", "Text Blue", 1f, 0f, 1f, "Blue part of the notification text colour.");
            HideRoomCodes = config.Bind("Streamer", "Hide Room Codes", false, "Show Hidden instead of room codes in every panel, the in-game panel and its keyboard, so they never appear on stream or in a recording.");

            HudFont = config.Bind("HUD", "Font Index", 0, new ConfigDescription("Index into the fonts found in the game, shared by the leaderboard and closest-lava overlays.", new AcceptableValueRange<int>(0, 255)));

            LeaderboardEnabled = config.Bind("Leaderboard", "Enabled", false, "Show the player leaderboard.");
            LeaderboardScale = Range(config, "Leaderboard", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            LeaderboardPlatformIcons = config.Bind("Leaderboard", "Platform Icons", true, "Show each player's Steam or Meta icon.");
            LeaderboardFps = config.Bind("Leaderboard", "Show FPS", false, "Show each player's frame rate next to their name.");
            LeaderboardOffsetX = Range(config, "Leaderboard", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            LeaderboardOffsetY = Range(config, "Leaderboard", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");
            LeaderboardPosition = config.Bind("Leaderboard", "Position", HudPosition.BottomLeft, "Corner the leaderboard sits in: BottomLeft, BottomRight or TopRight. Only one overlay can use a corner.");

            ClosestLavaEnabled = config.Bind("ClosestLava", "Enabled", false, "Show the bottom-center distance to the nearest tagged (lava) player from the spectated player.");
            ClosestLavaScale = Range(config, "ClosestLava", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            ClosestLavaOffsetX = Range(config, "ClosestLava", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            ClosestLavaOffsetY = Range(config, "ClosestLava", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");

            ScoreboardEnabled = config.Bind("Scoreboard", "Enabled", false, "Show the two-team scoreboard with a timer.");
            ScoreboardScale = Range(config, "Scoreboard", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            ScoreboardOffsetX = Range(config, "Scoreboard", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            ScoreboardOffsetY = Range(config, "Scoreboard", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");
            ScoreboardPosition = config.Bind("Scoreboard", "Position", HudPosition.TopCenter, "Where the scoreboard sits: TopCenter, BottomLeft, BottomRight or TopRight. Only one overlay can use a corner.");
            ScoreboardTeamAName = config.Bind("Scoreboard", "Team A Name", "TEAM A", "Left team name.");
            ScoreboardTeamBName = config.Bind("Scoreboard", "Team B Name", "TEAM B", "Right team name.");
            ScoreboardTeamAScore = config.Bind("Scoreboard", "Team A Score", 0, new ConfigDescription("Left team score.", new AcceptableValueRange<int>(0, 999)));
            ScoreboardTeamBScore = config.Bind("Scoreboard", "Team B Score", 0, new ConfigDescription("Right team score.", new AcceptableValueRange<int>(0, 999)));
            ScoreboardStartKey = config.Bind("Scoreboard", "Start Key", Key.B, "Start the match timer.");
            ScoreboardPauseKey = config.Bind("Scoreboard", "Pause Key", Key.N, "Pause the match timer.");
            ScoreboardResetKey = config.Bind("Scoreboard", "Reset Key", Key.M, "Reset the match timer to the pre-roll.");
            ScoreboardTeamAUpKey = config.Bind("Scoreboard", "Team A Score Up Key", Key.H, "Add a point to team A.");
            ScoreboardTeamADownKey = config.Bind("Scoreboard", "Team A Score Down Key", Key.J, "Remove a point from team A.");
            ScoreboardTeamBUpKey = config.Bind("Scoreboard", "Team B Score Up Key", Key.K, "Add a point to team B.");
            ScoreboardTeamBDownKey = config.Bind("Scoreboard", "Team B Score Down Key", Key.L, "Remove a point from team B.");

            KillFeedEnabled = config.Bind("KillFeed", "Enabled", false, "Show the feed of players as they get tagged.");
            KillFeedScale = Range(config, "KillFeed", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            KillFeedOffsetX = Range(config, "KillFeed", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            KillFeedOffsetY = Range(config, "KillFeed", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");
            KillFeedPosition = config.Bind("KillFeed", "Position", HudPosition.TopRight, "Corner the kill feed sits in: BottomLeft, BottomRight or TopRight. Only one overlay can use a corner.");

            TagManagerEnabled = config.Bind("Tag Manager", "Enabled", false, "Take full control of the room's tag game while you are master.");
            TagManagerRefereeOnly = config.Bind("Tag Manager", "Only Me Tagged", false, "Keep yourself as the only tagged player. Nobody else can be tagged until this is off.");
            TagManagerPlayerTags = config.Bind("Tag Manager", "Player Tags", false, "Let players tag each other normally. Off means only this panel changes who is tagged.");
            TagManagerLocalFirst = config.Bind("Tag Manager", "Me First Each Round", false, "With Take Control on, keep the normal game's rounds running but have it pick you instead of a random player whenever it starts a round or needs a new tagged player. Only Me Tagged and Player Tags are off while this is on.");

            PlayerName = config.Bind("Player", "Name", "ZETORI", "Up to 12 letters or digits.");
            PlayerRed = Range(config, "Player", "Red", 0f, 0f, 1f);
            PlayerGreen = Range(config, "Player", "Green", 0f, 0f, 1f);
            PlayerBlue = Range(config, "Player", "Blue", 0f, 0f, 1f);

            DisableVSync = config.Bind("Game", "Disable VSync", true, "Turn off VSync so the spectator view runs at the highest frame rate.");
            UncapFps = config.Bind("Game", "Uncap FPS", false, "Remove the game's frame rate cap so the spectator view renders as fast as possible.");
            DisableAfkKick = config.Bind("Game", "Disable AFK Kick", true, "Stop the game from kicking you for being idle while spectating.");

            Theme = config.Bind("Interface", "Theme Name", "Red", "Name of a theme from the Interface panel's list.");
            UiScale = Range(config, "Interface", "UI Scale", MinimumUiScale, MinimumUiScale, MaximumUiScale, "Multiplier on top of automatic resolution scaling.");
            SnapPanels = config.Bind("Interface", "Snap Panels", true, "Stack panels into columns from the top of the screen. When off, panels stay where they are dropped.");
            ShowTips = config.Bind("Interface", "Show Tips", true, "Show a line at the top of each in-game panel page saying what it does, and remind you in the headset how to open the in-game panel until you have opened it.");
            MenuKey = config.Bind("Interface", "Menu Key", Key.Escape, "Key that shows and hides the panels.");
            AutoSave = config.Bind("Interface", "Autosave", true, "Save setting changes automatically. When off, save with the Presets panel's Save Settings button or by saving a preset.");

            InGamePanelSize = Range(config, "In-Game Panel", "Size", 1f, MinimumInGamePanelSize, MaximumInGamePanelSize, "Size multiplier on the in-game panel opened by double pressing left primary in VR.");
            InGamePanelHaptics = config.Bind("In-Game Panel", "Haptics", true, "Vibrate the controller when a finger presses the in-game panel.");
            InGamePanelOnStream = config.Bind("In-Game Panel", "Show On Stream", false, "Let the Spectator, Freecam and Web views see the in-game panel. The VR camera view always shows it.");

            IgnoredUpdate = config.Bind("Updates", "Ignored Version", string.Empty, "Release the update popup no longer offers.");

            BindPanelPositions(config);

            try { ConfigCleanup.RemoveUnusedSettings(BoundKeys(config)); }
            catch (Exception e) { Log.Exception(e, "Removing unused settings failed"); }

            try { Write(config); }
            catch (Exception e) { Log.Exception(e, "Saving the config failed"); }

            config.SettingChanged += (_, _) =>
            {
                dirty = true;
                lastChange = Time.unscaledTime;
            };
        }

        private static ConfigEntry<float> Range(ConfigFile config, string section, string key, float value, float min, float max, string description = "") =>
            config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));

        private static void BindPanelPositions(ConfigFile config)
        {
            for (int i = 0; i < panelPositions.Length; i++)
                panelPositions[i] = config.Bind("Layout", $"{PanelCatalog.PositionKey((PanelId)i)} Position", string.Empty, "Saved x,y at 1080p. Empty uses the default layout.");
        }

        internal static ConfigEntry<string> PanelPosition(PanelId id) => panelPositions[(int)id];

        private static HashSet<(string Section, string Key)> BoundKeys(ConfigFile config)
        {
            var keys = new HashSet<(string Section, string Key)>();
            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> entry in config) keys.Add((entry.Key.Section, entry.Key.Key));
            return keys;
        }

        internal static string? ConfigFilePath => file?.ConfigFilePath;

        internal static void Tick()
        {
            if (AutoSave.Value && dirty && Time.unscaledTime - lastChange >= SaveDelay) Save();
        }

        internal static void SaveOnExit()
        {
            if (AutoSave.Value) Save();
        }

        internal static void Save()
        {
            if (dirty) ForceSave();
        }

        internal static void ForceSave()
        {
            if (file == null) return;
            dirty = false;
            try { Write(file); }
            catch (Exception e) { Log.Exception(e, "Saving the config failed"); }
        }

        private static void Write(ConfigFile config)
        {
            config.Save();
            string path = config.ConfigFilePath;
            File.WriteAllText(path, ConfigFormat.KeepKeys(File.ReadAllText(path), BoundKeys(config)), new UTF8Encoding(false));
        }

        internal static void Reload()
        {
            if (file == null) return;
            try
            {
                HashSet<(string Section, string Key)> stored = ConfigFormat.StoredKeys(File.ReadAllText(file.ConfigFilePath));
                string ignoredUpdate = IgnoredUpdate.Value;
                file.Reload();
                ResetUnstored(file, stored);
                IgnoredUpdate.Value = ignoredUpdate;
            }
            catch (Exception e) { Log.Exception(e, "Reloading the config failed"); }
        }

        private static void ResetUnstored(ConfigFile config, HashSet<(string Section, string Key)> stored)
        {
            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> entry in config)
                if (!stored.Contains((entry.Key.Section, entry.Key.Key))) entry.Value.BoxedValue = entry.Value.DefaultValue;
        }

        internal static bool KeyPressed(ConfigEntry<Key> entry) => KeyPressed(entry.Value);

        internal static bool KeyPressed(Key key)
        {
            Keyboard? keyboard = Keyboard.current;
            if (key == Key.None || keyboard == null || KeyBinds.Capturing) return false;
            try { return keyboard[key].wasPressedThisFrame; }
            catch (ArgumentOutOfRangeException) { return false; }
        }

        internal static bool KeyHeld(ConfigEntry<Key> entry) => KeyHeld(entry.Value);

        internal static bool KeyHeld(Key key)
        {
            Keyboard? keyboard = Keyboard.current;
            if (key == Key.None || keyboard == null || KeyBinds.Capturing) return false;
            try { return keyboard[key].isPressed; }
            catch (ArgumentOutOfRangeException) { return false; }
        }
    }
}
