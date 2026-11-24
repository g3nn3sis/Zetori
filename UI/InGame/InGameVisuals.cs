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
        private const float MinimumIconLuminance = 0.25f;
        private const float DimIconLift = 0.5f;
        private const float RuleThickness = 0.0008f;
        private const float RuleTint = 0.45f;
        private const float BrandIconRatio = 0.86f;
        private const float BrandGap = 0.005f;
        private const string BrandSpacing = "0.08em";
        private const string BrandDetailSize = "45%";
        private const string BrandDetailColor = "8C8F9E";
        private const float CardTint = 0.1f;
        private const float CardBarWidth = 0.0025f;
        private const float CardInset = 0.007f;
        private const float CardIconRatio = 0.48f;
        private const float CardGap = 0.005f;
        private const float CardTitleRise = 0.15f;
        private const float CardTitleRatio = 0.4f;
        private const float CardDetailDrop = 0.19f;
        private const float CardDetailRatio = 0.3f;
        private const float TileIconRatio = 0.42f;
        private const float TileIconRise = 0.15f;
        private const float TileLabelDrop = 0.26f;
        private const float TileLabelRatio = 0.32f;
        private const float TileStripHeight = 0.0016f;
        private const float TileStripTint = 0.35f;

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
        private Transform canvas;
        private int used;
        private int iconsUsed;
        private int imagesUsed;
        private Color accent;
        private Color accentText;
        private Color iconTint;
        private string brandName = string.Empty;
        private string brandDetail = string.Empty;
        private string brandMarkup = string.Empty;

        internal InGameVisuals(InGameModel model)
        {
            this.model = model;
            canvas = model.Content;
        }

        internal void Render(InGameSurface surface, InGameLayout layout, InGamePointer[] pointers, Color32 accentColor)
        {
            SyncFont();
            accent = accentColor;
            accentText = Luminance(accent) > LightAccentLuminance ? DarkTextColor : TextColor;
            iconTint = Luminance(accent) < MinimumIconLuminance ? Color.Lerp(accent, Color.white, DimIconLift) : accent;
            used = 0;
            iconsUsed = 0;
            imagesUsed = 0;
            canvas = model.Content;
            DrawRules(layout);
            DrawElements(surface, pointers, InGamePlane.Panel, false, 0f);
            if (layout.Scrollable) DrawScrollbar(layout, AnyScrolling(pointers));
            if (layout.HasMenu)
            {
                DrawMenuBackdrop(layout);
                DrawElements(surface, pointers, InGamePlane.Panel, true, InGameMetrics.MenuLift);
            }
            if (layout.HasKeyboard)
            {
                canvas = model.Keyboard;
                DrawKeyboardBackdrop(layout);
                DrawElements(surface, pointers, InGamePlane.Keyboard, false, 0f);
            }
            for (int i = 0; i < used; i++) pool[i].EndFrame();
            for (int i = used; i < pool.Count; i++) pool[i].Hide();
            for (int i = 0; i < iconsUsed; i++) icons[i].EndFrame();
            for (int i = iconsUsed; i < icons.Count; i++) icons[i].Hide();
            for (int i = 0; i < imagesUsed; i++) images[i].EndFrame();
            for (int i = imagesUsed; i < images.Count; i++) images[i].Hide();
        }

        private void DrawElements(InGameSurface surface, InGamePointer[] pointers, InGamePlane plane, bool menu, float lift)
        {
            for (int i = 0; i < surface.Count; i++)
            {
                InGameElement element = surface[i];
                if (!element.Visible || element.Plane != plane || (element.Region == InGameRegion.Menu) != menu) continue;
                Draw(element, AnyTargets(pointers, i), AnyHovers(pointers, i), lift);
            }
        }

        private static bool AnyTargets(InGamePointer[] pointers, int index)
        {
            foreach (InGamePointer pointer in pointers)
                if (pointer.Target == index) return true;
            return false;
        }

        private static bool AnyHovers(InGamePointer[] pointers, int index)
        {
            foreach (InGamePointer pointer in pointers)
                if (pointer.Hovered == index) return true;
            return false;
        }

        private static bool AnyScrolling(InGamePointer[] pointers)
        {
            foreach (InGamePointer pointer in pointers)
                if (pointer.Scrolling) return true;
            return false;
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
                case InGameElementKind.Brand:
                    DrawBrand(element, lift);
                    break;
                case InGameElementKind.Card:
                    DrawCard(element, pressed, hovered, lift);
                    break;
                case InGameElementKind.Tile:
                    DrawTile(element, pressed, hovered, lift);
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
            InGameVisual visual = Next();
            visual.ShowQuad(element.Center, element.Size, InGameMetrics.QuadDepth - lift, background);
            visual.ShowText(element.Text, element.Center, element.Size, InGameMetrics.TextDepth - lift, InGameTextStyle.Control, text);
        }

        private void DrawBrand(InGameElement element, float lift)
        {
            Vector2 center = element.Center;
            Vector2 size = element.Size;
            float left = center.x - size.x * 0.5f;
            float right = center.x + size.x * 0.5f;
            float depth = InGameMetrics.TextDepth - lift;
            float textLeft = left;
            if (Glyph(element) is Mesh icon)
            {
                float iconSize = size.y * BrandIconRatio;
                NextIcon().Show(icon, new Vector2(left + iconSize * 0.5f, center.y), iconSize, depth, iconTint);
                textLeft += iconSize + BrandGap;
            }
            var textCenter = new Vector2((textLeft + right) * 0.5f, center.y);
            Next().ShowText(BrandMarkup(element), textCenter, new Vector2(right - textLeft, size.y), depth, InGameTextStyle.Brand, TextColor);
        }

        private void DrawCard(InGameElement element, bool pressed, bool hovered, float lift)
        {
            Vector2 center = element.Center;
            Vector2 size = element.Size;
            float left = center.x - size.x * 0.5f;
            float right = center.x + size.x * 0.5f;
            float depth = InGameMetrics.TextDepth - lift;
            float textLeft = left + CardBarWidth + CardInset;
            if (Glyph(element) is Mesh icon)
            {
                float iconSize = size.y * CardIconRatio;
                NextIcon().Show(icon, new Vector2(textLeft + iconSize * 0.5f, center.y), iconSize, depth, pressed ? TextColor : iconTint);
                textLeft += iconSize + CardGap;
            }
            float textX = (textLeft + right) * 0.5f;
            float textWidth = right - textLeft;
            InGameVisual body = Next();
            body.ShowQuad(center, size, InGameMetrics.QuadDepth - lift, CardBackground(pressed, hovered));
            body.ShowText(element.Text, new Vector2(textX, center.y + size.y * CardTitleRise), new Vector2(textWidth, size.y * CardTitleRatio), depth, InGameTextStyle.Heading, TextColor);
            InGameVisual bar = Next();
            bar.ShowQuad(new Vector2(left + CardBarWidth * 0.5f, center.y), new Vector2(CardBarWidth, size.y), InGameMetrics.FillDepth - lift, accent);
            bar.ShowText(element.Detail, new Vector2(textX, center.y - size.y * CardDetailDrop), new Vector2(textWidth, size.y * CardDetailRatio), depth, InGameTextStyle.Caption, LabelColor);
        }

        private void DrawTile(InGameElement element, bool pressed, bool hovered, float lift)
        {
            Vector2 center = element.Center;
            Vector2 size = element.Size;
            float depth = InGameMetrics.TextDepth - lift;
            Color background = Background(element, pressed, hovered);
            Color strip = pressed || hovered ? accent : Color.Lerp(background, accent, TileStripTint);
            InGameVisual body = Next();
            body.ShowQuad(center, size, InGameMetrics.QuadDepth - lift, background);
            body.ShowText(element.Text, new Vector2(center.x, center.y - size.y * TileLabelDrop), new Vector2(size.x, size.y * TileLabelRatio), depth, InGameTextStyle.Control, TextColor);
            Next().ShowQuad(new Vector2(center.x, center.y - (size.y - TileStripHeight) * 0.5f), new Vector2(size.x, TileStripHeight), InGameMetrics.FillDepth - lift, strip);
            if (Glyph(element) is Mesh icon) NextIcon().Show(icon, new Vector2(center.x, center.y + size.y * TileIconRise), size.y * TileIconRatio, depth, pressed ? TextColor : iconTint);
        }

        private void DrawRules(InGameLayout layout)
        {
            Color rule = Color.Lerp(InGameModel.FaceColor, accent, RuleTint);
            float x = (InGameMetrics.Left + InGameMetrics.Right) * 0.5f;
            var size = new Vector2(InGameMetrics.Right - InGameMetrics.Left, RuleThickness);
            Next().ShowQuad(new Vector2(x, layout.BodyTop + InGameMetrics.RegionGap * 0.5f), size, InGameMetrics.QuadDepth, rule);
            Next().ShowQuad(new Vector2(x, layout.BodyBottom - InGameMetrics.RegionGap * 0.5f), size, InGameMetrics.QuadDepth, rule);
        }

        private string BrandMarkup(InGameElement element)
        {
            if (string.Equals(element.Text, brandName, StringComparison.Ordinal) && string.Equals(element.Detail, brandDetail, StringComparison.Ordinal)) return brandMarkup;
            brandName = element.Text;
            brandDetail = element.Detail;
            brandMarkup = $"<cspace={BrandSpacing}>{brandName}</cspace> <size={BrandDetailSize}><color=#{BrandDetailColor}>{brandDetail}</color></size>";
            return brandMarkup;
        }

        private Color CardBackground(bool pressed, bool hovered)
        {
            if (pressed) return Color.Lerp(accent, Color.black, PressedShade);
            return Color.Lerp(hovered ? HoverColor : IdleColor, accent, CardTint);
        }

        private Mesh? Glyph(InGameElement element) => element.Icon is IconGlyph glyph ? IconMesh(glyph) : null;

        private void DrawMenuBackdrop(InGameLayout layout)
        {
            Next().ShowQuad(layout.MenuCenter, layout.MenuSize, InGameMetrics.MenuBorderDepth, accent);
            Vector2 inner = layout.MenuSize - Vector2.one * (InGameMetrics.MenuBorder * 2f);
            Next().ShowQuad(layout.MenuCenter, inner, InGameMetrics.MenuDepth, MenuColor);
        }

        private void DrawKeyboardBackdrop(InGameLayout layout)
        {
            var center = new Vector2(0f, -layout.KeyboardHeight * 0.5f);
            var size = new Vector2(InGameMetrics.FaceHalfWidth * 2f, layout.KeyboardHeight);
            Next().ShowQuad(center, size + Vector2.one * (InGameMetrics.KeyboardBorder * 2f), InGameMetrics.KeyboardRimDepth, accent);
            Next().ShowQuad(center, size, InGameMetrics.FaceDepth, InGameModel.FaceColor);
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
            next.BeginFrame(canvas);
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
            next.BeginFrame(canvas);
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
            next.BeginFrame(canvas);
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
