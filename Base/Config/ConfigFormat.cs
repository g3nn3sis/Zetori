// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using System.Text;

namespace Zetori.Base.Config
{
    internal static class ConfigFormat
    {
        private const char CommentMarker = '#';
        private const char SectionMarker = '[';
        private const char SectionEndMarker = ']';
        private const char ValueSeparator = '=';

        internal static string StripComments(string text)
        {
            var result = new StringBuilder(text.Length);
            foreach (string content in ContentLines(text))
            {
                if (content[0] == SectionMarker && result.Length > 0) result.Append(Environment.NewLine);
                result.Append(content).Append(Environment.NewLine);
            }
            return result.ToString();
        }

        internal static string KeepKeys(string text, HashSet<(string Section, string Key)> keep)
        {
            var result = new StringBuilder(text.Length);
            string section = string.Empty;
            string? pendingHeader = null;
            foreach (string content in ContentLines(text))
            {
                if (IsSection(content))
                {
                    section = SectionName(content);
                    pendingHeader = content;
                    continue;
                }
                if (!TryReadKey(content, out string key) || !keep.Contains((section, key))) continue;
                if (pendingHeader != null)
                {
                    if (result.Length > 0) result.Append(Environment.NewLine);
                    result.Append(pendingHeader).Append(Environment.NewLine);
                    pendingHeader = null;
                }
                result.Append(content).Append(Environment.NewLine);
            }
            return result.ToString();
        }

        internal static HashSet<(string Section, string Key)> StoredKeys(string text)
        {
            var keys = new HashSet<(string Section, string Key)>();
            string section = string.Empty;
            foreach (string content in ContentLines(text))
            {
                if (IsSection(content))
                {
                    section = SectionName(content);
                    continue;
                }
                if (TryReadKey(content, out string key)) keys.Add((section, key));
            }
            return keys;
        }

        private static IEnumerable<string> ContentLines(string text)
        {
            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string content = line.Trim();
                if (content.Length > 0 && content[0] != CommentMarker) yield return content;
            }
        }

        private static bool IsSection(string content) => content[0] == SectionMarker && content[content.Length - 1] == SectionEndMarker;

        private static string SectionName(string content) => content.Substring(1, content.Length - 2);

        private static bool TryReadKey(string content, out string key)
        {
            int separator = content.IndexOf(ValueSeparator);
            key = separator > 0 ? content.Substring(0, separator).Trim() : string.Empty;
            return separator > 0;
        }
    }
}
