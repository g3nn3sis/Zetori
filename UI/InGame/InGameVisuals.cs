// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using TMPro;
using UnityEngine;
using Zetori.Game;
using Zetori.UI.Icons;

namespace Zetori.UI.InGame
{
    internal sealed class InGameVisuals
    {
        private const float TrackThickness = 0.006f;
        private const float KnobWidth = 0.007f;
        private const float KnobHeightRatio = 0.8f;
        private const float SwatchBorder = 0.0015f;
        private const float LightAccentLuminance = 0.6f;
        private const float HoverLift = 0.2f;
        private const float PressedShade = 0.35f;
        private const float FocusedTint = 0.35f;
        private const float DisabledActiveTint = 0.3f;
        private const float IconSize = 0.014f;
        private const float IconInset = 0.006f;
        private const float IconHeightLimit = 0.7f;

        private static readonly Color IdleColor = new Color(0.17f, 0.18f, 0.21f);
        private static readonly Color HoverColor = new Color(0.26f, 0.27f, 0.32f);
        private static readonly Color DisabledColor = new Color(0.09f, 0.09f, 0.11f);
        private static readonly Color FieldColor = new Color(0.07f, 0.07f, 0.09f);
        private static readonly Color TrackColor = new Color(0.1f, 0.1f, 0.12f);
        private static readonly Color KnobColor = new Color(0.92f, 0.92f, 0.95f);
        private static readonly Color ScrollTrackColor = new Color(0.07f, 0.07f, 0.09f);
        private static readonly Color ThumbColor = new Color(0.32f, 0.33f, 0.38f);
        private static readonly Color TextColor = Color.white;
        private static readonly Color DarkTextColor = new Color(0.06f, 0.06f, 0.08f);
        private static readonly Color LabelColor = new Color(0.82f, 0.83f, 0.88f);
        private static readonly Color DisabledTextColor = new Color(0.42f, 0.43f, 0.48f);
        private static readonly Color MenuColor = new Color(0.11f, 0.12f, 0.14f);

        private readonly InGameModel model;
        private readonly List<InGameVisual> pool = new List<InGameVisual>();
        private readonly List<InGameIcon> icons = new List<InGameIcon>();
        private readonly List<InGameImage> images = new List<InGameImage>();
        private readonly Dictionary<IconGlyph, Mesh?> iconMeshes = new Dictionary<IconGlyph, Mesh?>();
        private TMP_FontAsset? font;
        private int used;
        private int iconsUsed;
        private int imagesUsed;
        private Color accent;
        private Color accentText;

        internal InGameVisuals(InGameModel model) => this.model = model;

        internal void Render(InGameSurface surface, InGameLayout layout, InGamePointer left, InGamePointer right, Color32 accentColor)
        {
            SyncFont();
            accent = accentColor;
            accentText = Luminance(accent) > LightAccentLuminance ? DarkTextColor : TextColor;
            used = 0;
            iconsUsed = 0;
            imagesUsed = 0;
            DrawElements(surface, left, right, false, 0f);
            if (layout.Scrollable) DrawScrollbar(layout, left.Scrolling || right.Scrolling);
            if (layout.HasMenu)
            {
                DrawMenuBackdrop(layout);
                DrawElements(surface, left, right, true, InGameMetrics.MenuLift);
            }
            for (int i = 0; i < used; i++) pool[i].EndFrame();
            for (int i = used; i < pool.Count; i++) pool[i].Hide();
            for (int i = 0; i < iconsUsed; i++) icons[i].EndFrame();
            for (int i = iconsUsed; i < icons.Count; i++) icons[i].Hide();
            for (int i = 0; i < imagesUsed; i++) images[i].EndFrame();
            for (int i = imagesUsed; i < images.Count; i++) images[i].Hide();
        }

