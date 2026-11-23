// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI.Icons
{
    internal sealed class IconGeometry
    {
        internal float[] Vertices { get; }
        internal int[] Triangles { get; }
        internal int VertexCount => Vertices.Length / 2;

        internal IconGeometry(float[] vertices, int[] triangles)
        {
            Vertices = vertices;
            Triangles = triangles;
        }
    }
}
