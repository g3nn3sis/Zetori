// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Replay
{
    internal sealed class ReplayRecording
    {
        internal const float SampleInterval = 1f / 20f;

        internal List<ReplayActor> Actors { get; } = new List<ReplayActor>();
        internal List<ReplayEvent> Events { get; } = new List<ReplayEvent>();
        internal List<(float Time, EnvironmentState State)> Environment { get; } = new List<(float, EnvironmentState)>();
        internal float Duration { get; set; }

        internal ReplayActor? Actor(int id) => id >= 0 && id < Actors.Count ? Actors[id] : null;

        internal EnvironmentState? EnvironmentAt(float time)
        {
            EnvironmentState? state = null;
            foreach ((float at, EnvironmentState value) in Environment)
            {
                if (at > time) break;
                state = value;
            }
            return state;
        }

        internal string Describe(ReplayEvent e)
        {
            string name = Actor(e.ActorId)?.Name ?? "Player";
            return e.Kind switch
            {
                ReplayEventKind.Joined => $"{name} joined",
                ReplayEventKind.Left => $"{name} left",
                ReplayEventKind.Tagged => $"{name} TAGGED",
                ReplayEventKind.TimeOfDay => "Time of day changed",
                ReplayEventKind.Rain => e.Value != 0 ? "Rain started" : "Rain stopped",
                ReplayEventKind.Marker => $"Marker {e.Value}",
                _ => string.Empty
            };
        }

        internal ReplayRecording Slice(float from, float to)
        {
            var slice = new ReplayRecording { Duration = to - from };
            var ids = new Dictionary<int, int>();
            foreach (ReplayActor actor in Actors)
            {
                ReplayActor? part = actor.Slice(slice.Actors.Count, from, to);
                if (part == null) continue;
                ids[actor.Id] = part.Id;
                slice.Actors.Add(part);
            }
            if (EnvironmentAt(from) is EnvironmentState opening) slice.Environment.Add((0f, opening));
            foreach ((float at, EnvironmentState state) in Environment)
                if (at > from && at <= to) slice.Environment.Add((at - from, state));
            foreach (ReplayEvent e in Events)
            {
                if (e.Time < from || e.Time > to) continue;
                if (e.ActorId < 0) slice.Events.Add(new ReplayEvent(e.Time - from, e.Kind, e.ActorId, e.Value));
                else if (ids.TryGetValue(e.ActorId, out int id)) slice.Events.Add(new ReplayEvent(e.Time - from, e.Kind, id, e.Value));
            }
            return slice;
        }

        internal void ForgetBefore(float time)
        {
            foreach (ReplayActor actor in Actors) actor.ForgetBefore(time);
            Events.RemoveAll(e => e.Time < time);
            int current = Environment.FindLastIndex(entry => entry.Time <= time);
            if (current > 0) Environment.RemoveRange(0, current);
        }
    }
}