        private void DrawElements(InGameSurface surface, InGamePointer left, InGamePointer right, bool menu, float lift)
        {
            for (int i = 0; i < surface.Count; i++)
            {
                InGameElement element = surface[i];
                if (!element.Visible || (element.Region == InGameRegion.Menu) != menu) continue;
                bool pressed = left.Target == i || right.Target == i;
                bool hovered = left.Hovered == i || right.Hovered == i;
                Draw(element, pressed, hovered, lift);
            }
        }

        private void Draw(InGameElement element, bool pressed, bool hovered, float lift)
        {
            switch (element.Kind)
            {
                case InGameElementKind.Label:
                    DrawText(element, InGameTextStyle.Label, LabelColor, lift);
                    break;
                case InGameElementKind.Title:
                    DrawText(element, InGameTextStyle.Title, element.Color, lift);
                    break;
                case InGameElementKind.Block:
                    DrawText(element, InGameTextStyle.Block, LabelColor, lift);
                    break;
                case InGameElementKind.Image:
                    DrawImage(element, lift);
                    break;
                case InGameElementKind.Slider:
                    DrawSlider(element, pressed || hovered, lift);
                    break;
                case InGameElementKind.Swatch:
                    DrawSwatch(element, lift);
                    break;
                case InGameElementKind.Spacer:
                    break;
                default:
                    DrawControl(element, pressed, hovered, lift);
                    break;
            }
        }

        private void DrawText(InGameElement element, InGameTextStyle style, Color color, float lift)
        {
            Next().ShowText(element.Text, element.Center, element.Size, InGameMetrics.TextDepth - lift, style, color);
        }

        private void DrawImage(InGameElement element, float lift)
        {
            Texture? texture = element.Texture;
            if (texture != null) NextImage().Show(texture, element.Center, element.Size, InGameMetrics.QuadDepth - lift);
        }

        private void DrawControl(InGameElement element, bool pressed, bool hovered, float lift)
        {
            Color background = Background(element, pressed, hovered);
            bool onAccent = element.Active && !pressed && element.Kind != InGameElementKind.Field;
            Color text = !element.Interactable ? DisabledTextColor : onAccent ? accentText : TextColor;
            float textDepth = InGameMetrics.TextDepth - lift;
            InGameVisual visual = Next();
            visual.ShowQuad(element.Center, element.Size, InGameMetrics.QuadDepth - lift, background);
            Mesh? icon = element.Icon is IconGlyph glyph ? IconMesh(glyph) : null;
            if (icon == null)
            {
                visual.ShowText(element.Text, element.Center, element.Size, textDepth, InGameTextStyle.Control, text);
                return;
            }
            float size = Mathf.Min(IconSize, element.Size.y * IconHeightLimit);
            float left = element.Center.x - element.Size.x * 0.5f;
            float right = element.Center.x + element.Size.x * 0.5f;
            float textLeft = left + IconInset + size;
            NextIcon().Show(icon, new Vector2(left + IconInset + size * 0.5f, element.Center.y), size, textDepth, text);
            visual.ShowText(element.Text, new Vector2((textLeft + right) * 0.5f, element.Center.y), new Vector2(right - textLeft, element.Size.y), textDepth, InGameTextStyle.Control, text);
        }

        private void DrawMenuBackdrop(InGameLayout layout)
        {
            Next().ShowQuad(layout.MenuCenter, layout.MenuSize, InGameMetrics.MenuBorderDepth, accent);
            Vector2 inner = layout.MenuSize - Vector2.one * (InGameMetrics.MenuBorder * 2f);
            Next().ShowQuad(layout.MenuCenter, inner, InGameMetrics.MenuDepth, MenuColor);
        }

        private Mesh? IconMesh(IconGlyph glyph)
        {
            if (iconMeshes.TryGetValue(glyph, out Mesh? cached)) return cached;
            Mesh? mesh = IconLibrary.CreateMesh(glyph);
            if (mesh != null) model.Adopt(mesh);
            iconMeshes[glyph] = mesh;
            return mesh;
        }

