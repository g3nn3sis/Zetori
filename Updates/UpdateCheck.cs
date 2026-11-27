// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Diagnostics;
using Zetori.Base;
using Zetori.Base.Config;

namespace Zetori.Updates
{
    internal static class UpdateCheck
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/g3nn3sis/Zetori/releases/latest";
        private const string GitHubJson = "application/vnd.github+json";
        private const int TimeoutMilliseconds = 5000;

        private static readonly TimeSpan PanelWait = TimeSpan.FromSeconds(3);
        private static readonly Stopwatch elapsed = new Stopwatch();
        private static Task<ReleaseInfo?>? check;

        internal static bool Settled => check == null || check.IsCompleted || elapsed.Elapsed >= PanelWait;

        internal static ReleaseInfo? Offered
        {
            get
            {
                ReleaseInfo? newer = check is { Status: TaskStatus.RanToCompletion } ? check.Result : null;
                return newer != null && newer.Version.ToString() != CasterConfig.IgnoredUpdate.Value ? newer : null;
            }
        }

        internal static void Begin()
        {
            if (check != null) return;
            elapsed.Start();
            check = Task.Run(() => FetchNewer());
        }

        private static ReleaseInfo? FetchNewer()
        {
            try
            {
                ReleaseInfo? latest = ReleaseInfo.Parse(UpdateHttp.GetText(LatestReleaseUrl, GitHubJson, TimeoutMilliseconds));
                if (latest == null || latest.Version <= PluginFiles.Running) return null;
                Log.Info($"{PluginInfo.Name} {latest.Version} is available.");
                return latest;
            }
            catch (Exception e)
            {
                Log.Info($"Checking for updates failed: {e.Message}");
                return null;
            }
        }
    }
}
