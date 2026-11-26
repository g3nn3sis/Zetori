// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Globalization;
using System.Text;
using UnityEngine;
using Zetori.Cheats.Signals;

namespace Zetori.UI.InGame
{
    internal static class InGameSignalReport
    {
        internal const int Lines = 22;
        private const int ShownModKeys = 3;
        private const int ReasonLength = 36;
        private const string Ellipsis = "...";
        private const int ShownOverrides = 3;
        private const string RaisedColor = "FF8C1A";
        private const string ClearColor = "8CE08C";
        private const string Caveat =
            "Hints, not proof. Game reports are the game's own cheat checks, run on your headset. Lag, launchers, ropes and gliders can raise the speed, teleport and tag signals; bad connections can raise lag tricks, and doors or moving platforms can raise through walls. The game caps arm reach at 1 m. Quest tweaks are headset settings changed with developer tools; only Quest headsets report them.";

        private static readonly (SystemProperties Flag, string Name)[] OverrideNames =
        {
            (SystemProperties.CPULevel, "CPU level"),
            (SystemProperties.GPULevel, "GPU level"),
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
            return $"Cheat and mod signals: {raised}, watched {DisplayFormat.Clock(record.Watched)}.{lobby}";
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
            Line(builder, "Arm reach", ReachText(record), record.LongArms);
            Line(builder, "Hand motion", HandText(record.Hands), record.InhumanHands);
            Line(builder, "Tag reach", TagText(record), record.FarTagging);
            Line(builder, "Tag bursts", BurstText(record.TagBursts), record.BurstTagging);
            Line(builder, "Gravity", GravityText(record.Flight), record.LowGravity);
            Line(builder, "Network", NetworkText(record.Messages), record.Spamming);
            Line(builder, "Walls", WallText(record.WallPasses), record.ThroughWalls);
            Line(builder, "Under the map", SecondsText(record.UnderMap, "below the floor"), record.OutOfBounds);
            Line(builder, "Head turning", HeadText(record.Head), record.SpinningHead);
            Line(builder, "Colour and name", IdentityText(record), record.IdentitySpam);
            Line(builder, "Projectiles", BurstText(record.Projectiles, "launch", "launches"), record.ProjectileSpam);
            Line(builder, "Master client", record.TookMaster ? "Made themselves master" : "No", record.TookMaster);
            Line(builder, "Properties", CountText(record.Tampering, "changed without being master", "change", "changes"), record.Tampered);
            Line(builder, "Timing", TimingText(record.Timing), record.LagTricks);
            Line(builder, "Flagged before", PastText(record), record.FlaggedBefore);
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
            string text = $"Top {DisplayFormat.Decimal(record.TopSpeed)} m/s";
            if (record.LongestFast >= SignalRecord.FastSeconds) text += $", over {Whole(SignalRecord.FastSpeed)} for {DisplayFormat.Decimal(record.LongestFast)} s";
            if (record.FastPushes > 0)
                text += $", {Pushes(record.FastPushes)} past {DisplayFormat.Decimal(SignalRecord.RoleMargin)}x their {DisplayFormat.Decimal(record.LimitAtFastest)} m/s limit";
            return text;
        }

        private static string ReachText(SignalRecord record)
        {
            string longest = $"{record.LongestReach.ToString("0.00", CultureInfo.InvariantCulture)} m";
            string text = record.ReachSeconds > 0f ? $"{longest}, past {DisplayFormat.Decimal(SignalRecord.LongReach)} m for {DisplayFormat.Decimal(record.ReachSeconds)} s" : longest;
            return record.LongAirReachSeconds > 0f ? $"{text}, past {DisplayFormat.Decimal(SignalRecord.AirReach)} m in the air for {DisplayFormat.Decimal(record.LongAirReachSeconds)} s" : text;
        }

        private static string HandText(HandMotion hands)
        {
            string top = $"Top {DisplayFormat.Decimal(hands.TopSpeed)} m/s";
            if (hands.Snaps == 0 && hands.FastSeconds <= 0f && hands.OverreachSeconds <= 0f) return top;
            string snaps = hands.Snaps == 1 ? "1 jerk" : $"{hands.Snaps} jerks";
            return $"{top}, {snaps}, flung out {DisplayFormat.Decimal(hands.OverreachSeconds)} s";
        }

        private static string TagText(SignalRecord record)
        {
            if (record.Tags == 0) return "No tags seen";
            string tags = record.Tags == 1 ? "1 tag" : $"{record.Tags} tags";
            string far = record.FarTags > 0 ? $", {record.FarTags} from {Whole(SignalRecord.FarTagDistance)}+ m" : string.Empty;
            return $"{tags}, longest {DisplayFormat.Decimal(record.LongestTag)} m{far}";
        }

