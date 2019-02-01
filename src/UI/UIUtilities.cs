using System;
using UnityEngine;

namespace ModIO.UI
{
    public static class UIUtilities
    {
        public static string ValueToDisplayString(int value)
        {
            if(value < 1000) // 0 - 999
            {
                return value.ToString();
            }
            else if(value < 100000) // 1.0K - 99.9K
            {
                // remove tens
                float truncatedValue = (value / 100) / 10f;
                return(truncatedValue.ToString() + "K");
            }
            else if(value < 10000000) // 100K - 999K
            {
                // remove hundreds
                int truncatedValue = (value / 1000);
                return(truncatedValue.ToString() + "K");
            }
            else if(value < 1000000000) // 1.0M - 99.9M
            {
                // remove tens of thousands
                float truncatedValue = (value / 100000) / 10f;
                return(truncatedValue.ToString() + "M");
            }
            else // 100M+
            {
                // remove hundreds of thousands
                int truncatedValue = (value / 1000000);
                return(truncatedValue.ToString() + "M");
            }
        }

        // TODO(@jackson): Add smallest unit param
        public static string ByteCountToDisplayString(Int64 value)
        {
            string[] sizeSuffixes = new string[]{"B", "KB", "MB", "GB"};
            int sizeIndex = 0;
            Int64 adjustedSize = value;
            Int64 lastSize = 0;
            while(adjustedSize > 0x0400
                  && (sizeIndex+1) < sizeSuffixes.Length)
            {
                lastSize = adjustedSize;
                adjustedSize /= 0x0400;
                ++sizeIndex;
            }

            if(sizeIndex > 0
               && adjustedSize < 100)
            {
                decimal displayValue = (decimal)lastSize / (decimal)0x0400;
                return displayValue.ToString("0.0") + sizeSuffixes[sizeIndex];
            }
            else
            {
                return adjustedSize + sizeSuffixes[sizeIndex];
            }
        }

        public static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            return Sprite.Create(texture,
                                 new Rect(0.0f, 0.0f, texture.width, texture.height),
                                 Vector2.zero);
        }

        public static void OpenYouTubeVideoURL(string youTubeVideoId)
        {
            if(!String.IsNullOrEmpty(youTubeVideoId))
            {
                Application.OpenURL(@"https://youtu.be/" + youTubeVideoId);
            }
        }
    }
}
