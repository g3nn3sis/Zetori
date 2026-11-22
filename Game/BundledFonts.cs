// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Zetori.Game
{
    internal static class BundledFonts
    {
        private const string ResourcePrefix = "Zetori.Fonts.";
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 1024;

        private static readonly string[] FontExtensions = { ".ttf", ".otf" };

        internal static TMP_FontAsset[] Load()
        {
            string folder = DataFolders.Fonts;
            Directory.CreateDirectory(folder);
            ExtractEmbedded(folder);
            var fonts = new List<TMP_FontAsset>();
            foreach (string path in FontFiles(folder))
            {
                TMP_FontAsset? font = CreateFont(path);
                if (font != null) fonts.Add(font);
            }
            Log.Debug($"Loaded {fonts.Count} fonts from {folder}.");
            return fonts.ToArray();
        }

        private static void ExtractEmbedded(string folder)
        {
            foreach (string resource in typeof(BundledFonts).Assembly.GetManifestResourceNames())
            {
                if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal)) continue;
                byte[]? bytes = EmbeddedResources.Read(resource);
                if (bytes == null) continue;
                string target = Path.Combine(folder, resource.Substring(ResourcePrefix.Length));
                if (File.Exists(target) && new FileInfo(target).Length == bytes.Length) continue;
                try { File.WriteAllBytes(target, bytes); }
                catch (Exception e) { Log.Exception(e, $"Extracting font {target} failed"); }
            }
        }

        private static IEnumerable<string> FontFiles(string folder) =>
            Directory.GetFiles(folder)
                .Where(path => FontExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

        private static TMP_FontAsset? CreateFont(string path)
        {
            try
            {
                TMP_FontAsset? font = TMP_FontAsset.CreateFontAsset(path, 0, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize);
                if (font == null)
                {
                    Log.Warning($"Font {path} could not be read.");
                    return null;
                }
                font.name = Path.GetFileNameWithoutExtension(path);
                font.hideFlags = HideFlags.DontUnloadUnusedAsset;
                UnityEngine.Object.DontDestroyOnLoad(font);
                return font;
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Loading font {path} failed");
                return null;
            }
        }
    }
}
