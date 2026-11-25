// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using System.Text;
using UnityEngine;

namespace Zetori.Replay
{
    internal static class RenderAudio
    {
        private const int SampleRate = 48000;
        private const short Channels = 1;
        private const short BitsPerSample = 16;
        private const int FormatChunkSize = 16;
        private const short PcmFormat = 1;
        private const int HeaderSize = 36;

        internal static void Write(string path, ReplayRecording recording, float from, float to)
        {
            var mix = new float[Mathf.Max(0, Mathf.CeilToInt((to - from) * SampleRate))];
            foreach (ReplayActor actor in recording.Actors)
            {
                Add(mix, actor.Voice, from, to);
                foreach (CosmeticSoundTrack sound in actor.Sounds) Add(mix, sound.Track, from, to);
            }
            WriteWave(path, mix);
        }

        private static void Add(float[] mix, VoiceTrack track, float from, float to)
        {
            foreach (VoiceSegment segment in track.Segments)
            {
                if (segment.End <= from || segment.Start >= to) continue;
                float[] samples = segment.ToFloats();
                int first = Mathf.Max(0, Mathf.FloorToInt((segment.Start - from) * SampleRate));
                for (int i = first; i < mix.Length; i++)
                {
                    double position = (from + i / (double)SampleRate - segment.Start) * segment.SampleRate;
                    if (position < 0d) continue;
                    int index = (int)position;
                    if (index + 1 >= samples.Length) break;
                    float blend = (float)(position - index);
                    mix[i] += samples[index] + (samples[index + 1] - samples[index]) * blend;
                }
            }
        }

        private static void WriteWave(string path, float[] samples)
        {
            int blockAlign = Channels * BitsPerSample / 8;
            int dataSize = samples.Length * blockAlign;
            using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(file);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(HeaderSize + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(FormatChunkSize);
            writer.Write(PcmFormat);
            writer.Write(Channels);
            writer.Write(SampleRate);
            writer.Write(SampleRate * blockAlign);
            writer.Write((short)blockAlign);
            writer.Write(BitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            foreach (float sample in samples) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        }
    }
}
