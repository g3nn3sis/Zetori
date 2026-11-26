// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Player.Voice
{
    internal sealed class VoiceGain : MonoBehaviour
    {
        private const float NoChange = 1f;

        private volatile float gain = NoChange;

        internal float Gain
        {
            get => gain;
            set => gain = value;
        }

        internal void OnAudioFilterRead(float[] data, int channels)
        {
            float current = gain;
            if (current == NoChange) return;
            for (int i = 0; i < data.Length; i++) data[i] *= current;
        }
    }
}
