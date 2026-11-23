// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;

namespace Zetori.UI.Icons
{
    internal static class SvgPathData
    {
        private const char NoCommand = '\0';

        private sealed class Scanner
        {
            private readonly string text;
            private int position;

            internal Scanner(string text) => this.text = text;

            internal bool HasMore
            {
                get
                {
                    SkipSeparators();
                    return position < text.Length;
                }
            }

            internal bool TryReadCommand(out char command)
            {
                SkipSeparators();
                command = position < text.Length ? text[position] : NoCommand;
                if (!IsCommand(command)) return false;
                position++;
                return true;
            }

            internal float Number()
            {
                SkipSeparators();
                int start = position;
                if (position < text.Length && (text[position] == '-' || text[position] == '+')) position++;
                SkipDigits();
                if (position < text.Length && text[position] == '.')
                {
                    position++;
                    SkipDigits();
                }
                if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
                {
                    position++;
                    if (position < text.Length && (text[position] == '-' || text[position] == '+')) position++;
                    SkipDigits();
                }
                if (position == start) throw new FormatException($"Expected a number at {start} in path data.");
                return float.Parse(text.Substring(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            internal bool Flag()
            {
                SkipSeparators();
                char flag = position < text.Length ? text[position] : NoCommand;
                if (flag != '0' && flag != '1') throw new FormatException($"Expected an arc flag at {position} in path data.");
                position++;
                return flag == '1';
            }

            private void SkipSeparators()
            {
                while (position < text.Length && (char.IsWhiteSpace(text[position]) || text[position] == ',')) position++;
            }

            private void SkipDigits()
            {
                while (position < text.Length && char.IsDigit(text[position])) position++;
            }

            private static bool IsCommand(char value) => value != NoCommand && "MmLlHhVvCcSsQqTtAaZz".IndexOf(value) >= 0;
        }

        internal static void Trace(string data, IconPathBuilder path)
        {
            var scanner = new Scanner(data);
            char command = NoCommand;
            char previous = NoCommand;
            float controlX = 0f;
            float controlY = 0f;
            while (scanner.HasMore)
            {
                if (scanner.TryReadCommand(out char read)) command = read;
                else if (command == NoCommand) throw new FormatException("Expected a command in path data.");
                char kind = char.ToUpperInvariant(command);
                bool relative = command != kind;
                float originX = relative ? path.X : 0f;
                float originY = relative ? path.Y : 0f;
                switch (kind)
                {
                    case 'Z':
                        path.Close();
                        command = NoCommand;
                        break;
                    case 'M':
                        path.MoveTo(originX + scanner.Number(), originY + scanner.Number());
                        command = relative ? 'l' : 'L';
                        break;
                    case 'L':
                        path.LineTo(originX + scanner.Number(), originY + scanner.Number());
                        break;
                    case 'H':
                        path.LineTo(originX + scanner.Number(), path.Y);
                        break;
                    case 'V':
                        path.LineTo(path.X, originY + scanner.Number());
                        break;
                    case 'C':
                    {
                        float x1 = originX + scanner.Number();
                        float y1 = originY + scanner.Number();
                        controlX = originX + scanner.Number();
                        controlY = originY + scanner.Number();
                        path.CubicTo(x1, y1, controlX, controlY, originX + scanner.Number(), originY + scanner.Number());
                        break;
                    }
                    case 'S':
                    {
                        bool smooth = previous == 'C' || previous == 'S';
                        float x1 = smooth ? 2f * path.X - controlX : path.X;
                        float y1 = smooth ? 2f * path.Y - controlY : path.Y;
                        controlX = originX + scanner.Number();
                        controlY = originY + scanner.Number();
                        path.CubicTo(x1, y1, controlX, controlY, originX + scanner.Number(), originY + scanner.Number());
                        break;
                    }
                    case 'Q':
                        controlX = originX + scanner.Number();
                        controlY = originY + scanner.Number();
                        path.QuadraticTo(controlX, controlY, originX + scanner.Number(), originY + scanner.Number());
                        break;
                    case 'T':
                    {
                        bool smooth = previous == 'Q' || previous == 'T';
                        controlX = smooth ? 2f * path.X - controlX : path.X;
                        controlY = smooth ? 2f * path.Y - controlY : path.Y;
                        path.QuadraticTo(controlX, controlY, originX + scanner.Number(), originY + scanner.Number());
                        break;
                    }
                    case 'A':
                    {
                        float radiusX = scanner.Number();
                        float radiusY = scanner.Number();
                        float rotation = scanner.Number();
                        bool largeArc = scanner.Flag();
                        bool sweep = scanner.Flag();
                        path.ArcTo(radiusX, radiusY, rotation, largeArc, sweep, originX + scanner.Number(), originY + scanner.Number());
                        break;
                    }
                    default:
                        throw new FormatException($"Unsupported path command '{command}'.");
                }
                previous = kind;
            }
        }
    }
}
