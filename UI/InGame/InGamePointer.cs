// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.UI.InGame
{
    internal sealed class InGamePointer
    {
        internal const int Nothing = -1;

        private const float TouchDistance = 0.014f;
        private const float ReleaseDistance = 0.028f;
        private const float HoverDistance = 0.06f;
        private const float BehindLimit = 0.05f;
        private const float EdgeTolerance = 0.012f;
        private const float HoldSeconds = 0.6f;

        private bool armed = true;
        private bool scrolling;
        private bool clickOnRelease;
        private InGameElementKind targetKind;
        private float pressedAt;

        internal bool IsLeft { get; }
        internal int Target { get; private set; } = Nothing;
        internal int Hovered { get; private set; } = Nothing;
        internal bool Scrolling => scrolling;

        internal InGamePointer(bool isLeft) => IsLeft = isLeft;

        internal InGamePress Update(Vector3 local, bool blocked, InGameSurface surface, InGameLayout layout, float now)
        {
            var point = new Vector2(local.x, local.y);
            float distance = InGameMetrics.FaceDepth - local.z;
            bool inside = !blocked
                && Mathf.Abs(point.x) <= InGameMetrics.FaceHalfWidth + EdgeTolerance
                && Mathf.Abs(point.y) <= InGameMetrics.FaceHalfHeight + EdgeTolerance
                && distance > -BehindLimit;
            if (Target != Nothing && (Target >= surface.Count || surface[Target].Kind != targetKind)) Cancel();
            Hovered = !inside || distance > HoverDistance ? Nothing : Target != Nothing ? Target : Hit(surface, point);
            if (!inside || distance > ReleaseDistance)
            {
                InGamePress released = Release(surface);
                armed = !blocked && distance > 0f;
                return released;
            }
            if (scrolling)
            {
                layout.ScrollTo(point.y);
                return InGamePress.None;
            }
            if (Target != Nothing) return Continue(surface, surface[Target], point, now);
            if (!armed || distance > TouchDistance) return InGamePress.None;
            armed = false;
            return Begin(surface, layout, point, now);
        }

        internal void Reset()
        {
            Cancel();
            Hovered = Nothing;
        }

        private InGamePress Begin(InGameSurface surface, InGameLayout layout, Vector2 point, float now)
        {
            if (layout.OnScrollbar(point))
            {
                scrolling = true;
                layout.ScrollTo(point.y);
                return InGamePress.Tap;
            }
            int hit = Hit(surface, point);
            if (hit == Nothing) return InGamePress.None;
            InGameElement element = surface[hit];
            Target = hit;
            targetKind = element.Kind;
            pressedAt = now;
            switch (element.Kind)
            {
                case InGameElementKind.Field:
                    surface.QueueFocus(element);
                    break;
                case InGameElementKind.Slider:
                    surface.QueueSlide(element, Fraction(element, point));
                    break;
                case InGameElementKind.Button when element.HasAlternate:
                    clickOnRelease = true;
                    break;
                default:
                    surface.QueueClick(element);
                    break;
            }
            return InGamePress.Tap;
        }

        private InGamePress Continue(InGameSurface surface, InGameElement element, Vector2 point, float now)
        {
            if (element.Kind == InGameElementKind.Slider)
            {
                surface.QueueSlide(element, Fraction(element, point));
                return InGamePress.None;
            }
            if (!clickOnRelease || now - pressedAt < HoldSeconds) return InGamePress.None;
            clickOnRelease = false;
            surface.QueueHold(element);
            return InGamePress.Hold;
        }

        private InGamePress Release(InGameSurface surface)
        {
            InGamePress press = InGamePress.None;
            if (clickOnRelease && Target != Nothing)
            {
                surface.QueueClick(surface[Target]);
                press = InGamePress.Tap;
            }
            Cancel();
            return press;
        }

        private void Cancel()
        {
            Target = Nothing;
            scrolling = false;
            clickOnRelease = false;
        }

        private static int Hit(InGameSurface surface, Vector2 point)
        {
            for (int i = 0; i < surface.Count; i++)
            {
                InGameElement element = surface[i];
                if (element.Pressable && element.Contains(point)) return i;
            }
            return Nothing;
        }

        private static float Fraction(InGameElement element, Vector2 point) =>
            Mathf.Clamp01((point.x - (element.Center.x - element.Size.x * 0.5f)) / Mathf.Max(element.Size.x, Mathf.Epsilon));
    }
}
