// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Player;

namespace Zetori.UI.Panels
{
    internal sealed class ReportMenu
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

        private readonly Action toggle;
        private readonly Action cancel;
        private readonly Action optionsRow;
        private readonly Action[] send;
        private IPanelSurface? drawing;
        private GorillaPlayerScoreboardLine? drawingLine;
        private NetPlayer? openFor;

        internal ReportMenu()
        {
            toggle = Toggle;
            cancel = Close;
            optionsRow = DrawOptionsRow;
            send = new Action[Reasons.Length];
            for (int i = 0; i < Reasons.Length; i++)
            {
                GorillaPlayerLineButton.ButtonType reason = Reasons[i];
                send[i] = () => Send(reason);
            }
        }

        internal void DrawButton(IPanelSurface panel, GorillaPlayerScoreboardLine? line)
        {
            bool available = PlayerReport.CanReport(line);
            string label = available && PlayerReport.HasReportedAny(line!) ? ReportedLabel : ReportLabel;
            drawingLine = line;
            try { panel.Button(label, toggle, available); }
            finally { drawingLine = null; }
        }

        internal void DrawOptions(IPanelSurface panel, GorillaPlayerScoreboardLine? line)
        {
            if (!IsOpen(line)) return;
            drawing = panel;
            drawingLine = line;
            try { panel.HorizontalArea(optionsRow); }
            finally
            {
                drawing = null;
                drawingLine = null;
            }
        }

        private bool IsOpen(GorillaPlayerScoreboardLine? line) => openFor != null && PlayerReport.CanReport(line) && line!.linePlayer == openFor;

        private void DrawOptionsRow()
        {
            IPanelSurface panel = drawing!;
            GorillaPlayerScoreboardLine line = drawingLine!;
            for (int i = 0; i < Reasons.Length; i++) panel.Button(ReasonLabels[i], send[i], !PlayerReport.HasReported(line, Reasons[i]));
            panel.Button(CancelLabel, cancel);
        }

        private void Toggle()
        {
            if (drawingLine == null) return;
            NetPlayer player = drawingLine.linePlayer;
            openFor = openFor == player ? null : player;
        }

        private void Send(GorillaPlayerLineButton.ButtonType reason)
        {
            if (drawingLine != null) PlayerReport.Send(drawingLine, reason);
            openFor = null;
        }

        private void Close() => openFor = null;
    }
}
