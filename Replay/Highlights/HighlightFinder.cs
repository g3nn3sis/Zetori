// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay.Highlights
{
    internal static class HighlightFinder
    {
        private const float CloseDistance = 1f;
        private const float EscapeDistance = 3f;
        private const float LongRunSeconds = 45f;
        private const float StreakGap = 10f;
        private const int StreakTags = 3;
        private const float TaggerReach = 3f;
        private const float CentimetresPerMetre = 100f;
        private const float NoRun = -1f;

        private sealed class Runner
        {
            internal int Cursor;
            internal float RunStart = NoRun;
            internal bool Close;
            internal float ClosestTime;
            internal float ClosestDistance;
        }

        internal static List<ReplayHighlight> Find(ReplayRecording recording)
        {
            var found = new List<ReplayHighlight>();
            FindChases(recording, found);
            FindStreaks(recording, found);
            FindSignals(recording, found);
            found.Sort((a, b) => a.Time.CompareTo(b.Time));
            return found;
        }

        private static void FindChases(ReplayRecording recording, List<ReplayHighlight> found)
        {
            List<ReplayActor> actors = recording.Actors;
            Runner[] runners = actors.Select(_ => new Runner()).ToArray();
            var present = new bool[actors.Count];
            var frames = new ActorFrame[actors.Count];
            int steps = Mathf.FloorToInt(recording.Duration / ReplayRecording.SampleInterval);
            for (int step = 0; step <= steps; step++)
            {
                float time = step * ReplayRecording.SampleInterval;
                int taggers = 0, untagged = 0;
                for (int i = 0; i < actors.Count; i++)
                {
                    present[i] = TryFollow(actors[i], ref runners[i].Cursor, time, out frames[i]);
                    if (!present[i]) continue;
                    if (frames[i].Tag == ReplayTagState.None) untagged++;
                    else taggers++;
                }
                bool outnumbered = taggers > 0 && taggers >= untagged;
                for (int i = 0; i < actors.Count; i++)
                {
                    bool running = present[i] && frames[i].Tag == ReplayTagState.None;
                    float nearest = running ? NearestTagger(frames, present, i) : float.PositiveInfinity;
                    Chase(runners[i], actors[i].Id, time, running, outnumbered, nearest, found);
                }
            }
            for (int i = 0; i < runners.Length; i++) EndRun(runners[i], actors[i].Id, recording.Duration, found);
        }

        private static void Chase(Runner runner, int actorId, float time, bool running, bool outnumbered, float nearestTagger, List<ReplayHighlight> found)
        {
            if (!running)
            {
                EndRun(runner, actorId, time, found);
                runner.Close = false;
                return;
            }
            if (!outnumbered) EndRun(runner, actorId, time, found);
            else if (runner.RunStart == NoRun) runner.RunStart = time;
            TrackEscape(runner, actorId, time, nearestTagger, found);
        }

        private static void TrackEscape(Runner runner, int actorId, float time, float distance, List<ReplayHighlight> found)
        {
            if (distance <= CloseDistance)
            {
                if (!runner.Close || distance < runner.ClosestDistance)
                {
                    runner.ClosestDistance = distance;
                    runner.ClosestTime = time;
                }
                runner.Close = true;
                return;
            }
            if (!runner.Close || distance < EscapeDistance) return;
            runner.Close = false;
            found.Add(new ReplayHighlight(runner.ClosestTime, HighlightKind.CloseEscape, actorId, Mathf.RoundToInt(runner.ClosestDistance * CentimetresPerMetre)));
        }

        private static void EndRun(Runner runner, int actorId, float time, List<ReplayHighlight> found)
        {
            if (runner.RunStart == NoRun) return;
            float length = time - runner.RunStart;
            if (length >= LongRunSeconds) found.Add(new ReplayHighlight(runner.RunStart, HighlightKind.LongRun, actorId, Mathf.RoundToInt(length)));
            runner.RunStart = NoRun;
        }

        private static float NearestTagger(ActorFrame[] frames, bool[] present, int runner)
        {
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < frames.Length; i++)
            {
                if (i == runner || !present[i] || frames[i].Tag == ReplayTagState.None) continue;
                nearest = Mathf.Min(nearest, Vector3.Distance(frames[i].Position, frames[runner].Position));
            }
            return nearest;
        }

        private static void FindStreaks(ReplayRecording recording, List<ReplayHighlight> found)
        {
            var tags = new Dictionary<int, List<float>>();
            foreach (ReplayEvent e in recording.Events)
            {
                if (e.Kind != ReplayEventKind.Tagged || TaggerOf(recording, e) is not int tagger) continue;
                if (!tags.TryGetValue(tagger, out List<float> times)) tags[tagger] = times = new List<float>();
                times.Add(e.Time);
            }
            foreach (KeyValuePair<int, List<float>> entry in tags) AddStreaks(entry.Key, entry.Value, found);
        }

        private static void AddStreaks(int tagger, List<float> times, List<ReplayHighlight> found)
        {
            times.Sort();
            int start = 0;
            for (int i = 1; i <= times.Count; i++)
            {
                if (i < times.Count && times[i] - times[i - 1] <= StreakGap) continue;
                int count = i - start;
                if (count >= StreakTags) found.Add(new ReplayHighlight(times[start], HighlightKind.TagStreak, tagger, count));
                start = i;
            }
        }

        private static int? TaggerOf(ReplayRecording recording, ReplayEvent tag)
        {
            ReplayActor? victim = recording.Actor(tag.ActorId);
            if (victim == null || !TryFrameAt(victim, tag.Time, out ActorFrame target)) return null;
            int? tagger = null;
            float nearest = TaggerReach;
            float before = tag.Time - ReplayRecording.SampleInterval;
            foreach (ReplayActor actor in recording.Actors)
            {
                if (actor.Id == tag.ActorId || !TryFrameAt(actor, before, out ActorFrame frame) || frame.Tag == ReplayTagState.None) continue;
                float distance = Vector3.Distance(frame.Position, target.Position);
                if (distance >= nearest) continue;
                nearest = distance;
                tagger = actor.Id;
            }
            return tagger;
        }

        private static void FindSignals(ReplayRecording recording, List<ReplayHighlight> found)
        {
            foreach (ReplayEvent e in recording.Events)
                if (e.Kind == ReplayEventKind.Signal) found.Add(new ReplayHighlight(e.Time, HighlightKind.Signal, e.ActorId, e.Value));
        }

        private static bool TryFollow(ReplayActor actor, ref int cursor, float time, out ActorFrame frame)
        {
            List<ActorFrame> frames = actor.Frames;
            frame = default;
            if (frames.Count == 0) return false;
            while (cursor + 1 < frames.Count && frames[cursor + 1].Time <= time) cursor++;
            frame = frames[cursor];
            return Mathf.Abs(frame.Time - time) <= ReplayRecording.SampleInterval;
        }

        private static bool TryFrameAt(ReplayActor actor, float time, out ActorFrame frame)
        {
            frame = default;
            if (!actor.TryFindSpan(time, ReplayRecording.SampleInterval, out int index, out _)) return false;
            frame = actor.Frames[index];
            return true;
        }
    }
}
