// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;
using Zetori.Shared;

namespace Zetori.Replay.CameraPaths
{
    internal sealed class CameraPath
    {
        private const float ReplaceWindow = 0.05f;
        private const float MinimumQuaternionLength = 1e-6f;

        private readonly List<CameraKeyframe> keyframes = new List<CameraKeyframe>();

        internal IReadOnlyList<CameraKeyframe> Keyframes => keyframes;
        internal bool IsEmpty => keyframes.Count == 0;

        internal void Add(CameraKeyframe keyframe)
        {
            keyframes.RemoveAll(existing => Mathf.Abs(existing.Time - keyframe.Time) <= ReplaceWindow);
            int index = keyframes.FindIndex(existing => existing.Time > keyframe.Time);
            keyframes.Insert(index < 0 ? keyframes.Count : index, keyframe);
        }

        internal void RemoveAt(int index)
        {
            if (index >= 0 && index < keyframes.Count) keyframes.RemoveAt(index);
        }

        internal void Clear() => keyframes.Clear();

        internal bool TryEvaluate(float time, out Pose pose, out float fieldOfView)
        {
            pose = default;
            fieldOfView = 0f;
            if (keyframes.Count == 0) return false;
            int last = keyframes.Count - 1;
            if (time <= keyframes[0].Time || last == 0) return Hold(keyframes[0], out pose, out fieldOfView);
            if (time >= keyframes[last].Time) return Hold(keyframes[last], out pose, out fieldOfView);
            int segment = keyframes.FindLastIndex(keyframe => keyframe.Time <= time);
            CameraKeyframe from = keyframes[segment];
            CameraKeyframe to = keyframes[segment + 1];
            float span = to.Time - from.Time;
            float progress = span > 0f ? (time - from.Time) / span : 0f;
            Vector4 placement = Hermite(Placement(from), Placement(to), PlacementSlope(segment) * span, PlacementSlope(segment + 1) * span, progress);
            pose = new Pose(placement, Rotate(segment, span, progress));
            fieldOfView = placement.w;
            return true;
        }

        private static bool Hold(CameraKeyframe keyframe, out Pose pose, out float fieldOfView)
        {
            pose = new Pose(keyframe.Position, keyframe.Rotation);
            fieldOfView = keyframe.FieldOfView;
            return true;
        }

        private static Vector4 Placement(CameraKeyframe keyframe) =>
            new Vector4(keyframe.Position.x, keyframe.Position.y, keyframe.Position.z, keyframe.FieldOfView);

        private Vector4 PlacementSlope(int index)
        {
            if (index <= 0 || index >= keyframes.Count - 1) return Vector4.zero;
            CameraKeyframe before = keyframes[index - 1], after = keyframes[index + 1];
            return (Placement(after) - Placement(before)) / (after.Time - before.Time);
        }

        private Quaternion Rotate(int segment, float span, float progress)
        {
            Quaternion reference = keyframes[segment].Rotation;
            Vector4 turned = Hermite(
                Aligned(segment, reference), Aligned(segment + 1, reference),
                RotationSlope(segment, reference) * span, RotationSlope(segment + 1, reference) * span, progress);
            if (turned.sqrMagnitude < MinimumQuaternionLength) return Quaternion.Slerp(reference, keyframes[segment + 1].Rotation, progress);
            turned.Normalize();
            return new Quaternion(turned.x, turned.y, turned.z, turned.w);
        }

        private Vector4 RotationSlope(int index, Quaternion reference)
        {
            if (index <= 0 || index >= keyframes.Count - 1) return Vector4.zero;
            return (Aligned(index + 1, reference) - Aligned(index - 1, reference)) / (keyframes[index + 1].Time - keyframes[index - 1].Time);
        }

        private Vector4 Aligned(int index, Quaternion reference)
        {
            Quaternion rotation = keyframes[index].Rotation;
            var value = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
            return Quaternion.Dot(rotation, reference) < 0f ? -value : value;
        }

        private static Vector4 Hermite(Vector4 from, Vector4 to, Vector4 fromSlope, Vector4 toSlope, float progress)
        {
            float squared = progress * progress;
            float cubed = squared * progress;
            return (2f * cubed - 3f * squared + 1f) * from
                + (cubed - 2f * squared + progress) * fromSlope
                + (3f * squared - 2f * cubed) * to
                + (cubed - squared) * toSlope;
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(keyframes.Count);
            foreach (CameraKeyframe keyframe in keyframes)
            {
                writer.Write(keyframe.Time);
                BinaryIO.Write(writer, keyframe.Position);
                writer.Write(keyframe.Rotation.x);
                writer.Write(keyframe.Rotation.y);
                writer.Write(keyframe.Rotation.z);
                writer.Write(keyframe.Rotation.w);
                writer.Write(keyframe.FieldOfView);
            }
        }

        internal static CameraPath Read(BinaryReader reader)
        {
            var path = new CameraPath();
            int count = BinaryIO.ReadCount(reader);
            for (int i = 0; i < count; i++)
            {
                float time = reader.ReadSingle();
                Vector3 position = BinaryIO.ReadVector3(reader);
                var rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                path.Add(new CameraKeyframe(time, position, rotation.normalized, reader.ReadSingle()));
            }
            return path;
        }
    }
}
