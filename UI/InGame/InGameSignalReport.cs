// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using System.Text;
using Zetori.Player;

namespace Zetori.UI.InGame
{
    internal static class InGameSignalReport
    {
        internal const int Lines = 10;
        private const int ShownModKeys = 3;
        private const int ReasonLength = 36;
        private const string Ellipsis = "...";
        private const int ShownOverrides = 3;
        private const string RaisedColor = "FF8C1A";
        private const string ClearColor = "8CE08C";
        private const string Caveat =
            "Hints, not proof. Game reports are the game's own cheat checks, run on your headset. Lag, launchers, ropes, gliders and water can raise the speed, teleport, hover and tag signals. The game caps arm reach at 1 m. Quest tweaks are headset settings changed with developer tools; only Quest headsets report them.";

        private static readonly (SystemProperties Flag, string Name)[] OverrideNames =
        {
            (SystemProperties.CPULevel, "CPU level"),
            (SystemProperties.GPULevel, "GPU level"),
            (SystemProperties.RefreshRate, "Refresh rate"),
            (SystemProperties.HalfRefreshRate, "Half refresh rate"),
            (SystemProperties.SwapInterval, "Swap interval"),
            (SystemProperties.PredictionTime, "Prediction time"),
            (SystemProperties.Headlock, "Headlock"),
            (SystemProperties.HeadlockTranslationX, "Headlock X"),
            (SystemProperties.HeadlockTranslationY, "Headlock Y"),
            (SystemProperties.HeadlockTranslationZ, "Headlock Z"),
            (SystemProperties.PhaseSync, "Phase sync"),
            (SystemProperties.PhaseSyncAdditionalPadding, "Phase sync padding"),
            (SystemProperties.PhaseSyncDelayOverride, "Phase sync delay"),
            (SystemProperties.PhaseSyncPredictionTime, "Phase sync prediction")
        };

        internal static string CaveatText => Caveat;

        internal static string Heading(SignalRecord? record)
        {
            string lobby = CheatSignals.ModdedLobby ? " Modded lobby: mods are allowed here." : string.Empty;
            if (record == null) return $"Cheat and mod signals: watching...{lobby}";
            string raised = record.Count == 1 ? "1 raised" : $"{record.Count} raised";
            return $"Cheat and mod signals: {raised}, watched {InGameDetails.Clock(record.Watched)}.{lobby}";
        }

        internal static string Describe(SignalRecord record, StringBuilder builder)
        {
            builder.Clear();
            Line(builder, "Game reports", ReportText(record), record.Reported);
            Line(builder, "Mod data", record.Modded ? ModText(record.ModKeys) : "None", record.Modded);
            Line(builder, "Name", NameText(record.Name), record.OddName);
            Line(builder, "Quest tweaks", record.Tweaked ? OverrideText(record.QuestOverrides) : "None reported", record.Tweaked);
            Line(builder, "Cosmetics", CosmeticsText(record), record.Unowned);
            Line(builder, "Speed", SpeedText(record), record.Fast);
            Line(builder, "Teleports", record.Teleports.ToString(CultureInfo.InvariantCulture), record.Teleporting);
            Line(builder, "Hovering", record.LongestHover >= 1f ? $"Longest {Decimal(record.LongestHover)} s off the ground" : "No", record.Hovering);
            Line(builder, "Arm reach", ReachText(record), record.LongArms);
            Line(builder, "Tag reach", TagText(record), record.FarTagging);
            return builder.ToString();
        }

        internal static string RowSuffix(SignalRecord? record)
        {
            if (record == null || record.Count == 0) return string.Empty;
            return record.Count == 1 ? "  [1 signal]" : $"  [{record.Count} signals]";
        }

        private static void Line(StringBuilder builder, string label, string value, bool raised) =>
            InGameDetails.Append(builder, label, $"<noparse>{value}</noparse>", raised ? RaisedColor : ClearColor);

        private static string ModText(string[] keys)
        {
            string shown = string.Join(", ", keys.Take(ShownModKeys));
            return keys.Length > ShownModKeys ? $"{shown} +{keys.Length - ShownModKeys} more" : shown;
        }

        private static string ReportText(SignalRecord record)
        {
            IReadOnlyList<string> reasons = record.ReportReasons;
            if (reasons.Count == 0) return "None";
            string first = reasons[0].Length > ReasonLength ? reasons[0].Substring(0, ReasonLength - Ellipsis.Length) + Ellipsis : reasons[0];
            return reasons.Count > 1 ? $"{first} +{reasons.Count - 1} more" : first;
        }

        private static string NameText(NameIssue issue) => issue switch
        {
            NameIssue.Banned => "Has a word the game bans in names",
            NameIssue.Symbols => "Symbols the game's keyboard lacks",
            NameIssue.TooLong => "Over 12 letters",
            _ => "Normal"
        };

        private static string OverrideText(SystemProperties overrides)
        {
            string[] names = OverrideNames.Where(entry => (overrides & entry.Flag) != 0).Select(entry => entry.Name).ToArray();
            string shown = string.Join(", ", names.Take(ShownOverrides));
            return names.Length > ShownOverrides ? $"{shown} +{names.Length - ShownOverrides} more" : shown;
        }

        private static string CosmeticsText(SignalRecord record)
        {
            if (record.UnownedCosmetics < 0) return "Not loaded yet";
            if (record.UnownedCosmetics == 0) return "All owned";
            string count = record.UnownedCosmetics == 1 ? "1 worn" : $"{record.UnownedCosmetics} worn";
            return record.Unowned ? $"{count} that they do not own" : $"Checking {count}";
        }

        private static string SpeedText(SignalRecord record)
        {
            string top = $"Top {Decimal(record.TopSpeed)} m/s";
            return record.Fast ? $"{top}, over {Whole(SignalRecord.FastSpeed)} for {Decimal(record.LongestFast)} s" : top;
        }

        private static string ReachText(SignalRecord record)
        {
            string longest = $"{record.LongestReach.ToString("0.00", CultureInfo.InvariantCulture)} m";
            return record.ReachSeconds > 0f ? $"{longest}, past {Decimal(SignalRecord.LongReach)} m for {Decimal(record.ReachSeconds)} s" : longest;
        }

        private static string TagText(SignalRecord record)
        {
            if (record.Tags == 0) return "No tags seen";
            string tags = record.Tags == 1 ? "1 tag" : $"{record.Tags} tags";
            string far = record.FarTags > 0 ? $", {record.FarTags} from {Whole(SignalRecord.FarTagDistance)}+ m" : string.Empty;
            return $"{tags}, longest {Decimal(record.LongestTag)} m{far}";
        }

        private static string Decimal(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);

        private static string Whole(float value) => value.ToString("0", CultureInfo.InvariantCulture);
    }
}
