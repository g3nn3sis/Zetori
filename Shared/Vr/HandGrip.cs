// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Shared.Vr
{
    internal sealed class HandGrip
    {
        private Vector3 holdPosition;
        private Quaternion holdRotation = Quaternion.identity;

        internal VrHand Left { get; } = new VrHand(true);
        internal VrHand Right { get; } = new VrHand(false);
        internal VrHand? Holder { get; private set; }

        internal void Sync(GorillaTagger tagger, ControllerInputPoller input)
        {
            Left.Sync(tagger.leftHandTransform, tagger.leftHandTriggerCollider.transform, input.leftGrab);
            Right.Sync(tagger.rightHandTransform, tagger.rightHandTriggerCollider.transform, input.rightGrab);
        }

        internal void Release() => Holder = null;

        internal VrHand? Hold(IGrabbable target)
        {
            VrHand? previous = Holder;
            if (Holder != null && !Holder.Gripping) HandOver(target, Holder);
            if (Right.GripStarted) TryHold(target, Right);
            if (Left.GripStarted) TryHold(target, Left);
            if (Holder != null) Carry(target, Holder);
            return Holder != previous ? Holder : null;
        }

        private void HandOver(IGrabbable target, VrHand released)
        {
            VrHand otherHand = released == Left ? Right : Left;
            Holder = null;
            if (otherHand.Gripping) TryHold(target, otherHand);
        }

        private void TryHold(IGrabbable target, VrHand hand)
        {
            if (Holder != null) return;
            Transform palm = hand.Palm;
            if (!target.WithinGrabReach(palm.position)) return;
            Quaternion inversePalm = Quaternion.Inverse(palm.rotation);
            holdPosition = inversePalm * (target.Root.position - palm.position);
            holdRotation = inversePalm * target.Root.rotation;
            Holder = hand;
        }

        private void Carry(IGrabbable target, VrHand holder)
        {
            Transform palm = holder.Palm;
            target.Root.SetPositionAndRotation(palm.position + palm.rotation * holdPosition, palm.rotation * holdRotation);
        }
    }
}
