// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.Vr
{
    internal static class VrCameraDirector
    {
        private const int NoActor = -1;

        private static VRRig? subject;
        private static int subjectActor = NoActor;
        private static Pose? placement;

        internal static Pose? TabletPose { get; set; }

        internal static VRRig? Subject
        {
            get
            {
                if (subject == null) return null;
                if (subject.isOfflineVRRig) return subject;
                NetPlayer? player = subject.Creator;
                return player != null && player.InRoom && player.ActorNumber == subjectActor ? subject : null;
            }
        }

        internal static bool IsFilming(VRRig? rig) => rig != null && Subject == rig;

        internal static void Film(VRRig rig)
        {
            subject = rig;
            subjectActor = rig.Creator != null ? rig.Creator.ActorNumber : NoActor;
        }

        internal static void StopFilming()
        {
            subject = null;
            subjectActor = NoActor;
        }

        internal static void PlaceTablet(Pose pose) => placement = pose;

        internal static bool TryTakePlacement(out Pose pose)
        {
            pose = placement ?? default;
            bool requested = placement.HasValue;
            placement = null;
            return requested;
        }
    }
}
