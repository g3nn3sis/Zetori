// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Replay
{
    [Flags]
    internal enum RecordedSignal
    {
        None = 0,
        Speed = 1 << 0,
        Teleports = 1 << 1,
        Hovering = 1 << 2,
        ArmReach = 1 << 3,
        TagReach = 1 << 4,
        HandMotion = 1 << 5,
        LowGravity = 1 << 6,
        TagBurst = 1 << 7,
        NetworkSpam = 1 << 8
    }
}
