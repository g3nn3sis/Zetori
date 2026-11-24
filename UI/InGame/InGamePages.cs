// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Photon.Pun;
using UnityEngine;
using Zetori.Base;
using Zetori.Replay;
using Zetori.UI.Icons;
using Zetori.UI.Panels;

namespace Zetori.UI.InGame
{
    internal sealed class InGamePages : IDisposable
    {
        private const int Home = -1;
        private const int PlayerPage = -2;
        private const int GridColumns = 3;
        private const string HomeTitle = "Zetori";
        private const string RoomTitle = "Room";
        private const string PlayersTitle = "Players";
        private const string HomeLabel = "Home";
        private const string BackLabel = "Back";
        private const float TitleWeight = 3f;
        private const float StatusInterval = 0.5f;
        private const float FrameRateSmoothing = 0.1f;

        private readonly struct Section
        {
            internal readonly string Title;
            internal readonly IconGlyph Icon;
            internal readonly Action<IPanelSurface> Draw;

            internal Section(string title, IconGlyph icon, Action<IPanelSurface> draw)
            {
                Title = title;
                Icon = icon;
                Draw = draw;
            }
        }

        private readonly InGameSurface surface;
        private readonly InGameKeyboard keyboard;
        private readonly InGamePlayerPage playerPage;
        private readonly Section[] sections;
        private readonly int playersSection;
        private readonly Action[] openSection;
        private readonly Action[] gridRows;
        private readonly Action headerRow;
        private readonly Action goHome;
        private readonly Action backToPlayers;
        private readonly Action close;
        private readonly Action exitReplay = ReplaySession.ExitReplay;
        private int page = Home;
        private int? requestedPage;
        private string status = string.Empty;
        private float nextStatus;
        private float smoothedFrameTime;

        internal InGamePages(InGameSurface surface, PanelOrchestrator owner, Action recenter, Action close)
        {
            this.surface = surface;
            this.close = close;
            keyboard = new InGameKeyboard(surface);
            playerPage = new InGamePlayerPage(surface);
            var players = new InGamePlayersPage(surface, OpenPlayer);
            sections = BuildSections(new PanelContent(owner), _ => players.Draw(), recenter, close);
            playersSection = Array.FindIndex(sections, section => section.Title == PlayersTitle);
            openSection = new Action[sections.Length];
            for (int i = 0; i < sections.Length; i++)
            {
                int index = i;
                openSection[i] = () => requestedPage = index;
            }
            gridRows = new Action[(sections.Length + GridColumns - 1) / GridColumns];
            for (int row = 0; row < gridRows.Length; row++)
            {
                int first = row * GridColumns;
                gridRows[row] = () => DrawGridRow(first);
            }
            goHome = () => requestedPage = Home;
            backToPlayers = () => requestedPage = playersSection;
            headerRow = DrawHeaderRow;
        }

        private string Title => page == Home ? HomeTitle : sections[page].Title;

        internal bool ApplyRequestedPage()
        {
            if (requestedPage is not int next) return false;
            requestedPage = null;
            if (next == page) return false;
            page = next;
            return true;
        }

        internal void Draw()
        {
            surface.EnterRegion(InGameRegion.Header);
            surface.HorizontalArea(headerRow);
            surface.EnterRegion(InGameRegion.Body);
            if (page == Home) DrawHome();
            else if (page == PlayerPage) DrawPlayer();
            else DrawSection(sections[page]);
            if (page != PlayerPage) playerPage.Conceal();
            if (surface.Editing)
            {
                surface.EnterRegion(InGameRegion.Keyboard);
                keyboard.Draw();
            }
            surface.EnterRegion(InGameRegion.Footer);
            surface.Label(Status());
        }

        internal void Conceal() => playerPage.Conceal();

        private void OpenPlayer(VRRig rig)
        {
            playerPage.Open(rig);
            requestedPage = PlayerPage;
        }

        private void DrawPlayer()
        {
            try { playerPage.Draw(); }
            catch (Exception e) { Log.ExceptionOnce("in-game-panel-player", e, "Drawing the in-game player page failed"); }
        }

        private void DrawSection(Section section)
        {
            try { section.Draw(surface); }
            catch (Exception e) { Log.ExceptionOnce($"in-game-panel-{section.Title}", e, $"Drawing the in-game {section.Title} page failed"); }
        }

        private static Section[] BuildSections(PanelContent content, Action<IPanelSurface> drawPlayers, Action recenter, Action close)
        {
            var list = new List<Section>
            {
                new Section(RoomTitle, PanelCatalog.Icon(PanelId.Main), content.For(PanelId.Main)),
                new Section(PlayersTitle, IconGlyph.People, drawPlayers),
                new Section("VR Camera", IconGlyph.Camcorder, VrCameraPanel.Draw)
            };
            foreach (PanelId[] column in PanelCatalog.Columns)
                foreach (PanelId id in column)
                    if (id != PanelId.Main) list.Add(new Section(PanelCatalog.Title(id), PanelCatalog.Icon(id), content.For(id)));
            list.Add(new Section("Panel", IconGlyph.Panel, new InGameSettingsPanel(recenter, close).Draw));
            return list.ToArray();
        }

        private void DrawHeaderRow()
        {
            if (page == PlayerPage) surface.Button(BackLabel, backToPlayers);
            else surface.Button(HomeLabel, goHome, page != Home);
            surface.Weighted(TitleWeight);
            if (page == PlayerPage) surface.Title(playerPage.Title, playerPage.TitleColor);
            else surface.Title(Title);
            if (ReplaySession.IsReplaying) surface.Button("Exit Replay", exitReplay);
            surface.Button("Close", close);
        }

        private void DrawHome()
        {
            foreach (Action row in gridRows) surface.HorizontalArea(row);
        }

        private void DrawGridRow(int first)
        {
            for (int i = first; i < first + GridColumns; i++)
            {
                if (i < sections.Length) surface.Button(sections[i].Title, sections[i].Icon, openSection[i]);
                else surface.Spacer();
            }
        }

        private string Status()
        {
            float deltaTime = Time.unscaledDeltaTime;
            smoothedFrameTime = smoothedFrameTime <= 0f ? deltaTime : Mathf.Lerp(smoothedFrameTime, deltaTime, FrameRateSmoothing);
            float now = Time.unscaledTime;
            if (now < nextStatus) return status;
            nextStatus = now + StatusInterval;
            int frameRate = smoothedFrameTime > 0f ? Mathf.RoundToInt(1f / smoothedFrameTime) : 0;
            status = $"{RoomStatus()}   {frameRate} FPS   {DateTime.Now:HH:mm}";
            return status;
        }

        public void Dispose() => playerPage.Dispose();

        private static string RoomStatus()
        {
            var room = PhotonNetwork.CurrentRoom;
            return PhotonNetwork.InRoom && room != null
                ? $"{room.Name}  {room.PlayerCount}/{room.MaxPlayers}  {PhotonNetwork.GetPing()} ms"
                : "Not in a room";
        }
    }
}
