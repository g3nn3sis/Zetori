// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay.CameraPaths
{
    internal readonly struct CameraKeyframe
    {
        internal float Time { get; }
        internal Vector3 Position { get; }
        internal Quaternion Rotation { get; }
        internal float FieldOfView { get; }

        internal CameraKeyframe(float time, Vector3 position, Quaternion rotation, float fieldOfView)
        {
            Time = time;
            Position = position;
            Rotation = rotation;
            FieldOfView = fieldOfView;
        }
    }
}
