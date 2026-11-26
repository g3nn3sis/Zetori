// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.Base.Config;
using Zetori.Replay;
using GorillaGameModes;
using UnityEngine;

namespace Zetori.TagManager
{
    internal sealed class TagManagerControl : ObjectComponent
    {
        private const float EnforceInterval = 0.25f;
        private const float TagMessageInterval = 0.25f;
        private const string FallbackName = "Player";
        private const string LocalName = "You";
        private const string NobodyTagged = "None";
        private const string NameSeparator = ", ";

        private static float nextEnforce;
        private static float nextTagMessage;

        internal static bool ReadyToTag => Time.unscaledTime >= nextTagMessage;

        internal static bool CanControl =>
            !ReplaySession.IsReplaying &&
            InPrivateRoom &&
            Network!.IsMasterClient &&
            TagManager != null;

        internal static string Status
        {
            get
            {
                if (ReplaySession.IsReplaying) return "Unavailable in replay.";
                if (Network == null || !Network.InRoom) return "Join a private room to control tags.";
                if (!Network.SessionIsPrivate) return "Only works in private rooms.";
                if (!Network.IsMasterClient) return "Only the master can control tags.";
                if (TagManager == null) return "Only works in an infection tag room.";
                if (!CasterConfig.TagManagerEnabled.Value) return "Ready to take control.";
                string mode = IsInfection ? "Infection: several players can be tagged." : "Tag: one player is tagged at a time.";
                return CasterConfig.TagManagerLocalFirst.Value ? $"{mode} You start every round tagged." : mode;
            }
        }

        internal static bool AnyoneTagged => TaggedPlayers.Any();

        internal static string TaggedNames
        {
            get
            {
                string names = string.Join(NameSeparator, TaggedPlayers.Select(NameOf));
                return names.Length == 0 ? NobodyTagged : names;
            }
        }

        internal static NetPlayer? LocalPlayer => Network != null ? Network.LocalPlayer : null;

        private static bool IsInfection
        {
            get
            {
                GorillaTagManager? manager = TagManager;
                return manager != null && !manager.isCurrentlyTag;
            }
        }

        private static IEnumerable<NetPlayer> TaggedPlayers
        {
            get
            {
                GorillaTagManager? manager = TagManager;
                if (manager == null) return Enumerable.Empty<NetPlayer>();
                IEnumerable<NetPlayer> tagged = manager.isCurrentlyTag ? new[] { manager.currentIt } : manager.currentInfected;
                return tagged.Where(player => player != null);
            }
        }

        private static int LocalFirstIndex
        {
            get
            {
                if (!CasterConfig.TagManagerLocalFirst.Value || !Active) return -1;
                NetPlayer? local = LocalPlayer;
                return GameMode.ParticipatingPlayers.FindIndex(player => SamePlayer(player, local));
            }
        }

        private static NetworkSystem? Network => NetworkSystem.Instance;

        private static bool InPrivateRoom => Network != null && Network.InRoom && Network.SessionIsPrivate;

        private static bool Active => CasterConfig.TagManagerEnabled.Value && CanControl;

        private static bool HoldsTags => Active && !CasterConfig.TagManagerLocalFirst.Value;

        private static GorillaTagManager? TagManager =>
            GorillaGameManager.instance is GorillaTagManager manager && manager.GameType() == GameModeType.Infection ? manager : null;

        internal static bool Controls(GorillaTagManager manager) => HoldsTags && ReferenceEquals(manager, TagManager);

        internal static bool BlocksPlayerTags(GorillaTagManager manager) =>
            Controls(manager) && (CasterConfig.TagManagerRefereeOnly.Value || !CasterConfig.TagManagerPlayerTags.Value);

        internal static bool HandlesPlayerLeaving(GorillaTagManager manager) =>
            Controls(manager) || (InPrivateRoom && manager.isCurrentlyTag && manager.currentIt == null);

        internal void LateUpdate()
        {
            if (!HoldsTags || Time.unscaledTime < nextEnforce) return;
            nextEnforce = Time.unscaledTime + EnforceInterval;
            try { Enforce(TagManager!); }
            catch (Exception e) { Log.ExceptionOnce("tag-manager-enforce", e, "Holding the room's tag state failed"); }
        }

        internal static void TagLocal()
        {
            NetPlayer? local = LocalPlayer;
            if (local != null) Tag(local);
        }

        internal static void Tag(NetPlayer target)
        {
            if (!target.InRoom) return;
            Apply(manager =>
            {
                if (manager.isCurrentlyTag) SetIt(manager, target);
                else Infect(manager, target);
            });
        }

        internal static void Untag(NetPlayer target) => Apply(manager =>
        {
            if (!manager.isCurrentlyTag) manager.currentInfected.RemoveAll(infected => SamePlayer(infected, target));
            else if (SamePlayer(manager.currentIt, target)) manager.currentIt = null;
        });

        internal static void ClearTags() => Apply(manager =>
        {
            manager.currentIt = null;
            manager.ClearInfectionState();
            manager.lastTag = Time.time;
            manager.lastTaggedActorNr.Clear();
        });

