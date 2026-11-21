// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using UnityEngine;

namespace Zetori.Replay
{
    internal sealed class PuppetHandTaps
    {
        private const float MaxCatchUp = 0.5f;

        private readonly IReadOnlyList<HandTap> taps;
        private readonly AudioSource left;
        private readonly AudioSource right;
        private int next;

        internal PuppetHandTaps(IReadOnlyList<HandTap> taps, AudioSource left, AudioSource right)
        {
            this.taps = taps;
            this.left = left;
            this.right = right;
        }

        internal void Place(Vector3 leftHand, Vector3 rightHand)
        {
            left.transform.localPosition = leftHand;
            right.transform.localPosition = rightHand;
        }

        internal void Advance(float from, float to, bool audible)
        {
            if (!audible || to < from || to - from > MaxCatchUp)
            {
                next = FirstAfter(to);
                return;
            }
            while (next < taps.Count && taps[next].Time <= to)
            {
                HandTap tap = taps[next++];
                if (tap.Time > from) Play(tap);
            }
        }

        private void Play(HandTap tap)
        {
            AudioClip? clip = SurfaceClip(tap.Surface);
            if (clip == null) return;
            AudioSource source = tap.IsLeftHand ? left : right;
            source.volume = tap.Volume;
            source.PlayOneShot(clip);
        }

        private static AudioClip? SurfaceClip(int surface)
        {
            List<GTPlayer.MaterialData>? surfaces = GTPlayer.Instance != null ? GTPlayer.Instance.materialData : null;
            if (surfaces == null || surfaces.Count == 0) return null;
            GTPlayer.MaterialData data = surface >= 0 && surface < surfaces.Count ? surfaces[surface] : surfaces[0];
            return data.overrideAudio ? data.audio : surfaces[0].audio;
        }

        private int FirstAfter(float time)
        {
            int low = 0, high = taps.Count;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (taps[mid].Time <= time) low = mid + 1;
                else high = mid;
            }
            return low;
        }
    }
}
