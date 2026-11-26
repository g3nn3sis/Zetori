// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using Zetori.Base;
using Zetori.Replay;

namespace Zetori.Player.Voice
{
    internal sealed class PlayerVolume : ObjectComponent
    {
        internal const float Minimum = 0f;
        internal const float Maximum = 2f;
        internal const float Normal = 1f;
        private const float RefreshInterval = 0.25f;

        private static readonly Dictionary<string, float> volumes = new Dictionary<string, float>();

        private float nextRefresh;

        internal static float Of(VRRig rig) => KeyOf(rig) is string key && volumes.TryGetValue(key, out float volume) ? volume : Normal;

        internal static void Set(VRRig rig, float volume)
        {
            if (rig.isOfflineVRRig || KeyOf(rig) is not string key) return;
            volume = Mathf.Clamp(volume, Minimum, Maximum);
            if (Mathf.Approximately(volume, Normal)) volumes.Remove(key);
            else volumes[key] = volume;
            Apply(rig, volume);
        }

        internal void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            try
            {
                foreach (VRRig rig in VRRigCache.ActiveRigs)
                    if (rig != null && !rig.isOfflineVRRig) Apply(rig, Of(rig));
            }
            catch (Exception e) { Log.ExceptionOnce("player-volume", e, "Applying player volumes failed"); }
        }

        private static void Apply(VRRig rig, float volume)
        {
            AudioSource? voice = RigAppearance.VoiceOf(rig);
            if (voice == null) return;
            VoiceGain? gain = voice.GetComponent<VoiceGain>();
            if (gain == null)
            {
                if (Mathf.Approximately(volume, Normal)) return;
                gain = voice.gameObject.AddComponent<VoiceGain>();
            }
            gain.Gain = volume;
        }

        private static string? KeyOf(VRRig rig)
        {
            NetPlayer? player = rig.Creator;
            if (player == null) return null;
            return string.IsNullOrEmpty(player.UserId) ? $"actor:{player.ActorNumber}" : player.UserId;
        }
    }
}
