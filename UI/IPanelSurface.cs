// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.UI
{
    internal interface IPanelSurface
    {
        void Label(string text);

        void Button(string text, Action? onClick, bool interactable = true);

        void Button(string text, Action? onClick, Action onRightClick, bool interactable = true);

        bool Toggle(string label, bool value, string on = "On", string off = "Off", bool interactable = true);

        float Slider(string label, float value, float min, float max, string format = "F2", string unit = "");

        string TextField(string text, int maxLength, Func<string, string>? normalize = null);

        void ColorSwatch(Color color);

        void HorizontalArea(Action draw);

        Vector2 ScrollArea(Vector2 scroll, float maximumHeight, int buttonRows, Action draw);
    }
}
