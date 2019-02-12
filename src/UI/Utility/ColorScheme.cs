using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [CreateAssetMenu(fileName = "New ModIO Color Scheme.asset", menuName = "mod.io/Color Scheme")]
    public class ColorScheme : ScriptableObject
    {
        // ---------[ DEFAULT MANAGEMENT ]---------
        private static ColorScheme _defaultInstance;
        public static ColorScheme defaultInstance
        {
            get
            {
                if(ColorScheme._defaultInstance == null)
                {
                    ColorScheme[] schemes = Resources.LoadAll<ColorScheme>(string.Empty);

                    foreach(var scheme in schemes)
                    {
                        if(scheme.m_isDefault)
                        {
                            ColorScheme._defaultInstance = scheme;
                            break;
                        }
                    }
                }

                return ColorScheme._defaultInstance;
            }
        }

        #if UNITY_EDITOR
        public static void SetDefault(ColorScheme newDefault)
        {
            ColorScheme._defaultInstance = newDefault;
        }
        #endif

        [SerializeField][HideInInspector]
        private bool m_isDefault = false;


        // ---------[ BUTTONS ]---------
        [Header("Selectable Colors")]
        public ColorBlock selectableColors;
    }
}
