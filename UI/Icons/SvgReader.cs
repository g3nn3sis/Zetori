// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using System.Xml;

namespace Zetori.UI.Icons
{
    internal static class SvgReader
    {
        private const string None = "none";
        private static readonly char[] ListSeparators = { ' ', ',', '\t', '\r', '\n' };

        private readonly struct Paint
        {
            internal readonly bool Filled;
            internal readonly bool Stroked;
            internal readonly float StrokeWidth;

            internal Paint(bool filled, bool stroked, float strokeWidth)
            {
                Filled = filled;
                Stroked = stroked;
                StrokeWidth = strokeWidth;
            }

            internal Paint Inherit(XmlElement element)
            {
                string fill = element.GetAttribute("fill");
                string stroke = element.GetAttribute("stroke");
                string width = element.GetAttribute("stroke-width");
                return new Paint(
                    fill.Length > 0 ? fill != None : Filled,
                    stroke.Length > 0 ? stroke != None : Stroked,
                    width.Length > 0 ? Parse(width) : StrokeWidth);
            }
        }

        internal static IconGraphic Read(string svg)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg.TrimStart('﻿'));
            XmlElement root = document.DocumentElement ?? throw new FormatException("The icon has no root element.");
            if (root.LocalName != "svg") throw new FormatException($"The icon root is <{root.LocalName}>, not <svg>.");
            float[] viewBox = Numbers(root.GetAttribute("viewBox"));
            if (viewBox.Length != 4 || viewBox[2] <= 0f || viewBox[3] <= 0f) throw new FormatException("The icon needs a positive viewBox.");
            var shapes = new List<IconShape>();
            Collect(root, new Paint(true, false, 1f).Inherit(root), shapes);
            return new IconGraphic(viewBox[0], viewBox[1], viewBox[2], viewBox[3], shapes);
        }

        private static void Collect(XmlElement parent, Paint inherited, List<IconShape> shapes)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (!(node is XmlElement element)) continue;
                Paint paint = inherited.Inherit(element);
                if (element.LocalName == "g")
                {
                    Collect(element, paint, shapes);
                    continue;
                }
                if (!paint.Filled && !paint.Stroked) continue;
                IReadOnlyList<IconContour>? contours = Trace(element);
                if (contours == null || contours.Count == 0) continue;
                shapes.Add(new IconShape(contours, paint.Filled, paint.Stroked ? paint.StrokeWidth : 0f));
            }
        }

        private static IReadOnlyList<IconContour>? Trace(XmlElement element)
        {
            var path = new IconPathBuilder();
            switch (element.LocalName)
            {
                case "path":
                    SvgPathData.Trace(element.GetAttribute("d"), path);
                    break;
                case "circle":
                {
                    float radius = Number(element, "r");
                    TraceEllipse(path, Number(element, "cx"), Number(element, "cy"), radius, radius);
                    break;
                }
                case "ellipse":
                    TraceEllipse(path, Number(element, "cx"), Number(element, "cy"), Number(element, "rx"), Number(element, "ry"));
                    break;
                case "rect":
                    TraceRect(element, path);
                    break;
                case "line":
                    path.MoveTo(Number(element, "x1"), Number(element, "y1"));
                    path.LineTo(Number(element, "x2"), Number(element, "y2"));
                    break;
                case "polyline":
                case "polygon":
                    TracePoints(Numbers(element.GetAttribute("points")), element.LocalName == "polygon", path);
                    break;
                default:
                    return null;
            }
            return path.Finish();
        }

        private static void TraceEllipse(IconPathBuilder path, float centerX, float centerY, float radiusX, float radiusY)
        {
            if (radiusX <= 0f || radiusY <= 0f) return;
            path.MoveTo(centerX + radiusX, centerY);
            path.ArcTo(radiusX, radiusY, 0f, false, true, centerX - radiusX, centerY);
            path.ArcTo(radiusX, radiusY, 0f, false, true, centerX + radiusX, centerY);
            path.Close();
        }

        private static void TraceRect(XmlElement element, IconPathBuilder path)
        {
            float x = Number(element, "x");
            float y = Number(element, "y");
            float width = Number(element, "width");
            float height = Number(element, "height");
            if (width <= 0f || height <= 0f) return;
            bool hasRadiusX = element.HasAttribute("rx");
            bool hasRadiusY = element.HasAttribute("ry");
            float rx = hasRadiusX ? Number(element, "rx") : hasRadiusY ? Number(element, "ry") : 0f;
            float ry = hasRadiusY ? Number(element, "ry") : rx;
            rx = Math.Min(Math.Max(0f, rx), width * 0.5f);
            ry = Math.Min(Math.Max(0f, ry), height * 0.5f);
            float right = x + width;
            float bottom = y + height;
            path.MoveTo(x + rx, y);
            path.LineTo(right - rx, y);
            path.ArcTo(rx, ry, 0f, false, true, right, y + ry);
            path.LineTo(right, bottom - ry);
            path.ArcTo(rx, ry, 0f, false, true, right - rx, bottom);
            path.LineTo(x + rx, bottom);
            path.ArcTo(rx, ry, 0f, false, true, x, bottom - ry);
            path.LineTo(x, y + ry);
            path.ArcTo(rx, ry, 0f, false, true, x + rx, y);
            path.Close();
        }

        private static void TracePoints(float[] points, bool closed, IconPathBuilder path)
        {
            if (points.Length < 4) return;
            path.MoveTo(points[0], points[1]);
            for (int i = 2; i + 1 < points.Length; i += 2) path.LineTo(points[i], points[i + 1]);
            if (closed) path.Close();
        }

        private static float Number(XmlElement element, string name)
        {
            string value = element.GetAttribute(name);
            return value.Length > 0 ? Parse(value) : 0f;
        }

        private static float[] Numbers(string value) =>
            value.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries).Select(Parse).ToArray();

        private static float Parse(string value) => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
