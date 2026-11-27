// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using Zetori.Base;

namespace Zetori.Updates
{
    internal static class UpdateInstaller
    {
        private const string PartialSuffix = ".tmp";
        private const int TimeoutMilliseconds = 30000;

        internal static Task<bool> Install(ReleaseInfo release) => Task.Run(() => TryInstall(release));

        private static bool TryInstall(ReleaseInfo release)
        {
            string? folder = PluginFiles.Folder;
            if (folder == null)
            {
                Log.Warning($"Downloading {release.AssetName} failed: the plugin folder is unknown.");
                return false;
            }
            string target = Path.Combine(folder, release.AssetName);
            string partial = target + PartialSuffix;
            try
            {
                string expected = ExpectedHash(release) ?? throw new InvalidDataException($"The release publishes no SHA-256 hash for {release.AssetName}.");
                UpdateHttp.Download(release.AssetUrl, partial, TimeoutMilliseconds);
                if (!string.Equals(ReleaseHash.Of(partial), expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"{release.AssetName} does not match its published SHA-256 hash.");
                if (File.Exists(target)) File.Delete(target);
                File.Move(partial, target);
                Log.Info($"Downloaded {release.AssetName}. Restart the game to apply the update.");
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Downloading {release.AssetName} failed");
                DeletePartial(partial);
                return false;
            }
        }

        private static string? ExpectedHash(ReleaseInfo release)
        {
            if (release.HashUrl != null)
            {
                string? published = ReleaseHash.Find(UpdateHttp.GetText(release.HashUrl, UpdateHttp.BinaryContent, TimeoutMilliseconds), release.AssetName);
                if (published != null) return published;
            }
            return ReleaseHash.Find(release.Notes, release.AssetName);
        }

        private static void DeletePartial(string partial)
        {
            try
            {
                if (File.Exists(partial)) File.Delete(partial);
            }
            catch (Exception e) { Log.Exception(e, $"Deleting {Path.GetFileName(partial)} failed"); }
        }
    }
}
