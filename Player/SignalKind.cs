// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Player
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
        TagReach = 1 << 9
    }
}
