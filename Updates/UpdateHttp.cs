// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using System.Net;
using Zetori.Base;

namespace Zetori.Updates
{
    internal static class UpdateHttp
    {
        internal const string BinaryContent = "application/octet-stream";

        private static string UserAgent => $"{PluginInfo.Name}/{PluginInfo.Version}";

        internal static string GetText(string url, string accept, int timeoutMilliseconds)
        {
            using HttpWebResponse response = Send(url, accept, timeoutMilliseconds);
            using var reader = new StreamReader(response.GetResponseStream());
            return reader.ReadToEnd();
        }

        internal static void Download(string url, string path, int timeoutMilliseconds)
        {
            using HttpWebResponse response = Send(url, BinaryContent, timeoutMilliseconds);
            using Stream source = response.GetResponseStream();
            using FileStream target = File.Create(path);
            source.CopyTo(target);
        }

        private static HttpWebResponse Send(string url, string accept, int timeoutMilliseconds)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = UserAgent;
            request.Accept = accept;
            request.Timeout = timeoutMilliseconds;
            request.ReadWriteTimeout = timeoutMilliseconds;
            return (HttpWebResponse)request.GetResponse();
        }
    }
}
