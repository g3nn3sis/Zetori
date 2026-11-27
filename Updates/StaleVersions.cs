// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using Zetori.Base;

namespace Zetori.Updates
{
    internal static class StaleVersions
    {
        internal static void Remove()
        {
            string? folder = PluginFiles.Folder;
            if (folder == null) return;
            string[] candidates;
            try { candidates = Directory.GetFiles(folder, PluginFiles.SearchPattern); }
            catch (Exception e)
            {
                Log.Exception(e, "Listing old versions failed");
                return;
            }
            foreach (string path in candidates) RemoveIfOlder(path);
        }

        private static void RemoveIfOlder(string path)
        {
            string name = Path.GetFileName(path);
            Version? version = PluginFiles.VersionOf(name);
            if (version == null || version >= PluginFiles.Running || PluginFiles.IsRunning(path)) return;
            try
            {
                File.Delete(path);
                Log.Info($"Removed old version {name}.");
            }
            catch (Exception e) { Log.Exception(e, $"Removing old version {name} failed"); }
        }
    }
}
