// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay
{
    internal static class HalfFloat
    {
        private static readonly float[] Values = DecodeAll();

        internal static float ToFloat(ushort half) => Values[half];

        private static float[] DecodeAll()
        {
            var values = new float[ushort.MaxValue + 1];
            for (int half = 0; half < values.Length; half++) values[half] = Mathf.HalfToFloat((ushort)half);
            return values;
        }
    }
}
