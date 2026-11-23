// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI.Icons
{
    internal sealed class IconPathBuilder
    {
        private const float SegmentLength = 0.6f;
        private const int MaximumSegments = 96;
        private const float SamePointTolerance = 1e-4f;

        private readonly List<IconContour> contours = new List<IconContour>();
        private readonly List<float> points = new List<float>();
        private float startX;
        private float startY;

        internal float X { get; private set; }
        internal float Y { get; private set; }

        internal void MoveTo(float x, float y)
        {
            Flush(false);
            Add(x, y);
            startX = x;
            startY = y;
        }

        internal void LineTo(float x, float y)
        {
            Begin();
            Add(x, y);
        }

        internal void CubicTo(float x1, float y1, float x2, float y2, float x, float y)
        {
            Begin();
            float x0 = X;
            float y0 = Y;
            float hull = Distance(x0, y0, x1, y1) + Distance(x1, y1, x2, y2) + Distance(x2, y2, x, y);
            int segments = Segments((hull + Distance(x0, y0, x, y)) * 0.5f);
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float u = 1f - t;
                float a = u * u * u;
                float b = 3f * u * u * t;
                float c = 3f * u * t * t;
                float d = t * t * t;
                Add(a * x0 + b * x1 + c * x2 + d * x, a * y0 + b * y1 + c * y2 + d * y);
            }
        }

        internal void QuadraticTo(float x1, float y1, float x, float y)
        {
            Begin();
            float x0 = X;
            float y0 = Y;
            int segments = Segments((Distance(x0, y0, x1, y1) + Distance(x1, y1, x, y) + Distance(x0, y0, x, y)) * 0.5f);
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float u = 1f - t;
                Add(u * u * x0 + 2f * u * t * x1 + t * t * x, u * u * y0 + 2f * u * t * y1 + t * t * y);
            }
        }

        internal void ArcTo(float radiusX, float radiusY, float rotationDegrees, bool largeArc, bool sweep, float x, float y)
        {
            Begin();
            float x0 = X;
            float y0 = Y;
            if (Distance(x0, y0, x, y) < SamePointTolerance) return;
            double rx = Math.Abs(radiusX);
            double ry = Math.Abs(radiusY);
            if (rx < SamePointTolerance || ry < SamePointTolerance)
            {
                Add(x, y);
                return;
            }
            double rotation = rotationDegrees * Math.PI / 180.0;
            double cos = Math.Cos(rotation);
            double sin = Math.Sin(rotation);
            double halfX = (x0 - x) * 0.5;
            double halfY = (y0 - y) * 0.5;
            double px = cos * halfX + sin * halfY;
            double py = -sin * halfX + cos * halfY;
            double excess = px * px / (rx * rx) + py * py / (ry * ry);
            if (excess > 1.0)
            {
                rx *= Math.Sqrt(excess);
                ry *= Math.Sqrt(excess);
            }
            double numerator = rx * rx * ry * ry - rx * rx * py * py - ry * ry * px * px;
            double denominator = rx * rx * py * py + ry * ry * px * px;
            double factor = Math.Sqrt(Math.Max(0.0, numerator / denominator)) * (largeArc == sweep ? -1.0 : 1.0);
            double cx = factor * rx * py / ry;
            double cy = -factor * ry * px / rx;
            double centerX = cos * cx - sin * cy + (x0 + x) * 0.5;
            double centerY = sin * cx + cos * cy + (y0 + y) * 0.5;
            double start = Angle(1.0, 0.0, (px - cx) / rx, (py - cy) / ry);
            double span = Angle((px - cx) / rx, (py - cy) / ry, (-px - cx) / rx, (-py - cy) / ry);
            if (!sweep && span > 0.0) span -= 2.0 * Math.PI;
            else if (sweep && span < 0.0) span += 2.0 * Math.PI;
            int segments = Segments((float)(Math.Abs(span) * Math.Max(rx, ry)));
            for (int i = 1; i < segments; i++)
            {
                double angle = start + span * i / segments;
                double ex = rx * Math.Cos(angle);
                double ey = ry * Math.Sin(angle);
                Add((float)(centerX + cos * ex - sin * ey), (float)(centerY + sin * ex + cos * ey));
            }
            Add(x, y);
        }

        internal void Close()
        {
            Flush(true);
            X = startX;
            Y = startY;
        }

        internal IReadOnlyList<IconContour> Finish()
        {
            Flush(false);
            var finished = contours.ToArray();
            contours.Clear();
            return finished;
        }

        private void Begin()
        {
            if (points.Count > 0) return;
            startX = X;
            startY = Y;
            Add(X, Y);
        }

        private void Add(float x, float y)
        {
            points.Add(x);
            points.Add(y);
            X = x;
            Y = y;
        }

        private void Flush(bool closed)
        {
            int count = points.Count;
            if (closed && count >= 6 && Distance(points[0], points[1], points[count - 2], points[count - 1]) < SamePointTolerance)
                points.RemoveRange(count - 2, 2);
            if (points.Count >= 4) contours.Add(new IconContour(points.ToArray(), closed));
            points.Clear();
        }

        private static int Segments(float length) => Math.Max(1, Math.Min(MaximumSegments, (int)Math.Ceiling(length / SegmentLength)));

        private static float Distance(float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static double Angle(double ux, double uy, double vx, double vy) => Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
    }
}
