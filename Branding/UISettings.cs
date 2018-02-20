using UnityEngine;

namespace ModIO
{
    public class UISettings : ScriptableObject
    {
        // ------[ SINGLETON ]------
        static UISettings _instance = null;
        public static UISettings Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = Resources.FindObjectsOfTypeAll<UISettings>()[0];
                }
                return _instance;
            }
        }

        // ------[ FIELDS ]------
        // 0x44BFD5FF (Blue)
        public Color PrimaryColor   = new Color(0x44/255.0f, 0xBF/255.0f, 0xD5/255.0f, 1.0f);
        // 0xFFFFFFFF (White)
        public Color WhiteColor     = new Color(0xFF/255.0f, 0xFF/255.0f, 0xFF/255.0f, 1.0f);
        // 0xF5F5F5FF (Off-White)
        public Color LightColor     = new Color(0xF5/255.0f, 0xF5/255.0f, 0xF5/255.0f, 1.0f);
        // 0x2C2C3FFF (Blue-Gray)
        public Color DarkColor      = new Color(0x2C/255.0f, 0x2C/255.0f, 0x3F/255.0f, 1.0f);
        // 0x171727FF (Dark Blue-Gray)
        public Color DarkestColor   = new Color(0x17/255.0f, 0x17/255.0f, 0x27/255.0f, 1.0f);

        // Default loading textures
        public Texture2D LoadingLogo320x180;

        // Default editor textures
        public Texture2D EditorTexture_UndoButton;
    }
}