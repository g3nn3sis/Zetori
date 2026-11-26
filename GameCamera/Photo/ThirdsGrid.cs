// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.Photo
{
    internal static class ThirdsGrid
    {
        private const float LineWidth = 2f;
        private const int Divisions = 3;

        private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.45f);

        internal static void Draw()
        {
            if (Event.current.type != EventType.Repaint) return;
            float width = Screen.width, height = Screen.height;
            Color previous = GUI.color;
            GUI.color = LineColor;
            for (int line = 1; line < Divisions; line++)
            {
                GUI.DrawTexture(new Rect(width * line / Divisions - LineWidth * 0.5f, 0f, LineWidth, height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, height * line / Divisions - LineWidth * 0.5f, width, LineWidth), Texture2D.whiteTexture);
            }
            GUI.color = previous;
        }
    }
}
