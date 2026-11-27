// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using Zetori.Base;
using Zetori.Player;

namespace Zetori.History
{
    internal static class RoomBook
    {
        private const string SavedFileName = "SavedRooms.txt";
        private const int MaximumSaved = 20;

        private static List<string>? saved;

        private static string SavedPath => Path.Combine(DataFolders.Root, SavedFileName);

        internal static IReadOnlyList<string> Saved => saved ??= LoadSaved();

        internal static bool IsSaved(string code) => Saved.Contains(code, StringComparer.OrdinalIgnoreCase);

        internal static void Save(string code)
        {
            string room = LocalPlayerManager.NormalizeName(code);
            if (room.Length == 0 || IsSaved(room)) return;
            List<string> list = saved ??= LoadSaved();
            if (list.Count >= MaximumSaved) list.RemoveAt(list.Count - 1);
            list.Insert(0, room);
            WriteSaved(list);
        }

        internal static void Remove(string code)
        {
            List<string> list = saved ??= LoadSaved();
            if (list.RemoveAll(entry => string.Equals(entry, code, StringComparison.OrdinalIgnoreCase)) > 0) WriteSaved(list);
        }

        private static List<string> LoadSaved()
        {
            try
            {
                if (!File.Exists(SavedPath)) return new List<string>();
                return File.ReadAllLines(SavedPath)
                    .Select(LocalPlayerManager.NormalizeName)
                    .Where(code => code.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(MaximumSaved)
                    .ToList();
            }
            catch (Exception e)
            {
                Log.Exception(e, "Loading the saved rooms failed");
                return new List<string>();
            }
        }

        private static void WriteSaved(List<string> list)
        {
            try
            {
                Directory.CreateDirectory(DataFolders.Root);
                File.WriteAllLines(SavedPath, list);
            }
            catch (Exception e) { Log.Exception(e, "Saving the saved rooms failed"); }
        }
    }
}
