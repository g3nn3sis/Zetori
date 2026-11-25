// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.GameCamera
{
    internal static class CutoutMatte
    {
        private const int SourceChannels = 3;
        private const int MatteChannels = 4;
        private const int EncodeSteps = 4096;
        private const float ByteMaximum = 255f;

        private static readonly float[] ToLinear = Enumerable.Range(0, 256).Select(value => Linearize(value / ByteMaximum)).ToArray();
        private static readonly byte[] ToEncoded = Enumerable.Range(0, EncodeSteps + 1).Select(step => Encode(step / (float)EncodeSteps)).ToArray();

        internal static byte[] Extract(byte[] overBlack, byte[] overWhite)
        {
            int pixels = overBlack.Length / SourceChannels;
            var matte = new byte[pixels * MatteChannels];
            for (int pixel = 0; pixel < pixels; pixel++)
            {
                int source = pixel * SourceChannels;
                int target = pixel * MatteChannels;
                float alpha = Clamp01(1f - Spread(overBlack, overWhite, source) / SourceChannels);
                if (alpha <= 0f) continue;
                for (int channel = 0; channel < SourceChannels; channel++)
                    matte[target + channel] = ToEncoded[(int)(Clamp01(ToLinear[overBlack[source + channel]] / alpha) * EncodeSteps + 0.5f)];
                matte[target + SourceChannels] = (byte)(alpha * ByteMaximum + 0.5f);
            }
            return matte;
        }

        private static float Spread(byte[] overBlack, byte[] overWhite, int source)
        {
            float spread = 0f;
            for (int channel = 0; channel < SourceChannels; channel++)
                spread += ToLinear[overWhite[source + channel]] - ToLinear[overBlack[source + channel]];
            return spread;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

        private static float Linearize(float encoded) =>
            encoded <= 0.04045f ? encoded / 12.92f : (float)Math.Pow((encoded + 0.055f) / 1.055f, 2.4f);

        private static byte Encode(float linear)
        {
            float encoded = linear <= 0.0031308f ? linear * 12.92f : 1.055f * (float)Math.Pow(linear, 1f / 2.4f) - 0.055f;
            return (byte)(Clamp01(encoded) * ByteMaximum + 0.5f);
        }
    }
}
