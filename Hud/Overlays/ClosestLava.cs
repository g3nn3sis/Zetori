// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base.Config;
using Zetori.Player;
using Zetori.Replay;
using Zetori.Replay.Playback;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace Zetori.Hud.Overlays
{
    internal sealed class ClosestLava : HudOverlay
    {
        internal static new bool enabled { get => CasterConfig.ClosestLavaEnabled.Value; set => CasterConfig.ClosestLavaEnabled.Value = value; }

        private const decimal NoDistance = -1m;
        private const float LargestRoundedDistance = 1e9f;

        private RectTransform? root;
        private TMP_Text? value;
        private Vector2 basePos;
        private decimal? shownTenths;

        protected override bool Active => enabled && (ReplaySession.Player is ReplayPlayer replay
            ? replay.Spectated != null
            : PhotonNetwork.InRoom && NetworkedPlayerManager.selectedPlayer != null);

        protected override GameObject? Prefab => HudAssets.ClosestLavaPrefab;

        protected override bool Bind(GameObject canvas)
        {
            root = canvas.transform.Find("ClosestLava") as RectTransform;
            value = root != null ? root.Find("Value")?.GetComponent<TMP_Text>() : null;
            if (root == null || value == null) return false;
            basePos = root.anchoredPosition;
            return true;
        }

        protected override void Refresh()
        {
            Place(root!, basePos, CasterConfig.ClosestLavaScale, CasterConfig.ClosestLavaOffsetX, CasterConfig.ClosestLavaOffsetY);
            ApplyFontIfChanged();

            float? closest = ReplaySession.Player is ReplayPlayer replay ? ReplayClosest(replay) : LiveClosest();
            if (closest != null) ShowDistance(closest.Value);
        }

        private void ShowDistance(float distance)
        {
            decimal? tenths = Tenths(distance);
            if (tenths != null && tenths == shownTenths) return;
            shownTenths = tenths;
            string text = float.IsPositiveInfinity(distance) ? "--" : $"{distance:0.0}m";
            value!.text = $"CLOSEST LAVA  <color=#FFFFFF>{text}</color>";
        }

        private static decimal? Tenths(float distance)
        {
            if (float.IsPositiveInfinity(distance)) return NoDistance;
            if (distance >= LargestRoundedDistance) return null;
            return Math.Round((decimal)distance, 1, MidpointRounding.AwayFromZero);
        }

        private static float? LiveClosest()
        {
            VRRig? target = NetworkedPlayerManager.selectedPlayer;
            if (target == null) return null;
            float best = float.PositiveInfinity;
            Vector3 from = target.transform.position;
            foreach (VRRig rig in NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>())
            {
                if (rig == null || !RigTagState.IsLava(rig)) continue;
                float distance = Vector3.Distance(from, rig.transform.position);
                if (distance < best) best = distance;
            }
            return best;
        }

        private static float? ReplayClosest(ReplayPlayer replay)
        {
            ReplayPuppet? target = replay.Spectated;
            if (target == null) return null;
            float best = float.PositiveInfinity;
            Vector3 from = target.Body.position;
            foreach (ReplayPuppet puppet in replay.Puppets)
            {
                if (!puppet.Visible || puppet.Current.Tag != ReplayTagState.Lava) continue;
                float distance = Vector3.Distance(from, puppet.Body.position);
                if (distance < best) best = distance;
            }
            return best;
        }
    }
}
