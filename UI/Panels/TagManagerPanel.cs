// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base.Config;
using Zetori.TagManager;
using UnityEngine;

namespace Zetori.UI.Panels
{
    internal sealed class TagManagerPanel
    {
        private const float RefreshInterval = 0.5f;

        private VRRig[] rows = Array.Empty<VRRig>();
        private Vector2 scroll;
        private float nextRefresh;

        internal void Draw(IPanelSurface panel)
        {
            panel.Label(TagManagerControl.Status);
            bool master = TagManagerControl.CanControl;
            bool controlling = CasterConfig.TagManagerEnabled.Value;
            CasterConfig.TagManagerEnabled.Value = panel.Toggle("Take Control", controlling, interactable: master || controlling);
            if (controlling) DrawControls(panel, master);
        }

        private void DrawControls(IPanelSurface panel, bool master)
        {
            bool localFirst = CasterConfig.TagManagerLocalFirst.Value;
            CasterConfig.TagManagerLocalFirst.Value = panel.Toggle("Me First Each Round", localFirst, interactable: master);
            bool manual = master && !localFirst;
            bool onlyMe = CasterConfig.TagManagerRefereeOnly.Value;
            CasterConfig.TagManagerRefereeOnly.Value = panel.Toggle("Only Me Tagged", onlyMe, interactable: manual);
            CasterConfig.TagManagerPlayerTags.Value = panel.Toggle("Player Tags", CasterConfig.TagManagerPlayerTags.Value, interactable: manual && !onlyMe);
            panel.Label($"Tagged: {TagManagerControl.TaggedNames}");

            bool canTag = master && (localFirst || !onlyMe);
            bool readyToTag = canTag && TagManagerControl.ReadyToTag;
            panel.HorizontalArea(() =>
            {
                panel.Button("Tag Me", TagManagerControl.TagLocal, readyToTag && !TagManagerControl.IsTagged(TagManagerControl.LocalPlayer));
                panel.Button("Clear Tags", TagManagerControl.ClearTags, canTag && TagManagerControl.AnyoneTagged);
            });
            DrawPlayers(panel, readyToTag);
        }

        private void DrawPlayers(IPanelSurface panel, bool interactable)
        {
            RefreshRows();
            if (rows.Length == 0)
            {
                panel.Label("No players.");
                return;
            }
            scroll = panel.ScrollArea(scroll, Panel.ListHeight, rows.Length, () =>
            {
                foreach (VRRig rig in rows)
                {
                    bool tagged = TagManagerControl.IsTagged(rig);
                    if (panel.Toggle(TagManagerControl.NameOf(rig.Creator), tagged, "Tagged", "Runner", interactable) != tagged) TagManagerControl.Toggle(rig);
                }
            });
        }

        private void RefreshRows()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            rows = VRRigCache.ActiveRigs
                .Where(rig => rig != null && rig.Creator != null && rig.Creator.InRoom)
                .OrderBy(rig => rig.isOfflineVRRig ? 0 : 1)
                .ThenBy(rig => TagManagerControl.NameOf(rig.Creator), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
