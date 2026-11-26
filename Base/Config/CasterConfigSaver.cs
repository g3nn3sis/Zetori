// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Base.Config
{
    internal sealed class CasterConfigSaver : ObjectComponent
    {
        internal void Update() => CasterConfig.Tick();

        protected override void OnApplicationQuit()
        {
            CasterConfig.SaveOnExit();
            base.OnApplicationQuit();
        }
    }
}
