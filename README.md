# Zetori

Zetori is a free, open-source spectating mod for Gorilla Tag, built for casters. It turns the desktop view into a broadcast camera with first-person and orbit spectating, a freecam, nametags, a minimap, a player leaderboard, a closest-lava readout, a two-team scoreboard with a match timer, and Discord Rich Presence showing your map, camera mode, game mode, public or private lobby and player count (never the room code), with a Get Zetori button linking to [zetori.dev](https://zetori.dev). Every feature is configured from in-game panels and saved between sessions, and whole setups can be stored as named presets.

Zetori is a fan-made mod. It is not affiliated with or endorsed by Another Axiom.

## Install

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64) into your Gorilla Tag folder and start the game once so BepInEx creates its folders.
2. Download `Zetori-<version>.dll` from the [releases page](https://github.com/g3nn3sis/Zetori/releases).
3. Install it into the BepInEx plugins folder, it should end up at `Gorilla Tag\BepInEx\plugins\Zetori-<version>.dll`.
4. Start the game on PC. The Zetori panel opens in the top-left corner.

Each release is built against the Gorilla Tag version that was current when it was published. If a game update breaks the mod, check for a newer release.

## Features

| Panel | What it configures |
| --- | --- |
| Zetori | Join a room by code, join a public room, leave the room, see your connection status, and reset the panel layout. **Rejoin** goes back to the last room you were in (remembered between sessions), **Save This Room** (or **Save Code** for a typed code) keeps up to 20 codes in a list you tap to join, and `x` removes one |
| Recent Players | The last 50 players you shared a room with, newest first, with when and in which room you last saw them (public rooms are marked). Tap a player who is in your room to spectate them; **Join** goes to the room they were last seen in; **Clear List** empties it |
| WASD | Fly your own player with WASD, mouse click, movement speed, mouse sensitivity |
| Freecam | Movement speed and rotation sensitivity of the Freecam camera (also used while editing a camera web), and Teleport Here, which moves your own player to where the Freecam camera is. Teleport works in Freecam mode in any room, but not in replay mode |
| Replay | Record the match — every player's body and hand movement, the cosmetics they wear and hold (with their moving parts, particle and light effects, colour changes and sounds) and their skins, time of day and rain, joins, leaves and tags, and other players' voice chat — for up to 60 minutes; stopping saves it to a file, and saved replays are listed to play or delete. In replay mode every panel stays available so you can change camera, nametag, theme and other casting settings while you watch; the panels move down under a timeline bar across the top, with the Replay panel first in its column. Drag the bar by its clock or edges to move it; on release it snaps to whichever of the top or bottom edge is closer, and the panels only move down while it sits at the top. The bar has play/pause, drag to scrub, event markers (tags orange, joins green, leaves grey, time/weather blue) with hover details, and the spectated player's voice activity under the track. Spectate any recorded player (orbit or first person), fly a FreeCam, or watch through the Web camera while they move. Recording never changes what you see live: alongside each player's body it stores their raw networked position, head and hands (the highest rig lerp), so you can keep adjusting Rig Interpolation while casting. In replay mode the Replay panel's Rig Lerp slider (0.01 to 1, starting at your live interpolation) re-applies the lerp locally to those raw targets, where 1 is raw and lower values are smoother, and the head and arms are re-posed to follow them. The Leaderboard, Closest Lava and Kill Feed overlays and the Scoreboard keep working in replay mode, showing the recorded players, distances and tags at the current point in the replay (the MiniMap is hidden). Recorded players get the same nametags as live casting (font, player or tag color, FPS and platform icon as they were at that moment); Stop leaves replay mode and restores the live game. **Add Marker** (or `F7`) while recording drops a numbered yellow marker on the timeline. The **0.25x**, **0.5x**, **1x** and **2x** buttons, or the speed button on the timeline that cycles through them, change the playback speed, with voices and cosmetic sounds pitched to match; **Frame Back** and **Frame Forward** (or `,` and `.`) pause the replay and step one recorded frame (1/20 of a second). **Highlights** are found when a replay loads, so they work on replays saved before this version too, and show as pink markers on the timeline and as a list in the Replay panel: close escapes (a runner comes within 1 metre of a tagger and gets 3 metres away without being tagged), long runs (a runner stays untagged for 45 seconds or more while at least half the players are tagged), tag streaks (one tagger makes 3 or more tags with no more than 10 seconds between them, where the tagger is taken to be the nearest tagged player), and cheat signals (speed, teleports, hovering, arm reach or tag reach first raised on a player during the recording; these are only in replays recorded with this version). Click a highlight, or **Previous Highlight** and **Next Highlight**, to jump to 3 seconds before it and spectate that player. For cinematic shots, put the camera where you want it (with FreeCam, the spectator camera or the web), then **Add Keyframe** stores its position, direction and field of view at the current replay time; keyframes show as purple squares under the timeline and in a list where clicking one jumps to it and **x** removes it. The **Path** camera flies smoothly through the keyframes as the replay plays, easing in from the first and out to the last. **Render Frames** saves the replay as a numbered PNG image sequence plus a WAV of the recorded voices and cosmetic sounds (no hand taps or game sounds), ready for a video editor such as DaVinci Resolve or Premiere. Pick 1080p, 1440p or 4K and 30 or 60 fps; **Set Start** and **Set End** limit it to part of the replay, shown as a light band on the timeline, and **Whole Replay** resets that. Every frame is rendered from the current camera (spectator, FreeCam, web or path) at an exact step of replay time, however slowly your PC renders, so the result plays back smoothly. While it renders, the game's clock is slowed to match, so in a room your own player moves slowly until the render finishes or you cancel it |
| Capture | **Instant Replay** keeps the last moments of play in memory while you are not recording (off by default, since it records everyone all the time), and **Clip Last 30 s** (or `F8`) saves them as a replay named `Clip` and the date, which plays like any other replay. **Clip Length** sets how much a clip keeps, from 10 to 120 seconds; while a recording runs, Clip saves its last moments without stopping it. **Record**, **Stop Recording** and **Add Marker** work as in the Replay panel, and markers dropped while Instant Replay runs land in clips. **Take Photo** (or `F9`) saves a 3840x2160 PNG of what the camera sees, without the panels, overlays or the in-game panel, after the **Photo Timer** (0 to 10 seconds, 3 by default; the left controller ticks each second in VR). The background buttons pick what the photo shows: **Scene** is everything the camera sees, **Cutout** is only the spectated player on a transparent background (worked out from one shot over black and one over white, so soft edges keep their see-through parts), and **Green** and **Blue** put only that player on a solid green-screen or blue-screen colour for chroma keying. With nobody spectated, the photo falls back to the whole scene. **Thirds Grid** draws rule-of-thirds lines over the game window to help frame a shot; they are never in the photo |
| Camera | Camera mode (Default, Spectator, Freecam, Web, Camera), field of view, near clip, first person or orbit distance, orbit speed, position and rotation smoothing, auto orbit, and one-tap FOV and view presets |
| Camera Web | A web of fixed camera positions (nodes) for Web camera mode: the camera sits on a node and turns to watch the best action near it — a chase first, then taggers, then lone runners — without locking onto one player. **Selection** is Dynamic (the node is picked from how close players are to it, whether a runner is being chased, and who is a tagger) or Manual (step through nodes with the arrow keys or the Previous/Next buttons). **Edit Nodes** flies the camera like Freecam and shows every node as a numbered marker, with the one nearest the camera in white; add a node where the camera is, remove the nearest, or clear them all. Save the web under a name, and load or delete saved webs from the list. Web mode also works in replay mode, where it watches the recorded players, and nodes can be edited there too |
| MiniMap | Top-down minimap in the top-right (default), bottom-left or bottom-right corner; corner position, size, camera height, dragging, position reset |
| NameTags | Nametags, size (0.5x to 2x), font, player color, FPS, platform icon (Steam or Meta) left of the name, and a Zetori icon right of the name for other players running Zetori (live rooms only) |
| Leaderboard | Stack of rounded pills in the bottom-left (default), bottom-right or top-right corner, each showing the keyboard number that spectates that player, their name, an optional FPS readout and their Steam/Meta icon; tagged names are orange, others white, and the spectated player is bold. Enable, position, scale, position offset, platform icons, FPS, font |
| Scoreboard | Two-team scoreboard with a central match timer, all under the same theme, shown top-center (default) or in the bottom-left, bottom-right or top-right corner. You set the team names and scores from its panel; the timer runs a 10 second pre-roll (orange, shown negative) then counts up in white and stops at 3:00, with start/pause and reset. Enable, position, scale, position offset, font |
| Closest Lava | Bottom-center rounded box with the distance from the spectated player to the nearest tagged (lava) player; 0 when spectating a tagged player. Enable, scale, position offset, font |
| Kill Feed | Stack of rounded pills in the top-right (default), bottom-left or bottom-right corner that announces each player the moment they are tagged — their name in orange next to a `TAGGED` label — with the newest nearest the screen edge, up to five at once, each fading in and out after a few seconds. Enable, position, scale, position offset, font |
| Time | Morning, day, evening, night, rain |
| Maps | Load any map |
| Session Stats | Since the game started: time in rooms, rooms joined, players met, tags you made, times you were tagged, your current and longest run (time spent untagged while someone is tagged), distance moved and top speed. A tag counts when the room's master confirms you made it. **Reset** starts the counts again |
| Tag Manager | While you are master of a private (join by code) standard infection tag room, take control of the room's tag game. In public rooms it does nothing and the game runs normally. The room follows the game's own rule: with fewer than 4 players it is tag, where one player is tagged at a time (the rock look), and with 4 or more it is infection, where several players can be tagged at once (the red lava look); when the room crosses 4 players the tagged player carries over. **Take Control** turns the Tag Manager on, with these options under it. **Me First Each Round** keeps the normal game running but makes you the first tagged player whenever it would pick one at random: at the start of every infection round, when the room switches between tag and infection, and when the tagged player leaves (only while you are taking part in the game). With Me First Each Round off, the game stops changing who is tagged on its own: whoever is tagged when you take control stays tagged, nobody else is tagged until you or the players do, an infection round never ends by itself, and players who leave are untagged. **Only Me Tagged** keeps you as the only tagged player; **Player Tags** lets players tag each other normally (off by default, so only the panel changes who is tagged); both are off while Me First Each Round is on. **Tag Me** tags you, **Clear Tags** untags everyone, and the player list shows everyone as Tagged or Runner, where clicking a player tags or untags them; in infection several players can be tagged from the list. Turning Take Control off hands the room back to the normal game. To stay well inside the game's own network limits, the Tag Manager sends at most one tag change every quarter second, so the tag buttons grey out briefly after each tag. Not available in replay mode |
| Player | Your name and color, with a preview, and your microphone mode: all chat, or push to talk with the Hold To Talk key |
| Settings | Audio from the spectator camera, player list updates, Discord presence, **Hide Room Codes**, VSync, AFK kick, rig interpolation, and Rig Exaggeration, which multiplies the spectated player's arm movements from 1x to 3x on your screen only, live and in replays (paused while recording or keeping an instant replay). Hide Room Codes shows `Hidden` wherever Zetori would show a room code (panels, the in-game panel and its footer, recent players and saved rooms) and stars in the room code field, so codes stay off stream |
| Keybinds | Every key the mod uses: click a row, then press the new key (click the row again to cancel); **Reset Keys** restores the defaults. The number keys that pick a player are fixed |
| Theme | Accent color of every panel and the minimap |
| Presets | Save the current settings as named presets and load or delete them later; choose which preset loads at startup on desktop and in VR; toggle autosave and save settings on demand. Settings left over from removed features are deleted from the Default config and every preset at startup |

The Leaderboard, Closest Lava, Scoreboard and Kill Feed overlays are Unity canvases loaded from an asset bundle embedded in the DLL, so they render at the same size on any resolution. They all share one font, picked from the game's fonts with the Previous/Next Font buttons in any of their panels. The Leaderboard, Scoreboard, Kill Feed and MiniMap each have a **Position** button that steps through their spots. Only one of them can use a corner at a time: the button skips corners that are in use, and turning one on in a corner that is already used moves it to a free corner. If all three corners are needed, the Scoreboard steps back to top-center.

Every panel is a thin title bar with an arrow. Click the bar or its arrow to slide the panel open or closed. The bars stack themselves into columns grouped by purpose and slide to make room as panels open. Drag a bar to move it: the other panels slide apart as you go, and it settles into the column and place where you let go, so panels never overlap. Turn **Panel Snapping** off in the UI Control panel to leave panels exactly where you drop them instead, where they are free to overlap. The arrangement is saved, and the **Reset Layout** button in the Zetori panel puts every panel back. The interface scales with your resolution, and the **UI Scale** buttons in the UI Control panel add your own multiplier on top, from 0.75x to 1x in 0.05 steps.

## VR camera

When a headset is active, the panels start closed (`Esc` still opens them) and Zetori switches to **Camera** mode, which gives you a camera tablet you can hold in the game. It rises in front of you shortly after the game opens, and again whenever you switch back to Camera mode. Switch to Default, Spectator or Web from the Camera panel and the tablet disappears, so you can keep casting other players from the keyboard while you play. Camera mode is only available with a headset, and Freecam and WASD only without one; when a VR Default preset is set, it decides the starting mode instead. Its accents and lit buttons follow the panel theme. Other players cannot see it. The desktop window shows what the camera sees, including the tablet when it is in view.

| VR input | Action |
| --- | --- |
| Double-press the right primary button (`A`) | Raise the tablet out of the ground with a fade in, or fade it away if it is out; from Follower, raise it and return to Handheld |
| Grip either side bar | Hold it; let go and it stays in the air |
| Touch `FIRST`, `THIRD`, `FOLLOW` or `HAND` beside the screen | Choose First Person, Third Person, Follower or Handheld; the tablet disappears while following you |
| Touch `FOV -` or `FOV +` under the screen | Change the field of view in steps of 5, from 30 to 130 |
| Touch the flip button under the screen (Handheld) | Switch between the rear view and the view from the screen side |
| Touch `COSMETICS` right of the flip button (First Person) | Show or hide your own face and cosmetics in the first-person view; the button is lit while they are shown |

## In-game panel

With a headset active, double-press the left primary button (`X`) and a panel pops up in front of you that you work with your fingers. Its home page shows the Zetori watermark in the theme colour, two rows of live cards — **Room** (the room code), **Players** (the player count), **VR Camera** (the shot, and whether it is live), **Capture** (recording time, photo countdown or instant replay), **Session Stats** (your tags and best run) and **Host** (the room type and whether you host it) — and a grid of icon tiles that opens every other desktop panel (camera, overlays, scoreboard, tag manager, recent players, player, settings, themes, presets, replays and the rest). These pages are made for playing:

- **Players**: everyone in the room with their FPS, platform and tag state. Toggle **Mute** or press **Report** for the game's Hate Speech, Cheating and Toxicity reports (the same mute and report as the game's scoreboard). Tap a name to open the player: a live view of their rig with its colour and cosmetics on the left, and their name, platform, FPS (with their refresh rate, or "uncapped" for a desktop player with no frame cap), ping, colour code, tag state, speed, distance, voice, time in room, Zetori and host status on the right, with **cheat and mod signals** under that info, then Spectate, **Film**, Mute and Report and a **Volume** slider (0 to 200%) that changes how loud they are for you only, kept until the game closes. Recordings and clips hear them at the volume you set.

  The signals are hints for deciding whether to report someone, not proof. Zetori watches every other player from the moment they are in your room (the heading says how long), raises a signal in orange when it trips, and shows the count next to their name in the Players list. The heading also says when the room is a modded lobby, where mods are allowed.

  | Signal | Raised when |
  | --- | --- |
  | Mod data | Their player data holds entries the game never sets itself, which mods add (Zetori's own entry is left out; it shows as Zetori above) |
  | Name | Their name has symbols or is over 12 letters, which the game's keyboard cannot type |
  | Quest tweaks | Their Quest headset reports headset settings changed with developer tools (such as SideQuest or adb): CPU level, GPU level, refresh rate, half refresh rate, swap interval, prediction time, headlock and its X/Y/Z translation, and phase sync with its padding, delay and prediction. The game sends only whether each one is changed, not its value, and only Quest headsets report them |
  | Cosmetics | They have cosmetics equipped that they do not own, in two checks two seconds apart; the game hides those items, so you do not see them |
  | Speed | They stay above 14 m/s for 1.5 seconds |
  | Teleports | Their position jumps over 6 m faster than 60 m/s three times; respawns and the game's own teleporters count too |
  | Hovering | They stay over 2.5 m above the ground for 3 seconds without falling and without touching anything; ropes, gliders, zip lines and deep water can trip it |
  | Arm reach | A hand stays more than 1.3 m from their head for a second in total; the game itself stops arms at 1 m |
  | Tag reach | Two of their tags land 4 m or more from the tagged player, measured when the room's master announces the tag; lag adds distance |

  Speeds and distances are scaled to the player's size.
- **VR Camera**: stream from the camera tablet, pick its shot, show your cosmetics in first person and set the field of view. **Film Me** (or **Film** on a player's page) makes a Hand shot tablet you have let go of turn to keep that player in frame, so it works as a tripod camera operator; **Stop Filming** ends it. **Save Spot** remembers where the tablet is, and tapping a saved spot puts the tablet back there in the Hand shot (`x` deletes it).
- **Capture** and **Session Stats**: the same as the desktop panels.
- **Host**: for running your own matches. Shows whether the room is private and who hosts it, turns the scoreboard on, changes the team scores and starts, pauses or resets the match timer, and has the full Tag Manager below.
- **Panel**: size, haptics, **Show On Stream** and **Hide Room Codes**, plus **Bring Here** and **Close**. The VR camera's view always shows the panel; **Show On Stream** also shows it in Spectator, Freecam and Web (off by default).

The panel stays where you open or leave it; double-press again or use **Bring Here** to bring it back in front of you. The footer shows the room, player count, ping, frame rate and time. Other players cannot see the panel.

While a replay plays, your own body is hidden so it does not overlap the recorded one. In VR your hands show as glowing markers (palm, fingertip and the line between them) in the theme colour so you can still aim at the panel, and the panel header gets an **Exit Replay** button. Only your headset sees the markers.

| VR input | Action |
| --- | --- |
| Double-press the left primary button (`X`) | Open the panel in front of you, or close it |
| Touch a button | Press it; hold it to do its desktop right-click action, such as renaming a preset or replay |
| Touch and slide along a slider | Set its value; the `-` and `+` beside it step in small increments |
| Touch a text field | Type on the keyboard that appears below the panel, in front of it and tilted up toward you; `Done` closes it |
| Touch and slide the bar on the right | Scroll a page that does not fit |
| Grip either side handle | Move the panel; let go and it stays there |

## Controls

| Input | Action |
| --- | --- |
| `Esc` | Show or hide the panels (configurable) |
| `0` | Spectate the local player (yourself); in replay mode, your recorded player |
| `1` to `9` | Spectate the first nine other players; in replay mode, the recorded players with those leaderboard numbers |
| `]` and `[` | Spectate the next or previous player (configurable); in replay mode, the next or previous recorded player |
| `P` | Pause and unpause the replay in replay mode (configurable) |
| `,` and `.` | Pause the replay and step back or forward one recorded frame in replay mode (configurable) |
| `F1` | Show or hide the replay timeline in replay mode; `Esc` hides only the panels (configurable) |
| `F7` | Drop a marker while recording or keeping an instant replay (configurable) |
| `F8` | Save a clip of the last moments (configurable) |
| `F9` | Take a photo after the Photo Timer (configurable) |
| `V` (held) | Hold to talk in all chat; release returns to the Microphone setting (configurable) |
| `B` `N` `M` | Scoreboard timer: start, pause, reset (configurable) |
| `H` `J` / `K` `L` | Scoreboard: team A score up/down, team B score up/down (configurable) |
| Arrow keys | Orbit the spectated player when Head Distance is above 0 |
| `←` and `→` | Web camera mode with Manual selection: previous or next node (configurable) |
| `F2` and `F3` | While editing a camera web: add a node where the camera is, remove the nearest node (configurable) |
| Hold right mouse | Fly the camera in Freecam mode or while editing a camera web, or your player when WASD is on (WASD turns off in VR, in Freecam mode and while editing a web) |
| `W` `A` `S` `D` | Move while flying |
| `Space` and `Left Shift` | Move up and down while flying |
| `E` (held) | Noclip while flying |
| Left click | Press what is under the cursor with your right hand (Mouse Click must be on) |

## Configuration

Settings are saved to `Gorilla Tag\Zetori\Configs\Default.cfg` a second after you change them, and when the game closes. Turn **Autosave** off in the Presets panel to stop that; settings are then written only when you press **Save Settings** or save a preset. The file holds only section headers and `Key = value` lines, with no comments. You can edit it while the game is closed; a value outside a setting's allowed range is clamped when the game loads it.

Presets are stored as `.cfg` files next to `Default.cfg` in `Gorilla Tag\Zetori\Configs` (`Default` is reserved for the live settings). Loading one applies it immediately, including the panel theme and layout. **Desktop Default** and **VR Default** in the Presets panel pick the preset loaded when the game starts in that mode: click to step forward through the presets and None, right-click to step back. The choices are kept in `Gorilla Tag\Zetori\PresetDefaults.txt`, so loading a preset never changes them. On first launch, an existing config and presets from `BepInEx\config` are moved there automatically.

Replays are saved as compressed `.gcreplay` files in `Gorilla Tag\Zetori\Replays`, named by the date and time recording stopped. The Replay panel lists them to play or delete. A replay's camera path is saved next to it as a `.gcpath` file and is renamed and deleted with it.

Clips are saved next to the replays, named `Clip` and the date and time. Photos are saved as `.png` files in `Gorilla Tag\Zetori\Photos`, named by the date and time they were taken. Rendered replays are saved in `Gorilla Tag\Zetori\Renders`, one folder per render named after the replay and the time, holding `frame_000000.png` onward and `audio.wav`.

Saved VR camera spots are kept in `Gorilla Tag\Zetori\Spots.txt` (one spot per line: its name, position and rotation), saved room codes in `SavedRooms.txt`, the last room in `LastRoom.txt` and recent players in `RecentPlayers.txt`, all in `Gorilla Tag\Zetori`. They stay on your computer and are never sent anywhere.

Camera webs are saved as `.gcweb` text files in `Gorilla Tag\Zetori\Webs`: a header line, then one node per line as `x y z`. The Camera Web panel lists them to load or delete, and the web in use is remembered in the settings (and in presets).

## Troubleshooting

- **No panel appears.** Open `BepInEx\LogOutput.log` and search for `Zetori`. If it is missing, BepInEx is not installed correctly or the DLL is not under `BepInEx\plugins`.
- **The camera does not move.** The mod takes over the game's desktop shoulder camera. The log says `The game's shoulder camera was not found` when a game update has moved it.
- **No Discord presence.** Discord must be running before the game starts, and Rich Presence must be on in Player Settings.
- **Something else.** Open an [issue](https://github.com/g3nn3sis/Zetori/issues/new/choose) and attach `LogOutput.log`.

## Build from source

You need Windows, a local Gorilla Tag install with BepInEx 5, and the .NET SDK (any version that can build `net48`).

The project compiles against the assemblies in your own game install. The game path is set in `Directory.Build.props` and defaults to the standard Steam location; edit it there if your game is elsewhere.

```powershell
dotnet build .\Zetori.csproj -c Release --nologo
```

Every build drops `Zetori-<version>.dll` straight into `Gorilla Tag\BepInEx\plugins\Zetori\`. Game and BepInEx assemblies are never copied into the output.

The version lives in `Directory.Build.props` and `Base/PluginInfo.cs`; the build stops with an error if they differ. See [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## License

Zetori is released under the [Zetori Source-Available License](LICENSE): the source is public to view, but it may not be used, run, modified, or redistributed without permission. This is **not** an OSI-approved open-source license. The bundled Discord RPC source carries its own [MIT license](External/DiscordRPC/LICENSE); see [third-party notices](THIRD_PARTY_NOTICES.md).
