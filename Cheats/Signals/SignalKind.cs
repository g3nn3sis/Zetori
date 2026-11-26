// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Cheats.Signals
{
    [Flags]
    internal enum SignalKind
    {
        None = 0,
        GameReport = 1 << 0,
        ModData = 1 << 1,
        Name = 1 << 2,
        QuestTweaks = 1 << 3,
        Cosmetics = 1 << 4,
        Speed = 1 << 5,
        Teleports = 1 << 6,
        Hovering = 1 << 7,
        ArmReach = 1 << 8,
        TagReach = 1 << 9,
        HandMotion = 1 << 10,
        NetworkSpam = 1 << 11,
        LowGravity = 1 << 12,
        TagBurst = 1 << 13,
        FlaggedBefore = 1 << 14,
        ThroughWalls = 1 << 15,
        OutOfBounds = 1 << 16,
        HeadSpin = 1 << 17,
        FakeTaps = 1 << 18,
        IdentitySpam = 1 << 19,
        ProjectileSpam = 1 << 20,
        MasterTheft = 1 << 21,
        PropertyTampering = 1 << 22,
        Timing = 1 << 23
    }
}
