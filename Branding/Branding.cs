using UnityEngine;

namespace ModIO
{
    public static class ColorPalette
    {
        // 0x44BFD5FF
        public static readonly Color PRIMARY    = new Color(0x44/255.0f, 0xBF/255.0f, 0xD5/255.0f, 1.0f);
        // 0xFFFFFFFF
        public static readonly Color WHITE      = new Color(0xFF/255.0f, 0xFF/255.0f, 0xFF/255.0f, 1.0f);
        // 0xF5F5F5FF
        public static readonly Color LIGHT      = new Color(0xF5/255.0f, 0xF5/255.0f, 0xF5/255.0f, 1.0f);
        // 0x2C2C3FFF
        public static readonly Color DARK       = new Color(0x2C/255.0f, 0x2C/255.0f, 0x3F/255.0f, 1.0f);
        // 0x171727FF
        public static readonly Color DARKEST    = new Color(0x17/255.0f, 0x17/255.0f, 0x27/255.0f, 1.0f);
    }
}