// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Photon.Voice.Unity;
using UnityEngine;

namespace Zetori.Player
{
    internal sealed class MicrophoneDeviceWatcher : ObjectComponent
    {
        private const float PollInterval = 1f;

        private float nextPollTime;
        private string? knownDeviceId;
        private bool restartPending;

        internal void Update()
        {
            if (Time.unscaledTime < nextPollTime) return;
            nextPollTime = Time.unscaledTime + PollInterval;
            DetectDeviceChange();
            if (restartPending) TryRestartRecorder();
        }

        private void DetectDeviceChange()
        {
            string? deviceId;
            try { deviceId = DefaultCaptureDevice.CurrentId(); }
            catch (Exception e)
            {
                Log.Exception(e, "Reading the default microphone failed");
                enabled = false;
                return;
            }
            if (deviceId == knownDeviceId) return;
            if (knownDeviceId != null) restartPending = true;
            knownDeviceId = deviceId;
        }

        private void TryRestartRecorder()
        {
            Recorder? recorder = GorillaTagger.Instance != null ? GorillaTagger.Instance.myRecorder : null;
            if (recorder == null) return;
            restartPending = false;
            if (!recorder.IsRecording) return;
            try
            {
                recorder.RestartRecording(true);
                Log.Info("Default microphone changed; voice recording restarted.");
            }
            catch (Exception e) { Log.Exception(e, "Restarting voice recording failed"); }
        }
    }
}
