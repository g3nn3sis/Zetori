// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;

namespace Zetori.Replay
{
    internal readonly struct HandTap
    {
        internal readonly float Time;
        internal readonly bool IsLeftHand;
        internal readonly int Surface;
        internal readonly float Volume;

        internal HandTap(float time, bool isLeftHand, int surface, float volume)
        {
            Time = time;
            IsLeftHand = isLeftHand;
            Surface = surface;
            Volume = volume;
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(Time);
            writer.Write(IsLeftHand);
            writer.Write(Surface);
            writer.Write(Volume);
        }

        internal static HandTap Read(BinaryReader reader) =>
            new HandTap(reader.ReadSingle(), reader.ReadBoolean(), reader.ReadInt32(), reader.ReadSingle());
    }
}
