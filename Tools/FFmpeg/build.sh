#!/usr/bin/env bash
# Builds Resources/FFmpeg/ffmpeg.exe for replay video renders. Run in the MSYS2 MINGW64 environment.
set -euo pipefail

FFMPEG_VERSION=9.1
FFMPEG_SHA256=1a4d394ae63d190a526cd052eb4415c156b5c5e2df37efab41a7de0b8fde2433
X264_COMMIT=b35605ace3ddf7c1a5d67a2eb553f034aef41d55
X264_SHA256=6eeb82934e69fd51e043bd8c5b0d152839638d1ce7aa4eea65a3fedcf83ff224

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUTPUT_DIR="$REPO_DIR/Resources/FFmpeg"
BUILD_DIR="${BUILD_DIR:-$HOME/zetori-ffmpeg}"
PREFIX="$BUILD_DIR/prefix"
JOBS="$(nproc)"

fetch()
{
    local url="$1" file="$2" sha="$3"
    [ -f "$file" ] || curl -fsSL -o "$file" "$url"
    echo "$sha  $file" | sha256sum -c -
}

mkdir -p "$BUILD_DIR" "$PREFIX" "$OUTPUT_DIR"
cd "$BUILD_DIR"

fetch "https://ffmpeg.org/releases/ffmpeg-$FFMPEG_VERSION.tar.xz" "ffmpeg-$FFMPEG_VERSION.tar.xz" "$FFMPEG_SHA256"
fetch "https://code.videolan.org/videolan/x264/-/archive/$X264_COMMIT/x264-$X264_COMMIT.tar.bz2" "x264-$X264_COMMIT.tar.bz2" "$X264_SHA256"

rm -rf "ffmpeg-$FFMPEG_VERSION" "x264-$X264_COMMIT"
tar -xf "ffmpeg-$FFMPEG_VERSION.tar.xz"
tar -xf "x264-$X264_COMMIT.tar.bz2"

(
    cd "x264-$X264_COMMIT"
    ./configure --prefix="$PREFIX" --enable-static --disable-cli --disable-opencl --disable-lavf \
        --disable-swscale --disable-ffms --disable-gpac --disable-lsmash \
        --bit-depth=8 --chroma-format=420 --enable-strip
    make -j"$JOBS"
    make install
)

(
    cd "ffmpeg-$FFMPEG_VERSION"
    PKG_CONFIG_PATH="$PREFIX/lib/pkgconfig" ./configure --prefix="$PREFIX" --arch=x86_64 --target-os=mingw32 \
        --pkg-config-flags=--static --extra-cflags="-I$PREFIX/include" --extra-ldflags="-L$PREFIX/lib -static" \
        --enable-gpl --enable-libx264 --enable-static --disable-shared --disable-autodetect --enable-w32threads \
        --disable-everything --disable-doc --disable-network --disable-debug --enable-small \
        --disable-ffplay --disable-ffprobe --disable-avdevice \
        --enable-protocol=file,pipe --enable-demuxer=rawvideo,wav --enable-decoder=rawvideo,pcm_s16le \
        --enable-encoder=libx264,aac --enable-muxer=mp4 --enable-parser=h264,aac \
        --enable-filter=vflip,scale,format,null,anull,aformat,aresample
    make -j"$JOBS"
    strip -o "$OUTPUT_DIR/ffmpeg.exe" ffmpeg.exe
    cp COPYING.GPLv2 "$OUTPUT_DIR/COPYING.GPLv2"
)

ls -l "$OUTPUT_DIR/ffmpeg.exe"
