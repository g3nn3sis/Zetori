// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.UI.InGame
{
    internal sealed class InGameElement
    {
        internal int Index { get; private set; }
        internal InGameElementKind Kind { get; private set; }
        internal InGameRegion Region { get; private set; }
        internal int Group { get; private set; }
        internal float Weight { get; private set; }
        internal float Height { get; private set; }
        internal string Text { get; set; } = string.Empty;
        internal bool Interactable { get; private set; }
        internal bool Active { get; set; }
        internal bool HasAlternate { get; set; }
        internal float Fraction { get; set; }
        internal Color Color { get; set; }
        internal Vector2 Center { get; set; }
        internal Vector2 Size { get; set; }
        internal bool Visible { get; set; }

        internal bool Pressable => Interactable && Visible && Kind != InGameElementKind.Label && Kind != InGameElementKind.Title && Kind != InGameElementKind.Swatch && Kind != InGameElementKind.Spacer;

        internal void Reset(int index, InGameElementKind kind, InGameRegion region, int group, float weight, float height, string text, bool interactable)
        {
            Index = index;
            Kind = kind;
            Region = region;
            Group = group;
            Weight = weight;
            Height = height;
            Text = text;
            Interactable = interactable;
            Active = false;
            HasAlternate = false;
            Fraction = 0f;
            Color = Color.white;
            Visible = false;
        }

        internal bool Contains(Vector2 point) =>
            Mathf.Abs(point.x - Center.x) <= Size.x * 0.5f && Mathf.Abs(point.y - Center.y) <= Size.y * 0.5f;
    }
}
