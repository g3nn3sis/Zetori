// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera;
using Zetori.Player;
using Zetori.Player.Exaggeration;
using Zetori.Replay.CameraPaths;
using Zetori.Replay.Cosmetics;
using Zetori.Replay.Highlights;
using Zetori.World;
using UnityEngine;

namespace Zetori.Replay.Playback
{
    internal sealed class ReplayPlayer : IDisposable
    {
        internal const float MinRigLerp = 0.01f;
        internal const float MaxRigLerp = 1f;
        internal const float NormalSpeed = 1f;

        private const float HighlightLeadIn = 3f;
        private const float HighlightRepeatWindow = 0.5f;
        private const float HighlightStep = 0.05f;

        internal static readonly float[] Speeds = { 0.25f, 0.5f, NormalSpeed, 2f };

        private readonly List<ReplayPuppet> puppets;
        private readonly LiveWorldMask mask = new LiveWorldMask();
        private readonly CosmeticLibrary cosmetics = new CosmeticLibrary();
        private readonly TimeManager.Snapshot? liveEnvironment;
        private EnvironmentState? appliedEnvironment;
        private int spectated;
        private float posedCursor;

        internal string Name { get; }
        internal ReplayRecording Recording { get; }
        internal IReadOnlyList<ReplayHighlight> Highlights { get; }
        internal CameraPath Path { get; }
        internal float PosedAt => posedCursor;
        internal float Cursor { get; private set; }
        internal bool Playing { get; set; } = true;
        internal CameraMode Camera { get; set; } = CameraMode.Spectator;
        internal float Speed { get; set; } = NormalSpeed;
        internal bool Rendering { get; set; }
        internal float RangeStart { get; private set; }
        internal float RangeEnd { get; private set; }
        internal bool Freecam => Camera == CameraMode.Freecam;
        internal IReadOnlyList<ReplayPuppet> Puppets => puppets;
        private float rigLerp;

        internal float RigLerp
        {
            get => rigLerp;
            set => rigLerp = Mathf.Clamp(value, MinRigLerp, MaxRigLerp);
        }

        internal ReplayPuppet? Spectated
        {
            get
            {
                ReplayPuppet? puppet = spectated >= 0 && spectated < puppets.Count ? puppets[spectated] : null;
                return puppet != null && puppet.Visible ? puppet : null;
            }
        }

        internal ReplayPlayer(LoadedReplay loaded)
        {
            Name = loaded.Name;
            Recording = loaded.Recording;
            Highlights = loaded.Highlights;
            Path = loaded.Path;
            RangeEnd = Recording.Duration;
            puppets = BuildPuppets(Recording, cosmetics);
            RigLerp = NetworkedPlayerManager.LiveRigLerp;
            liveEnvironment = TimeManager.Capture();
        }

        private static List<ReplayPuppet> BuildPuppets(ReplayRecording recording, CosmeticLibrary cosmetics)
        {
            var built = new List<ReplayPuppet>(recording.Actors.Count);
            foreach (ReplayActor actor in recording.Actors)
            {
                RigAppearance? appearance = RigAppearance.Resolve(actor.Skeleton, actor.Color);
                if (appearance == null)
                {
                    Log.Warning($"Replay: {actor.Name}'s body could not be rebuilt and is skipped.");
                    continue;
                }
                built.Add(new ReplayPuppet(actor, appearance, cosmetics));
            }
            return built;
        }

        internal void Tick(float deltaTime)
        {
            if (Playing && !Rendering)
            {
                Cursor += deltaTime * Speed;
                if (Cursor >= Recording.Duration)
                {
                    Cursor = Recording.Duration;
                    Playing = false;
                }
            }
            mask.Enforce();
            float step = Cursor - posedCursor;
            posedCursor = Cursor;
            bool audible = Playing && !Rendering;
            foreach (ReplayPuppet puppet in puppets)
            {
                puppet.Pose(Cursor, step, RigLerp, ArmExaggeration.Active && IsSpectating(puppet), Playing);
                puppet.UpdateVoice(Cursor, audible, Speed);
                puppet.PlayHandTaps(Cursor - step, Cursor, audible);
            }
            ApplyEnvironment();
            if (Spectated == null) SpectateNextVisible(1);
        }

        internal void Seek(float time) => Cursor = Mathf.Clamp(time, 0f, Recording.Duration);

        internal void MarkRangeStart() => RangeStart = Mathf.Min(Cursor, RangeEnd);

        internal void MarkRangeEnd() => RangeEnd = Mathf.Max(Cursor, RangeStart);

