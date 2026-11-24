// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Rendering;
using Zetori.Base;
using Zetori.GameCamera;
using Zetori.GameCamera.Modes;
using Zetori.GameCamera.Vr;
using Zetori.Replay;

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
        private readonly InGamePointer[] pointers =
        {
            new InGamePointer(true, InGamePlane.Panel),
            new InGamePointer(false, InGamePlane.Panel),
            new InGamePointer(true, InGamePlane.Keyboard),
            new InGamePointer(false, InGamePlane.Keyboard)
        };
        private readonly InGameSurface surface = new InGameSurface();
        private readonly InGameLayout layout = new InGameLayout();
        private readonly InGamePages pages;
        private InGameModel? model;
        private InGameVisuals? visuals;
        private InGameHands? hands;
        private VrCameraHand? holder;
        private Vector3 holdPosition;
        private Quaternion holdRotation = Quaternion.identity;
        private bool open;
        private float openness;

        internal InGamePanel(PanelOrchestrator owner)
        {
            pages = new InGamePages(surface, owner, Recenter, Close);
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private static float Size => CasterConfig.InGamePanelSize.Value;

        private bool Interactive => open && openness >= 1f;

        private static bool ShownOffHeadset => CasterConfig.InGamePanelOnStream.Value || CameraManager.ActiveMode == CameraMode.Camera;

        internal void Tick()
        {
            if (!VrCameraRig.HeadsetActive)
            {
                HideNow();
                hands?.Hide();
                return;
            }
            GorillaTagger? tagger = GorillaTagger.Instance;
            GTPlayer? player = GTPlayer.Instance;
            ControllerInputPoller? input = ControllerInputPoller.instance;
            if (tagger == null || player == null || input == null || tagger.offlineVRRig == null) return;

            float now = Time.unscaledTime;
            leftHand.Sync(tagger.leftHandTransform, tagger.leftHandTriggerCollider.transform, input.leftGrab);
            rightHand.Sync(tagger.rightHandTransform, tagger.rightHandTriggerCollider.transform, input.rightGrab);
            UpdateHands(player, now);
            if (summonPress.Update(input.leftControllerPrimaryButton, now)) Toggle(tagger, player);
            if (model == null || visuals == null) return;

            openness = Mathf.MoveTowards(openness, open ? 1f : 0f, Time.unscaledDeltaTime / (open ? OpenSeconds : CloseSeconds));
            if (openness <= 0f)
            {
                HideNow();
                return;
            }
            model.Show();
            if (Interactive) UpdateGrab(tagger, model);
            else holder = null;
            model.SetScale(player.scale * Size * Pop(openness));
            Color32 accent = ColorSpace.Current.Color;
            model.SetAccent(accent);
            UpdateContent(tagger, model, visuals, accent, now);
        }

        private void UpdateHands(GTPlayer player, float now)
        {
            if (!ReplaySession.IsReplaying)
            {
                hands?.Hide();
                return;
            }
            hands ??= new InGameHands();
            hands.Show(leftHand, rightHand, player.scale, ColorSpace.Current.Color, now);
        }

        private void UpdateContent(GorillaTagger tagger, InGameModel panel, InGameVisuals drawn, Color32 accent, float now)
        {
            if (pages.ApplyRequestedPage()) ResetInteraction();
            surface.Begin();
            pages.Draw();
            surface.End();
            layout.Arrange(surface);
            foreach (InGamePointer pointer in pointers)
            {
                if (Interactive) Point(tagger, panel, pointer, now);
                else pointer.Reset();
            }
            drawn.Render(surface, layout, pointers, accent);
        }

        private void Point(GorillaTagger tagger, InGameModel panel, InGamePointer pointer, float now)
        {
            VrCameraHand hand = pointer.IsLeft ? leftHand : rightHand;
            Transform plane = pointer.Plane == InGamePlane.Keyboard ? panel.Keyboard : panel.Root;
            Vector3 local = plane.InverseTransformPoint(hand.Finger.position);
            InGamePress press = pointer.Update(local, hand == holder, surface, layout, now);
            if (press == InGamePress.None || !CasterConfig.InGamePanelHaptics.Value) return;
            if (press == InGamePress.Hold) tagger.StartVibration(hand.IsLeft, HoldStrength, HoldSeconds);
            else tagger.StartVibration(hand.IsLeft, TapStrength, TapSeconds);
        }

        private void ResetInteraction()
        {
            surface.ResetInteraction();
            layout.ResetScroll();
            foreach (InGamePointer pointer in pointers) pointer.Reset();
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
            model.Root.SetPositionAndRotation(position, VrCameraShots.LookAt(head.position, position, head.rotation));
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
            pages.Conceal();
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
            GTPlayer? player = GTPlayer.Instance;
            bool headset = player != null && camera == player.mainCamera;
            if (model != null && model.Shown) model.SetHiddenFromView(!headset && !ShownOffHeadset);
            if (hands != null && hands.Shown) hands.SetHiddenFromView(!headset);
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            pages.Dispose();
            model?.Dispose();
            model = null;
            visuals = null;
            hands?.Dispose();
            hands = null;
        }
    }
}
