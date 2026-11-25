// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Player
{
    internal sealed class SignalRecord
    {
        internal const float FastSpeed = 14f;
        internal const float FastSeconds = 1.5f;
        internal const float HoverSeconds = 3f;
        internal const float VanillaReach = 1f;
        internal const float LongReach = 1.3f;
        internal const float LongReachSeconds = 1f;
        internal const float FarTagDistance = 4f;
        private const int FarTagsToFlag = 2;
        private const int TeleportsToFlag = 3;
        private const int UnownedChecksToFlag = 2;
        private const float TeleportDistance = 6f;
        private const float TeleportSpeed = 60f;
        private const float MinimumElapsed = 0.001f;

        private Vector3? lastSync;
        private float lastSyncTime;
        private float fastRun;
        private float hoverRun;
        private int unownedChecks;

        internal float Watched { get; private set; }
        internal float TopSpeed { get; private set; }
        internal float LongestFast { get; private set; }
        internal int Teleports { get; private set; }
        internal float LongestHover { get; private set; }
        internal float LongestReach { get; private set; }
        internal float ReachSeconds { get; private set; }
        internal int Tags { get; private set; }
        internal int FarTags { get; private set; }
        internal float LongestTag { get; private set; }
        internal int UnownedCosmetics { get; private set; } = -1;
        internal string[] ModKeys { get; private set; } = Array.Empty<string>();
        internal bool OddName { get; private set; }
        internal SystemProperties QuestOverrides { get; private set; }

        internal bool Fast => LongestFast >= FastSeconds;
        internal bool Teleporting => Teleports >= TeleportsToFlag;
        internal bool Hovering => LongestHover >= HoverSeconds;
        internal bool LongArms => ReachSeconds >= LongReachSeconds;
        internal bool FarTagging => FarTags >= FarTagsToFlag;
        internal bool Unowned => unownedChecks >= UnownedChecksToFlag;
        internal bool Modded => ModKeys.Length > 0;
        internal bool Tweaked => QuestOverrides != 0;

        internal int Count =>
            Flag(OddName) + Flag(Modded) + Flag(Tweaked) + Flag(Unowned) + Flag(Fast) + Flag(Teleporting) + Flag(Hovering) + Flag(LongArms) + Flag(FarTagging);

        internal void Observe(float deltaTime, float speed, bool hovering, float reach)
        {
            Watched += deltaTime;
            TopSpeed = Mathf.Max(TopSpeed, speed);
            fastRun = speed >= FastSpeed ? fastRun + deltaTime : 0f;
            LongestFast = Mathf.Max(LongestFast, fastRun);
            hoverRun = hovering ? hoverRun + deltaTime : 0f;
            LongestHover = Mathf.Max(LongestHover, hoverRun);
            LongestReach = Mathf.Max(LongestReach, reach);
            if (reach >= LongReach) ReachSeconds += deltaTime;
        }

        internal void ObservePosition(Vector3 synced, float now, float scale)
        {
            if (lastSync is Vector3 previous)
            {
                if (synced == previous) return;
                float distance = Vector3.Distance(previous, synced);
                float elapsed = Mathf.Max(MinimumElapsed, now - lastSyncTime);
                if (distance > TeleportDistance * scale && distance / elapsed > TeleportSpeed * scale) Teleports++;
            }
            lastSync = synced;
            lastSyncTime = now;
        }

        internal void ObserveTag(float distance)
        {
            Tags++;
            LongestTag = Mathf.Max(LongestTag, distance);
            if (distance >= FarTagDistance) FarTags++;
        }

        internal void ObserveCosmetics(int unowned)
        {
            UnownedCosmetics = unowned;
            unownedChecks = unowned > 0 ? unownedChecks + 1 : 0;
        }

        internal void ObserveProfile(string[] modKeys, bool oddName, SystemProperties questOverrides)
        {
            ModKeys = modKeys;
            OddName = oddName;
            QuestOverrides = questOverrides;
        }

        private static int Flag(bool raised) => raised ? 1 : 0;
    }
}
