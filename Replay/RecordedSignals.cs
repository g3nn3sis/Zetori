// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Player;

namespace Zetori.Replay
{
    internal static class RecordedSignals
    {
        private static readonly (RecordedSignal Signal, string Name)[] Names =
        {
            (RecordedSignal.Speed, "speed"),
            (RecordedSignal.Teleports, "teleports"),
            (RecordedSignal.Hovering, "hovering"),
            (RecordedSignal.ArmReach, "arm reach"),
            (RecordedSignal.TagReach, "tag reach"),
            (RecordedSignal.HandMotion, "inhuman hands"),
            (RecordedSignal.LowGravity, "low gravity"),
            (RecordedSignal.TagBurst, "tag burst"),
            (RecordedSignal.NetworkSpam, "network spam")
        };

        internal static RecordedSignal Of(SignalRecord? record)
        {
            if (record == null) return RecordedSignal.None;
            RecordedSignal signals = RecordedSignal.None;
            if (record.Fast) signals |= RecordedSignal.Speed;
            if (record.Teleporting) signals |= RecordedSignal.Teleports;
            if (record.Hovering) signals |= RecordedSignal.Hovering;
            if (record.LongArms) signals |= RecordedSignal.ArmReach;
            if (record.FarTagging) signals |= RecordedSignal.TagReach;
            if (record.InhumanHands) signals |= RecordedSignal.HandMotion;
            if (record.LowGravity) signals |= RecordedSignal.LowGravity;
            if (record.BurstTagging) signals |= RecordedSignal.TagBurst;
            if (record.Spamming) signals |= RecordedSignal.NetworkSpam;
            return signals;
        }

        internal static string Describe(RecordedSignal signals) =>
            string.Join(", ", Names.Where(entry => (signals & entry.Signal) != 0).Select(entry => entry.Name));
    }
}
