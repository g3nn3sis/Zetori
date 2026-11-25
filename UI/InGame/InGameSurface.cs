// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using Zetori.UI.Icons;

namespace Zetori.UI.InGame
{
    internal sealed class InGameSurface : IPanelSurface
    {
        private const string Caret = "_";
        private const char MaskCharacter = '*';
        private const string EmptyField = "Tap to type";
        private const string StepDown = "-";
        private const string StepUp = "+";
        private const float SliderStep = 0.02f;
        private const float StepWeight = 1f;
        private const float TrackWeight = 5f;
        private const float DefaultWeight = 1f;
        private const int NotEditing = -1;
        private const int NoMenu = -1;

        private enum InteractionType
        {
            Click,
            Hold,
            Focus,
            Slide
        }

        private readonly struct Interaction
        {
            internal readonly InteractionType Type;
            internal readonly int Index;
            internal readonly InGameElementKind Kind;
            internal readonly string Text;
            internal readonly float Fraction;

            internal Interaction(InteractionType type, InGameElement element, float fraction)
            {
                Type = type;
                Index = element.Index;
                Kind = element.Kind;
                Text = element.Text;
                Fraction = fraction;
            }

            internal bool Targets(InteractionType type, InGameElement element) =>
                Type == type && Index == element.Index && Kind == element.Kind && (Kind != InGameElementKind.Toggle || Text == element.Text);
        }

        private readonly List<InGameElement> elements = new List<InGameElement>();
        private readonly List<Interaction> queued = new List<Interaction>();
        private readonly List<Interaction> pending = new List<Interaction>();
        private InGameRegion region;
        private int nextGroup;
        private int rowDepth;
        private int rowGroup;
        private float nextWeight = DefaultWeight;
        private bool editedThisFrame;
        private bool dismissMenu;
        private bool menuChosen;
        private string editText = string.Empty;
        private int editMaxLength;

        internal int Count { get; private set; }
        internal int EditingIndex { get; private set; } = NotEditing;
        internal bool Editing => EditingIndex != NotEditing;
        internal int MenuAnchor { get; private set; } = NoMenu;
        internal int MenuFirst { get; private set; }
        internal int MenuEnd { get; private set; }
        internal bool HasMenu => MenuAnchor != NoMenu;

        internal InGameElement this[int index] => elements[index];

        private float RegionWidth => region == InGameRegion.Body ? InGameMetrics.BodyContentWidth : InGameMetrics.Right - InGameMetrics.Left;

        internal void Begin()
        {
            Count = 0;
            nextGroup = 0;
            rowDepth = 0;
            nextWeight = DefaultWeight;
            region = InGameRegion.Body;
            editedThisFrame = false;
            pending.Clear();
            pending.AddRange(queued);
            queued.Clear();
            dismissMenu = HasMenu && PendingOutsideMenu();
            MenuAnchor = NoMenu;
            MenuFirst = 0;
            MenuEnd = 0;
        }

        internal void End()
        {
            if (Editing && !editedThisFrame) StopEditing();
        }

        internal void EnterRegion(InGameRegion value)
        {
            region = value;
            rowDepth = 0;
        }

        internal void Weighted(float weight) => nextWeight = weight;

        internal void Spacer() => Add(InGameElementKind.Spacer, string.Empty, 0f, false);

        internal void Title(string text) => Title(text, Color.white);

        internal void Title(string text, Color color) => Add(InGameElementKind.Title, text, InGameMetrics.TitleHeight, false).Color = color;

        internal void Image(Texture texture, float height) => Add(InGameElementKind.Image, string.Empty, height, false).Texture = texture;

        internal void Block(string text, float height) => Add(InGameElementKind.Block, text, height, false);

        internal void Brand(string name, string detail, IconGlyph icon) => Decorate(Add(InGameElementKind.Brand, name, InGameMetrics.BrandHeight, false), detail, icon);

        internal void Card(string title, string detail, IconGlyph icon, Action onClick) =>
            Decorate(Launcher(InGameElementKind.Card, title, InGameMetrics.CardHeight, onClick), detail, icon);

