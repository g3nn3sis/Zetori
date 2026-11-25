// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using UnityEngine;

namespace Zetori.Player
{
    internal sealed class MetPlayer
    {
        private const char Separator = '\t';
        private const int FieldCount = 7;

        internal string UserId { get; }
        internal string Name { get; set; }
        internal Color Color { get; set; }
        internal DateTime LastSeen { get; set; }
        internal string Room { get; set; }
        internal bool PrivateRoom { get; set; }

        internal MetPlayer(string userId, string name, Color color, DateTime lastSeen, string room, bool privateRoom)
        {
            UserId = userId;
            Name = name;
            Color = color;
            LastSeen = lastSeen;
            Room = room;
            PrivateRoom = privateRoom;
        }

        internal string Format() => string.Join(Separator.ToString(), new[]
        {
            UserId,
            Name.Replace(Separator, ' '),
            Number(Color.r),
            Number(Color.g),
            Number(Color.b),
            LastSeen.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            (PrivateRoom ? "1" : "0") + Room
        });

        internal static MetPlayer? Parse(string line)
        {
            string[] fields = line.Split(Separator);
            if (fields.Length != FieldCount || fields[0].Length == 0 || fields[6].Length == 0) return null;
            if (!TryNumber(fields[2], out float red) || !TryNumber(fields[3], out float green) || !TryNumber(fields[4], out float blue)) return null;
            if (!DateTime.TryParse(fields[5], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime seen)) return null;
            return new MetPlayer(fields[0], fields[1], new Color(red, green, blue), seen.ToLocalTime(), fields[6].Substring(1), fields[6][0] == '1');
        }

        private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool TryNumber(string text, out float value) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
