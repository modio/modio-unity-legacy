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
                    if(Resources.LoadAll<UISettings>("").Length > 0)
                    {
                        _instance = Resources.FindObjectsOfTypeAll<UISettings>()[0];
                    }
                    else
                    {
                        Debug.LogWarning("Unable to locate the mod.io UISettings. Creating run-time instance.");
                        _instance = ScriptableObject.CreateInstance<UISettings>();
                    }
                }
                return _instance;
            }
        }

        // ------[ INNER CLASSES ]------
        // NOTE(@jackson): In order to be used correctly, textures need to
        //  have the Import Setting: Advanced > Read/Write Enabled checked
        //  and Compression = None set
        [System.Serializable]
        public class PlaceholderImages
        {
            public Texture2D modLogo;
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

        public Texture2D AppIconLight;
        public Texture2D AppIconDark;
        public Texture2D AppIconColour;
        public Texture2D AppLogoLight;
        public Texture2D AppLogoDark;
        public Texture2D AppLogoColour;

        // Default loading textures
        public PlaceholderImages DownloadingPlaceholderImages;

        // Default editor textures
        public Texture2D EditorTexture_UndoButton;
    }
}