        private Color Background(InGameElement element, bool pressed, bool hovered)
        {
            if (!element.Interactable) return element.Active ? Color.Lerp(DisabledColor, accent, DisabledActiveTint) : DisabledColor;
            if (pressed) return Color.Lerp(accent, Color.black, PressedShade);
            if (element.Kind == InGameElementKind.Field) return element.Active ? Color.Lerp(FieldColor, accent, FocusedTint) : hovered ? HoverColor : FieldColor;
            if (element.Active) return hovered ? Color.Lerp(accent, Color.white, HoverLift) : accent;
            return hovered ? HoverColor : IdleColor;
        }

        private void DrawSlider(InGameElement element, bool engaged, float lift)
        {
            Vector2 center = element.Center;
            float width = element.Size.x;
            float left = center.x - width * 0.5f;
            float fill = width * element.Fraction;
            Next().ShowQuad(center, new Vector2(width, TrackThickness), InGameMetrics.QuadDepth - lift, TrackColor);
            if (fill > 0f) Next().ShowQuad(new Vector2(left + fill * 0.5f, center.y), new Vector2(fill, TrackThickness), InGameMetrics.FillDepth - lift, accent);
            float knobX = Mathf.Clamp(left + fill, left + KnobWidth * 0.5f, left + width - KnobWidth * 0.5f);
            Color knob = engaged ? Color.Lerp(KnobColor, accent, HoverLift) : KnobColor;
            Next().ShowQuad(new Vector2(knobX, center.y), new Vector2(KnobWidth, element.Size.y * KnobHeightRatio), InGameMetrics.KnobDepth - lift, knob);
        }

        private void DrawSwatch(InGameElement element, float lift)
        {
            Next().ShowQuad(element.Center, element.Size, InGameMetrics.QuadDepth - lift, accent);
            Vector2 inner = element.Size - Vector2.one * (SwatchBorder * 2f);
            Next().ShowQuad(element.Center, inner, InGameMetrics.FillDepth - lift, element.Color);
        }

        private void DrawScrollbar(InGameLayout layout, bool dragging)
        {
            float x = layout.TrackX;
            float trackHeight = layout.ViewportHeight;
            Next().ShowQuad(new Vector2(x, layout.BodyBottom + trackHeight * 0.5f), new Vector2(InGameMetrics.ScrollbarWidth, trackHeight), InGameMetrics.QuadDepth, ScrollTrackColor);
            Next().ShowQuad(new Vector2(x, layout.ThumbCenterY), new Vector2(InGameMetrics.ScrollbarWidth, layout.ThumbHeight), InGameMetrics.FillDepth, dragging ? accent : ThumbColor);
        }

        private InGameVisual Next()
        {
            if (used == pool.Count)
            {
                var visual = new InGameVisual(model.Content, model.Quad, model.CreateMaterial(IdleColor), font);
                model.Register(visual.QuadRenderer);
                model.Register(visual.TextRenderer);
                pool.Add(visual);
            }
            InGameVisual next = pool[used++];
            next.BeginFrame();
            return next;
        }

        private InGameIcon NextIcon()
        {
            if (iconsUsed == icons.Count)
            {
                var icon = new InGameIcon(model.Content, model.CreateMaterial(TextColor));
                model.Register(icon.Renderer);
                icons.Add(icon);
            }
            InGameIcon next = icons[iconsUsed++];
            next.BeginFrame();
            return next;
        }

        private InGameImage NextImage()
        {
            if (imagesUsed == images.Count)
            {
                var image = new InGameImage(model.Content, model.Quad, model.CreateMaterial(Color.white));
                model.Register(image.Renderer);
                images.Add(image);
            }
            InGameImage next = images[imagesUsed++];
            next.BeginFrame();
            return next;
        }

        private void SyncFont()
        {
            TMP_FontAsset? current = HudAssets.CurrentFont;
            if (current == null || current == font) return;
            font = current;
            foreach (InGameVisual visual in pool) visual.SetFont(current);
        }

        private static float Luminance(Color color) => 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
    }
}
