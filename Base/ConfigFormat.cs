// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using System.Text;

namespace Zetori.Base
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
            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string content = line.Trim();
                if (content.Length == 0 || content[0] == CommentMarker) continue;
                if (content[0] == SectionMarker && result.Length > 0) result.Append(Environment.NewLine);
                result.Append(content).Append(Environment.NewLine);
            }
            return result.ToString();
        }

        internal static HashSet<(string Section, string Key)> StoredKeys(string text)
        {
            var keys = new HashSet<(string Section, string Key)>();
            string section = string.Empty;
            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string content = line.Trim();
                if (content.Length == 0 || content[0] == CommentMarker) continue;
                if (content[0] == SectionMarker && content[content.Length - 1] == SectionEndMarker)
                {
                    section = content.Substring(1, content.Length - 2);
                    continue;
                }
                int separator = content.IndexOf(ValueSeparator);
                if (separator > 0) keys.Add((section, content.Substring(0, separator).Trim()));
            }
            return keys;
        }
    }
}
