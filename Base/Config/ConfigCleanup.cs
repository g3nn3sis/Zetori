// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using System.Text;

namespace Zetori.Base.Config
{
    internal static class ConfigCleanup
    {
        internal static void RemoveUnusedSettings(HashSet<(string Section, string Key)> known)
        {
            if (!Directory.Exists(DataFolders.Configs)) return;
            foreach (string path in Directory.GetFiles(DataFolders.Configs, "*" + DataFolders.ConfigExtension))
            {
                try { RemoveUnusedSettings(path, known); }
                catch (Exception e) { Log.Exception(e, $"Removing unused settings from {Path.GetFileName(path)} failed"); }
            }
        }

        private static void RemoveUnusedSettings(string path, HashSet<(string Section, string Key)> known)
        {
            string text = File.ReadAllText(path);
            int unused = ConfigFormat.StoredKeys(text).Count(key => !known.Contains(key));
            if (unused == 0) return;
            File.WriteAllText(path, ConfigFormat.KeepKeys(text, known), new UTF8Encoding(false));
            Log.Info($"Removed {unused} unused {(unused == 1 ? "setting" : "settings")} from {Path.GetFileName(path)}.");
        }
    }
}