        private static string Pushes(int count) => count == 1 ? "1 push" : $"{count} pushes";

        private static string BurstText(TagBurst bursts)
        {
            if (bursts.Bursts == 0) return "None";
            string count = bursts.Bursts == 1 ? "1 burst" : $"{bursts.Bursts} bursts";
            return $"{count}, up to {bursts.LargestBurst} tags in {DisplayFormat.Decimal(TagBurst.Window)} s";
        }

        private static string GravityText(BodyFlight flight)
        {
            if (float.IsPositiveInfinity(flight.WeakestPull)) return "No free fall measured yet";
            string weakest = $"Weakest pull {Percent(flight.WeakestPull)}% of gravity";
            return flight.FloatSeconds > 0f ? $"{weakest}, under {Percent(BodyFlight.LowPull)}% for {DisplayFormat.Decimal(flight.FloatSeconds)} s" : weakest;
        }

        private static string NetworkText(MessageRate messages)
        {
            string top = $"Top {messages.TopRate} messages/s";
            if (messages.SpamSeconds == 0) return top;
            string seconds = messages.SpamSeconds == 1 ? "1 s" : $"{messages.SpamSeconds} s";
            return $"{top}, {seconds} far above the room";
        }

        private static string WallText(TalliedSignal passes)
        {
            int count = Mathf.RoundToInt(passes.Total);
            if (count == 0) return "None";
            return count == 1 ? "Passed through 1 wall" : $"Passed through {count} walls";
        }

        private static string SecondsText(TalliedSignal seconds, string what) =>
            seconds.Total > 0f ? $"{DisplayFormat.Decimal(seconds.Total)} s {what}" : "No";

        private static string CountText(TalliedSignal tally, string what, string one, string many)
        {
            int count = Mathf.RoundToInt(tally.Total);
            if (count == 0) return "None";
            return count == 1 ? $"1 {one} {what}" : $"{count} {many} {what}";
        }

        private static string HeadText(HeadSpin head)
        {
            string fastest = $"Fastest {Whole(head.FastestTurn)} deg/s";
            return head.SpinSeconds > 0f ? $"{fastest}, past {Whole(HeadSpin.SpinDegrees / HeadSpin.Window)} for {DisplayFormat.Decimal(head.SpinSeconds)} s" : fastest;
        }

        private static string IdentityText(SignalRecord record)
        {
            string colour = record.ColorChanges.Bursts == 0 ? "Colour normal" : $"Colour bursts {record.ColorChanges.Bursts}, up to {record.ColorChanges.Busiest} in {DisplayFormat.Decimal(record.ColorChanges.Window)} s";
            int names = Mathf.RoundToInt(record.NameChanges.Total);
            return names == 0 ? colour : $"{colour}, {names} name {(names == 1 ? "change" : "changes")}";
        }

        private static string BurstText(RateBurst bursts, string one, string many)
        {
            if (bursts.Events == 0) return "None seen";
            string events = bursts.Events == 1 ? $"1 {one}" : $"{bursts.Events} {many}";
            return $"{events}, up to {bursts.Busiest} in {DisplayFormat.Decimal(bursts.Window)} s";
        }

        private static string TimingText(SyncTiming timing)
        {
            if (timing.Gaps == 0 && timing.Backwards == 0 && timing.SkewedSeconds == 0) return "Normal";
            return $"{timing.Gaps} stall jumps, {timing.Backwards} backwards, {timing.SkewedSeconds} s out of step";
        }

        private static string PastText(SignalRecord record)
        {
            if (!record.PastChecked) return "Checking...";
            return record.FlaggedBefore ? $"{SignalNames.Describe(record.PastSignals)}, {Age(record.PastFlagged)}" : "No";
        }

        private static string Age(DateTime then)
        {
            TimeSpan age = DateTime.Now - then;
            if (age.TotalHours < 1d) return "under an hour ago";
            if (age.TotalDays < 1d) return $"{(int)age.TotalHours} h ago";
            return age.TotalDays < 2d ? "yesterday" : $"{(int)age.TotalDays} days ago";
        }

        private static string Percent(float fraction) => Mathf.RoundToInt(fraction * 100f).ToString(CultureInfo.InvariantCulture);

        private static string Whole(float value) => value.ToString("0", CultureInfo.InvariantCulture);
    }
}
