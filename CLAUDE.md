# Working in Zetori

Instructions for an AI agent (or any new contributor) working in this repository. Read this
before changing code.

## What this is

Zetori is a **BepInEx plugin for Gorilla Tag** (a Unity VR game) that turns the game into
a spectator/casting tool: free/orbit camera, nametags, minimap, leaderboard, scoreboard,
closest-lava readout, Discord Rich Presence, and an in-game IMGUI control panel. Target
framework is **net48**; it builds to a single `Zetori-<version>.dll`.

## The one rule that shapes everything

**The game cannot be launched from here.** The compiler is the entire safety net. Therefore:

- After **every** code change, run the Release build and keep it green.
- The build is configured **warnings-as-errors** — a new warning fails the build. Keep it at
  zero warnings.
- Behaviour you cannot verify in-game must be preserved exactly. Prefer compiler-checkable,
  behaviour-preserving changes over speculative rewrites.

## Build

```bash
dotnet build Zetori.csproj -c Release
```

A successful plugin build **copies `Zetori-<version>.dll` into the game's
`BepInEx\plugins\Zetori\`** folder automatically (see `Directory.Build.targets`). Paths
to the game and BepInEx are in `Directory.Build.props` (`GamePath`).

## Git

Commit directly on `master`. Do not create a local branch for each feature and merge it back
into `master`.

## Layout

Every feature has its own folder. A feature's folder sits at the root, or inside the area it
belongs to (`Replay/`, `GameCamera/`, `Player/`, `UI/`).

```
Base/                  Plugin bootstrap: BaseLoader, ObjectComponent, ObjectPatcher, ComponentOrder, HarmonyPatches, Log, PluginInfo, DataFolders, EmbeddedResources
Base/Config/           CasterConfig, presets, key binds, the config file format and cleanup
Shared/                Helpers no single feature owns: BinaryIO, DisplayNames, Smoothing
Shared/Assets/         Embedded asset loading: AssetManager (icons, fonts), .zmdl models
Shared/Vr/             VR controller input: VrHand, HandGrip (grab and carry), DoublePressDetector
Player/                Local and networked players: rig list, local rig, platform, tag state, PlayerKey, Zetori users
Player/Movement/       WASD flight, Teleport Here
Player/Exaggeration/   Rig Exaggeration and its patch
Player/Voice/          Microphone device, per-player volume
Player/Moderation/     Mute and report through the game's scoreboard
History/               Recent players, saved rooms, session stats
Cheats/                Cheat alerts: on-screen feed, headset toast
Cheats/Signals/        Cheat and mod signal detection, flagged players, the patches that feed them
Hud/                   Canvas HUD framework: HudOverlay, slots, positions, HUD asset bundle
Hud/Overlays/          Leaderboard, Scoreboard, Closest Lava, Kill Feed, MiniMap
NameTags/              Nametags
TagManager/            Tag Manager and its patches
World/                 Time of day and weather
GameCamera/            Spectator camera: CameraManager, CameraWrapper, CameraMode
GameCamera/Modes/      One class per camera mode
GameCamera/Photo/      Photos: capture, cutout and chroma-key backgrounds, player isolation, thirds grid
GameCamera/Vr/         VR camera tablet
GameCamera/Webs/       Camera webs: nodes, director, file format, on-screen markers
Replay/                ReplaySession and the recording model the recorder and player share
Replay/Recording/      Recorder and the hand-tap patch
Replay/Playback/       Player, puppets, live-world mask, replay scoreboard and its patches
Replay/Cosmetics/      Cosmetic capture, library, and puppet cosmetics and skins
Replay/Voice/          Voice capture and voice tracks
Replay/Storage/        Replay file format and packed values
Replay/Render/         Video renders and the bundled FFmpeg
Replay/Highlights/     Highlight detection
Replay/CameraPaths/    Keyframed camera paths
Replay/Timeline/       Replay timeline bar
UI/                    IMGUI panel system: CoreUI, PanelOrchestrator, PanelLayout, PanelCatalog, Panel, theme, scaling, DisplayFormat
UI/Panels/             What each panel draws, one class per column group (Main, View, Overlays, World, Setup, Replay)
UI/InGame/             In-game VR panel: world-space surface, layout, finger input, keyboard, pages, model
UI/Icons/              Panel icons: SVG reader, rasterizer (desktop textures), tessellator (in-game meshes)
DiscordRichPresence/   Mod-side Discord integration
External/              Vendored third-party source (DiscordRPC) — do not modify
Resources/             Embedded icons (panel icons are SVG in Icons/Panels/), HUD asset bundle, FFmpeg binary for replay renders
Tools/                 Build scripts for bundled binaries (FFmpeg/build.sh, run in MSYS2 MINGW64)
```

Namespaces mirror folders: `Zetori.<Folder>.<Sub>` (e.g. `Zetori.Hud.Overlays`, `Zetori.UI.Panels`).
There is no `Classes.` segment. One top-level type per file, named after the type.

### Where code goes

- **A feature's code lives in the feature's folder**: its components, data, file format, Harmony
  patches and whatever it draws on screen. There is no shared `Patches/` folder; a patch sits
  beside the feature it serves.
- **A new feature gets a new folder** (a sub-folder when it belongs to an area such as `Replay/`
  or `GameCamera/`). Name the folder after the feature, never after a type inside it: a
  `Leaderboard/` folder makes the namespace `Leaderboard` shadow the `Leaderboard` class.
- **Code several features of one area share goes at that area's root**: `Replay/` holds the
  recording model the recorder and player both use; `UI/` holds `UiScale`, `Textures` and
  `DisplayFormat` for the desktop and in-game panels.
- **Helpers that several areas use and no single feature owns go in `Shared/`.** Before writing a
  helper, look in `Shared/` and the area root, and reuse or extend what is there instead of
  copying it into another class.
- **A type stays with the feature that owns it** even when other features read it
  (`CameraManager`, `ReplaySession`, `NetworkedPlayerManager`, `CasterConfig`, `UI.ColorSpace`).
- **Panel draw code stays in `UI/Panels/`**; a feature folder holds no panel code.
- **Moving a type changes its namespace.** Update every `using` and keep them exact, with no unused
  `using Zetori.*`. Outside `Zetori.UI`, write `UI.ColorSpace`: `UnityEngine.ColorSpace` has the
  same name.

## Code standard

Professional, production-grade C#. **No explanatory comments** — the code must document itself
through precise naming, small cohesive methods, and clear structure. If a piece of logic needs a
comment to be understood, rename it or extract it until it doesn't. The one exception is the
three-line license header required at the top of every source file (copyright, SPDX identifier,
usage notice); a new `.cs` file must carry it verbatim, matching the existing files. (This
applies to all of the mod's own code, everything outside `External/`; the vendored `External/`
tree is left exactly as upstream ships it.)

Whitespace is consistent and deliberate: four-space indentation, braces on their own line, a
single blank line between members (never two or more), no trailing whitespace, exactly one
newline at end of file. Keep related code grouped and ordered the same way across features.

## Conventions

- Block-scoped namespaces mirroring folders (`Zetori.<Folder>.<Sub>`).
- `internal` and `sealed` by default; `public` only when required.
- Nullable reference types are **on**; honour the annotations.
- Log through `Log`; per-frame failures use `Log.*Once`. Never an empty `catch`.
- Every user-facing setting goes in `CasterConfig`. **Never rename an existing config
  section/key** — it is a saved-data contract. Every entry is bound in `CasterConfig.Bind`;
  at startup any key no longer bound is deleted from `Default.cfg` and every preset, so removing
  a binding removes its saved value everywhere. Config is for settings the user changes in the
  UI and presets snapshot; internal tuning values (animation timing, lerp/damping factors,
  layout spacing, thresholds) stay as `private const` in the class that uses them, never in
  config.
- `PluginInfo.Version` and `<Version>` in `Directory.Build.props` must match (build enforces it).
- `Zetori.csproj` references only the assemblies actually used; add a `<Reference>` when a
  new one is needed (the build error names it).

## Gotchas

- `NetworkedPlayerManager.activeRigs` can be null; the camera/rig may not exist yet.
- UI is authored at 1080p through `UiScale`; it must look right from 720p to 4K.
- Input uses the new Input System via `CasterConfig.KeyPressed/KeyHeld`, not legacy `Input`.
- Every panel is a collapsible title bar (`Panel`); there is no switch list and no section headers
  inside panels. With `CasterConfig.SnapPanels` on (the default), panels sit in a column slot (`PanelLayout`, pure logic with no Unity types so
  it can be exercised outside the game) and never overlap; dragging one reorders the slots live. With it off, panels stay where they are dropped and may overlap. A new panel needs a `PanelId`, a title, an icon and default column in `PanelCatalog`, and a draw
  method wired in `PanelContent.For`. Never rename a `PanelId` member — it is the saved `Layout` key.
- Panel content draws against `IPanelSurface`, implemented by the desktop `Panel` and the in-game VR
  `InGameSurface`, so every desktop panel also appears on the in-game panel's home page. Draw methods
  must only use the interface; anything desktop-specific breaks the in-game panel.
- A panel icon is an SVG on a 24-unit viewBox in `Resources/Icons/Panels/`, named after its `IconGlyph` member in lower case.
  `SvgReader` reads `path`, `circle`, `ellipse`, `rect`, `line`, `polyline`, `polygon` and `g` with `fill`, `stroke` and `stroke-width`;
  strokes always get round caps and joins. Desktop panels rasterize it to a texture, the in-game panel tessellates it into a mesh.
- A canvas HUD overlay subclasses `HudOverlay` in `Hud/` (`Active`, `Prefab`, `Bind`, `Refresh`); the
  base owns building, showing, hiding, font and teardown. The overlay itself goes in `Hud/Overlays/`.
- Features are auto-discovered by reflection — a new `ObjectComponent` subclass runs with no
  registration, so an incomplete one still loads. Guard with `enabled`.
- The order the components are created in, and so the order their `Awake` runs, is pinned in
  `Base/ComponentOrder`, so moving or renaming files never changes it. A component missing from
  that list is created after the listed ones; add it to the list only where it must run earlier.

## Also see

- [AGENTS.md](AGENTS.md) — entry point / map of these docs
- [CONTRIBUTING.md](CONTRIBUTING.md) — contribution policy, code style, release steps
- [README.md](README.md) — what the mod does, install, controls, build-from-source
