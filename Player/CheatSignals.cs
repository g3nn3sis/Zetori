// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Zetori.Base;

namespace Zetori.Player
{
    internal sealed class CheatSignals : ObjectComponent
    {
        private const float SampleInterval = 0.05f;
        private const float MaximumStep = 0.25f;
        private const float ProfileInterval = 2f;
        private const float HoverHeight = 2.5f;
        private const float HoverFallSpeed = -0.5f;
        private const float HandContactRadius = 0.2f;
        private const float BodyContactRadius = 0.45f;
        private const float MinimumScale = 0.01f;
        private const float DuplicateTagWindow = 0.5f;
        private const int MaximumNameLength = 12;
        private const string GameModeKey = "gameMode";
        private const string ModdedMarker = "MODDED";

        private static readonly HashSet<string> GameKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "didTutorial", "platform", "mothershipId", "gtfcExpirationTime", "isRedTeam", ZetoriUsers.PropertyKey
        };

        private static readonly Dictionary<string, string> ReadableReasons = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["invlProj"] = "invalid projectile",
            ["NmError"] = "name error"
        };

        private static readonly (string Prefix, string Replacement)[] ReadablePrefixes =
        {
            ("inappropriate tag data being sent ", "bad data: "),
            ("too many rpc calls! ", "RPC spam: ")
        };

        private static readonly AccessTools.FieldRef<VRRig, bool>? CosmeticsReady = ReadyAccessor();
        private static readonly Dictionary<int, SignalRecord> records = new Dictionary<int, SignalRecord>();

        private static string room = string.Empty;
        private static (int Tagged, int Tagging, float Time) lastTag = (-1, -1, float.NegativeInfinity);

        private float nextSample;
        private float lastSample;
        private float nextProfile;

        internal static bool ModdedLobby =>
            PhotonNetwork.CurrentRoom != null
            && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameModeKey, out object mode)
            && mode is string name
            && name.IndexOf(ModdedMarker, StringComparison.OrdinalIgnoreCase) >= 0;

        internal static SignalRecord? For(VRRig rig) =>
            rig.isOfflineVRRig || rig.Creator == null ? null : records.TryGetValue(rig.Creator.ActorNumber, out SignalRecord record) ? record : null;

        internal static void ObserveTag(NetPlayer tagged, NetPlayer tagging)
        {
            float now = Time.unscaledTime;
            if (lastTag.Tagged == tagged.ActorNumber && lastTag.Tagging == tagging.ActorNumber && now - lastTag.Time < DuplicateTagWindow) return;
            lastTag = (tagged.ActorNumber, tagging.ActorNumber, now);
            if (tagging.IsLocal) SessionStats.CountTag();
            if (!TryRig(tagging, out VRRig tagger) || !TryRig(tagged, out VRRig target) || tagger.isOfflineVRRig) return;
            RecordOf(tagging.ActorNumber).ObserveTag(TagDistance(tagger, target) / ScaleOf(tagger));
        }

        internal static void ObserveSync(VRRig rig, bool flaggedTeleport)
        {
            if (!IsRemote(rig)) return;
            RecordOf(rig.Creator.ActorNumber).ObservePosition(rig.syncPos, Time.unscaledTime, ScaleOf(rig), flaggedTeleport);
        }

        internal static void ObserveReport(string? userId, string? reason)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(reason) || PhotonNetwork.CurrentRoom == null) return;
            foreach (Photon.Realtime.Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (player.IsLocal || player.UserId != userId) continue;
                RecordOf(player.ActorNumber).ObserveReport(ReadableReason(reason!));
                return;
            }
        }

        internal void Update()
        {
            float now = Time.unscaledTime;
            try
            {
                if (!TrackRoom()) return;
                if (now >= nextSample)
                {
                    float elapsed = lastSample > 0f ? Mathf.Min(now - lastSample, MaximumStep) : SampleInterval;
                    lastSample = now;
                    nextSample = now + SampleInterval;
                    Sample(elapsed);
                }
                if (now >= nextProfile)
                {
                    nextProfile = now + ProfileInterval;
                    ReadProfiles();
                }
            }
            catch (Exception e) { Log.ExceptionOnce("cheat-signals", e, "Watching players for cheat signals failed"); }
        }

        private static bool TrackRoom()
        {
            string current = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : string.Empty;
            if (current != room)
            {
                room = current;
                records.Clear();
            }
            return current.Length > 0;
        }

        private static void Sample(float deltaTime)
        {
            int layers = GTPlayer.LocomotionEnabledLayers;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (!IsRemote(rig)) continue;
                float scale = ScaleOf(rig);
                Vector3 velocity = rig.LatestVelocity();
                RecordOf(rig.Creator.ActorNumber).Observe(deltaTime, velocity.magnitude / scale, IsHovering(rig, velocity, scale, layers), Reach(rig) / scale);
            }
        }

        private static void ReadProfiles()
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (!IsRemote(rig)) continue;
                SignalRecord record = RecordOf(rig.Creator.ActorNumber);
                record.ObserveProfile(ModKeys(rig.Creator.ActorNumber), NameIssueOf(rig.Creator.NickName), QuestOverrides(rig));
                if (UnownedCosmetics(rig) is int unowned) record.ObserveCosmetics(unowned);
            }
        }

        private static bool IsRemote(VRRig? rig) => rig != null && !rig.isOfflineVRRig && rig.Creator != null && rig.Creator.InRoom;

        private static SignalRecord RecordOf(int actor)
        {
            if (records.TryGetValue(actor, out SignalRecord record)) return record;
            record = new SignalRecord();
            records[actor] = record;
            return record;
        }

        private static bool TryRig(NetPlayer player, out VRRig rig)
        {
            foreach (VRRig candidate in VRRigCache.ActiveRigs)
            {
                if (candidate == null || candidate.Creator == null || candidate.Creator.ActorNumber != player.ActorNumber) continue;
                rig = candidate;
                return true;
            }
            rig = null!;
            return false;
        }

        private static float ScaleOf(VRRig rig) => Mathf.Max(MinimumScale, rig.scaleFactor);

        private static bool IsHovering(VRRig rig, Vector3 velocity, float scale, int layers)
        {
            if (velocity.y < HoverFallSpeed * scale) return false;
            Vector3 body = rig.transform.position;
            if (Physics.Raycast(body, Vector3.down, HoverHeight * scale, layers, QueryTriggerInteraction.Ignore)) return false;
            if (Touching(body, BodyContactRadius * scale, layers)) return false;
            return !Touching(PositionOf(rig.leftHand, body), HandContactRadius * scale, layers)
                && !Touching(PositionOf(rig.rightHand, body), HandContactRadius * scale, layers);
        }

        private static bool Touching(Vector3 point, float radius, int layers) =>
            Physics.CheckSphere(point, radius, layers, QueryTriggerInteraction.Ignore);

        private static float Reach(VRRig rig)
        {
            Vector3 head = PositionOf(rig.head, rig.transform.position);
            return Mathf.Max(Vector3.Distance(head, PositionOf(rig.leftHand, head)), Vector3.Distance(head, PositionOf(rig.rightHand, head)));
        }

        private static float TagDistance(VRRig tagger, VRRig target)
        {
            Vector3 body = target.transform.position;
            Vector3 head = PositionOf(target.head, body);
            float closest = float.PositiveInfinity;
            foreach (Vector3 hand in new[] { PositionOf(tagger.leftHand, tagger.transform.position), PositionOf(tagger.rightHand, tagger.transform.position) })
                closest = Mathf.Min(closest, Vector3.Distance(hand, body), Vector3.Distance(hand, head));
            return closest;
        }

        private static Vector3 PositionOf(VRMap? map, Vector3 fallback) =>
            map != null && map.rigTarget != null ? map.rigTarget.position : fallback;

        private static string[] ModKeys(int actor)
        {
            Photon.Realtime.Player? player = PhotonNetwork.CurrentRoom?.GetPlayer(actor);
            if (player == null) return Array.Empty<string>();
            return player.CustomProperties.Keys
                .Select(key => key?.ToString() ?? string.Empty)
                .Where(key => key.Length > 0 && !GameKeys.Contains(key))
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static SystemProperties QuestOverrides(VRRig rig)
        {
            RigContainer? container = rig.GetComponent<RigContainer>();
            return container != null ? container.PlayerStats.SystemPropertiesFlags : 0;
        }

        private static string ReadableReason(string reason)
        {
            string readable = reason.Trim();
            if (ReadableReasons.TryGetValue(readable, out string named)) readable = named;
            foreach ((string prefix, string replacement) in ReadablePrefixes)
                if (readable.StartsWith(prefix, StringComparison.Ordinal)) readable = replacement + readable.Substring(prefix.Length);
            return readable.Length > 0 ? char.ToUpperInvariant(readable[0]) + readable.Substring(1) : readable;
        }

        private static NameIssue NameIssueOf(string? name)
        {
            if (name == null || name.Length == 0) return NameIssue.None;
            if (IsBannedName(name)) return NameIssue.Banned;
            if (!name.All(IsTypeable)) return NameIssue.Symbols;
            return name.Length > MaximumNameLength ? NameIssue.TooLong : NameIssue.None;
        }

        private static bool IsTypeable(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');

        private static bool IsBannedName(string name)
        {
            GorillaComputer? computer = GorillaComputer.instance;
            if (computer == null) return false;
            try
            {
                return !computer.CheckAutoBanListForName(name);
            }
            catch (Exception e)
            {
                Log.ExceptionOnce("cheat-signals-name", e, "Checking a player's name against the game's banned words failed");
                return false;
            }
        }

        private static int? UnownedCosmetics(VRRig rig)
        {
            if (CosmeticsReady == null || !CosmeticsReady(rig)) return null;
            CosmeticsController.CosmeticItem[]? items = rig.cosmeticSet?.items;
            if (items == null) return null;
            try
            {
                int unowned = 0;
                foreach (CosmeticsController.CosmeticItem item in items)
                    if (!item.isNullItem && !string.IsNullOrEmpty(item.itemName) && !rig.IsItemAllowed(item.itemName)) unowned++;
                return unowned;
            }
            catch (Exception e)
            {
                Log.ExceptionOnce("cheat-signals-cosmetics", e, "Checking a player's cosmetics failed");
                return null;
            }
        }

        private static AccessTools.FieldRef<VRRig, bool>? ReadyAccessor()
        {
            if (AccessTools.Field(typeof(VRRig), "initializedCosmetics") == null)
            {
                Log.Warning("Cosmetic ownership cannot be read in this game version; the cosmetics signal is off.");
                return null;
            }
            return AccessTools.FieldRefAccess<VRRig, bool>("initializedCosmetics");
        }
    }
}
