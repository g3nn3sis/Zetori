# Replay render to video: design

## Intent

The Replay panel's render button writes a PNG for every frame plus `audio.wav`, so the
user has to join them into a video themselves. Make it write one finished video file that
goes straight into a video editor, with nothing extra to install, on Windows and on Linux.

## Goals

- One `.mp4` per render, H.264 video plus AAC audio, in `Gorilla Tag\Zetori\Renders`,
  named after the replay and the time (as the folder is named today).
- Quality meant for editing: the video should survive being exported again by an editor.
- FFmpeg ships inside the mod DLL. Installing the mod stays a single DLL.
- Works on Linux, where the game runs under Proton.

## Non-goals

- PNG image sequence output. It is replaced, not kept as an option.
- A user-facing quality or codec setting. Quality values stay internal constants.
- Changing how frames are captured. The frame-by-frame stepping and the resolution and
  frame-rate choices stay exactly as they are.

## Anti-goals

- A render that silently produces a broken or truncated file. Any FFmpeg failure is
  logged with FFmpeg's own error output and the partial file is removed.
- A DLL tens of megabytes larger. The bundled FFmpeg is cut down to what rendering needs.

## Constraints

- The game can't be launched from here. The encoder is tested outside the game with
  synthetic frames and audio, and has no Unity types so that's possible.
- Under Proton, the Windows `ffmpeg.exe` runs through Wine, so there is no separate
  Linux build. Linux can't be tested from here.
- H.264 needs x264, which is GPL, so the bundled `ffmpeg.exe` is a GPL program. It is
  shipped as a separate executable that the mod starts and pipes frames into, never
  linked into the mod. Its license text and the exact FFmpeg and x264 source versions go
  into the release and `THIRD_PARTY_NOTICES.md`.
- Known limit: DaVinci Resolve's free version on Linux can't open H.264, and no version
  of Resolve on Linux reads AAC audio. The user accepted this over ProRes.

## Approaches

| Approach | Trade-off | Outcome |
|---|---|---|
| Windows Media Foundation encoder | No download, but unreliable under Wine | Rejected: must work on Linux |
| FFmpeg the user installs | Simple, but every user has to install it | Rejected: user asked for it embedded |
| Full official FFmpeg embedded | Easy to get, but 80–130 MB | Rejected: DLL size |
| Cut-down FFmpeg built from source, embedded | Small (about 5 MB); needs a one-time toolchain | **Chosen** |
| ProRes `.mov` instead of H.264 | Opens in every editor, but about 7 times larger | User chose H.264 `.mp4` |

## Decided

- Format: H.264 `.mp4`, AAC audio (user).
- FFmpeg built from pinned FFmpeg and x264 source releases, static, Windows x64, with only:
  the pipe and file protocols, raw-video and WAV input, the libx264 and AAC encoders, the
  MP4 muxer, and the filters needed for flipping and colour conversion. The build script
  and the built `ffmpeg.exe` are committed, so nobody else needs the toolchain.
- The mod extracts the embedded `ffmpeg.exe` to disk on the first render, re-extracting
  when the embedded copy changes, and starts it as a separate process.
- Frames go to FFmpeg's standard input as raw pixels, written in order on a background
  thread, with the existing limit of 4 frames in flight.
- The audio mix is written to a temporary WAV, given to FFmpeg as a second input, and
  deleted afterwards.
- Colour: converted and tagged as BT.709, `yuv420p`, so editors show the right colours.
- Cancelling stops FFmpeg and deletes the partial video and temporary audio.
- The panel button becomes "Render Video"; when a render finishes, the panel shows the file name.
  README is updated.

## Left to the builder

- Exact x264 rate control (around CRF 16), preset, and keyframe interval (about one second
  so editors scrub smoothly), and the AAC bitrate (about 320 kbps).
- Where the extracted `ffmpeg.exe` lives and how "changed" is detected.
- Whether the embedded binary is compressed.
- How the encoder is checked outside the game.
