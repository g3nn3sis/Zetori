// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using UnityEngine.Rendering;
using Zetori.Game;

namespace Zetori.UI.InGame
{
    internal sealed class InGameMirror : IDisposable
    {
        private const string ObjectName = "_Zetori Player Mirror";
        private const int IsolatedLayer = 3;
        private const int HiddenLayers = (int)UnityLayerMask.FirstPersonOnly | (int)UnityLayerMask.NoMirror;
        private const int TextureWidth = 640;
        private const int TextureHeight = 588;
        private const int DepthBits = 24;
        private const float CameraDepth = -100f;
        private const float FieldOfView = 28f;
        private const float FrameHalfHeight = 0.66f;
        private const float FrameLift = -0.04f;
        private const float NearClip = 0.05f;
        private const float DepthMargin = 2f;
        private const float MinimumScale = 0.01f;
        private const float TurnSmoothTime = 0.6f;
        private const float RescanInterval = 1f;

        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Renderer> scanned = new List<Renderer>();
        private readonly List<GameObject> isolated = new List<GameObject>();
        private readonly List<int> isolatedLayers = new List<int>();
        private GameObject? cameraObject;
        private Camera? camera;
        private RenderTexture? texture;
        private Transform? target;
        private float yaw;
        private float yawVelocity;
        private float nextScan;

        internal Texture Follow(Transform body, float scale)
        {
            if (camera == null || texture == null) Create();
            bool retargeted = body != target;
            if (retargeted)
            {
                target = body;
                nextScan = 0f;
            }
            if (Time.unscaledTime >= nextScan) Rescan(body);
            Camera view = camera!;
            Aim(view, body, scale, retargeted || !view.enabled);
            if (!view.enabled) view.enabled = true;
            return texture!;
        }

        internal void Conceal()
        {
            if (camera != null && camera.enabled) camera.enabled = false;
            Restore();
        }

        private void Create()
        {
            Release();
            cameraObject = new GameObject(ObjectName);
            UnityEngine.Object.DontDestroyOnLoad(cameraObject);
            texture = new RenderTexture(TextureWidth, TextureHeight, DepthBits) { name = ObjectName };
            camera = cameraObject.AddComponent<Camera>();
            camera.cameraType = CameraType.Reflection;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = InGameModel.FaceColor;
            camera.cullingMask = 1 << IsolatedLayer;
            camera.fieldOfView = FieldOfView;
            camera.useOcclusionCulling = false;
            camera.allowHDR = false;
            camera.depth = CameraDepth;
            camera.targetTexture = texture;
            camera.enabled = false;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void Rescan(Transform body)
        {
            nextScan = Time.unscaledTime + RescanInterval;
            renderers.Clear();
            body.GetComponentsInChildren(false, scanned);
            foreach (Renderer renderer in scanned)
                if (!NameTag.IsTagPart(renderer.transform)) renderers.Add(renderer);
            scanned.Clear();
        }

        private void Aim(Camera view, Transform body, float bodyScale, bool snap)
        {
            float scale = Mathf.Max(MinimumScale, bodyScale);
            Turn(body.forward, snap);
            Quaternion facing = Quaternion.Euler(0f, yaw, 0f);
            float distance = FrameHalfHeight * scale / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector3 center = body.position + Vector3.up * (FrameLift * scale);
            view.transform.SetPositionAndRotation(center - facing * Vector3.forward * distance, facing);
            view.nearClipPlane = NearClip * scale;
            view.farClipPlane = distance + DepthMargin * scale;
        }

        private void Turn(Vector3 forward, bool snap)
        {
            float goal = forward.x * forward.x + forward.z * forward.z > Mathf.Epsilon ? Mathf.Atan2(-forward.x, -forward.z) * Mathf.Rad2Deg : yaw;
            if (snap)
            {
                yaw = goal;
                yawVelocity = 0f;
                return;
            }
            yaw = Mathf.SmoothDampAngle(yaw, goal, ref yawVelocity, TurnSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering == camera) Isolate();
            else Restore();
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering == camera) Restore();
        }

        private void Isolate()
        {
            if (isolated.Count > 0) return;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                GameObject part = renderer.gameObject;
                int layer = part.layer;
                if (layer == IsolatedLayer || ((1 << layer) & HiddenLayers) != 0) continue;
                isolated.Add(part);
                isolatedLayers.Add(layer);
                part.layer = IsolatedLayer;
            }
        }

        private void Restore()
        {
            for (int i = 0; i < isolated.Count; i++)
                if (isolated[i] != null) isolated[i].layer = isolatedLayers[i];
            isolated.Clear();
            isolatedLayers.Clear();
        }

        private void Release()
        {
            Restore();
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            if (camera != null) camera.targetTexture = null;
            if (texture != null)
            {
                texture.Release();
                UnityEngine.Object.Destroy(texture);
            }
            if (cameraObject != null) UnityEngine.Object.Destroy(cameraObject);
            camera = null;
            texture = null;
            cameraObject = null;
        }

        public void Dispose()
        {
            Release();
            renderers.Clear();
            target = null;
        }
    }
}
