// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base.Config;

namespace Zetori.UI
{
    internal static class StreamerMode
    {
        private const string HiddenCode = "Hidden";

        internal static bool HidesRoomCodes => CasterConfig.HideRoomCodes.Value;

        internal static string RoomCode(string code) => HidesRoomCodes ? HiddenCode : code;
    }
}
