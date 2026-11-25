# Replay Render to Video Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The Replay panel's render button writes one H.264/AAC `.mp4` through an FFmpeg embedded in the mod DLL, instead of a PNG sequence and a WAV.

**Architecture:** A cut-down static `ffmpeg.exe` (libx264 + AAC + MP4 only) is built once from pinned sources, committed, and embedded in the DLL. On a render, the mod extracts it, mixes the audio to a temporary WAV, starts FFmpeg with raw RGB24 frames on standard input and the WAV as a second input, and feeds it the frames that `ReplayRender` already captures. The encoder and extractor have no Unity or BepInEx types so a console harness outside the game can drive them.

**Tech Stack:** C# (net48, LangVersion latest, nullable on), `System.Diagnostics.Process`, MSYS2 MinGW64 (gcc, make, nasm) for the FFmpeg build, FFmpeg + x264 source releases.

**Spec:** `docs/superpowers/specs/2026-11-25-replay-video-render-design.md`

## Global Constraints

- Output: one `.mp4` per render, H.264 video + AAC audio, in `DataFolders.Renders`, named `<replay name> <yyyy-MM-dd HH-mm-ss>.mp4` with ` (2)`, ` (3)`… on collision.
- PNG sequence output is removed, not kept as an option. No new `CasterConfig` entries; quality values are `private const`.
- Capture is unchanged: resolution (1080p/1440p/4K), frame rate (30/60), frame-exact stepping, `Time.captureFramerate` handling, the 4-frames-in-flight limit.
- Colour: RGB→YUV with the BT.709 matrix, limited range, `yuv420p`, streams tagged bt709.
- FFmpeg is GPL (x264): shipped as a separate executable the mod starts, never linked. Its license and exact FFmpeg/x264 source versions go in the release archive and `THIRD_PARTY_NOTICES.md`.
- Every new `.cs` file starts with the three-line license header; no explanatory comments; one top-level type per file; `internal sealed` by default.
- `dotnet build Zetori.csproj -c Release` stays at zero warnings after every task.
- Downloads (MSYS2, packages, FFmpeg/x264 sources) only after the user says yes to the named files, sources and sizes.
- Commit only when the user says so, staging only this plan's files: the working tree holds unrelated uncommitted work.
- The harness lives outside the repo (`$SCRATCH\render-harness`, where `$SCRATCH` is the session scratchpad); every `.cs` under the repo compiles into the plugin.

## Review Focus

1. **Cancel while preparing** (audio still mixing, FFmpeg not started): no FFmpeg process survives and the temporary WAV is deleted once preparation ends. Review check in Task 4 (Unity-bound, no harness).
2. **FFmpeg dies mid-render** (disk full, killed): the render stops within a frame or two, the log carries FFmpeg's own error lines, the partial `.mp4` is deleted, the panel returns to idle. Harness test `FaultsWhenFfmpegDies` (Task 3) plus review check in Task 4.
3. **FFmpeg can't start** (antivirus quarantined the extracted exe): `Start` throws, the render ends cleanly with a logged error, no exception per frame. Harness test `StartFailsForMissingExecutable` (Task 3) plus review check in Task 4.
4. **Replay names with spaces, parentheses or apostrophes:** paths are quoted and encode fine. Harness test `HandlesPathsWithSpaces` (Task 3).
5. **Game closes mid-render:** `OnDestroy` aborts FFmpeg so no orphan process keeps the file locked. Review check in Task 4.

---

### Task 1: Build the cut-down FFmpeg and record its licence

**Files:**
- Create: `Tools/FFmpeg/build.sh`
- Create: `Resources/FFmpeg/ffmpeg.exe` (built output, committed)
- Create: `Resources/FFmpeg/COPYING.GPLv2` (copied from the FFmpeg source tarball)
- Modify: `THIRD_PARTY_NOTICES.md`, `CONTRIBUTING.md:28`, `CLAUDE.md` (Layout: add `Tools/` and FFmpeg under `Resources/`)

**Interfaces:**
- Produces: `Resources/FFmpeg/ffmpeg.exe`, a static Windows x64 executable with no DLL dependencies beyond Windows system DLLs.

