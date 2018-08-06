using UnityEngine;

namespace ModIO
{
    [CreateAssetMenu(fileName = "New Color Scheme", menuName = "ModIO/Theming/Color Scheme")]
    public class ColorScheme : ScriptableObject
    {
        // ------[ SINGLETON ]------
        static ColorScheme _instance = null;
        public static ColorScheme Instance
        {
            get
            {
                if (!_instance)
                {
                    if(Resources.LoadAll<ColorScheme>("").Length > 0)
                    {
                        _instance = Resources.FindObjectsOfTypeAll<ColorScheme>()[0];
                    }
                    else
                    {
                        Debug.LogWarning("[mod.io] Unable to locate the mod.io ColorScheme. Creating run-time instance.");
                        _instance = ScriptableObject.CreateInstance<ColorScheme>();
                    }
                }
                return _instance;
            }
        }

        // ------[ FIELDS ]------
        // 0x44BFD5FF (Blue)
        public Color primaryColor   = new Color(0x44/255.0f, 0xBF/255.0f, 0xD5/255.0f, 1.0f);
        // 0xFFFFFFFF (White)
        public Color whiteColor     = new Color(0xFF/255.0f, 0xFF/255.0f, 0xFF/255.0f, 1.0f);
        // 0xF5F5F5FF (Off-White)
        public Color lightColor     = new Color(0xF5/255.0f, 0xF5/255.0f, 0xF5/255.0f, 1.0f);
        // 0x2C2C3FFF (Blue-Gray)
        public Color darkColor      = new Color(0x2C/255.0f, 0x2C/255.0f, 0x3F/255.0f, 1.0f);
        // 0x171727FF (Dark Blue-Gray)
        public Color darkestColor   = new Color(0x17/255.0f, 0x17/255.0f, 0x27/255.0f, 1.0f);

        // ---------[ STATIC ACCESSORS ]---------
        public static Color PrimaryColor   { get { return Instance.primaryColor;  } }
        public static Color WhiteColor     { get { return Instance.whiteColor;    } }
        public static Color LightColor     { get { return Instance.lightColor;    } }
        public static Color DarkColor      { get { return Instance.darkColor;     } }
        public static Color DarkestColor   { get { return Instance.darkestColor;  } }
    }
}
