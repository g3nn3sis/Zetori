# Third-party notices

`External/DiscordRPC` contains code from [discord-rpc-csharp](https://github.com/Lachee/discord-rpc-csharp), copyright 2021 Lachee, licensed under MIT. Its license is included at `External/DiscordRPC/LICENSE` in the source and as `DISCORD_RPC_LICENSE.txt` in the release archive. The release DLL includes this code.

The release DLL embeds `ffmpeg.exe`, which replay renders run as a separate program to encode video. It is built by `Tools/FFmpeg/build.sh` from [FFmpeg](https://ffmpeg.org) 9.1 (<https://ffmpeg.org/releases/ffmpeg-9.1.tar.xz>) and [x264](https://www.videolan.org/developers/x264.html) commit `b35605ace3ddf7c1a5d67a2eb553f034aef41d55` (<https://code.videolan.org/videolan/x264/-/archive/b35605ace3ddf7c1a5d67a2eb553f034aef41d55/x264-b35605ace3ddf7c1a5d67a2eb553f034aef41d55.tar.bz2>), and is licensed under the GNU General Public License version 2 or later, which governs `ffmpeg.exe` regardless of Zetori's own license. Its license is included at `Resources/FFmpeg/COPYING.GPLv2` in the source and as `FFMPEG_LICENSE.txt` in the release archive. Its complete corresponding source, those two archives and the build script, is attached to every GitHub release that ships it.

Gorilla Tag, Unity, BepInEx, Harmony, and other game or framework assemblies are build dependencies. They are not included in the public release archive.
