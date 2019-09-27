using System;
using UnityEngine;
using Verse;

namespace D9Extended
{
    public static class D9SkinColors
    {
        private struct SkinColorData
        {
            public float melanin;

            public float selector;

            public Color color;

            public SkinColorData(float melanin, float selector, Color color)
            {
                this.melanin = melanin;
                this.selector = selector;
                this.color = color;
            }
        }

        private static readonly SkinColorData[] SkinColors = new SkinColorData[]
        {
            new SkinColorData(0f, 0f, new Color(0.9490196f, 0.929411769f, 0.8784314f)),
            new SkinColorData(0.25f, 0.2f, new Color(1f, 0.9372549f, 0.8352941f)),
            new SkinColorData(0.5f, 0.7f, new Color(1f, 0.9372549f, 0.7411765f)),
            new SkinColorData(0.75f, 0.8f, new Color(0.894117653f, 0.619607866f, 0.3529412f)),
            new SkinColorData(0.9f, 0.9f, new Color(0.509803951f, 0.356862754f, 0.1882353f)),
            new SkinColorData(1f, 1f, new Color(0.3882353f, 0.274509817f, 0.141176477f))
        };

        public static bool IsDarkSkin(Color color)
        {
            Color skinColor = GetSkinColor(0.5f);
            return color.r + color.g + color.b <= skinColor.r + skinColor.g + skinColor.b + 0.01f;
        }

        public static Color GetSkinColor(float melanin)
        {
            int skinDataIndexOfMelanin = GetSkinDataIndexOfMelanin(melanin);
            if (skinDataIndexOfMelanin == SkinColors.Length - 1)
            {
                return SkinColors[skinDataIndexOfMelanin].color;
            }
            float t = Mathf.InverseLerp(SkinColors[skinDataIndexOfMelanin].melanin, SkinColors[skinDataIndexOfMelanin + 1].melanin, melanin);
            return Color.Lerp(SkinColors[skinDataIndexOfMelanin].color, SkinColors[skinDataIndexOfMelanin + 1].color, t);
        }

        public static float RandomMelanin(PawnKindDefME fac)
        {
            float num;
            if (fac == null)
            {
                num = Rand.Value;
            }
            else
            {
                num = Rand.Range(Mathf.Clamp01((float)fac.centralMelanin - (float)fac.melaninVariance), Mathf.Clamp01((float)fac.centralMelanin + (float)fac.melaninVariance));
            }
            int num2 = 0;
            for (int i = 0; i < SkinColors.Length; i++)
            {
                if (num < SkinColors[i].selector)
                {
                    break;
                }
                num2 = i;
            }
            if (num2 == SkinColors.Length - 1)
            {
                return SkinColors[num2].melanin;
            }
            float t = Mathf.InverseLerp(SkinColors[num2].selector, SkinColors[num2 + 1].selector, num);
            return Mathf.Lerp(SkinColors[num2].melanin, SkinColors[num2 + 1].melanin, t);
        }

        public static float GetMelaninCommonalityFactor(float melanin)
        {
            int skinDataIndexOfMelanin = GetSkinDataIndexOfMelanin(melanin);
            if (skinDataIndexOfMelanin == SkinColors.Length - 1)
            {
                return GetSkinDataCommonalityFactor(skinDataIndexOfMelanin);
            }
            float t = Mathf.InverseLerp(SkinColors[skinDataIndexOfMelanin].melanin, SkinColors[skinDataIndexOfMelanin + 1].melanin, melanin);
            return Mathf.Lerp(GetSkinDataCommonalityFactor(skinDataIndexOfMelanin), GetSkinDataCommonalityFactor(skinDataIndexOfMelanin + 1), t);
        }

        public static float GetRandomMelaninSimilarTo(float value, float clampMin = 0f, float clampMax = 1f)
        {
            return Mathf.Clamp01(Mathf.Clamp(Rand.Gaussian(value, 0.05f), clampMin, clampMax));
        }

        private static float GetSkinDataCommonalityFactor(int skinDataIndex)
        {
            float num = 0f;
            for (int i = 0; i < SkinColors.Length; i++)
            {
                num = Mathf.Max(num, GetTotalAreaWhereClosestToSelector(i));
            }
            return GetTotalAreaWhereClosestToSelector(skinDataIndex) / num;
        }

        private static float GetTotalAreaWhereClosestToSelector(int skinDataIndex)
        {
            float num = 0f;
            if (skinDataIndex == 0)
            {
                num += SkinColors[skinDataIndex].selector;
            }
            else if (SkinColors.Length > 1)
            {
                num += (SkinColors[skinDataIndex].selector - SkinColors[skinDataIndex - 1].selector) / 2f;
            }
            if (skinDataIndex == SkinColors.Length - 1)
            {
                num += 1f - SkinColors[skinDataIndex].selector;
            }
            else if (SkinColors.Length > 1)
            {
                num += (SkinColors[skinDataIndex + 1].selector - SkinColors[skinDataIndex].selector) / 2f;
            }
            return num;
        }

        private static int GetSkinDataIndexOfMelanin(float melanin)
        {
            int result = 0;
            for (int i = 0; i < SkinColors.Length; i++)
            {
                if (melanin < SkinColors[i].melanin)
                {
                    break;
                }
                result = i;
            }
            return result;
        }
    }
}
