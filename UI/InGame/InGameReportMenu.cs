// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Player.Moderation;

namespace Zetori.UI.InGame
{
    internal sealed class InGameReportMenu
    {
        private const string ReportLabel = "Report";
        private const string ReportedLabel = "Reported";
        private const string CancelLabel = "Cancel";

        private static readonly GorillaPlayerLineButton.ButtonType[] Reasons =
        {
            GorillaPlayerLineButton.ButtonType.HateSpeech,
            GorillaPlayerLineButton.ButtonType.Cheating,
            GorillaPlayerLineButton.ButtonType.Toxicity
        };

        private static readonly string[] ReasonLabels = { "Hate Speech", "Cheating", "Toxicity" };

        private readonly InGameSurface surface;
        private readonly Action options;
        private readonly Action[] send;
        private GorillaPlayerScoreboardLine? drawingLine;
        private NetPlayer? openFor;

        internal InGameReportMenu(InGameSurface surface)
        {
            this.surface = surface;
            options = DrawOptions;
            send = new Action[Reasons.Length];
            for (int i = 0; i < Reasons.Length; i++)
            {
                GorillaPlayerLineButton.ButtonType reason = Reasons[i];
                send[i] = () => Send(reason);
            }
        }

        internal void Draw(GorillaPlayerScoreboardLine? line)
        {
            if (!PlayerReport.CanReport(line))
            {
                surface.Dropdown(ReportLabel, false, options, interactable: false);
                return;
            }
            NetPlayer player = line!.linePlayer;
            bool open = openFor == player;
            drawingLine = line;
            try
            {
                bool stillOpen = surface.Dropdown(PlayerReport.HasReportedAny(line) ? ReportedLabel : ReportLabel, open, options);
                if (stillOpen) openFor = player;
                else if (open) openFor = null;
            }
            finally { drawingLine = null; }
        }

        private void DrawOptions()
        {
            GorillaPlayerScoreboardLine line = drawingLine!;
            for (int i = 0; i < Reasons.Length; i++) surface.Button(ReasonLabels[i], send[i], !PlayerReport.HasReported(line, Reasons[i]));
            surface.Button(CancelLabel, null);
        }

        private void Send(GorillaPlayerLineButton.ButtonType reason)
        {
            if (drawingLine != null) PlayerReport.Send(drawingLine, reason);
        }
    }
}
