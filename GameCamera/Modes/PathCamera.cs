// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Replay;
using UnityEngine;

namespace Zetori.GameCamera.Modes
{
    internal static class PathCamera
    {
        internal static void Run(CameraWrapper camera, ReplayPlayer replay)
        {
            if (!replay.Path.TryEvaluate(replay.PosedAt, out Pose pose, out float fieldOfView)) return;
            camera.SetPose(pose.position, pose.rotation);
            camera.Camera.fieldOfView = fieldOfView;
        }
    }
}