        internal void ResetRange()
        {
            RangeStart = 0f;
            RangeEnd = Recording.Duration;
        }

        internal bool HasRange => RangeStart > 0f || RangeEnd < Recording.Duration;

        internal void CycleSpeed()
        {
            int current = Array.IndexOf(Speeds, Speed);
            Speed = Speeds[(current + 1) % Speeds.Length];
        }

        internal void StepFrame(int direction)
        {
            Playing = false;
            float frame = Mathf.Round(Cursor / ReplayRecording.SampleInterval) + direction;
            Seek(frame * ReplayRecording.SampleInterval);
        }

        internal void TogglePlaying()
        {
            if (!Playing && Cursor >= Recording.Duration) Cursor = 0f;
            Playing = !Playing;
        }

        internal void JumpTo(ReplayHighlight highlight)
        {
            Seek(StartOf(highlight));
            int index = puppets.FindIndex(p => p.Actor.Id == highlight.ActorId);
            if (index >= 0) spectated = index;
        }

        internal void JumpToHighlight(int direction)
        {
            ReplayHighlight? target = direction > 0 ? NextHighlight() : PreviousHighlight();
            if (target is ReplayHighlight highlight) JumpTo(highlight);
        }

        private static float StartOf(ReplayHighlight highlight) => Mathf.Max(0f, highlight.Time - HighlightLeadIn);

        private ReplayHighlight? NextHighlight()
        {
            foreach (ReplayHighlight highlight in Highlights)
                if (StartOf(highlight) > Cursor + HighlightStep) return highlight;
            return null;
        }

        private ReplayHighlight? PreviousHighlight()
        {
            for (int i = Highlights.Count - 1; i >= 0; i--)
                if (StartOf(Highlights[i]) < Cursor - HighlightRepeatWindow) return Highlights[i];
            return null;
        }

        internal void AddKeyframe()
        {
            CameraWrapper? output = CameraManager.camera;
            if (output == null || !output.IsAlive) return;
            Path.Add(new CameraKeyframe(Cursor, output.Transform.position, output.Transform.rotation, output.Camera.fieldOfView));
            CameraPathFile.Save(Name, Path);
        }

        internal void RemoveKeyframe(int index)
        {
            Path.RemoveAt(index);
            CameraPathFile.Save(Name, Path);
            if (Path.IsEmpty && Camera == CameraMode.Path) Camera = CameraMode.Spectator;
        }

        internal void ClearPath()
        {
            Path.Clear();
            CameraPathFile.Save(Name, Path);
            if (Camera == CameraMode.Path) Camera = CameraMode.Spectator;
        }

        internal void Spectate(int index)
        {
            if (index < 0 || index >= puppets.Count) return;
            spectated = index;
            Camera = CameraMode.Spectator;
        }

        internal void SpectateLocal() => SpectateWhere(p => p.Actor.IsLocal);

        internal void SpectateOther(int otherIndex)
        {
            int seen = 0;
            SpectateWhere(p => p.IsOtherPresent && seen++ == otherIndex);
        }

        private void SpectateWhere(Func<ReplayPuppet, bool> match)
        {
            for (int i = 0; i < puppets.Count; i++)
            {
                if (!match(puppets[i])) continue;
                if (puppets[i].Visible) Spectate(i);
                return;
            }
        }

        internal void SpectateNext(int direction)
        {
            Camera = CameraMode.Spectator;
            SpectateNextVisible(direction);
        }

        private void SpectateNextVisible(int direction)
        {
            int count = puppets.Count;
            for (int step = 1; step <= count; step++)
            {
                int index = ((spectated + direction * step) % count + count) % count;
                if (!puppets[index].Visible) continue;
                spectated = index;
                return;
            }
        }

        internal bool IsSpectating(ReplayPuppet puppet) => Camera == CameraMode.Spectator && Spectated == puppet;

        private void ApplyEnvironment()
        {
            EnvironmentState? state = Recording.EnvironmentAt(Cursor);
            if (state is not EnvironmentState target) return;
            if (appliedEnvironment is EnvironmentState applied && applied.Equals(target)) return;
            appliedEnvironment = target;
            TimeManager.Apply(target.TimeIndex, target.Raining);
        }

        public void Dispose()
        {
            foreach (ReplayPuppet puppet in puppets) puppet.Dispose();
            puppets.Clear();
            cosmetics.Dispose();
            mask.Dispose();
            if (liveEnvironment != null) TimeManager.Restore(liveEnvironment);
        }
    }
}
