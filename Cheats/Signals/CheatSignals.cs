// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Zetori.Base;
using Zetori.History;
using Zetori.Player;

namespace Zetori.Cheats.Signals
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
        private const float HandClearRadius = 0.3f;
        private const float BodyClearRadius = 0.8f;
        private const float TrafficInterval = 1f;
        private const float MaximumTrafficWindow = 2f;
        private const float TrafficJoinGrace = 10f;
        private const float SpamRatio = 4f;
        private const float SpamTicks = 5f;
        private const float MasterAllowance = 2f;
        private const float MinimumScale = 0.01f;
        private const float DuplicateTagWindow = 0.5f;
        private const int MaximumNameLength = 12;
        private const float AirReachMaximumSpeed = 6f;
        private const float WallMinimumMove = 0.3f;
        private const float WallMaximumMove = 6f;
        private const float WallThickness = 0.2f;
        private const float MapProbeHeight = 150f;
        private const float MapProbeDepth = 100f;
        private const float TapSurfaceRadius = 0.5f;
        private const float TapPlayerRadius = 1f;
        private const int MinimumTimingPeers = 2;
        private const string GameModeKey = "gameMode";
        private const string ModdedMarker = "MODDED";
        private const string NotMasterClaim = "when they weren't the master client";
        private const string OwnershipBurst = "too many rpc calls! SetOwnershipFromMasterClient";
        private const SystemProperties IgnoredQuestOverrides = SystemProperties.RefreshRate;
        private const SignalKind ProfileSignals = SignalKind.ModData | SignalKind.Name | SignalKind.QuestTweaks | SignalKind.Cosmetics | SignalKind.FlaggedBefore;

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
        private static readonly Dictionary<int, int> counts = new Dictionary<int, int>();
        private static readonly Dictionary<int, float> rates = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> offsets = new Dictionary<int, float>();
        private static readonly List<float> others = new List<float>();

        private static string room = string.Empty;
        private static float roomJoined;
        private static (int Tagged, int Tagging, float Time) lastTag = (-1, -1, float.NegativeInfinity);
        private static long syncs;

        private float nextSample;
        private float lastSample;
        private float nextProfile;
        private float nextTraffic;
        private float lastTraffic;

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
            RecordOf(tagging.ActorNumber).ObserveTag(now, TagDistance(tagger, target) / ScaleOf(tagger));
        }

        internal static void ObserveSync(VRRig rig, bool flaggedTeleport, double serverTime, bool grounded)
        {
            if (!IsRemote(rig)) return;
            float now = Time.unscaledTime;
            float scale = ScaleOf(rig);
            SignalRecord record = RecordOf(rig.Creator.ActorNumber);
            record.Timing.ObserveSync(now, serverTime, PhotonNetwork.Time, rig.syncPos, scale, ++syncs, flaggedTeleport);
            if (record.LastSync is Vector3 previous && !flaggedTeleport && !Carried(rig, record, now) && PassesThroughWall(previous, rig.syncPos, scale))
                record.WallPasses.Add(now, 1f);
            record.ObservePosition(rig.syncPos, now, scale, flaggedTeleport);
            bool touched = record.TakeTouch(grounded, rig.LastTouchedGroundAtNetworkTime) || flaggedTeleport;
            record.ObserveFlight(serverTime, rig.syncPos.y, touched, now, scale);
            float? limit = record.AssistedAt(now) ? null : RoleSpeed.LimitOf(rig) * MovementAssists.SurfaceBoost;
            record.ObservePush(now, Horizontal(rig.LatestVelocity()) / scale, limit, touched);
            if (TrySyncedOffset(rig, rig.head, out Vector3 head) && TrySyncedOffset(rig, rig.leftHand, out Vector3 left) && TrySyncedOffset(rig, rig.rightHand, out Vector3 right))
                record.ObserveHands(now, head, left, right);
        }

        private static bool TrySyncedOffset(VRRig rig, VRMap? map, out Vector3 offset)
        {
            Transform? target = map?.rigTarget;
            if (map == null || target == null)
            {
                offset = Vector3.zero;
                return false;
            }
            Transform root = rig.transform;
            Transform parent = target.parent != null ? target.parent : root;
            offset = rig.syncRotation * root.InverseTransformPoint(parent.TransformPoint(map.syncPos));
            return true;
        }

        internal static void ObserveHandTap(VRRig rig, bool isLeftHand, bool onStilt)
        {
            if (onStilt || !IsRemote(rig)) return;
            float scale = ScaleOf(rig);
            Vector3 hand = PositionOf(isLeftHand ? rig.leftHand : rig.rightHand, rig.transform.position);
            if (NearSurface(hand, TapSurfaceRadius * scale) || NearOtherPlayer(rig, hand, TapPlayerRadius * scale)) return;
            RecordOf(rig.Creator.ActorNumber).FakeTaps.Add(Time.unscaledTime, 1f);
        }

        internal static void ObserveProjectile(int actor) => RemoteRecord(actor)?.Projectiles.Observe(Time.unscaledTime);

        internal static void ObserveColorChange(int actor) => RemoteRecord(actor)?.ColorChanges.Observe(Time.unscaledTime);

        internal static void ObserveMasterClaim(int actor) => RemoteRecord(actor)?.MasterTakeovers.Add(Time.unscaledTime, 1f);

        internal static void ObserveTampering(int actor) => RemoteRecord(actor)?.Tampering.Add(Time.unscaledTime, 1f);

        internal static void ObserveReport(string? userId, string? reason)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(reason) || PhotonNetwork.CurrentRoom == null) return;
            foreach (Photon.Realtime.Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (player.IsLocal || player.UserId != userId) continue;
                if (IsExpectedReport(player, reason!)) return;
                RecordOf(player.ActorNumber).ObserveReport(ReadableReason(reason!));
                return;
            }
        }

        private static bool IsExpectedReport(Photon.Realtime.Player player, string reason) =>
            reason == OwnershipBurst || (player.IsMasterClient && reason.EndsWith(NotMasterClaim, StringComparison.Ordinal));

        protected override void Awake()
        {
            base.Awake();
            try { NetworkTraffic.Listen(); }
            catch (Exception e) { Log.Exception(e, "Listening for players' network messages failed; the network spam signal is off."); }
            try { PropertyChanges.Listen(); }
            catch (Exception e) { Log.Exception(e, "Listening for room property changes failed; the master and property signals are off."); }
        }

        protected override void OnDestroy()
        {
            NetworkTraffic.Stop();
            PropertyChanges.Stop();
            base.OnDestroy();
        }

        internal void Update()
        {
            float now = Time.unscaledTime;
            try
            {
                if (!TrackRoom()) return;
                MovementAssists.Refresh(now);
                if (now >= nextTraffic)
                {
                    float window = now - lastTraffic;
                    lastTraffic = now;
                    nextTraffic = now + TrafficInterval;
                    ReadTraffic(now, window);
                    ReadTiming(now);
                }
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
                roomJoined = Time.unscaledTime;
                records.Clear();
                NetworkTraffic.TakeCounts(counts);
            }
            return current.Length > 0;
        }

        private static void Sample(float deltaTime)
        {
            float now = Time.unscaledTime;
            int layers = GTPlayer.LocomotionEnabledLayers;
            int water = GTPlayer.Instance != null ? GTPlayer.Instance.waterLayer.value : 0;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (!IsRemote(rig)) continue;
                float scale = ScaleOf(rig);
                Vector3 velocity = rig.LatestVelocity();
                Vector3 body = rig.transform.position;
                Vector3 left = PositionOf(rig.leftHand, body);
                Vector3 right = PositionOf(rig.rightHand, body);
                bool contact = Touching(body, BodyContactRadius * scale, layers) || Touching(left, HandContactRadius * scale, layers) || Touching(right, HandContactRadius * scale, layers);
                SignalRecord record = RecordOf(rig.Creator.ActorNumber);
                float speed = velocity.magnitude / scale;
                float airReach = speed < AirReachMaximumSpeed && !record.AssistedAt(now) ? Reach(rig, scale, layers, false) / scale : 0f;
                record.Observe(deltaTime, speed, IsHovering(body, velocity, scale, layers, contact), Reach(rig, scale, layers, true) / scale, airReach);
                record.ObserveSurroundings(now, MovementAssists.Assists(rig), IsClear(body, left, right, scale, layers, water));
                if (rig.head?.rigTarget is Transform head) record.Head.Observe(now, head.rotation);
                if (!record.AssistedAt(now) && IsUnderMap(body, scale, layers)) record.UnderMap.Add(now, deltaTime);
            }
        }

        private static void ReadTraffic(float now, float window)
        {
            NetworkTraffic.TakeCounts(counts);
            if (window <= 0f || window > MaximumTrafficWindow || now - roomJoined < TrafficJoinGrace) return;
            rates.Clear();
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (!IsRemote(rig) || !(For(rig) is SignalRecord record) || now - record.Created < TrafficJoinGrace) continue;
                int actor = rig.Creator.ActorNumber;
                rates[actor] = Mathf.RoundToInt((counts.TryGetValue(actor, out int count) ? count : 0) / window);
            }
            float tick = PhotonNetwork.SerializationRate;
            int master = PhotonNetwork.CurrentRoom?.MasterClientId ?? -1;
            foreach (KeyValuePair<int, float> entry in rates)
            {
                float spamRate = Mathf.Max(SpamRatio * (MedianWithout(rates, entry.Key) ?? tick), SpamTicks * tick);
                records[entry.Key].ObserveTraffic(Mathf.RoundToInt(entry.Value), entry.Key == master ? spamRate * MasterAllowance : spamRate);
            }
        }

        private static void ReadTiming(float now)
        {
            if (now - roomJoined < TrafficJoinGrace) return;
            offsets.Clear();
            foreach (KeyValuePair<int, SignalRecord> entry in records)
                if (entry.Value.Timing.Fresh(now)) offsets[entry.Key] = entry.Value.Timing.Offset;
            if (offsets.Count <= MinimumTimingPeers) return;
            foreach (KeyValuePair<int, float> entry in offsets)
                if (MedianWithout(offsets, entry.Key) is float roomOffset) records[entry.Key].Timing.ObserveRoomOffset(now, roomOffset);
        }

        private static float? MedianWithout(Dictionary<int, float> values, int actor)
        {
            others.Clear();
            foreach (KeyValuePair<int, float> entry in values)
                if (entry.Key != actor) others.Add(entry.Value);
            if (others.Count == 0) return null;
            others.Sort();
            int middle = others.Count / 2;
            return others.Count % 2 == 1 ? others[middle] : (others[middle - 1] + others[middle]) * 0.5f;
        }

        private static void ReadProfiles()
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (!IsRemote(rig)) continue;
                SignalRecord record = RecordOf(rig.Creator.ActorNumber);
                string userId = rig.Creator.UserId ?? string.Empty;
                if (!record.PastChecked && userId.Length > 0) record.ObservePast(FlaggedPlayers.Find(userId));
                record.ObserveProfile(ModKeys(rig.Creator.ActorNumber), NameIssueOf(rig.Creator.NickName), QuestOverrides(rig));
                record.ObserveName(Time.unscaledTime, rig.Creator.NickName ?? string.Empty);
                if (UnownedCosmetics(rig) is int unowned) record.ObserveCosmetics(unowned);
                if (!ModdedLobby) FlaggedPlayers.Remember(userId, rig.Creator.NickName ?? string.Empty, record.Raised & ~ProfileSignals);
            }
        }

        private static bool IsRemote(VRRig? rig) => rig != null && !rig.isOfflineVRRig && rig.Creator != null && rig.Creator.InRoom;

        private static SignalRecord RecordOf(int actor)
        {
            if (records.TryGetValue(actor, out SignalRecord record)) return record;
            record = new SignalRecord(Time.unscaledTime);
            records[actor] = record;
            return record;
        }

        private static SignalRecord? RemoteRecord(int actor)
        {
            if (actor <= 0 || room.Length == 0) return null;
            Photon.Realtime.Player? player = PhotonNetwork.CurrentRoom?.GetPlayer(actor);
            return player == null || player.IsLocal ? null : RecordOf(actor);
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

        private static bool IsHovering(Vector3 body, Vector3 velocity, float scale, int layers, bool contact) =>
            velocity.y >= HoverFallSpeed * scale && !contact && !Physics.Raycast(body, Vector3.down, HoverHeight * scale, layers, QueryTriggerInteraction.Ignore);

        private static bool IsClear(Vector3 body, Vector3 left, Vector3 right, float scale, int layers, int water) =>
            !Touching(body, BodyClearRadius * scale, layers)
            && !Touching(left, HandClearRadius * scale, layers)
            && !Touching(right, HandClearRadius * scale, layers)
            && (water == 0 || !Physics.CheckSphere(body, BodyClearRadius * scale, water, QueryTriggerInteraction.Collide));

        private static float Horizontal(Vector3 velocity) => new Vector2(velocity.x, velocity.z).magnitude;

        private static bool Touching(Vector3 point, float radius, int layers) =>
            Physics.CheckSphere(point, radius, layers, QueryTriggerInteraction.Ignore);

        private static float Reach(VRRig rig, float scale, int layers, bool grounded)
        {
            Vector3 head = PositionOf(rig.head, rig.transform.position);
            return Mathf.Max(HandReach(head, PositionOf(rig.leftHand, head), scale, layers, grounded), HandReach(head, PositionOf(rig.rightHand, head), scale, layers, grounded));
        }

        private static float HandReach(Vector3 head, Vector3 hand, float scale, int layers, bool grounded) =>
            Touching(hand, HandContactRadius * scale, layers) == grounded ? Vector3.Distance(head, hand) : 0f;

        private static bool Carried(VRRig rig, SignalRecord record, float now) =>
            record.AssistedAt(now) || rig.mountedMovingSurfaceId != -1 || rig.portalShenanigansBit;

        private static bool PassesThroughWall(Vector3 from, Vector3 to, float scale)
        {
            float distance = Vector3.Distance(from, to);
            if (distance < WallMinimumMove * scale || distance > WallMaximumMove * scale) return false;
            int layers = GTPlayer.LocomotionEnabledLayers;
            if (!Physics.Linecast(from, to, out RaycastHit entry, layers, QueryTriggerInteraction.Ignore)) return false;
            if (!Physics.Linecast(to, from, out RaycastHit exit, layers, QueryTriggerInteraction.Ignore)) return false;
            if (entry.rigidbody != null || exit.rigidbody != null) return false;
            return Vector3.Distance(entry.point, exit.point) >= WallThickness * scale;
        }

        private static bool IsUnderMap(Vector3 body, float scale, int layers) =>
            Physics.Raycast(body + Vector3.up * MapProbeHeight, Vector3.down, MapProbeHeight - BodyClearRadius * scale, layers, QueryTriggerInteraction.Ignore)
            && !Physics.Raycast(body, Vector3.down, MapProbeDepth, layers, QueryTriggerInteraction.Ignore);

        private static bool NearSurface(Vector3 point, float radius)
        {
            int water = GTPlayer.Instance != null ? GTPlayer.Instance.waterLayer.value : 0;
            return Touching(point, radius, GTPlayer.LocomotionEnabledLayers)
                || (water != 0 && Physics.CheckSphere(point, radius, water, QueryTriggerInteraction.Collide));
        }

        private static bool NearOtherPlayer(VRRig tapping, Vector3 hand, float radius)
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
                if (rig != null && rig != tapping && Vector3.Distance(rig.transform.position, hand) < radius) return true;
            return false;
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
            return container != null ? container.PlayerStats.SystemPropertiesFlags & ~IgnoredQuestOverrides : 0;
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
