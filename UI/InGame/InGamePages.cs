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
        private const int RecordedPlayerPage = -3;
        private const int FeaturedCount = 6;
        private const int FeaturedColumns = 3;
        private const int GridColumns = 5;
        private const string BrandVersion = "v" + PluginInfo.Version;
        private const string RoomTitle = "Room";
        private const string PlayersTitle = "Players";
        private const string HomeLabel = "Home";
        private const string BackLabel = "Back";
        private const string Offline = "Not in a room";
        private const float TitleWeight = 3f;
        private const float BrandWeight = 4f;
        private const float ReadoutInterval = 0.5f;
        private const float FrameRateSmoothing = 0.1f;

        private static readonly string BrandName = PluginInfo.Name.ToUpperInvariant();
        private static readonly PanelId[] FeaturedPanels = { PanelId.Main, PanelId.Capture, PanelId.Stats };

        private readonly struct Section
        {
            internal readonly string Title;
            internal readonly IconGlyph Icon;
            internal readonly Action<IPanelSurface> Draw;
            internal readonly Func<string>? Describe;

            internal Section(string title, IconGlyph icon, Action<IPanelSurface> draw, Func<string>? describe = null)
            {
                Title = title;
                Icon = icon;
                Draw = draw;
                Describe = describe;
            }
        }

        private readonly InGameSurface surface;
        private readonly InGameKeyboard keyboard;
        private readonly InGamePlayerPage playerPage;
        private readonly InGameRecordedPlayerPage recordedPlayerPage;
        private readonly Section[] sections;
        private readonly int playersSection;
        private readonly Action[] openSection;
        private readonly string[] featuredDetails = new string[FeaturedCount];
        private readonly Action[] featuredRows;
        private readonly Action[] gridRows;
        private readonly Action headerRow;
        private readonly Action goHome;
        private readonly Action backToPlayers;
        private readonly Action close;
        private readonly Action exitReplay = ReplaySession.ExitReplay;
        private int page = Home;
        private int? requestedPage;
        private string status = string.Empty;
        private float nextReadout;
        private float smoothedFrameTime;

        internal InGamePages(InGameSurface surface, PanelOrchestrator owner, Action recenter, Action close)
        {
            this.surface = surface;
            this.close = close;
            keyboard = new InGameKeyboard(surface);
            playerPage = new InGamePlayerPage(surface);
            recordedPlayerPage = new InGameRecordedPlayerPage(surface);
            var players = new InGamePlayersPage(surface, OpenPlayer, OpenRecordedPlayer);
            sections = BuildSections(new PanelContent(owner), _ => players.Draw(), recenter, close);
            playersSection = Array.FindIndex(sections, section => section.Title == PlayersTitle);
            openSection = new Action[sections.Length];
            for (int i = 0; i < sections.Length; i++)
            {
                int index = i;
                openSection[i] = () => requestedPage = index;
            }
            for (int i = 0; i < FeaturedCount; i++) featuredDetails[i] = string.Empty;
            featuredRows = new Action[(FeaturedCount + FeaturedColumns - 1) / FeaturedColumns];
            for (int row = 0; row < featuredRows.Length; row++)
            {
                int first = row * FeaturedColumns;
                featuredRows[row] = () => DrawFeaturedRow(first);
            }
            gridRows = new Action[(sections.Length - FeaturedCount + GridColumns - 1) / GridColumns];
            for (int row = 0; row < gridRows.Length; row++)
            {
                int first = FeaturedCount + row * GridColumns;
                gridRows[row] = () => DrawGridRow(first);
            }
            goHome = () => requestedPage = Home;
            backToPlayers = () => requestedPage = playersSection;
            headerRow = DrawHeaderRow;
        }

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
            RefreshReadouts();
            surface.EnterRegion(InGameRegion.Header);
            surface.HorizontalArea(headerRow);
            surface.EnterRegion(InGameRegion.Body);
            if (page == Home) DrawHome();
            else if (page == PlayerPage) DrawPlayer();
            else if (page == RecordedPlayerPage) DrawRecordedPlayer();
            else DrawSection(sections[page]);
            if (page != PlayerPage) playerPage.Conceal();
            if (page != RecordedPlayerPage) recordedPlayerPage.Conceal();
            if (surface.Editing)
            {
                surface.EnterRegion(InGameRegion.Keyboard);
                keyboard.Draw();
            }
            surface.EnterRegion(InGameRegion.Footer);
            surface.Label(status);
        }

        internal void Conceal()
        {
            playerPage.Conceal();
            recordedPlayerPage.Conceal();
        }

        private void OpenPlayer(VRRig rig)
        {
            playerPage.Open(rig);
            requestedPage = PlayerPage;
        }

        private void OpenRecordedPlayer(ReplayPlayer replay, ReplayPuppet puppet)
        {
            recordedPlayerPage.Open(replay, puppet);
            requestedPage = RecordedPlayerPage;
        }

        private void DrawPlayer()
        {
            try { playerPage.Draw(); }
            catch (Exception e) { Log.ExceptionOnce("in-game-panel-player", e, "Drawing the in-game player page failed"); }
        }

        private void DrawRecordedPlayer()
        {
            try { recordedPlayerPage.Draw(); }
            catch (Exception e) { Log.ExceptionOnce("in-game-panel-recorded-player", e, "Drawing the in-game recorded player page failed"); }
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
                new Section(RoomTitle, PanelCatalog.Icon(PanelId.Main), content.For(PanelId.Main), RoomSummary),
                new Section(PlayersTitle, IconGlyph.People, drawPlayers, PlayersSummary),
                new Section("VR Camera", IconGlyph.Camcorder, VrCameraPanel.Draw, VrCameraPanel.Summary),
                Featured(content, PanelId.Capture, CapturePanel.Summary),
                Featured(content, PanelId.Stats, StatsPanel.Summary),
                new Section("Host", IconGlyph.Crown, new HostPanel().Draw, HostPanel.Summary)
            };
            foreach (PanelId[] column in PanelCatalog.Columns)
                foreach (PanelId id in column)
                    if (!Array.Exists(FeaturedPanels, featured => featured == id)) list.Add(new Section(PanelCatalog.Title(id), PanelCatalog.Icon(id), content.For(id)));
            list.Add(new Section("Panel", IconGlyph.Panel, new InGameSettingsPanel(recenter, close).Draw));
            return list.ToArray();
        }

        private static Section Featured(PanelContent content, PanelId id, Func<string> describe) =>
            new Section(PanelCatalog.Title(id), PanelCatalog.Icon(id), content.For(id), describe);

        private void DrawHeaderRow()
        {
            if (page == Home) DrawBrand();
            else if (page == PlayerPage) DrawNavigation(BackLabel, backToPlayers, playerPage.Title, playerPage.TitleColor);
            else if (page == RecordedPlayerPage) DrawNavigation(BackLabel, backToPlayers, recordedPlayerPage.Title, recordedPlayerPage.TitleColor);
            else DrawNavigation(HomeLabel, goHome, sections[page].Title, Color.white);
            if (ReplaySession.IsReplaying) surface.Button("Exit Replay", exitReplay);
            surface.Button("Close", close);
        }

        private void DrawBrand()
        {
            surface.Weighted(BrandWeight);
            surface.Brand(BrandName, BrandVersion, IconGlyph.Zetori);
        }

        private void DrawNavigation(string label, Action navigate, string title, Color titleColor)
        {
            surface.Button(label, navigate);
            surface.Weighted(TitleWeight);
            surface.Title(title, titleColor);
        }

        private void DrawHome()
        {
            foreach (Action row in featuredRows) surface.HorizontalArea(row);
            foreach (Action row in gridRows) surface.HorizontalArea(row);
        }

        private void DrawFeaturedRow(int first)
        {
            for (int i = first; i < first + FeaturedColumns; i++)
            {
                if (i < FeaturedCount) surface.Card(sections[i].Title, featuredDetails[i], sections[i].Icon, openSection[i]);
                else surface.Spacer();
            }
        }

        private void DrawGridRow(int first)
        {
            for (int i = first; i < first + GridColumns; i++)
            {
                if (i < sections.Length) surface.Tile(sections[i].Title, sections[i].Icon, openSection[i]);
                else surface.Spacer();
            }
        }

        private void RefreshReadouts()
        {
            float deltaTime = Time.unscaledDeltaTime;
            smoothedFrameTime = smoothedFrameTime <= 0f ? deltaTime : Mathf.Lerp(smoothedFrameTime, deltaTime, FrameRateSmoothing);
            float now = Time.unscaledTime;
            if (now < nextReadout) return;
            nextReadout = now + ReadoutInterval;
            int frameRate = smoothedFrameTime > 0f ? Mathf.RoundToInt(1f / smoothedFrameTime) : 0;
            status = $"{RoomStatus()}   {frameRate} FPS   {DateTime.Now:HH:mm}";
            RefreshFeaturedDetails();
        }

        private void RefreshFeaturedDetails()
        {
            for (int i = 0; i < FeaturedCount; i++)
            {
                Func<string>? describe = sections[i].Describe;
                try { featuredDetails[i] = describe != null ? describe() : string.Empty; }
                catch (Exception e) { Log.ExceptionOnce($"in-game-panel-describe-{sections[i].Title}", e, $"Describing the in-game {sections[i].Title} page failed"); }
            }
        }

        public void Dispose()
        {
            playerPage.Dispose();
            recordedPlayerPage.Dispose();
        }

        private static string RoomStatus()
        {
            var room = PhotonNetwork.CurrentRoom;
            return PhotonNetwork.InRoom && room != null
                ? $"{StreamerMode.RoomCode(room.Name)}  {room.PlayerCount}/{room.MaxPlayers}  {PhotonNetwork.GetPing()} ms"
                : Offline;
        }

        private static string RoomSummary()
        {
            var room = PhotonNetwork.CurrentRoom;
            return PhotonNetwork.InRoom && room != null ? $"Code {StreamerMode.RoomCode(room.Name)}" : Offline;
        }

        private static string PlayersSummary()
        {
            if (ReplaySession.Player is ReplayPlayer replay)
            {
                int recorded = InGamePlayersPage.RecordedCount(replay);
                return recorded == 1 ? "1 in replay" : $"{recorded} in replay";
            }
            var room = PhotonNetwork.CurrentRoom;
            if (!PhotonNetwork.InRoom || room == null) return Offline;
            return room.PlayerCount == 1 ? "1 player" : $"{room.PlayerCount} players";
        }
    }
}
