// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Zetori.Updates
{
    internal static class ReleaseHash
    {
        private static readonly Regex Sha256Hex = new Regex(@"\b[0-9a-fA-F]{64}\b");

        internal static string? Find(string text, string fileName)
        {
            string? first = null;
            foreach (string line in text.Split('\n'))
            {
                Match match = Sha256Hex.Match(line);
                if (!match.Success) continue;
                if (line.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0) return match.Value;
                first ??= match.Value;
            }
            return first;
        }

        internal static string Of(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }
    }
}
