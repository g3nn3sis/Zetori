// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using System.IO;
using UnityEngine;
using Zetori.Base;

namespace Zetori.GameCamera.Vr
{
    internal static class CameraSpots
    {
        private const string FileName = "Spots.txt";
        private const string NamePrefix = "Spot ";
        private const char Separator = '\t';
        private const int FieldCount = 8;

        private static List<(string Name, Pose Pose)>? spots;

        private static string FilePath => Path.Combine(DataFolders.Root, FileName);

        internal static IReadOnlyList<(string Name, Pose Pose)> All => spots ??= Load();

        internal static void Add(Pose pose)
        {
            List<(string Name, Pose Pose)> list = spots ??= Load();
            list.Add((NamePrefix + NextNumber(list).ToString(CultureInfo.InvariantCulture), pose));
            Save(list);
        }

        internal static void Remove(int index)
        {
            List<(string Name, Pose Pose)> list = spots ??= Load();
            if (index < 0 || index >= list.Count) return;
            list.RemoveAt(index);
            Save(list);
        }

        private static int NextNumber(List<(string Name, Pose Pose)> list)
        {
            int highest = 0;
            foreach ((string name, Pose _) in list)
                if (name.StartsWith(NamePrefix, StringComparison.Ordinal)
                    && int.TryParse(name.Substring(NamePrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                    highest = Math.Max(highest, number);
            return highest + 1;
        }

        private static List<(string Name, Pose Pose)> Load()
        {
            var list = new List<(string Name, Pose Pose)>();
            try
            {
                if (!File.Exists(FilePath)) return list;
                foreach (string line in File.ReadAllLines(FilePath))
                    if (TryParse(line, out string name, out Pose pose)) list.Add((name, pose));
            }
            catch (Exception e) { Log.Exception(e, "Loading the camera spots failed"); }
            return list;
        }

        private static bool TryParse(string line, out string name, out Pose pose)
        {
            name = string.Empty;
            pose = default;
            string[] fields = line.Split(Separator);
            if (fields.Length != FieldCount || fields[0].Length == 0) return false;
            var numbers = new float[FieldCount - 1];
            for (int i = 0; i < numbers.Length; i++)
                if (!float.TryParse(fields[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i])) return false;
            name = fields[0];
            var rotation = new Quaternion(numbers[3], numbers[4], numbers[5], numbers[6]);
            pose = new Pose(new Vector3(numbers[0], numbers[1], numbers[2]), Quaternion.Normalize(rotation));
            return true;
        }

        private static void Save(List<(string Name, Pose Pose)> list)
        {
            try
            {
                Directory.CreateDirectory(DataFolders.Root);
                File.WriteAllLines(FilePath, list.Select(spot => Format(spot.Name, spot.Pose)));
            }
            catch (Exception e) { Log.Exception(e, "Saving the camera spots failed"); }
        }

        private static string Format(string name, Pose pose)
        {
            Vector3 p = pose.position;
            Quaternion r = pose.rotation;
            return string.Join(Separator.ToString(), new[] { name }.Concat(new[] { p.x, p.y, p.z, r.x, r.y, r.z, r.w }.Select(Number)));
        }

        private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
