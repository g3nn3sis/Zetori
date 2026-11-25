// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using System.IO;
using System.Text;

namespace Zetori.Replay
{
    internal static class CameraPathFile
    {
        private const string Extension = ".gcpath";
        private const uint Magic = 0x48544150;
        private const int FormatVersion = 1;

        internal static CameraPath Load(string name)
        {
            string path = PathOf(name);
            if (!File.Exists(path)) return new CameraPath();
            try
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(file, Encoding.UTF8);
                if (reader.ReadUInt32() != Magic || reader.ReadInt32() != FormatVersion) throw new InvalidDataException("Not a Zetori camera path.");
                return CameraPath.Read(reader);
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Loading the camera path of replay '{name}' failed");
                return new CameraPath();
            }
        }

        internal static void Save(string name, CameraPath cameraPath)
        {
            try
            {
                if (cameraPath.IsEmpty)
                {
                    Delete(name);
                    return;
                }
                Directory.CreateDirectory(DataFolders.Replays);
                using var file = new FileStream(PathOf(name), FileMode.Create, FileAccess.Write);
                using var writer = new BinaryWriter(file, Encoding.UTF8);
                writer.Write(Magic);
                writer.Write(FormatVersion);
                cameraPath.Write(writer);
            }
            catch (Exception e) { Log.Exception(e, $"Saving the camera path of replay '{name}' failed"); }
        }

        internal static void Rename(string name, string newName)
        {
            string source = PathOf(name);
            if (!File.Exists(source)) return;
            try { File.Move(source, PathOf(newName)); }
            catch (Exception e) { Log.Exception(e, $"Renaming the camera path of replay '{name}' failed"); }
        }

        internal static void Delete(string name)
        {
            string path = PathOf(name);
            if (!File.Exists(path)) return;
            try { File.Delete(path); }
            catch (Exception e) { Log.Exception(e, $"Deleting the camera path of replay '{name}' failed"); }
        }

        private static string PathOf(string name) => Path.Combine(DataFolders.Replays, name + Extension);
    }
}
