// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Runtime.InteropServices;

namespace Zetori.Player.Voice
{
    internal static class DefaultCaptureDevice
    {
        private const int CaptureFlow = 1;
        private const int CommunicationsRole = 2;
        private const int ConsoleRole = 0;

        internal static string? CurrentId()
        {
            IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            try
            {
                return EndpointId(enumerator, ConsoleRole) + "|" + EndpointId(enumerator, CommunicationsRole);
            }
            finally
            {
                Marshal.ReleaseComObject(enumerator);
            }
        }

        private static string? EndpointId(IMMDeviceEnumerator enumerator, int role)
        {
            if (enumerator.GetDefaultAudioEndpoint(CaptureFlow, role, out IMMDevice device) != 0 || device == null) return null;
            try
            {
                return device.GetId(out string id) == 0 ? id : null;
            }
            finally
            {
                Marshal.ReleaseComObject(device);
            }
        }

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator
        {
        }

        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig]
            int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);

            [PreserveSig]
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        }

        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int context, IntPtr activationParams, out IntPtr instance);

            [PreserveSig]
            int OpenPropertyStore(int access, out IntPtr properties);

            [PreserveSig]
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        }
    }
}
