// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Replay
{
    internal readonly struct ReplayHighlight
    {
        internal float Time { get; }
        internal HighlightKind Kind { get; }
        internal int ActorId { get; }
        internal int Value { get; }

        internal ReplayHighlight(float time, HighlightKind kind, int actorId, int value)
        {
            Time = time;
            Kind = kind;
            ActorId = actorId;
            Value = value;
        }
    }
}
