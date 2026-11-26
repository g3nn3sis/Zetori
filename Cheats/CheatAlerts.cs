// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Text;
using BepInEx.Configuration;
using UnityEngine;
using Zetori.Base;
using Zetori.Base.Config;
using Zetori.Cheats.Signals;
using Zetori.TagManager;

namespace Zetori.Cheats
{
    internal sealed class CheatAlerts : ObjectComponent
    {
        private const float ScanInterval = 0.25f;
        private const int MaximumAlerts = 4;
        private const int ReasonLength = 32;
        private const string Ellipsis = "...";
        private const string SignalColor = "FF8C1A";
        private const string TestName = "Test Player";
        private const SignalKind TestSignals = SignalKind.Speed | SignalKind.Teleports;

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
            SignalKind shown = Shown();
            foreach (VRRig? rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || !(CheatSignals.For(rig) is SignalRecord record)) continue;
                SignalKind raised = record.TakeNewlyRaised() & shown;
                if (CheatSignals.ModdedLobby) raised &= ~SignalKind.ModData;
                if (raised != SignalKind.None && Enabled) Push(new CheatAlert(Compose(TagManagerControl.NameOf(rig.Creator), record, raised), now));
            }
        }

        internal static void ShowTest()
        {
            if (Enabled) Push(new CheatAlert(Compose(TestName, null, TestSignals), Time.unscaledTime));
        }

        private static SignalKind Shown()
        {
            SignalKind shown = SignalKind.None;
            foreach ((SignalKind kind, _) in SignalNames.All)
                if (SettingOf(kind).Value) shown |= kind;
            return shown;
        }

        private static ConfigEntry<bool> SettingOf(SignalKind kind) => kind switch
        {
            SignalKind.GameReport => CasterConfig.CheatAlertGameReports,
            SignalKind.ModData => CasterConfig.CheatAlertModData,
            SignalKind.Name => CasterConfig.CheatAlertOddNames,
            SignalKind.QuestTweaks => CasterConfig.CheatAlertQuestTweaks,
            SignalKind.Cosmetics => CasterConfig.CheatAlertCosmetics,
            SignalKind.Speed => CasterConfig.CheatAlertSpeed,
            SignalKind.Teleports => CasterConfig.CheatAlertTeleports,
            SignalKind.Hovering => CasterConfig.CheatAlertHovering,
            SignalKind.ArmReach => CasterConfig.CheatAlertArmReach,
            SignalKind.HandMotion => CasterConfig.CheatAlertHandMotion,
            SignalKind.NetworkSpam => CasterConfig.CheatAlertNetworkSpam,
            SignalKind.LowGravity => CasterConfig.CheatAlertLowGravity,
            SignalKind.TagBurst => CasterConfig.CheatAlertTagBursts,
            SignalKind.FlaggedBefore => CasterConfig.CheatAlertFlaggedBefore,
            _ => CasterConfig.CheatAlertTagReach
        };

        private static string Compose(string player, SignalRecord? record, SignalKind raised)
        {
            builder.Clear();
            builder.Append("<b><noparse>").Append(player).Append("</noparse></b>  flagged: <color=#").Append(SignalColor).Append('>');
            bool first = true;
            foreach ((SignalKind kind, string name) in SignalNames.All)
            {
                if ((raised & kind) == 0) continue;
                if (!first) builder.Append(", ");
                first = false;
                builder.Append(name);
                if (kind == SignalKind.GameReport && record != null && record.ReportReasons.Count > 0 && CasterConfig.CheatAlertsReasons.Value)
                    builder.Append(" (<noparse>").Append(Shorten(record.ReportReasons[0])).Append("</noparse>)");
                if (kind == SignalKind.FlaggedBefore && record != null)
                    builder.Append(" (").Append(Shorten(SignalNames.Describe(record.PastSignals).ToLowerInvariant())).Append(')');
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
