// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI.InGame
{
    internal sealed class InGameKeyboard
    {
        private const float WideKeyWeight = 1.5f;
        private const float SpaceWeight = 4f;

        private static readonly string[] Rows = { "1234567890", "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };

        private readonly InGameSurface surface;
        private readonly string[][] labels;
        private readonly Action[][] keys;
        private readonly Action[] rows;
        private readonly Action actionRow;
        private readonly Action backspace;
        private readonly Action space;
        private readonly Action clear;
        private readonly Action done;

        internal InGameKeyboard(InGameSurface surface)
        {
            this.surface = surface;
            labels = new string[Rows.Length][];
            keys = new Action[Rows.Length][];
            rows = new Action[Rows.Length];
            for (int row = 0; row < Rows.Length; row++)
            {
                string characters = Rows[row];
                labels[row] = new string[characters.Length];
                keys[row] = new Action[characters.Length];
                for (int i = 0; i < characters.Length; i++)
                {
                    char character = characters[i];
                    labels[row][i] = character.ToString();
                    keys[row][i] = () => surface.Type(character);
                }
                int index = row;
                rows[row] = () => DrawRow(index);
            }
            backspace = surface.Backspace;
            space = () => surface.Type(' ');
            clear = surface.ClearText;
            done = surface.StopEditing;
            actionRow = DrawActionRow;
        }

        internal void Draw()
        {
            foreach (Action row in rows) surface.HorizontalArea(row);
            surface.HorizontalArea(actionRow);
        }

        private void DrawRow(int row)
        {
            string[] rowLabels = labels[row];
            Action[] rowKeys = keys[row];
            for (int i = 0; i < rowLabels.Length; i++) surface.Button(rowLabels[i], rowKeys[i]);
            if (row != Rows.Length - 1) return;
            surface.Weighted(WideKeyWeight);
            surface.Button("Del", backspace);
        }

        private void DrawActionRow()
        {
            surface.Weighted(WideKeyWeight);
            surface.Button("Clear", clear);
            surface.Weighted(SpaceWeight);
            surface.Button("Space", space);
            surface.Weighted(WideKeyWeight);
            surface.Button("Done", done);
        }
    }
}
