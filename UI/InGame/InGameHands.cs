// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using UnityEngine.Rendering;
using Zetori.GameCamera.Vr;

namespace Zetori.UI.InGame
{
    internal sealed class InGameHands : IDisposable
    {
        private const float PalmDiameter = 0.045f;
        private const float TipDiameter = 0.02f;
        private const float BoneDiameter = 0.008f;
        private const float PulseSpeed = 5f;
        private const float PulseDepth = 0.25f;
        private const float TipWhiteness = 0.55f;
        private const float BoneDimming = 0.35f;
        private const float MinimumBoneLength = 0.0001f;

        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        private readonly GameObject root;
        private readonly Material palmMaterial;
        private readonly Material tipMaterial;
        private readonly Material boneMaterial;
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly Transform leftPalm;
        private readonly Transform leftTip;
        private readonly Transform leftBone;
        private readonly Transform rightPalm;
        private readonly Transform rightTip;
        private readonly Transform rightBone;
        private bool hiddenFromView;

        internal bool Shown => root.activeSelf;

        internal InGameHands()
        {
            root = new GameObject("_Zetori Replay Hands");
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.SetActive(false);
            palmMaterial = new Material(UberShader.ReferenceMaterial);
            tipMaterial = new Material(UberShader.ReferenceMaterial);
            boneMaterial = new Material(UberShader.ReferenceMaterial);
            leftPalm = Part(PrimitiveType.Sphere, palmMaterial);
            leftTip = Part(PrimitiveType.Sphere, tipMaterial);
            leftBone = Part(PrimitiveType.Cylinder, boneMaterial);
            rightPalm = Part(PrimitiveType.Sphere, palmMaterial);
            rightTip = Part(PrimitiveType.Sphere, tipMaterial);
            rightBone = Part(PrimitiveType.Cylinder, boneMaterial);
        }

        internal void Show(VrCameraHand left, VrCameraHand right, float scale, Color accent, float time)
        {
            if (!root.activeSelf) root.SetActive(true);
            float pulse = 1f - PulseDepth * (0.5f + 0.5f * Mathf.Sin(time * PulseSpeed));
            Color glow = accent * pulse;
            glow.a = 1f;
            palmMaterial.SetColor(BaseColorProperty, glow);
            tipMaterial.SetColor(BaseColorProperty, Color.Lerp(glow, Color.white, TipWhiteness));
            boneMaterial.SetColor(BaseColorProperty, Color.Lerp(glow, Color.black, BoneDimming));
            Place(leftPalm, leftTip, leftBone, left, scale);
            Place(rightPalm, rightTip, rightBone, right, scale);
        }

        internal void Hide()
        {
            if (root.activeSelf) root.SetActive(false);
        }

        internal void SetHiddenFromView(bool hidden)
        {
            if (hidden == hiddenFromView) return;
            hiddenFromView = hidden;
            foreach (Renderer renderer in renderers) renderer.forceRenderingOff = hidden;
        }

        private static void Place(Transform palm, Transform tip, Transform bone, VrCameraHand hand, float scale)
        {
            Vector3 palmPosition = hand.Palm.position;
            Vector3 tipPosition = hand.Finger.position;
            palm.SetPositionAndRotation(palmPosition, hand.Palm.rotation);
            palm.localScale = Vector3.one * (PalmDiameter * scale);
            tip.position = tipPosition;
            tip.localScale = Vector3.one * (TipDiameter * scale);
            Vector3 span = tipPosition - palmPosition;
            float length = span.magnitude;
            bool visible = length > MinimumBoneLength;
            if (bone.gameObject.activeSelf != visible) bone.gameObject.SetActive(visible);
            if (!visible) return;
            bone.SetPositionAndRotation((palmPosition + tipPosition) * 0.5f, Quaternion.FromToRotation(Vector3.up, span / length));
            bone.localScale = new Vector3(BoneDiameter * scale, length * 0.5f, BoneDiameter * scale);
        }

        private Transform Part(PrimitiveType shape, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(root.transform, false);
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderers.Add(renderer);
            return part.transform;
        }

        public void Dispose()
        {
            if (palmMaterial != null) UnityEngine.Object.Destroy(palmMaterial);
            if (tipMaterial != null) UnityEngine.Object.Destroy(tipMaterial);
            if (boneMaterial != null) UnityEngine.Object.Destroy(boneMaterial);
            renderers.Clear();
            if (root != null) UnityEngine.Object.Destroy(root);
        }
    }
}
