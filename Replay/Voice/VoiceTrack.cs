// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;
using Zetori.Shared;

namespace Zetori.Replay.Voice
{
    internal sealed class VoiceTrack
    {
        private const float SilenceThreshold = 0.002f;
        private const float MaxSilenceSeconds = 0.6f;
        private const float MaxJoinGap = 0.25f;

        private readonly List<VoiceSegment> segments = new List<VoiceSegment>();
        private VoiceSegment? open;
        private int trailingSilence;

        internal IReadOnlyList<VoiceSegment> Segments => segments;

        internal void Append(float chunkEnd, float[] buffer, int count, int sampleRate)
        {
            if (count <= 0) return;
            bool silent = Peak(buffer, count) < SilenceThreshold;
            float chunkStart = chunkEnd - count / (float)sampleRate;
            if (open != null && chunkStart - open.End > MaxJoinGap) Close();
            if (open == null)
            {
                if (silent) return;
                open = new VoiceSegment(Mathf.Max(0f, chunkStart), sampleRate);
                segments.Add(open);
            }
            trailingSilence = silent ? trailingSilence + count : 0;
            open.Append(buffer, count);
            if (trailingSilence > MaxSilenceSeconds * sampleRate) Close();
        }

        internal void Close()
        {
            if (open == null) return;
            open.TrimEnd(trailingSilence);
            if (open.Length == 0) segments.Remove(open);
            open = null;
            trailingSilence = 0;
        }

        internal VoiceTrack Slice(float from, float to)
        {
            var slice = new VoiceTrack();
            foreach (VoiceSegment segment in segments)
            {
                if (segment.End <= from || segment.Start >= to) continue;
                VoiceSegment? part = segment.Slice(from, to);
                if (part != null) slice.segments.Add(part);
            }
            return slice;
        }

        internal void ForgetBefore(float time) => segments.RemoveAll(segment => segment != open && segment.End <= time);

        internal void Write(BinaryWriter writer)
        {
            Close();
            writer.Write(segments.Count);
            foreach (VoiceSegment segment in segments) segment.Write(writer);
        }

        internal void Read(BinaryReader reader)
        {
            int count = BinaryIO.ReadCount(reader);
            for (int i = 0; i < count; i++) segments.Add(VoiceSegment.Read(reader));
        }

        internal VoiceSegment? At(float time)
        {
            int low = 0, high = segments.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                VoiceSegment segment = segments[mid];
                if (time < segment.Start) high = mid - 1;
                else if (time >= segment.End) low = mid + 1;
                else return segment;
            }
            return null;
        }

        private static float Peak(float[] buffer, int count)
        {
            float peak = 0f;
            for (int i = 0; i < count; i++) peak = Mathf.Max(peak, Mathf.Abs(buffer[i]));
            return peak;
        }
    }
}
