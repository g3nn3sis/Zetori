// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using TMPro;
using UnityEngine;
using Zetori.Base.Config;
using Zetori.Shared.Assets;

namespace Zetori.Notifications
{
    internal static class NotificationStyle
    {
        private const int TextureSize = 48;
        private const int SliceBorder = 16;
        private const float PixelsPerUnit = 2400f;
        private const float BackgroundOpacity = 0.85f;

        private static readonly Vector4 SliceBorders = new Vector4(SliceBorder, SliceBorder, SliceBorder, SliceBorder);

        private static Sprite? roundedSprite;
        private static Sprite? squareSprite;

        internal static bool HasBackground => CasterConfig.NotificationBackground.Value;

        internal static Color BackgroundColor
        {
            get
            {
                Color color = CasterConfig.NotificationTheme.Value == NotificationTheme.Custom ? CustomColor : (Color)UI.ColorSpace.Current.Color;
                color.a = BackgroundOpacity;
                return color;
            }
        }

        internal static Color CustomColor => new Color(CasterConfig.NotificationRed.Value, CasterConfig.NotificationGreen.Value, CasterConfig.NotificationBlue.Value);

        internal static Color TextColor => new Color(CasterConfig.NotificationTextRed.Value, CasterConfig.NotificationTextGreen.Value, CasterConfig.NotificationTextBlue.Value);

        internal static TMP_FontAsset? Font
        {
            get
            {
                TMP_FontAsset[] fonts = AssetManager.discoveredFonts;
                if (fonts.Length == 0) return null;
                return fonts[Mathf.Clamp(CasterConfig.NotificationFont.Value, 0, fonts.Length - 1)];
            }
        }

        internal static Sprite BackgroundSprite
        {
            get
            {
                if (CasterConfig.NotificationRoundedCorners.Value)
                {
                    if (roundedSprite == null) roundedSprite = CreateSprite(SliceBorder);
                    return roundedSprite;
                }
                if (squareSprite == null) squareSprite = CreateSprite(0);
                return squareSprite;
            }
        }

        private static Sprite CreateSprite(int cornerRadius)
        {
            Texture2D texture = UI.Textures.CreateTexture(TextureSize, TextureSize, Color.white, Color.white, 0, cornerRadius);
            texture.hideFlags = HideFlags.HideAndDontSave;
            var rect = new Rect(0f, 0f, TextureSize, TextureSize);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect, SliceBorders);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
