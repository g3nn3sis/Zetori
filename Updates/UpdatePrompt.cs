// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.Base.Config;
using Zetori.UI;
using Zetori.UI.Icons;
using UnityEngine;

namespace Zetori.Updates
{
    internal sealed class UpdatePrompt : IDisposable
    {
        private const float Width = 320f;
        private const float NotesMaximumHeight = 200f;
        private const int NotesRows = 8;
        private const string Title = "Update Available";

        private readonly ReleaseInfo release;
        private readonly Panel panel;
        private Vector2 notesScroll;
        private Task<bool>? install;

        internal bool Dismissed { get; private set; }

        internal UpdatePrompt(ReleaseInfo release)
        {
            this.release = release;
            panel = new Panel(Width, Title, IconGlyph.Zetori, UI.ColorSpace.Current.Color) { Expanded = true };
        }

        internal void Tick(float dt)
        {
            panel.Expanded = true;
            panel.Animate(dt);
            Rect area = panel.Rect;
            panel.SetPosition(new Vector2((UiScale.Width - area.width) * 0.5f, (UiScale.Height - area.height) * 0.5f));
        }

        internal void Draw()
        {
            Matrix4x4 previous = UiScale.Begin();
            try
            {
                panel.Draw(DrawContent);
                panel.BringToFront();
            }
            finally { GUI.matrix = previous; }
        }

        private void DrawContent(IPanelSurface surface)
        {
            if (install == null) DrawOffer(surface);
            else if (!install.IsCompleted) surface.Label($"Downloading {PluginInfo.Name} {release.Version}.");
            else DrawOutcome(surface, install.Status == TaskStatus.RanToCompletion && install.Result);
        }

        private void DrawOffer(IPanelSurface surface)
        {
            surface.Label($"{PluginInfo.Name} {release.Version} is available. You have {PluginInfo.Version}.");
            string notes = release.Notes.Length > 0 ? release.Notes : "No release notes.";
            notesScroll = surface.ScrollArea(notesScroll, NotesMaximumHeight, NotesRows, () => surface.Label(notes));
            surface.HorizontalArea(() =>
            {
                surface.Button("Ignore", Ignore);
                surface.Button("Update", BeginInstall);
            });
        }

        private void DrawOutcome(IPanelSurface surface, bool installed)
        {
            surface.Label(installed
                ? $"{PluginInfo.Name} {release.Version} is downloaded. Restart the game for the update to apply."
                : "The update could not be downloaded. The BepInEx log has the details.");
            surface.Button("Close", Dismiss);
        }

        private void Ignore()
        {
            CasterConfig.IgnoredUpdate.Value = release.Version.ToString();
            CasterConfig.Save();
            Dismiss();
        }

        private void BeginInstall() => install ??= UpdateInstaller.Install(release);

        private void Dismiss() => Dismissed = true;

        public void Dispose() => panel.Dispose();
    }
}
