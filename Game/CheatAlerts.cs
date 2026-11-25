// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Text;
using UnityEngine;
using Zetori.Base;
using Zetori.Player;

namespace Zetori.Game
{
    internal sealed class CheatAlerts : ObjectComponent
    {
        private const float ScanInterval = 0.25f;
        private const int MaximumAlerts = 4;
        private const int ReasonLength = 32;
        private const string Ellipsis = "...";
        private const string SignalColor = "FF8C1A";

        private static readonly (SignalKind Kind, string Name)[] SignalNames =
        {
            (SignalKind.GameReport, "Game report"),
            (SignalKind.ModData, "Mod data"),
            (SignalKind.Name, "Odd name"),
            (SignalKind.QuestTweaks, "Quest tweaks"),
            (SignalKind.Cosmetics, "Unowned cosmetics"),
            (SignalKind.Speed, "Too fast"),
            (SignalKind.Teleports, "Teleporting"),
            (SignalKind.Hovering, "Hovering"),
            (SignalKind.ArmReach, "Long arms"),
            (SignalKind.TagReach, "Far tags")
        };

        private static readonly List<CheatAlert> alerts = new List<CheatAlert>();
        private static readonly StringBuilder builder = new StringBuilder();

        private float nextScan;

        internal static bool Enabled => CasterConfig.CheatAlerts.Value;

        internal static IReadOnlyList<CheatAlert> Active => alerts;

        internal void Update()
        {
            float now = Time.unscaledTime;
            if (!Enabled) alerts.Clear();
            Prune(now);
            if (now < nextScan) return;
            nextScan = now + ScanInterval;
            try { Scan(now); }
            catch (Exception e) { Log.ExceptionOnce("cheat-alerts", e, "Checking players for new cheat signals failed"); }
        }

        private static void Scan(float now)
        {
            foreach (VRRig? rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || !(CheatSignals.For(rig) is SignalRecord record)) continue;
                SignalKind raised = record.TakeNewlyRaised();
                if (CheatSignals.ModdedLobby) raised &= ~SignalKind.ModData;
                if (raised != SignalKind.None && Enabled) Push(new CheatAlert(Compose(rig, record, raised), now));
            }
        }

        private static string Compose(VRRig rig, SignalRecord record, SignalKind raised)
        {
            builder.Clear();
            builder.Append("<b><noparse>").Append(TagManagerControl.NameOf(rig.Creator)).Append("</noparse></b>  flagged: <color=#").Append(SignalColor).Append('>');
            bool first = true;
            foreach ((SignalKind kind, string name) in SignalNames)
            {
                if ((raised & kind) == 0) continue;
                if (!first) builder.Append(", ");
                first = false;
                builder.Append(name);
                if (kind == SignalKind.GameReport && record.ReportReasons.Count > 0)
                    builder.Append(" (<noparse>").Append(Shorten(record.ReportReasons[0])).Append("</noparse>)");
            }
            return builder.Append("</color>").ToString();
        }

        private static string Shorten(string reason) =>
            reason.Length > ReasonLength ? reason.Substring(0, ReasonLength - Ellipsis.Length) + Ellipsis : reason;

        private static void Push(CheatAlert alert)
        {
            alerts.Insert(0, alert);
            if (alerts.Count > MaximumAlerts) alerts.RemoveRange(MaximumAlerts, alerts.Count - MaximumAlerts);
        }

        private static void Prune(float now)
        {
            for (int i = alerts.Count - 1; i >= 0; i--)
                if (alerts[i].Expired(now)) alerts.RemoveAt(i);
        }
    }
}
