// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Rendering;
using Zetori.Base;
using Zetori.GameCamera.Modes;
using Zetori.GameCamera.Vr;

namespace Zetori.UI.InGame
{
    internal sealed class InGamePanel : IDisposable
    {
        private const float SummonWindow = 0.4f;
        private const float SummonDistance = 0.42f;
        private const float SummonDrop = 0.12f;
        private const float OpenSeconds = 0.25f;
        private const float CloseSeconds = 0.15f;
        private const float Overshoot = 1.70158f;
        private const float TapStrength = 0.15f;
        private const float TapSeconds = 0.03f;
        private const float HoldStrength = 0.35f;
        private const float HoldSeconds = 0.08f;
        private const float SummonStrength = 0.2f;
        private const float SummonSeconds = 0.05f;

        private readonly DoublePressDetector summonPress = new DoublePressDetector(SummonWindow);
        private readonly VrCameraHand leftHand = new VrCameraHand(true);
        private readonly VrCameraHand rightHand = new VrCameraHand(false);
        private readonly InGamePointer leftPointer = new InGamePointer(true);
        private readonly InGamePointer rightPointer = new InGamePointer(false);
        private readonly InGameSurface surface = new InGameSurface();
        private readonly InGameLayout layout = new InGameLayout();
        private readonly InGamePages pages;
        private InGameModel? model;
        private InGameVisuals? visuals;
        private VrCameraHand? holder;
        private Vector3 holdPosition;
        private Quaternion holdRotation = Quaternion.identity;
        private Transform? anchor;
        private Vector3 placedPosition;
        private Quaternion placedRotation = Quaternion.identity;
        private bool open;
        private float openness;

