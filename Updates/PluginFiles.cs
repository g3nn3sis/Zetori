// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using System.Text.RegularExpressions;
using Zetori.Base;

namespace Zetori.Updates
{
    internal static class PluginFiles
    {
        private static readonly Regex VersionedName = new Regex($@"^{Regex.Escape(PluginInfo.Name)}-(\d+(?:\.\d+){{1,3}})\.dll$", RegexOptions.IgnoreCase);

        internal static Version Running { get; } = new Version(PluginInfo.Version);

        internal static string SearchPattern => PluginInfo.Name + "-*.dll";

        private static string RunningPath => typeof(PluginFiles).Assembly.Location;

        internal static string? Folder => string.IsNullOrEmpty(RunningPath) ? null : Path.GetDirectoryName(RunningPath);

        internal static Version? VersionOf(string fileName)
        {
            Match match = VersionedName.Match(fileName);
            return match.Success && Version.TryParse(match.Groups[1].Value, out Version version) ? version : null;
        }

        internal static bool IsRunning(string path) =>
            !string.IsNullOrEmpty(RunningPath) && string.Equals(Path.GetFullPath(path), Path.GetFullPath(RunningPath), StringComparison.OrdinalIgnoreCase);
    }
}
