// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI.Icons
{
    internal static class IconTessellator
    {
        private const int RoundSegments = 12;
        private const float Epsilon = 1e-7f;

        private sealed class Builder
        {
            private readonly List<float> vertices = new List<float>();
            private readonly List<int> triangles = new List<int>();

            internal float X(int vertex) => vertices[vertex * 2];

            internal float Y(int vertex) => vertices[vertex * 2 + 1];

            internal int Vertex(float x, float y)
            {
                vertices.Add(x);
                vertices.Add(y);
                return vertices.Count / 2 - 1;
            }

            internal void Triangle(int a, int b, int c)
            {
                float cross = Cross(X(a), Y(a), X(b), Y(b), X(c), Y(c));
                if (Math.Abs(cross) < Epsilon) return;
                triangles.Add(a);
                triangles.Add(cross < 0f ? b : c);
                triangles.Add(cross < 0f ? c : b);
            }

            internal IconGeometry Build() => new IconGeometry(vertices.ToArray(), triangles.ToArray());
        }

        internal static IconGeometry Tessellate(IconGraphic graphic)
        {
            var builder = new Builder();
            float scale = 1f / graphic.Extent;
            foreach (IconShape shape in graphic.Shapes)
            {
                foreach (IconContour contour in shape.Contours)
                {
                    List<float> points = Normalize(contour, graphic, scale);
                    if (shape.Filled) Fill(builder, points);
                    if (shape.Stroked) Stroke(builder, points, contour.Closed, shape.StrokeWidth * scale * 0.5f);
                }
            }
            return builder.Build();
        }

        private static List<float> Normalize(IconContour contour, IconGraphic graphic, float scale)
        {
            var points = new List<float>(contour.Points.Length);
            for (int i = 0; i < contour.Count; i++)
            {
                float x = (contour.X(i) - graphic.CenterX) * scale;
                float y = (graphic.CenterY - contour.Y(i)) * scale;
                int last = points.Count - 2;
                if (last >= 0 && Math.Abs(points[last] - x) < Epsilon && Math.Abs(points[last + 1] - y) < Epsilon) continue;
                points.Add(x);
                points.Add(y);
            }
            return points;
        }

        private static void Stroke(Builder builder, List<float> points, bool closed, float halfWidth)
        {
            int count = points.Count / 2;
            int segments = closed && count > 2 ? count : count - 1;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % count;
                Band(builder, points[i * 2], points[i * 2 + 1], points[next * 2], points[next * 2 + 1], halfWidth);
            }
            for (int i = 0; i < count; i++) Disc(builder, points[i * 2], points[i * 2 + 1], halfWidth);
        }

        private static void Band(Builder builder, float ax, float ay, float bx, float by, float halfWidth)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < Epsilon) return;
            float nx = -dy / length * halfWidth;
            float ny = dx / length * halfWidth;
            int a = builder.Vertex(ax + nx, ay + ny);
            int b = builder.Vertex(ax - nx, ay - ny);
            int c = builder.Vertex(bx - nx, by - ny);
            int d = builder.Vertex(bx + nx, by + ny);
            builder.Triangle(a, b, c);
            builder.Triangle(a, c, d);
        }

        private static void Disc(Builder builder, float x, float y, float radius)
        {
            int center = builder.Vertex(x, y);
            int first = center + 1;
            for (int i = 0; i < RoundSegments; i++)
            {
                double angle = 2.0 * Math.PI * i / RoundSegments;
                builder.Vertex(x + radius * (float)Math.Cos(angle), y + radius * (float)Math.Sin(angle));
            }
            for (int i = 0; i < RoundSegments; i++) builder.Triangle(center, first + i, first + (i + 1) % RoundSegments);
        }

        private static void Fill(Builder builder, List<float> points)
        {
            int count = points.Count / 2;
            if (count >= 2 && Math.Abs(points[0] - points[count * 2 - 2]) < Epsilon && Math.Abs(points[1] - points[count * 2 - 1]) < Epsilon) count--;
            if (count < 3) return;
            var remaining = new List<int>(count);
            for (int i = 0; i < count; i++) remaining.Add(builder.Vertex(points[i * 2], points[i * 2 + 1]));
            float orientation = SignedArea(builder, remaining) >= 0f ? 1f : -1f;
            while (remaining.Count > 3)
            {
                if (!ClipEar(builder, remaining, orientation)) DropFlattest(builder, remaining);
            }
            builder.Triangle(remaining[0], remaining[1], remaining[2]);
        }

        private static bool ClipEar(Builder builder, List<int> remaining, float orientation)
        {
            int count = remaining.Count;
            for (int i = 0; i < count; i++)
            {
                int previous = remaining[(i + count - 1) % count];
                int current = remaining[i];
                int next = remaining[(i + 1) % count];
                if (orientation * Cross(builder, previous, current, next) <= Epsilon) continue;
                if (ContainsOther(builder, remaining, previous, current, next, orientation)) continue;
                builder.Triangle(previous, current, next);
                remaining.RemoveAt(i);
                return true;
            }
            return false;
        }

        private static void DropFlattest(Builder builder, List<int> remaining)
        {
            int count = remaining.Count;
            int flattest = 0;
            float smallest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                float bend = Math.Abs(Cross(builder, remaining[(i + count - 1) % count], remaining[i], remaining[(i + 1) % count]));
                if (bend >= smallest) continue;
                smallest = bend;
                flattest = i;
            }
            remaining.RemoveAt(flattest);
        }

        private static bool ContainsOther(Builder builder, List<int> remaining, int a, int b, int c, float orientation)
        {
            foreach (int vertex in remaining)
            {
                if (vertex == a || vertex == b || vertex == c) continue;
                float px = builder.X(vertex);
                float py = builder.Y(vertex);
                if (orientation * Cross(builder.X(a), builder.Y(a), builder.X(b), builder.Y(b), px, py) >= 0f
                    && orientation * Cross(builder.X(b), builder.Y(b), builder.X(c), builder.Y(c), px, py) >= 0f
                    && orientation * Cross(builder.X(c), builder.Y(c), builder.X(a), builder.Y(a), px, py) >= 0f)
                    return true;
            }
            return false;
        }

        private static float SignedArea(Builder builder, List<int> polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                int a = polygon[i];
                int b = polygon[(i + 1) % polygon.Count];
                area += builder.X(a) * builder.Y(b) - builder.X(b) * builder.Y(a);
            }
            return area * 0.5f;
        }

        private static float Cross(Builder builder, int a, int b, int c) =>
            Cross(builder.X(a), builder.Y(a), builder.X(b), builder.Y(b), builder.X(c), builder.Y(c));

        private static float Cross(float ax, float ay, float bx, float by, float cx, float cy) => (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
    }
}
