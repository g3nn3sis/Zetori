// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Replay
{
    internal sealed class LoadedReplay
    {
        internal string Name { get; }
        internal ReplayRecording Recording { get; }
        internal IReadOnlyList<ReplayHighlight> Highlights { get; }
        internal CameraPath Path { get; }

        private LoadedReplay(string name, ReplayRecording recording, IReadOnlyList<ReplayHighlight> highlights, CameraPath path)
        {
            Name = name;
            Recording = recording;
            Highlights = highlights;
            Path = path;
        }

        internal static LoadedReplay Load(string name)
        {
            ReplayRecording recording = ReplayFile.Load(name);
            return new LoadedReplay(name, recording, HighlightFinder.Find(recording), CameraPathFile.Load(name));
        }
    }
}