        internal static void Toggle(VRRig? rig)
        {
            NetPlayer? player = rig != null ? rig.Creator : null;
            if (player == null) return;
            if (IsTagged(player)) Untag(player);
            else Tag(player);
        }

        internal static bool IsTagged(VRRig? rig) => rig != null && IsTagged(rig.Creator);

        internal static bool IsTagged(NetPlayer? player)
        {
            GorillaTagManager? manager = TagManager;
            if (manager == null || player == null) return false;
            return manager.isCurrentlyTag ? SamePlayer(manager.currentIt, player) : IsInfected(manager, player);
        }

        internal static string NameOf(NetPlayer? player)
        {
            if (player == null) return FallbackName;
            if (player.IsLocal) return LocalName;
            return string.IsNullOrEmpty(player.NickName) ? FallbackName : player.NickName;
        }

        internal static int PickFirstTagged(int minInclusive, int maxExclusive)
        {
            int local = LocalFirstIndex;
            return local >= minInclusive && local < maxExclusive ? local : UnityEngine.Random.Range(minInclusive, maxExclusive);
        }

        internal static void ForgetPlayer(GorillaTagManager manager, NetPlayer player)
        {
            manager.currentNetPlayerArray = NetworkSystem.Instance.AllNetPlayers;
            manager.lastTaggedActorNr.Remove(player.ActorNumber);
            manager.currentInfected.RemoveAll(infected => SamePlayer(infected, player));
            if (SamePlayer(manager.currentIt, player)) manager.currentIt = null;
        }

        private static void Apply(Action<GorillaTagManager> change)
        {
            GorillaTagManager? manager = TagManager;
            if (manager == null || !Active) return;
            try
            {
                if (HoldsTags) MatchRoomSize(manager);
                change(manager);
            }
            catch (Exception e) { Log.Exception(e, "Changing the room's tag failed"); }
        }

        private static void Enforce(GorillaTagManager manager)
        {
            MatchRoomSize(manager);
            DropDepartedPlayers(manager);
            NetPlayer? local = LocalPlayer;
            if (CasterConfig.TagManagerRefereeOnly.Value && local != null) KeepOnlyTagged(manager, local);
        }

        private static void MatchRoomSize(GorillaTagManager manager)
        {
            if (manager.waitingToStartNextInfectionGame)
            {
                manager.waitingToStartNextInfectionGame = false;
                manager.timeInfectedGameEnded = 0;
            }
            bool wantsInfection = GameMode.ParticipatingPlayers.Count >= manager.infectedModeThreshold;
            bool inInfection = !manager.isCurrentlyTag;
            if (wantsInfection == inInfection)
            {
                if (!inInfection) manager.currentInfected.Clear();
                return;
            }
            if (!TrySpendTagMessage()) return;
            if (wantsInfection) BeginInfection(manager);
            else BeginTag(manager);
        }

        private static void BeginInfection(GorillaTagManager manager)
        {
            NetPlayer? it = manager.currentIt;
            manager.SetisCurrentlyTag(false);
            manager.currentInfected.Clear();
            if (it != null && it.InRoom) manager.currentInfected.Add(it);
        }

        private static void BeginTag(GorillaTagManager manager)
        {
            manager.SetisCurrentlyTag(true);
            manager.currentIt = manager.currentInfected.Find(infected => infected != null && infected.InRoom);
            manager.currentInfected.Clear();
        }

        private static void DropDepartedPlayers(GorillaTagManager manager)
        {
            NetPlayer? it = manager.currentIt;
            if (it != null && !it.InRoom) manager.currentIt = null;
            manager.currentInfected.RemoveAll(infected => infected == null || !infected.InRoom);
        }

        private static void KeepOnlyTagged(GorillaTagManager manager, NetPlayer local)
        {
            if (manager.isCurrentlyTag)
            {
                if (!SamePlayer(manager.currentIt, local)) SetIt(manager, local);
                return;
            }
            manager.currentInfected.RemoveAll(infected => !SamePlayer(infected, local));
            Infect(manager, local);
        }

        private static void SetIt(GorillaTagManager manager, NetPlayer target)
        {
            if (!TrySpendTagMessage()) return;
            manager.currentIt = target;
            manager.lastTag = Time.time;
            manager.lastTaggedActorNr.Clear();
            manager.AddLastTagged(target, LocalPlayer ?? target);
            manager.UpdateTagState(false);
        }

        private static void Infect(GorillaTagManager manager, NetPlayer target)
        {
            if (IsInfected(manager, target) || !TrySpendTagMessage()) return;
            manager.AddLastTagged(target, LocalPlayer ?? target);
            manager.AddInfectedPlayer(target, false);
        }

        private static bool TrySpendTagMessage()
        {
            if (!ReadyToTag) return false;
            nextTagMessage = Time.unscaledTime + TagMessageInterval;
            return true;
        }

        private static bool IsInfected(GorillaTagManager manager, NetPlayer player)
        {
            foreach (NetPlayer infected in manager.currentInfected)
                if (SamePlayer(infected, player)) return true;
            return false;
        }

        private static bool SamePlayer(NetPlayer? first, NetPlayer? second) =>
            first != null && second != null && first.ActorNumber == second.ActorNumber;
    }
}
