// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using System.Text;
using UnityEngine;

namespace Zetori.Shared.Assets
{
    internal static class ModelFile
    {
        private const string Magic = "ZMDL";
        private const int SupportedVersion = 1;

        internal static Dictionary<string, ModelPart> Read(byte[] data, string modelName)
        {
            using var reader = new BinaryReader(new MemoryStream(data), Encoding.ASCII);
            if (new string(reader.ReadChars(Magic.Length)) != Magic) throw new InvalidDataException($"The {modelName} model has an unknown format.");
            int version = reader.ReadInt32();
            if (version != SupportedVersion) throw new InvalidDataException($"The {modelName} model has unsupported version {version}.");
            int partCount = reader.ReadInt32();
            var parts = new Dictionary<string, ModelPart>(partCount);
            for (int i = 0; i < partCount; i++)
            {
                string name = reader.ReadString();
                parts[name] = ReadPart(reader, $"{modelName} {name}");
            }
            return parts;
        }

        private static ModelPart ReadPart(BinaryReader reader, string meshName)
        {
            Vector3 pivot = ReadVector3(reader);
            int vertexCount = reader.ReadInt32();
            int indexCount = reader.ReadInt32();
            var positions = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                positions[i] = ReadVector3(reader);
                normals[i] = ReadVector3(reader);
                uvs[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }
            var indices = new int[indexCount];
            for (int i = 0; i < indexCount; i++) indices[i] = reader.ReadInt32();
            var mesh = new Mesh { name = $"_Zetori {meshName}", vertices = positions, normals = normals, uv = uvs, triangles = indices };
            mesh.RecalculateBounds();
            return new ModelPart(pivot, mesh);
        }

        private static Vector3 ReadVector3(BinaryReader reader) =>
            new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
