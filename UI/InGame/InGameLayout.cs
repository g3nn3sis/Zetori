// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.UI.InGame
{
    internal sealed class InGameLayout
    {
        private const float ScrollbarReach = 0.006f;
        private const float CullTolerance = 0.0005f;

        internal float BodyTop { get; private set; }
        internal float BodyBottom { get; private set; }
        internal float Scroll { get; private set; }
        internal float ContentHeight { get; private set; }

        internal float ViewportHeight => BodyTop - BodyBottom;
        internal bool Scrollable => ContentHeight > ViewportHeight + CullTolerance;
        internal float TrackX => InGameMetrics.Right - InGameMetrics.ScrollbarWidth * 0.5f;
        internal float ThumbHeight => Mathf.Max(InGameMetrics.MinimumThumbHeight, ViewportHeight * ViewportHeight / Mathf.Max(ContentHeight, ViewportHeight));
        internal float ThumbCenterY => BodyTop - ThumbHeight * 0.5f - (ViewportHeight - ThumbHeight) * ScrollFraction;

        private float MaximumScroll => Mathf.Max(0f, ContentHeight - ViewportHeight);
        private float ScrollFraction => MaximumScroll > 0f ? Scroll / MaximumScroll : 0f;

        internal void ResetScroll() => Scroll = 0f;

        internal bool OnScrollbar(Vector2 point) =>
            Scrollable
            && Mathf.Abs(point.x - TrackX) <= InGameMetrics.ScrollbarWidth * 0.5f + ScrollbarReach
            && point.y <= BodyTop && point.y >= BodyBottom;

        internal void ScrollTo(float pointerY)
        {
            float travel = ViewportHeight - ThumbHeight;
            if (travel <= 0f) return;
            float fraction = Mathf.Clamp01((BodyTop - ThumbHeight * 0.5f - pointerY) / travel);
            Scroll = fraction * MaximumScroll;
        }

        internal void Arrange(InGameSurface surface)
        {
            float headerHeight = Place(surface, InGameRegion.Header, InGameMetrics.Right, InGameMetrics.Top);
            float footerHeight = Place(surface, InGameRegion.Footer, InGameMetrics.Right, 0f);
            float lowerEdge = InGameMetrics.Bottom + footerHeight;
            Place(surface, InGameRegion.Footer, InGameMetrics.Right, lowerEdge);
            if (surface.Editing)
            {
                float keyboardHeight = Place(surface, InGameRegion.Keyboard, InGameMetrics.Right, 0f);
                lowerEdge += InGameMetrics.RegionGap + keyboardHeight;
                Place(surface, InGameRegion.Keyboard, InGameMetrics.Right, lowerEdge);
            }
            BodyTop = InGameMetrics.Top - headerHeight - InGameMetrics.RegionGap;
            BodyBottom = lowerEdge + InGameMetrics.RegionGap;
            ContentHeight = Place(surface, InGameRegion.Body, InGameMetrics.Right, 0f);
            float bodyRight = Scrollable ? InGameMetrics.Right - InGameMetrics.ScrollbarWidth - InGameMetrics.ScrollbarGap : InGameMetrics.Right;
            Scroll = Mathf.Clamp(Scroll, 0f, MaximumScroll);
            Place(surface, InGameRegion.Body, bodyRight, BodyTop + Scroll);
            if (KeepEditedFieldInView(surface)) Place(surface, InGameRegion.Body, bodyRight, BodyTop + Scroll);
            CullBody(surface);
        }

        private bool KeepEditedFieldInView(InGameSurface surface)
        {
            if (!surface.Editing || surface.EditingIndex >= surface.Count) return false;
            InGameElement field = surface[surface.EditingIndex];
            float top = field.Center.y + field.Size.y * 0.5f;
            float bottom = field.Center.y - field.Size.y * 0.5f;
            float shift = bottom < BodyBottom ? BodyBottom - bottom : top > BodyTop ? BodyTop - top : 0f;
            if (Mathf.Approximately(shift, 0f)) return false;
            Scroll = Mathf.Clamp(Scroll + shift, 0f, MaximumScroll);
            return true;
        }

        private void CullBody(InGameSurface surface)
        {
            for (int i = 0; i < surface.Count; i++)
            {
                InGameElement element = surface[i];
                if (element.Region != InGameRegion.Body || !element.Visible) continue;
                float half = element.Size.y * 0.5f;
                element.Visible = element.Center.y + half <= BodyTop + CullTolerance && element.Center.y - half >= BodyBottom - CullTolerance;
            }
        }

        private static float Place(InGameSurface surface, InGameRegion region, float right, float top)
        {
            float y = top;
            bool placedRow = false;
            int count = surface.Count;
            int start = 0;
            while (start < count)
            {
                InGameElement first = surface[start];
                if (first.Region != region)
                {
                    start++;
                    continue;
                }
                int end = RowEnd(surface, start, out float totalWeight, out float rowHeight);
                if (placedRow) y -= InGameMetrics.RowGap;
                placedRow = true;
                PlaceRow(surface, start, end, right, y - rowHeight * 0.5f, totalWeight);
                y -= rowHeight;
                start = end;
            }
            return top - y;
        }

        private static int RowEnd(InGameSurface surface, int start, out float totalWeight, out float rowHeight)
        {
            InGameElement first = surface[start];
            totalWeight = first.Weight;
            rowHeight = first.Height;
            int end = start + 1;
            while (end < surface.Count && surface[end].Region == first.Region && surface[end].Group == first.Group)
            {
                totalWeight += surface[end].Weight;
                rowHeight = Mathf.Max(rowHeight, surface[end].Height);
                end++;
            }
            return end;
        }

        private static void PlaceRow(InGameSurface surface, int start, int end, float right, float centerY, float totalWeight)
        {
            float available = right - InGameMetrics.Left - InGameMetrics.ColumnGap * (end - start - 1);
            float x = InGameMetrics.Left;
            for (int i = start; i < end; i++)
            {
                InGameElement element = surface[i];
                float width = totalWeight > 0f ? available * element.Weight / totalWeight : 0f;
                element.Center = new Vector2(x + width * 0.5f, centerY);
                element.Size = new Vector2(width, element.Height);
                element.Visible = element.Kind != InGameElementKind.Spacer;
                x += width + InGameMetrics.ColumnGap;
            }
        }
    }
}