- [ ] **Step 1: Ask the user to approve the downloads**, naming each: MSYS2 base archive `msys2-base-x86_64-latest.sfx.exe` from `repo.msys2.org/distrib` (about 100 MB, unpacks to `C:\msys64`); MSYS2 packages `make nasm diffutils mingw-w64-x86_64-gcc mingw-w64-x86_64-pkgconf` (about 500 MB) and `mingw-w64-x86_64-ffmpeg` for ffprobe and decoding in tests only (about 300 MB); `ffmpeg-<latest release>.tar.xz` from `ffmpeg.org/releases` (about 11 MB); x264 stable-branch snapshot from `code.videolan.org/videolan/x264` (about 1 MB). Stop until the user says yes.

- [ ] **Step 2: Install MSYS2 and the packages**

Run: `C:\msys64\usr\bin\bash.exe -lc "pacman -Syu --noconfirm"` (twice if it asks), then `pacman -S --needed --noconfirm <packages above>`.
Expected: `C:\msys64\mingw64\bin\gcc.exe --version` and `C:\msys64\mingw64\bin\ffprobe.exe -version` both print versions.

- [ ] **Step 3: Write `Tools/FFmpeg/build.sh`** (run in the MSYS2 MINGW64 environment; writes `Resources/FFmpeg/ffmpeg.exe` and `Resources/FFmpeg/COPYING.GPLv2`)

Pins `FFMPEG_VERSION`, `FFMPEG_SHA256`, `X264_COMMIT`, `X264_SHA256` as variables at the top and fails on a hash mismatch. Builds into a work directory outside the repo (`${BUILD_DIR:-$HOME/zetori-ffmpeg}`), `set -euo pipefail`.

x264 configure:
```
--prefix=$PREFIX --enable-static --disable-cli --disable-opencl --disable-lavf --disable-swscale
--disable-ffms --disable-gpac --disable-lsmash --bit-depth=8 --chroma-format=420 --enable-strip
```
FFmpeg configure (with `PKG_CONFIG_PATH=$PREFIX/lib/pkgconfig`):
```
--prefix=$PREFIX --arch=x86_64 --target-os=mingw32 --pkg-config-flags=--static
--extra-cflags=-I$PREFIX/include --extra-ldflags="-L$PREFIX/lib -static"
--enable-gpl --enable-libx264 --enable-static --disable-shared --disable-autodetect --enable-w32threads
--disable-everything --disable-doc --disable-network --disable-debug --enable-small
--disable-ffplay --disable-ffprobe --disable-avdevice
--enable-protocol=file,pipe --enable-demuxer=rawvideo,wav --enable-decoder=rawvideo,pcm_s16le
--enable-encoder=libx264,aac --enable-muxer=mp4 --enable-parser=h264,aac
--enable-filter=vflip,scale,format,null,anull,aformat,aresample
```
Then `strip` the exe and copy it and `COPYING.GPLv2` into `Resources/FFmpeg/`.

- [ ] **Step 4: Pin the versions and run the script**

Pin the newest FFmpeg release listed at `ffmpeg.org/releases` and the current x264 `stable` head; record their SHA-256s in the script.
Run: `C:\msys64\usr\bin\env.exe MSYSTEM=MINGW64 /usr/bin/bash -lc "cd /c/Users/genne/source/repos/Zetori && Tools/FFmpeg/build.sh"`
Expected: exits 0; `Resources/FFmpeg/ffmpeg.exe` exists and is under 8 MB.

- [ ] **Step 5: Check the components and run a smoke encode**

Run `Resources/FFmpeg/ffmpeg.exe -hide_banner` with `-encoders`, `-muxers`, `-filters`, `-version`.
Expected: `libx264` and `aac` encoders; `mp4` muxer; `vflip`, `scale`, `format` filters; `--enable-gpl --enable-libx264` in the configuration line.
Then make inputs with the MSYS2 ffmpeg (`-f lavfi -i testsrc2=s=320x240:r=30 -t 2 -pix_fmt rgb24 -f rawvideo in.rgb`, `-f lavfi -i sine=f=440:d=2 -ac 1 -ar 48000 in.wav`), pipe `in.rgb` into the cut-down exe with the exact arguments from Task 3's `Arguments`, and probe the result with MSYS2 `ffprobe`.
Expected: h264 High yuv420p bt709 320x240 60 frames + aac 48000 mono, and no errors. A missing component (bitstream filter, parser) shows up here: add it to the configure list and rebuild.

- [ ] **Step 6: Write the licence notes**