        internal InGamePanel(PanelOrchestrator owner)
        {
            pages = new InGamePages(surface, owner, Recenter, Close);
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private static float Size => CasterConfig.InGamePanelSize.Value;

        private bool Interactive => open && openness >= 1f;

        internal void Tick()
        {
            if (!VrCameraRig.HeadsetActive)
            {
                HideNow();
                return;
            }
            GorillaTagger? tagger = GorillaTagger.Instance;
            GTPlayer? player = GTPlayer.Instance;
            ControllerInputPoller? input = ControllerInputPoller.instance;
            if (tagger == null || player == null || input == null || tagger.offlineVRRig == null) return;

            float now = Time.unscaledTime;
            if (summonPress.Update(input.leftControllerPrimaryButton, now)) Toggle(tagger, player);
            if (model == null || visuals == null) return;

            openness = Mathf.MoveTowards(openness, open ? 1f : 0f, Time.unscaledDeltaTime / (open ? OpenSeconds : CloseSeconds));
            if (openness <= 0f)
            {
                HideNow();
                return;
            }
            model.Show();
            leftHand.Sync(tagger.leftHandTransform, tagger.leftHandTriggerCollider.transform, input.leftGrab);
            rightHand.Sync(tagger.rightHandTransform, tagger.rightHandTriggerCollider.transform, input.rightGrab);
            if (Interactive) UpdateGrab(tagger, model);
            else holder = null;
            FollowAnchorSetting(player, model);
            ApplyPlacement(model);
            model.SetScale(player.scale * Size * Pop(openness));
            Color32 accent = ColorSpace.Current.Color;
            model.SetAccent(accent);
            UpdateContent(tagger, model, visuals, accent, now);
        }

        private void UpdateContent(GorillaTagger tagger, InGameModel panel, InGameVisuals drawn, Color32 accent, float now)
        {
            if (pages.ApplyRequestedPage()) ResetInteraction();
            surface.Begin();
            pages.Draw();
            surface.End();
            layout.Arrange(surface);
            if (Interactive)
            {
                Point(tagger, panel, leftPointer, leftHand, now);
                Point(tagger, panel, rightPointer, rightHand, now);
            }
            else
            {
                leftPointer.Reset();
                rightPointer.Reset();
            }
            drawn.Render(surface, layout, leftPointer, rightPointer, accent);
        }

        private void Point(GorillaTagger tagger, InGameModel panel, InGamePointer pointer, VrCameraHand hand, float now)
        {
            Vector3 local = panel.Root.InverseTransformPoint(hand.Finger.position);
            InGamePress press = pointer.Update(local, hand == holder, surface, layout, now);
            if (press == InGamePress.None || !CasterConfig.InGamePanelHaptics.Value) return;
            if (press == InGamePress.Hold) tagger.StartVibration(hand.IsLeft, HoldStrength, HoldSeconds);
            else tagger.StartVibration(hand.IsLeft, TapStrength, TapSeconds);
        }

        private void ResetInteraction()
        {
            surface.ResetInteraction();
            layout.ResetScroll();
            leftPointer.Reset();
            rightPointer.Reset();
        }

        private void Toggle(GorillaTagger tagger, GTPlayer player)
        {
            if (open) Close();
            else Summon(tagger, player);
            if (CasterConfig.InGamePanelHaptics.Value) tagger.StartVibration(true, SummonStrength, SummonSeconds);
        }

        private void Summon(GorillaTagger tagger, GTPlayer player)
        {
            if (model == null)
            {
                model = InGameModel.Create();
                visuals = new InGameVisuals(model);
            }
            Pose head = HeadView.Eyes(tagger.offlineVRRig);
            float scale = player.scale;
            Vector3 position = head.position
                + VrCameraShots.FlatForward(head.rotation) * (SummonDistance * scale)
                + Vector3.down * (SummonDrop * scale);
            Place(player, position, VrCameraShots.LookAt(head.position, position, head.rotation));
            if (!open) openness = 0f;
            open = true;
            holder = null;
        }

        private void Recenter()
        {
            GorillaTagger? tagger = GorillaTagger.Instance;
            GTPlayer? player = GTPlayer.Instance;
            if (tagger == null || player == null || tagger.offlineVRRig == null) return;
            Summon(tagger, player);
        }

        private void Close()
        {
            open = false;
            holder = null;
            surface.StopEditing();
        }

        private void HideNow()
        {
            open = false;
            openness = 0f;
            holder = null;
            model?.Hide();
        }

        private void Place(GTPlayer player, Vector3 position, Quaternion rotation)
        {
            anchor = CasterConfig.InGamePanelFollow.Value && player.turnParent != null ? player.turnParent.transform : null;
            placedPosition = anchor != null ? anchor.InverseTransformPoint(position) : position;
            placedRotation = anchor != null ? Quaternion.Inverse(anchor.rotation) * rotation : rotation;
        }

        private void FollowAnchorSetting(GTPlayer player, InGameModel panel)
        {
            bool follows = anchor != null;
            if (follows == CasterConfig.InGamePanelFollow.Value || holder != null) return;
            Place(player, panel.Root.position, panel.Root.rotation);
        }

        private void ApplyPlacement(InGameModel panel)
        {
            if (holder != null) return;
            if (anchor != null) panel.Root.SetPositionAndRotation(anchor.TransformPoint(placedPosition), anchor.rotation * placedRotation);
            else panel.Root.SetPositionAndRotation(placedPosition, placedRotation);
        }

        private void UpdateGrab(GorillaTagger tagger, InGameModel panel)
        {
            if (holder != null && !holder.Gripping) Release(tagger, panel);
            if (rightHand.GripStarted) TryHold(tagger, panel, rightHand);
            if (leftHand.GripStarted) TryHold(tagger, panel, leftHand);
            if (holder == null) return;
            Transform palm = holder.Palm;
            panel.Root.SetPositionAndRotation(palm.position + palm.rotation * holdPosition, palm.rotation * holdRotation);
        }

        private void Release(GorillaTagger tagger, InGameModel panel)
        {
            VrCameraHand otherHand = holder == leftHand ? rightHand : leftHand;
            holder = null;
            GTPlayer? player = GTPlayer.Instance;
            if (player != null) Place(player, panel.Root.position, panel.Root.rotation);
            if (otherHand.Gripping) TryHold(tagger, panel, otherHand);
        }

        private void TryHold(GorillaTagger tagger, InGameModel panel, VrCameraHand hand)
        {
            if (holder != null) return;
            Transform palm = hand.Palm;
            if (!panel.WithinGrabReach(palm.position)) return;
            Quaternion inversePalm = Quaternion.Inverse(palm.rotation);
            holdPosition = inversePalm * (panel.Root.position - palm.position);
            holdRotation = inversePalm * panel.Root.rotation;
            holder = hand;
            if (CasterConfig.InGamePanelHaptics.Value) tagger.StartVibration(hand.IsLeft, TapStrength, TapSeconds);
        }

        private static float Pop(float progress)
        {
            float t = progress - 1f;
            return 1f + (Overshoot + 1f) * t * t * t + Overshoot * t * t;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (model == null || !model.Shown) return;
            GTPlayer? player = GTPlayer.Instance;
            bool headset = player != null && camera == player.mainCamera;
            if (headset) ApplyPlacement(model);
            model.SetHiddenFromView(!headset && !CasterConfig.InGamePanelOnStream.Value);
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            model?.Dispose();
            model = null;
            visuals = null;
        }
    }
}