        internal void Tile(string title, IconGlyph icon, Action onClick) =>
            Decorate(Launcher(InGameElementKind.Tile, title, InGameMetrics.TileHeight, onClick), string.Empty, icon);

        internal void ResetInteraction()
        {
            queued.Clear();
            pending.Clear();
            StopEditing();
        }

        internal void QueueClick(InGameElement element) => queued.Add(new Interaction(InteractionType.Click, element, 0f));

        internal void QueueHold(InGameElement element) => queued.Add(new Interaction(InteractionType.Hold, element, 0f));

        internal void QueueFocus(InGameElement element) => queued.Add(new Interaction(InteractionType.Focus, element, 0f));

        internal void QueueSlide(InGameElement element, float fraction) => queued.Add(new Interaction(InteractionType.Slide, element, fraction));

        internal void Type(char character)
        {
            if (Editing && editText.Length < editMaxLength) editText += character;
        }

        internal void Backspace()
        {
            if (Editing && editText.Length > 0) editText = editText.Substring(0, editText.Length - 1);
        }

        internal void ClearText()
        {
            if (Editing) editText = string.Empty;
        }

        internal void StopEditing() => EditingIndex = NotEditing;

        public void Label(string text) => Add(InGameElementKind.Label, text, InGameMetrics.LabelHeight(text, RegionWidth), false);

        public void Button(string text, Action? onClick, bool interactable = true) => Press(text, onClick, null, interactable);

        public void Button(string text, Action? onClick, Action onRightClick, bool interactable = true) => Press(text, onClick, onRightClick, interactable);

        internal bool Dropdown(string text, bool open, Action drawOptions, bool interactable = true)
        {
            InGameElement anchor = Add(InGameElementKind.Button, text, InGameMetrics.ButtonHeight, interactable);
            if (!interactable) return false;
            if (Take(InteractionType.Click, anchor, out _)) open = !open;
            else if (dismissMenu) open = false;
            if (!open || HasMenu) return false;
            anchor.Active = true;
            return DrawMenu(anchor.Index, drawOptions);
        }

        public bool Toggle(string label, bool value, string on = "On", string off = "Off", bool interactable = true)
        {
            InGameElement element = Add(InGameElementKind.Toggle, $"{label}: {(value ? on : off)}", InGameMetrics.ButtonHeight, interactable);
            element.Active = value;
            return interactable && Take(InteractionType.Click, element, out _) ? !value : value;
        }

        public float Slider(string label, float value, float min, float max, string format = "F2", string unit = "")
        {
            Label($"{label}: {value.ToString(format)}{unit}");
            float step = (max - min) * SliderStep;
            BeginRow();
            try
            {
                if (Stepped(StepDown)) value -= step;
                nextWeight = TrackWeight;
                InGameElement track = Add(InGameElementKind.Slider, label, InGameMetrics.SliderHeight, true);
                if (Take(InteractionType.Slide, track, out float fraction)) value = Mathf.Lerp(min, max, fraction);
                if (Stepped(StepUp)) value += step;
                value = Mathf.Clamp(value, min, max);
                track.Fraction = Mathf.InverseLerp(min, max, value);
            }
            finally { EndRow(); }
            return value;
        }

        public string TextField(string text, int maxLength, Func<string, string>? normalize = null) => Field(text, maxLength, normalize, false);

        public string MaskedField(string text, int maxLength, Func<string, string>? normalize = null) => Field(text, maxLength, normalize, true);

        private string Field(string text, int maxLength, Func<string, string>? normalize, bool masked)
        {
            string current = text ?? string.Empty;
            InGameElement element = Add(InGameElementKind.Field, current, InGameMetrics.ButtonHeight, true);
            if (Take(InteractionType.Focus, element, out _)) StartEditing(element.Index, current);
            if (EditingIndex != element.Index)
            {
                element.Text = current.Length > 0 ? Shown(current, masked) : EmptyField;
                return current;
            }
            editedThisFrame = true;
            editMaxLength = maxLength;
            editText = Normalize(editText, maxLength, normalize);
            element.Active = true;
            element.Text = Shown(editText, masked) + Caret;
            return editText;
        }

