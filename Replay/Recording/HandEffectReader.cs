// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using HarmonyLib;
using UnityEngine;

namespace Zetori.Replay.Recording
{
    internal static class HandEffectReader
    {
        private static readonly Type? ContextType = AccessTools.TypeByName("HandEffectContext");
        private static readonly AccessTools.FieldRef<object, AudioClip>? SoundField = FieldOf<AudioClip>("soundFX");
        private static readonly AccessTools.FieldRef<object, float>? VolumeField = FieldOf<float>("soundVolume");

        internal static bool Available => SoundField != null && VolumeField != null;

        internal static bool TryReadSound(object context, out float volume)
        {
            volume = 0f;
            if (SoundField == null || VolumeField == null || SoundField(context) == null) return false;
            volume = VolumeField(context);
            return true;
        }

        private static AccessTools.FieldRef<object, T>? FieldOf<T>(string name) =>
            ContextType != null && AccessTools.Field(ContextType, name) != null ? AccessTools.FieldRefAccess<T>(ContextType, name) : null;
    }
}
