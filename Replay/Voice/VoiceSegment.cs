// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;
using Zetori.Shared;

namespace Zetori.Replay.Voice
{
    internal sealed class VoiceSegment
    {
        private const float SampleRange = short.MaxValue;

        private readonly List<short> samples = new List<short>();

        internal float Start { get; }
        internal int SampleRate { get; }
        internal int Length => samples.Count;
        internal float End => Start + samples.Count / (float)SampleRate;

        internal VoiceSegment(float start, int sampleRate)
        {
            Start = start;
            SampleRate = sampleRate;
        }

        internal void Append(float[] buffer, int count)
        {
            for (int i = 0; i < count; i++)
                samples.Add((short)Mathf.RoundToInt(Mathf.Clamp(buffer[i], -1f, 1f) * SampleRange));
        }

        internal VoiceSegment? Slice(float from, float to)
        {
            int first = Mathf.Clamp(Mathf.CeilToInt((from - Start) * SampleRate), 0, samples.Count);
            int end = Mathf.Clamp(Mathf.FloorToInt((to - Start) * SampleRate), 0, samples.Count);
            if (end <= first) return null;
            var slice = new VoiceSegment(Start + first / (float)SampleRate - from, SampleRate);
            slice.samples.AddRange(samples.GetRange(first, end - first));
            return slice;
        }

        internal void TrimEnd(int count)
        {
            int keep = Mathf.Max(0, samples.Count - count);
            samples.RemoveRange(keep, samples.Count - keep);
            samples.TrimExcess();
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(Start);
            writer.Write(SampleRate);
            writer.Write(samples.Count);
            foreach (short sample in samples) writer.Write(sample);
        }

        internal static VoiceSegment Read(BinaryReader reader)
        {
            var segment = new VoiceSegment(reader.ReadSingle(), reader.ReadInt32());
            if (segment.SampleRate <= 0) throw new InvalidDataException("Invalid voice sample rate.");
            int count = BinaryIO.ReadCount(reader);
            segment.samples.Capacity = count;
            for (int i = 0; i < count; i++) segment.samples.Add(reader.ReadInt16());
            return segment;
        }

        internal float[] ToFloats()
        {
            var result = new float[samples.Count];
            for (int i = 0; i < result.Length; i++) result[i] = samples[i] / SampleRange;
            return result;
        }
    }
}