        private static string Shown(string text, bool masked) => masked ? new string(MaskCharacter, text.Length) : text;

        public void ColorSwatch(Color color) => Add(InGameElementKind.Swatch, string.Empty, InGameMetrics.SwatchHeight, false).Color = color;

        public void HorizontalArea(Action draw)
        {
            BeginRow();
            try { draw(); }
            finally { EndRow(); }
        }

        public Vector2 ScrollArea(Vector2 scroll, float maximumHeight, int buttonRows, Action draw)
        {
            draw();
            return scroll;
        }

        private InGameElement Press(string text, Action? onClick, Action? onHold, bool interactable)
        {
            InGameElement element = Add(InGameElementKind.Button, text, InGameMetrics.ButtonHeight, interactable);
            element.HasAlternate = onHold != null;
            if (!interactable) return element;
            if (onHold != null && Take(InteractionType.Hold, element, out _)) onHold();
            else if (Take(InteractionType.Click, element, out _)) onClick?.Invoke();
            return element;
        }

        private InGameElement Launcher(InGameElementKind kind, string text, float height, Action onClick)
        {
            InGameElement element = Add(kind, text, height, true);
            if (Take(InteractionType.Click, element, out _)) onClick();
            return element;
        }

        private static void Decorate(InGameElement element, string detail, IconGlyph icon)
        {
            element.Detail = detail;
            element.Icon = icon;
        }

        private bool DrawMenu(int anchor, Action drawOptions)
        {
            InGameRegion outerRegion = region;
            int outerDepth = rowDepth;
            int outerGroup = rowGroup;
            region = InGameRegion.Menu;
            rowDepth = 0;
            menuChosen = false;
            MenuAnchor = anchor;
            MenuFirst = Count;
            try { drawOptions(); }
            finally
            {
                MenuEnd = Count;
                if (menuChosen) MenuAnchor = NoMenu;
                region = outerRegion;
                rowDepth = outerDepth;
                rowGroup = outerGroup;
            }
            return !menuChosen;
        }

        private bool PendingOutsideMenu()
        {
            foreach (Interaction interaction in pending)
                if (interaction.Index != MenuAnchor && (interaction.Index < MenuFirst || interaction.Index >= MenuEnd)) return true;
            return false;
        }

        private bool Stepped(string text)
        {
            nextWeight = StepWeight;
            InGameElement element = Add(InGameElementKind.Button, text, InGameMetrics.SliderHeight, true);
            return Take(InteractionType.Click, element, out _);
        }

        private void StartEditing(int index, string text)
        {
            EditingIndex = index;
            editText = text;
            editMaxLength = int.MaxValue;
        }

        private static string Normalize(string text, int maxLength, Func<string, string>? normalize)
        {
            string limited = text.Length > maxLength ? text.Substring(0, maxLength) : text;
            return normalize != null ? normalize(limited) : limited;
        }

        private bool Take(InteractionType type, InGameElement element, out float fraction)
        {
            for (int i = 0; i < pending.Count; i++)
            {
                Interaction interaction = pending[i];
                if (!interaction.Targets(type, element)) continue;
                fraction = interaction.Fraction;
                pending.RemoveAt(i);
                if (element.Region == InGameRegion.Menu) menuChosen = true;
                return true;
            }
            fraction = 0f;
            return false;
        }

        private void BeginRow()
        {
            if (rowDepth++ == 0) rowGroup = nextGroup++;
        }

        private void EndRow() => rowDepth--;

        private InGameElement Add(InGameElementKind kind, string text, float height, bool interactable)
        {
            if (Count == elements.Count) elements.Add(new InGameElement());
            InGameElement element = elements[Count];
            int group = rowDepth > 0 ? rowGroup : nextGroup++;
            element.Reset(Count, kind, region, group, nextWeight, height, text, interactable);
            nextWeight = DefaultWeight;
            Count++;
            return element;
        }
    }
}
