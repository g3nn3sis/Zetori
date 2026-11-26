// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Cheats.Signals
{
    internal sealed class SignalRecord
    {
        internal const float FastSpeed = 14f;
        internal const float FastSeconds = 1.5f;
        internal const float HoverSeconds = 3f;
        internal const float VanillaReach = 1f;
        internal const float LongReach = 1.3f;
        internal const float LongReachSeconds = 1f;
        internal const float AirReach = 1.6f;
        internal const float AirReachSeconds = 1.5f;
        internal const float FarTagDistance = 4f;
        internal const float RoleMargin = 1.5f;
        private const float FastPushesToFlag = 5f;
        private const float PushFadeSeconds = 30f;
        private const float MaximumPushStep = 0.5f;
        private const int SkippedPushUpdates = 1;
        private const int MeasuredPushUpdates = 2;
        private const int FarTagsToFlag = 2;
        private const int TeleportsToFlag = 3;
        private const int UnownedChecksToFlag = 2;
        private const int MaximumReasons = 8;
        private const float TeleportDistance = 6f;
        private const float TeleportSpeed = 60f;
        private const float MinimumElapsed = 0.001f;
        private const float AssistGrace = 3f;
        private const float ClearGrace = 0.3f;
        private const float WallFadeSeconds = 60f;
        private const float WallPassesToFlag = 3f;
        private const float UnderMapFadeSeconds = 30f;
        private const float UnderMapSecondsToFlag = 3f;
        private const float TapFadeSeconds = 30f;
        private const float FakeTapsToFlag = 8f;
        private const float ColorWindow = 2f;
        private const int ColorChangesPerBurst = 8;
        private const int ColorBurstsToFlag = 2;
        private const float NameFadeSeconds = 60f;
        private const float NameChangesToFlag = 4f;
        private const float ProjectileWindow = 3f;
        private const int ProjectilesPerBurst = 15;
        private const int ProjectileBurstsToFlag = 2;
        private const float TakeoverFadeSeconds = 600f;
        private const float TakeoversToFlag = 1f;
        private const float TamperFadeSeconds = 120f;
        private const float TamperingToFlag = 2f;

        private readonly List<string> reasons = new List<string>();
        private Vector3? lastSync;
        private float lastSyncTime;
        private float fastRun;
        private float hoverRun;
        private float lastTouched = float.NaN;
        private float lastPushCheck = float.NaN;
        private int? updatesSincePush;
        private float pushScore;
        private float assistedUntil = float.PositiveInfinity;
        private float obstructedUntil = float.PositiveInfinity;
        private int unownedChecks;
        private SignalKind announced;
        private string? lastName;

        internal SignalRecord(float now) => Created = now;

        internal float Created { get; }
        internal float Watched { get; private set; }
        internal float TopSpeed { get; private set; }
        internal float LongestFast { get; private set; }
        internal int FastPushes { get; private set; }
        internal float FastestPush { get; private set; }
        internal float LimitAtFastest { get; private set; }
        internal bool FastPushing { get; private set; }
        internal int Teleports { get; private set; }
        internal float LongestHover { get; private set; }
        internal float LongestReach { get; private set; }
        internal float ReachSeconds { get; private set; }
        internal float LongestAirReach { get; private set; }
        internal float LongAirReachSeconds { get; private set; }
        internal int Tags { get; private set; }
        internal int FarTags { get; private set; }
        internal float LongestTag { get; private set; }
        internal int UnownedCosmetics { get; private set; } = -1;
        internal string[] ModKeys { get; private set; } = Array.Empty<string>();
        internal NameIssue Name { get; private set; }
        internal SystemProperties QuestOverrides { get; private set; }
        internal HandMotion Hands { get; } = new HandMotion();
        internal MessageRate Messages { get; } = new MessageRate();
        internal BodyFlight Flight { get; } = new BodyFlight();
        internal TagBurst TagBursts { get; } = new TagBurst();
        internal TalliedSignal WallPasses { get; } = new TalliedSignal(WallFadeSeconds, WallPassesToFlag);
        internal TalliedSignal UnderMap { get; } = new TalliedSignal(UnderMapFadeSeconds, UnderMapSecondsToFlag);
        internal HeadSpin Head { get; } = new HeadSpin();
        internal TalliedSignal FakeTaps { get; } = new TalliedSignal(TapFadeSeconds, FakeTapsToFlag);
        internal RateBurst ColorChanges { get; } = new RateBurst(ColorWindow, ColorChangesPerBurst, ColorBurstsToFlag);
        internal TalliedSignal NameChanges { get; } = new TalliedSignal(NameFadeSeconds, NameChangesToFlag);
        internal RateBurst Projectiles { get; } = new RateBurst(ProjectileWindow, ProjectilesPerBurst, ProjectileBurstsToFlag);
        internal TalliedSignal MasterTakeovers { get; } = new TalliedSignal(TakeoverFadeSeconds, TakeoversToFlag);
        internal TalliedSignal Tampering { get; } = new TalliedSignal(TamperFadeSeconds, TamperingToFlag);
        internal SyncTiming Timing { get; } = new SyncTiming();
        internal bool PastChecked { get; private set; }
        internal SignalKind PastSignals { get; private set; }
        internal DateTime PastFlagged { get; private set; }

        internal bool Fast => LongestFast >= FastSeconds || FastPushing;
        internal bool Teleporting => Teleports >= TeleportsToFlag;
        internal bool Hovering => LongestHover >= HoverSeconds;
        internal bool LongArms => ReachSeconds >= LongReachSeconds || LongAirReachSeconds >= AirReachSeconds;
        internal bool FarTagging => FarTags >= FarTagsToFlag;
        internal bool InhumanHands => Hands.Inhuman;
        internal bool Spamming => Messages.Spamming;
        internal bool LowGravity => Flight.Floating;
        internal bool BurstTagging => TagBursts.Bursting;
        internal bool FlaggedBefore => PastSignals != SignalKind.None;
        internal bool ThroughWalls => WallPasses.Raised;
        internal bool OutOfBounds => UnderMap.Raised;
        internal bool SpinningHead => Head.Spinning;
        internal bool FakeTapping => FakeTaps.Raised;
        internal bool IdentitySpam => ColorChanges.Bursting || NameChanges.Raised;
        internal bool ProjectileSpam => Projectiles.Bursting;
        internal bool TookMaster => MasterTakeovers.Raised;
        internal bool Tampered => Tampering.Raised;
        internal bool LagTricks => Timing.Tricking;
        internal Vector3? LastSync => lastSync;
        internal bool Unowned => unownedChecks >= UnownedChecksToFlag;
        internal bool Modded => ModKeys.Length > 0;
        internal bool Tweaked => QuestOverrides != 0;
        internal bool OddName => Name != NameIssue.None;
        internal bool Reported => reasons.Count > 0;
        internal IReadOnlyList<string> ReportReasons => reasons;

        internal SignalKind Raised =>
            Kind(Reported, SignalKind.GameReport) | Kind(Modded, SignalKind.ModData) | Kind(OddName, SignalKind.Name) | Kind(Tweaked, SignalKind.QuestTweaks)
            | Kind(Unowned, SignalKind.Cosmetics) | Kind(Fast, SignalKind.Speed) | Kind(Teleporting, SignalKind.Teleports) | Kind(Hovering, SignalKind.Hovering)
            | Kind(LongArms, SignalKind.ArmReach) | Kind(FarTagging, SignalKind.TagReach) | Kind(InhumanHands, SignalKind.HandMotion)
            | Kind(Spamming, SignalKind.NetworkSpam) | Kind(LowGravity, SignalKind.LowGravity) | Kind(BurstTagging, SignalKind.TagBurst)
            | Kind(FlaggedBefore, SignalKind.FlaggedBefore) | Kind(ThroughWalls, SignalKind.ThroughWalls) | Kind(OutOfBounds, SignalKind.OutOfBounds)
            | Kind(SpinningHead, SignalKind.HeadSpin) | Kind(FakeTapping, SignalKind.FakeTaps) | Kind(IdentitySpam, SignalKind.IdentitySpam)
            | Kind(ProjectileSpam, SignalKind.ProjectileSpam) | Kind(TookMaster, SignalKind.MasterTheft) | Kind(Tampered, SignalKind.PropertyTampering)
            | Kind(LagTricks, SignalKind.Timing);

        internal int Count => CountOf(Raised);

        internal void Observe(float deltaTime, float speed, bool hovering, float reach, float airReach)
        {
            Watched += deltaTime;
            TopSpeed = Mathf.Max(TopSpeed, speed);
            fastRun = speed >= FastSpeed ? fastRun + deltaTime : 0f;
            LongestFast = Mathf.Max(LongestFast, fastRun);
            hoverRun = hovering ? hoverRun + deltaTime : 0f;
            LongestHover = Mathf.Max(LongestHover, hoverRun);
            LongestReach = Mathf.Max(LongestReach, reach);
            if (reach >= LongReach) ReachSeconds += deltaTime;
            LongestAirReach = Mathf.Max(LongestAirReach, airReach);
            if (airReach >= AirReach) LongAirReachSeconds += deltaTime;
        }

        internal bool TakeTouch(bool grounded, float touchedAt)
        {
            bool touched = grounded || touchedAt != lastTouched;
            lastTouched = touchedAt;
            return touched;
        }

        internal void ObservePush(float now, float horizontalSpeed, float? limit, bool touched)
        {
            float step = float.IsNaN(lastPushCheck) ? 0f : Mathf.Clamp(now - lastPushCheck, 0f, MaximumPushStep);
            lastPushCheck = now;
            pushScore *= Mathf.Exp(-step / PushFadeSeconds);
            if (touched)
            {
                updatesSincePush = 0;
                return;
            }
            if (!(updatesSincePush is int updates)) return;
            updatesSincePush = ++updates;
            if (updates <= SkippedPushUpdates) return;
            if (!(limit is float allowed) || horizontalSpeed < allowed * RoleMargin)
            {
                updatesSincePush = null;
                return;
            }
            if (updates < SkippedPushUpdates + MeasuredPushUpdates) return;
            updatesSincePush = null;
            FastPushes++;
            pushScore += 1f;
            if (horizontalSpeed > FastestPush)
            {
                FastestPush = horizontalSpeed;
                LimitAtFastest = allowed;
            }
            if (pushScore >= FastPushesToFlag) FastPushing = true;
        }

        internal void ObserveSurroundings(float now, bool assisted, bool clear)
        {
            assistedUntil = assisted ? now + AssistGrace : Mathf.Min(assistedUntil, now);
            obstructedUntil = clear ? Mathf.Min(obstructedUntil, now) : now + ClearGrace;
        }

        internal bool AssistedAt(float now) => now < assistedUntil;

        internal void ObservePosition(Vector3 synced, float now, float scale, bool flaggedTeleport)
        {
            if (lastSync is Vector3 previous && !flaggedTeleport)
            {
                float distance = Vector3.Distance(previous, synced);
                float elapsed = Mathf.Max(MinimumElapsed, now - lastSyncTime);
                if (distance > TeleportDistance * scale && distance / elapsed > TeleportSpeed * scale) Teleports++;
            }
            lastSync = synced;
            lastSyncTime = now;
        }

        internal void ObserveHands(float time, Vector3 head, Vector3 leftHand, Vector3 rightHand) => Hands.Observe(time, head, leftHand, rightHand);

        internal void ObserveFlight(double serverTime, float height, bool touched, float now, float scale) =>
            Flight.Observe(serverTime, height, !touched && now >= assistedUntil && now >= obstructedUntil, scale);

        internal void ObserveTraffic(int rate, float spamRate) => Messages.Observe(rate, spamRate);

        internal void ObservePast(FlaggedPlayer? past)
        {
            PastChecked = true;
            if (past == null) return;
            PastSignals = past.Signals;
            PastFlagged = past.LastFlagged;
        }

        internal void ObserveTag(float time, float distance)
        {
            TagBursts.Observe(time);
            Tags++;
            LongestTag = Mathf.Max(LongestTag, distance);
            if (distance >= FarTagDistance) FarTags++;
        }

        internal void ObserveCosmetics(int unowned)
        {
            UnownedCosmetics = unowned;
            unownedChecks = unowned > 0 ? unownedChecks + 1 : 0;
        }

        internal void ObserveProfile(string[] modKeys, NameIssue name, SystemProperties questOverrides)
        {
            ModKeys = modKeys;
            Name = name;
            QuestOverrides = questOverrides;
        }

        internal void ObserveName(float now, string name)
        {
            if (lastName != null && name != lastName) NameChanges.Add(now, 1f);
            lastName = name;
        }

        internal void ObserveReport(string reason)
        {
            if (reasons.Count < MaximumReasons && !reasons.Contains(reason)) reasons.Add(reason);
        }

        internal SignalKind TakeNewlyRaised()
        {
            SignalKind raised = Raised & ~announced;
            announced |= raised;
            return raised;
        }

        private static SignalKind Kind(bool raised, SignalKind kind) => raised ? kind : SignalKind.None;

        private static int CountOf(SignalKind kinds)
        {
            int count = 0;
            for (int bits = (int)kinds; bits != 0; bits &= bits - 1) count++;
            return count;
        }
    }
}
