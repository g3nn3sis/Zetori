// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI
{
    internal sealed class PanelGroup
    {
        internal PanelGroup(string title, params PanelId[] panels)
        {
            Title = title;
            Panels = panels;
        }

        internal string Title { get; }
        internal PanelId[] Panels { get; }
    }
}
