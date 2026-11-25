// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using System.Security.Cryptography;

namespace Zetori.Replay
{
    internal static class BundledFFmpeg
    {
        private const string ResourceName = "Zetori.FFmpeg.ffmpeg.exe";
        private const string FileName = "ffmpeg.exe";
        private const string PartialSuffix = ".tmp";

        internal static string Extract(string folder)
        {
            string path = Path.Combine(folder, FileName);
            byte[] embedded = EmbeddedBytes();
            if (Matches(path, embedded)) return path;
            Directory.CreateDirectory(folder);
            string partial = path + PartialSuffix;
            File.WriteAllBytes(partial, embedded);
            if (File.Exists(path)) File.Delete(path);
            File.Move(partial, path);
            return path;
        }

        private static byte[] EmbeddedBytes()
        {
            using Stream stream = typeof(BundledFFmpeg).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"The embedded {ResourceName} is missing.");
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            return copy.ToArray();
        }

        private static bool Matches(string path, byte[] embedded)
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length != embedded.Length) return false;
            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(File.ReadAllBytes(path)).SequenceEqual(sha.ComputeHash(embedded));
        }
    }
}
