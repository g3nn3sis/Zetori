// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Shared.Vr
{
    internal interface IGrabbable
    {
        Transform Root { get; }

        bool WithinGrabReach(Vector3 hand);
    }
}
