// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;

namespace Zetori.Replay.Voice
{
    internal sealed class LocalVoiceTap : IProcessor<float>, IProcessor<short>
    {
        private const float BufferSeconds = 2f;
        private const float ShortRange = short.MaxValue;

        private readonly object gate = new object();
        private readonly LocalVoice voice;
        private readonly int channels;
        private readonly float[] pending;
        private int count;
        private volatile bool detached;

        internal int SampleRate { get; }

        private LocalVoiceTap(LocalVoice voice)
        {
            this.voice = voice;
            SampleRate = Mathf.Max(1, voice.Info.SamplingRate);
            channels = Mathf.Max(1, voice.Info.Channels);
            pending = new float[Mathf.CeilToInt(SampleRate * BufferSeconds)];
        }

        internal bool IsAttachedTo(LocalVoice? candidate) => !detached && ReferenceEquals(voice, candidate);

        internal static LocalVoice? CurrentVoice()
        {
            Recorder? recorder = GorillaTagger.Instance != null ? GorillaTagger.Instance.myRecorder : null;
            return recorder != null ? recorder.Voice : null;
        }

        internal static LocalVoiceTap? AttachTo(LocalVoice? voice)
        {
            if (voice == null) return null;
            var tap = new LocalVoiceTap(voice);
            switch (voice)
            {
                case LocalVoiceFramed<float> floats:
                    floats.AddPostProcessor(tap);
                    return tap;
                case LocalVoiceFramed<short> shorts:
                    shorts.AddPostProcessor(tap);
                    return tap;
                default:
                    return null;
            }
        }

        internal void Detach() => detached = true;

        public float[] Process(float[] buf)
        {
            if (Audible()) Capture(buf.Length, i => buf[i]);
            return buf;
        }

        public short[] Process(short[] buf)
        {
            if (Audible()) Capture(buf.Length, i => buf[i] / ShortRange);
            return buf;
        }

        public void Dispose() => detached = true;

        internal int Drain(ref float[] into)
        {
            lock (gate)
            {
                if (into.Length < count) into = new float[pending.Length];
                Array.Copy(pending, into, count);
                int drained = count;
                count = 0;
                return drained;
            }
        }

        private bool Audible() => !detached && voice.TransmitEnabled && voice.IsCurrentlyTransmitting;

        private void Capture(int length, Func<int, float> sample)
        {
            lock (gate)
            {
                for (int i = 0; i + channels <= length && count < pending.Length; i += channels)
                {
                    float sum = 0f;
                    for (int c = 0; c < channels; c++) sum += sample(i + c);
                    pending[count++] = sum / channels;
                }
            }
        }
    }
}
