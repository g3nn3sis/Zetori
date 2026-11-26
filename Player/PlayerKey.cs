// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Player
{
    internal static class PlayerKey
    {
        internal static string? Of(VRRig rig)
        {
            NetPlayer? player = rig.Creator;
            if (player == null) return null;
            return string.IsNullOrEmpty(player.UserId) ? $"actor:{player.ActorNumber}" : player.UserId;
        }
    }
}
