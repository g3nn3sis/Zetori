// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Game
{
    internal sealed class ModelPart
    {
        internal Vector3 Pivot { get; }
        internal Mesh Mesh { get; }

        internal ModelPart(Vector3 pivot, Mesh mesh)
        {
            Pivot = pivot;
            Mesh = mesh;
        }
    }
}
