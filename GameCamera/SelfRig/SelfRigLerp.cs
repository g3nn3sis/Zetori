// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.Base.Config;
using Zetori.GameCamera.Modes;
using Zetori.Player;
using Zetori.Replay;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zetori.GameCamera.SelfRig
{
    internal sealed class SelfRigLerp : ObjectComponent
    {
        private const string AnchorName = "_Zetori Self Rig Anchor";
        private const float FallbackSendRate = 10f;

        private static SelfRigLerp? instance;

        private readonly InterpolatedRig pose = new InterpolatedRig();
        private readonly RenderPoseSwap swap = new RenderPoseSwap();
        private readonly RigProxies proxies = new RigProxies();
        private Transform? anchor;
        private VRRig? rig;
        private GorillaIK? ik;
        private float nextSample;

        internal static bool Enabled
        {
            get => CasterConfig.SelfRigLerp.Value;
            set => CasterConfig.SelfRigLerp.Value = value;
        }

        private bool Showing => rig != null && ik != null && anchor != null && pose.Ready && Enabled && !ReplaySession.IsReplaying;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            var anchorObject = new GameObject(AnchorName);
            DontDestroyOnLoad(anchorObject);
            anchor = anchorObject.transform;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        internal void Update()
        {
            RestoreRig();
            if (!Enabled || ReplaySession.IsReplaying)
            {
                Release();
                return;
            }
            if (!Bind(NetworkedPlayerManager.LocalRig)) return;
            try { Follow(rig!); }
            catch (Exception e) { Log.ExceptionOnce("self-rig-lerp-follow", e, "Interpolating your own rig failed"); }
        }

        internal static bool TryFollow(CameraWrapper camera, VRRig target)
        {
            SelfRigLerp? self = instance;
            if (self == null || !self.Showing || target != self.rig) return false;
            Transform body = self.anchor!;
            SpectatorCamera.Run(camera, body, HeadView.BodyEyes(body, target.headBodyOffset, self.HeadRotation(target)));
            return true;
        }

        private bool Bind(VRRig? localRig)
        {
            if (localRig != rig)
            {
                Release();
                rig = localRig;
                ik = rig != null ? rig.GetComponent<GorillaIK>() : null;
            }
            return rig != null && ik != null;
        }

        private void Follow(VRRig localRig)
        {
            Sample(localRig);
            pose.Advance(NetworkedPlayerManager.LiveRigLerp, localRig.headBodyOffset, localRig.scaleFactor);
            if (!pose.Ready) return;
            anchor!.SetPositionAndRotation(pose.BodyPosition, pose.BodyRotation);
            anchor.localScale = localRig.transform.localScale;
        }

        private void Sample(VRRig localRig)
        {
            float now = Time.unscaledTime;
            if (pose.Ready && now < nextSample) return;
            nextSample = now + 1f / SendRate;
            pose.Receive(RigNetworkSample.Capture(localRig), localRig.justTeleportedSendsRemaining > 0);
        }

        private static float SendRate => PhotonNetwork.SerializationRate > 0 ? PhotonNetwork.SerializationRate : FallbackSendRate;

        private Quaternion HeadRotation(VRRig target)
        {
            Transform head = target.head.rigTarget;
            Quaternion bodyDelta = pose.BodyRotation * Quaternion.Inverse(target.transform.rotation);
            Quaternion headParent = head.parent != null ? head.parent.rotation : Quaternion.identity;
            Quaternion meshOffset = Quaternion.Inverse(head.rotation) * target.headMesh.transform.rotation;
            return bodyDelta * headParent * pose.Head.rotation * meshOffset;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!IsSpectatorCamera(camera) || !Showing) return;
            try
            {
                swap.Apply(rig!, ik!, pose);
                proxies.Show(rig!);
            }
            catch (Exception e)
            {
                RestoreRig();
                Log.ExceptionOnce("self-rig-lerp-render", e, "Showing your interpolated rig failed");
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (IsSpectatorCamera(camera)) RestoreRig();
        }

        private static bool IsSpectatorCamera(Camera camera)
        {
            CameraWrapper? spectator = CameraManager.camera;
            return spectator != null && spectator.Camera == camera;
        }

        private void RestoreRig()
        {
            try
            {
                proxies.Hide();
                swap.Restore();
            }
            catch (Exception e) { Log.ExceptionOnce("self-rig-lerp-restore", e, "Restoring your rig after the spectator camera failed"); }
        }

        private void Release()
        {
            pose.Reset();
            proxies.Dispose();
        }

        protected override void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RestoreRig();
            proxies.Dispose();
            if (anchor != null) Destroy(anchor.gameObject);
            if (instance == this) instance = null;
            base.OnDestroy();
        }
    }
}
