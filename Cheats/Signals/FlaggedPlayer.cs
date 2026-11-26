// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;

namespace Zetori.Cheats.Signals
{
    internal sealed class FlaggedPlayer
    {
        private const char Separator = '\t';
        private const int FieldCount = 4;

        internal string UserId { get; }
        internal string Name { get; set; }
        internal SignalKind Signals { get; set; }
        internal DateTime LastFlagged { get; set; }

        internal FlaggedPlayer(string userId, string name, SignalKind signals, DateTime lastFlagged)
        {
            UserId = userId;
            Name = name;
            Signals = signals;
            LastFlagged = lastFlagged;
        }

        internal string Format() => string.Join(Separator.ToString(), new[]
        {
            UserId,
            Name.Replace(Separator, ' '),
            Signals.ToString(),
            LastFlagged.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
        });

        internal static FlaggedPlayer? Parse(string line)
        {
            string[] fields = line.Split(Separator);
            if (fields.Length != FieldCount || fields[0].Length == 0) return null;
            SignalKind signals = ParseSignals(fields[2]);
            if (signals == SignalKind.None) return null;
            if (!DateTime.TryParse(fields[3], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime flagged)) return null;
            return new FlaggedPlayer(fields[0], fields[1], signals, flagged.ToLocalTime());
        }

        private static SignalKind ParseSignals(string field)
        {
            SignalKind signals = SignalKind.None;
            foreach (string name in field.Split(','))
                if (Enum.TryParse(name.Trim(), out SignalKind kind)) signals |= kind;
            return signals;
        }
    }
}
