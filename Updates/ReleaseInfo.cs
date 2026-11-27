// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Newtonsoft.Json.Linq;

namespace Zetori.Updates
{
    internal sealed class ReleaseInfo
    {
        private const string HashExtension = ".sha256";

        internal Version Version { get; }
        internal string Notes { get; }
        internal string AssetName { get; }
        internal string AssetUrl { get; }
        internal string? HashUrl { get; }

        private ReleaseInfo(Version version, string notes, string assetName, string assetUrl, string? hashUrl)
        {
            Version = version;
            Notes = notes;
            AssetName = assetName;
            AssetUrl = assetUrl;
            HashUrl = hashUrl;
        }

        internal static ReleaseInfo? Parse(string json)
        {
            JObject release = JObject.Parse(json);
            string tag = ((string?)release["tag_name"] ?? string.Empty).Trim().TrimStart('v', 'V');
            if (!Version.TryParse(tag, out Version version)) return null;
            JToken[] assets = (release["assets"] as JArray)?.ToArray() ?? Array.Empty<JToken>();
            JToken? plugin = assets.FirstOrDefault(asset => PluginFiles.VersionOf(NameOf(asset)) == version);
            string? assetUrl = (string?)plugin?["browser_download_url"];
            if (plugin == null || string.IsNullOrEmpty(assetUrl)) return null;
            string assetName = NameOf(plugin);
            JToken? hash = assets.FirstOrDefault(asset => string.Equals(NameOf(asset), assetName + HashExtension, StringComparison.OrdinalIgnoreCase))
                ?? assets.FirstOrDefault(asset => NameOf(asset).EndsWith(HashExtension, StringComparison.OrdinalIgnoreCase));
            return new ReleaseInfo(version, ((string?)release["body"] ?? string.Empty).Trim(), assetName, assetUrl!, (string?)hash?["browser_download_url"]);
        }

        private static string NameOf(JToken asset) => (string?)asset["name"] ?? string.Empty;
    }
}
