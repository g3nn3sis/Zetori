// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI.Icons
{
    internal static class IconRasterizer
    {
        private const float EdgeSoftness = 0.5f;

        internal static float[] Coverage(IconGraphic graphic, int size)
        {
            var coverage = new float[size * size];
            float scale = size / graphic.Extent;
            float offsetX = size * 0.5f - graphic.CenterX * scale;
            float offsetY = size * 0.5f - graphic.CenterY * scale;
            foreach (IconShape shape in graphic.Shapes)
            {
                var contours = new IconContour[shape.Contours.Count];
                for (int i = 0; i < contours.Length; i++) contours[i] = Transform(shape.Contours[i], scale, offsetX, offsetY);
                if (shape.Filled) Fill(coverage, size, contours);
                if (shape.Stroked) Stroke(coverage, size, contours, shape.StrokeWidth * scale * 0.5f);
            }
            return coverage;
        }

        private static IconContour Transform(IconContour contour, float scale, float offsetX, float offsetY)
        {
            var points = new float[contour.Points.Length];
            for (int i = 0; i < points.Length; i += 2)
            {
                points[i] = contour.Points[i] * scale + offsetX;
                points[i + 1] = contour.Points[i + 1] * scale + offsetY;
            }
            return new IconContour(points, contour.Closed);
        }

        private static void Fill(float[] coverage, int size, IconContour[] contours)
        {
            Bounds(contours, EdgeSoftness, size, out int minX, out int minY, out int maxX, out int maxY);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float distance = Distance(contours, px, py, true);
                    float alpha = Winding(contours, px, py) != 0 ? Clamp01(EdgeSoftness + distance) : Clamp01(EdgeSoftness - distance);
                    Blend(coverage, y * size + x, alpha);
                }
            }
        }

        private static void Stroke(float[] coverage, int size, IconContour[] contours, float halfWidth)
        {
            Bounds(contours, halfWidth + EdgeSoftness, size, out int minX, out int minY, out int maxX, out int maxY);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = Distance(contours, x + 0.5f, y + 0.5f, false);
                    Blend(coverage, y * size + x, Clamp01(halfWidth + EdgeSoftness - distance));
                }
            }
        }

        private static void Blend(float[] coverage, int index, float alpha)
        {
            if (alpha > coverage[index]) coverage[index] = alpha;
        }

        private static void Bounds(IconContour[] contours, float margin, int size, out int minX, out int minY, out int maxX, out int maxY)
        {
            float left = float.MaxValue;
            float top = float.MaxValue;
            float right = float.MinValue;
            float bottom = float.MinValue;
            foreach (IconContour contour in contours)
            {
                for (int i = 0; i < contour.Count; i++)
                {
                    left = Math.Min(left, contour.X(i));
                    right = Math.Max(right, contour.X(i));
                    top = Math.Min(top, contour.Y(i));
                    bottom = Math.Max(bottom, contour.Y(i));
                }
            }
            minX = Math.Max(0, (int)Math.Floor(left - margin));
            minY = Math.Max(0, (int)Math.Floor(top - margin));
            maxX = Math.Min(size - 1, (int)Math.Ceiling(right + margin));
            maxY = Math.Min(size - 1, (int)Math.Ceiling(bottom + margin));
        }

        private static float Distance(IconContour[] contours, float px, float py, bool closeAll)
        {
            float nearest = float.MaxValue;
            foreach (IconContour contour in contours)
            {
                int count = contour.Count;
                int segments = closeAll || contour.Closed ? count : count - 1;
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % count;
                    nearest = Math.Min(nearest, SegmentDistanceSquared(px, py, contour.X(i), contour.Y(i), contour.X(next), contour.Y(next)));
                }
            }
            return (float)Math.Sqrt(nearest);
        }

        private static float SegmentDistanceSquared(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float lengthSquared = dx * dx + dy * dy;
            float t = lengthSquared > 0f ? Clamp01(((px - ax) * dx + (py - ay) * dy) / lengthSquared) : 0f;
            float ex = px - (ax + dx * t);
            float ey = py - (ay + dy * t);
            return ex * ex + ey * ey;
        }

        private static int Winding(IconContour[] contours, float px, float py)
        {
            int winding = 0;
            foreach (IconContour contour in contours)
            {
                int count = contour.Count;
                for (int i = 0; i < count; i++)
                {
                    int next = (i + 1) % count;
                    float ax = contour.X(i);
                    float ay = contour.Y(i);
                    float bx = contour.X(next);
                    float by = contour.Y(next);
                    float side = (bx - ax) * (py - ay) - (px - ax) * (by - ay);
                    if (ay <= py)
                    {
                        if (by > py && side > 0f) winding++;
                    }
                    else if (by <= py && side < 0f) winding--;
                }
            }
            return winding;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