`THIRD_PARTY_NOTICES.md`: a paragraph saying the DLL embeds `ffmpeg.exe` built from FFmpeg `<version>` and x264 `<commit>` with `Tools/FFmpeg/build.sh`, licensed GPL v2 or later, run as a separate program; the licence is `Resources/FFmpeg/COPYING.GPLv2` in the source and `FFMPEG_LICENSE.txt` in the release archive; source at the two pinned URLs.
`CONTRIBUTING.md` step 2: the release zip also holds `FFMPEG_LICENSE.txt`; step 3: rebuild FFmpeg only with `Tools/FFmpeg/build.sh` in MSYS2 MINGW64.
`CLAUDE.md` Layout: `Tools/  Build scripts for bundled binaries (FFmpeg)`; `Resources/` line mentions the FFmpeg binary.

- [ ] **Step 7: Commit** (on the user's go-ahead)

```bash
git add Tools/FFmpeg Resources/FFmpeg THIRD_PARTY_NOTICES.md CONTRIBUTING.md CLAUDE.md
git commit -m "Add a cut-down FFmpeg build for replay video renders"
```

### Task 2: Embed and extract FFmpeg

**Files:**
- Create: `Replay/BundledFFmpeg.cs`
- Modify: `Zetori.csproj` (new `EmbeddedResource` group), `Base/DataFolders.cs` (add `Tools`)
- Test: `$SCRATCH\render-harness\` (net48 console project that includes repo source files by path and embeds `Resources\FFmpeg\ffmpeg.exe` under the same logical name)

**Interfaces:**
- Produces: `internal static class BundledFFmpeg { internal static string Extract(string folder); }`, which returns the full path of `<folder>\ffmpeg.exe`, writing it only when it's missing or its bytes differ from the embedded copy. Embedded resource logical name: `Zetori.FFmpeg.ffmpeg.exe`.
- Produces: `DataFolders.Tools => Path.Combine(Root, "Tools")`.

- [ ] **Step 1: Create the harness and write the failing tests**

Harness: `Program.Main` runs each test by name, prints `PASS name` / `FAIL name: reason`, exits non-zero on any failure. Project: net48, `LangVersion latest`, `ImplicitUsings`, `Nullable`, `<Using Remove="System.Net.Http" />`, `TreatWarningsAsErrors`.
```csharp
static void ExtractWritesEmbeddedCopy()      // path == dir\ffmpeg.exe, File.Exists, Sha256(file) == Sha256(resource)
static void ExtractKeepsUnchangedCopy()      // second Extract leaves LastWriteTimeUtc unchanged (sleep 1.1 s between)
static void ExtractReplacesChangedCopy()     // overwrite with {1,2,3}; Extract; Sha256(file) == Sha256(resource)
static void ExtractCreatesMissingFolder()    // folder does not exist beforehand; Extract succeeds
```

- [ ] **Step 2: Run the harness to verify it fails**

Run: `dotnet run --project $SCRATCH\render-harness -- bundled`
Expected: build fails, `BundledFFmpeg` not found.

- [ ] **Step 3: Implement `BundledFFmpeg.Extract(string folder)`, the csproj embed and `DataFolders.Tools`**

Compare by length, then by SHA-256 against the resource stream; write to `ffmpeg.exe.tmp` and move it over the target so a half-written exe is never left as `ffmpeg.exe`.

- [ ] **Step 4: Run the harness and the Release build**

Run: `dotnet run --project $SCRATCH\render-harness -- bundled`, then `dotnet build Zetori.csproj -c Release`.
Expected: 4 PASS; build succeeds with 0 warnings and the DLL grows by about the exe's size.

- [ ] **Step 5: Commit** (on the user's go-ahead)

```bash
git add Replay/BundledFFmpeg.cs Zetori.csproj Base/DataFolders.cs
git commit -m "Embed FFmpeg and extract it on demand"
```

### Task 3: FFmpeg video encoder

**Files:**
- Create: `Replay/VideoFormat.cs`, `Replay/VideoEncoder.cs`
- Test: `$SCRATCH\render-harness\` (adds the two files)

**Interfaces:**
- Consumes: a path from `BundledFFmpeg.Extract`.
- Produces:
  - `internal readonly struct VideoFormat { internal VideoFormat(int width, int height, int frameRate); internal int Width { get; } internal int Height { get; } internal int FrameRate { get; } }`
  - `internal sealed class VideoEncoder : IDisposable`
    - `internal static VideoEncoder Start(string ffmpegPath, VideoFormat format, string audioPath, string outputPath)` starts the process and throws if it can't.
    - `internal static string Arguments(VideoFormat format, string audioPath, string outputPath)`
    - `internal void Write(byte[] frame)` takes one bottom-up RGB24 frame, `Width*Height*3` bytes, and queues it. It returns at once, and frames are written in order on one background thread.
    - `internal int Pending { get; }` counts frames queued or being written.
    - `internal bool Faulted { get; }` becomes true once writing failed (FFmpeg gone).
    - `internal Task Finish()` ends input, waits for exit, completes on exit code 0, and otherwise faults with `InvalidOperationException("FFmpeg exited with code N: <last stderr lines>")` after deleting the output.
    - `internal void Abort()` stops input, kills FFmpeg if it's running (waiting up to 2 s), and deletes the output.
    - `public void Dispose()` calls `Abort()` unless `Finish()` completed.

- [ ] **Step 1: Write the failing tests**

```csharp
static void ArgumentsMatchSpec()
{
    string expected = "-hide_banner -nostats -loglevel error -y -f rawvideo -pix_fmt rgb24 -video_size 1920x1080 -framerate 60 -i pipe:0 "
        + "-i \"C:\\a b\\x.wav\" -map 0:v:0 -map 1:a:0 -vf vflip,scale=out_color_matrix=bt709:out_range=tv,format=yuv420p "
        + "-c:v libx264 -preset fast -crf 16 -g 60 -profile:v high -colorspace bt709 -color_primaries bt709 -color_trc bt709 -color_range tv "
        + "-c:a aac -b:a 320k \"C:\\a b\\y.mp4\"";
    Assert(VideoEncoder.Arguments(new VideoFormat(1920, 1080, 60), @"C:\a b\x.wav", @"C:\a b\y.mp4") == expected);
}
static void EncodesPlayableMp4()
// 90 frames 320x240 @30; memory rows 0..119 blue, 120..239 red (bottom-up, so the picture's top is red); 3 s 440 Hz mono 48 kHz WAV.
// Finish completes. MSYS2 ffprobe (-count_frames, key=value per stream):
// v: codec_name=h264 profile=High pix_fmt=yuv420p color_space=bt709 color_primaries=bt709 color_transfer=bt709
//    width=320 height=240 r_frame_rate=30/1 nb_read_frames=90
// a: codec_name=aac sample_rate=48000 channels=1
// MSYS2 ffmpeg decodes frame 0 to rgb24: pixel (10,10) within 10 of (255,0,0); pixel (10,230) within 10 of (0,0,255).
static void HandlesPathsWithSpaces()          // output in folder "out dir (2)'s" encodes; file exists
static void ReportsFfmpegFailure()            // output in a missing folder: Finish faults, message has "FFmpeg exited" and a non-empty stderr line; no file
static void FaultsWhenFfmpegDies()            // write 5 frames, kill the ffmpeg process by its path; Faulted within 2 s of further Writes; Finish faults
static void AbortRemovesPartialFile()         // write 10 frames, Abort: output file gone, no ffmpeg process left with that path
static void StartFailsForMissingExecutable()  // Start(@"C:\nope\ffmpeg.exe", ...) throws Win32Exception
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet run --project $SCRATCH\render-harness -- encoder`
Expected: build fails, `VideoEncoder` and `VideoFormat` not found.

- [ ] **Step 3: Implement `VideoFormat` and `VideoEncoder`**

`ProcessStartInfo`: `UseShellExecute=false`, `CreateNoWindow=true`, `RedirectStandardInput=true`, `RedirectStandardError=true` read with `BeginErrorReadLine` into a queue that keeps the last 20 lines. The writer is one `LongRunning` task draining a `BlockingCollection<byte[]>` into `StandardInput.BaseStream`, and it sets `Faulted` on `IOException`. `Finish` runs `CompleteAdding`, awaits the writer, closes stdin, and runs `WaitForExit()` on a pool thread (no timeout, so stderr is fully read). Quality constants: `Crf = 16`, `Preset = "fast"`, keyframe interval = frame rate, `AudioBitrate = "320k"`.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project $SCRATCH\render-harness -- encoder`, then `dotnet build Zetori.csproj -c Release`.
Expected: 7 PASS; 0 warnings.

- [ ] **Step 5: Commit** (on the user's go-ahead)

```bash
git add Replay/VideoFormat.cs Replay/VideoEncoder.cs
git commit -m "Add an FFmpeg pipe encoder for replay renders"
```

### Task 4: Render replays to video

**Files:**
- Modify: `Replay/ReplayRender.cs`, `UI/Panels/ReplayPanel.cs:183-210`, `README.md:24`, `README.md:140`

**Interfaces:**
- Consumes: `BundledFFmpeg.Extract(DataFolders.Tools)`, `VideoEncoder.Start/Write/Pending/Faulted/Finish/Abort`, `VideoFormat`, `RenderAudio.Write(path, recording, from, to)` (unchanged).
- Produces: `ReplayRender.LastFile` (`string?`, file name only), which replaces `LastFolder`; `ReplayRender.Finishing` (`bool`, true while FFmpeg finalises). `Active`, `FramesDone`, `FramesTotal`, `Start`, `Cancel`, `Renders` keep their signatures.

- [ ] **Step 1: Rework `ReplayRender`**

- `Start` computes the output with `UniqueFile(player.Name)` and the audio path with `Path.ChangeExtension(output, ".wav")`. It reads `DataFolders.Tools` on the main thread, then starts `preparing = Task.Run(() => { RenderAudio.Write(...); return VideoEncoder.Start(BundledFFmpeg.Extract(tools), format, audioPath, output); })`. The camera, `captureFramerate`, seek and flags stay as they are today.
- `Advance` order:
  1. Session check, as today.
  2. Until `preparing` completes, return.
  3. If `preparing` faulted, `Log.Exception(..., "Couldn't start the video encoder")`, delete the audio and `Finish()`.
  4. Take the encoder.
  5. If `finishing` is set, return until it completes. Then on success set `LastFile` and `Log.Info`; on fault `Log.Exception(..., "Encoding the replay video failed")`. Either way delete the audio and `Finish()`.
  6. If `encoder.Faulted` or `next >= FramesTotal`, set `finishing = encoder.Finish()` and return.
  7. Run the rendered-frame gate, as today.
  8. If `encoder.Pending >= MaximumPendingFrames`, return.
  9. Capture, advance, seek.
- `Capture` passes `readback.GetRawTextureData()` to `encoder.Write`. The PNG path, `pending` and `ObservePending` go.
- `Cancel` and `OnDestroy`:
  - With an encoder: `Abort()` it and delete the audio.
  - While still preparing: attach a continuation that `Abort()`s the encoder if one was started and deletes the audio.
  - Then `Finish()`.
- `UniqueFile` names the file `<name> <yyyy-MM-dd HH-mm-ss>.mp4`, adding ` (n)` on collision, and creates `DataFolders.Renders`.

- [ ] **Step 2: Update the panel**

`DrawRendering` shows `Finishing the video` while `ReplayRender.Finishing`, otherwise the current frame label. The button reads `Render Video`, and the label reads `Rendered to: {ReplayRender.LastFile}`.

- [ ] **Step 3: Update the README**

Line 24: replace the **Render Frames** sentence with **Render Video**. It saves an H.264 `.mp4` with AAC audio of the recorded voices and cosmetic sounds (no hand taps or game sounds), made for video editors, using an FFmpeg built into the mod. Note that the free DaVinci Resolve on Linux can't open H.264. Keep the rest of the paragraph.
Line 140: rendered replays are saved in `Gorilla Tag\Zetori\Renders` as `.mp4` files named after the replay and the time.

- [ ] **Step 4: Build**

Run: `dotnet build Zetori.csproj -c Release`
Expected: 0 warnings, 0 errors; no remaining references to `LastFolder` or `frame_` (`git grep -n "LastFolder\|frame_" -- '*.cs'` is empty).

- [ ] **Step 5: Review checks** (Unity-bound, can't run here)

Walk the code for Review Focus items 1, 2, 3 and 5. For each: no FFmpeg process left, the temporary WAV deleted, the partial `.mp4` deleted on failure, one log entry rather than one per frame, and `Finish()` reached.

- [ ] **Step 6: Commit** (on the user's go-ahead)

```bash
git add Replay/ReplayRender.cs UI/Panels/ReplayPanel.cs README.md
git commit -m "Render replays to an H.264 video"
```
