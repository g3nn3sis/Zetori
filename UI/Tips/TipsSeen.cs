// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using Zetori.Base;

namespace Zetori.UI.Tips
{
    internal static class TipsSeen
    {
        private const string InGamePanelOpenedLine = "InGamePanelOpened";

        private static bool loaded;
        private static bool inGamePanelOpened;

        internal static bool InGamePanelOpened
        {
            get
            {
                EnsureLoaded();
                return inGamePanelOpened;
            }
        }

        internal static void MarkInGamePanelOpened()
        {
            if (InGamePanelOpened) return;
            inGamePanelOpened = true;
            Save();
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(DataFolders.TipsSeen)) return;
                foreach (string line in File.ReadAllLines(DataFolders.TipsSeen))
                    if (line.Trim() == InGamePanelOpenedLine) inGamePanelOpened = true;
            }
            catch (Exception e) { Log.Exception(e, "Reading the tips already seen failed"); }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(DataFolders.Root);
                File.WriteAllLines(DataFolders.TipsSeen, inGamePanelOpened ? new[] { InGamePanelOpenedLine } : Array.Empty<string>());
            }
            catch (Exception e) { Log.Exception(e, "Saving the tips already seen failed"); }
        }
    }
}
