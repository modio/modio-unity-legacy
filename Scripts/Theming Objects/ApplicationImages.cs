using UnityEngine;

namespace ModIO
{
    [CreateAssetMenu(fileName = "New Application Image Set", menuName = "ModIO/Theming/Application Image Set")]
    public class ApplicationImages : ScriptableObject
    {
        // ------[ SINGLETON ]------
        static ApplicationImages _instance = null;
        public static ApplicationImages Instance
        {
            get
            {
                if (!_instance)
                {
                    if(Resources.LoadAll<ApplicationImages>("").Length > 0)
                    {
                        _instance = Resources.FindObjectsOfTypeAll<ApplicationImages>()[0];
                    }
                    else
                    {
                        Debug.LogWarning("[mod.io] Unable to locate the mod.io ApplicationImages. Creating run-time instance.");
                        Debug.LogWarning("[mod.io] Unable to locate the mod.io ApplicationImages. Creating run-time instance.");
                        _instance = ScriptableObject.CreateInstance<ApplicationImages>();
                    }
                }
                return _instance;
            }
        }

        // ------[ FIELDS ]------
        public Texture2D loadingPlaceholder;

        // ------[ STATIC ACCESSORS ]------
        public static Texture2D LoadingPlaceholder { get { return Instance.loadingPlaceholder; } }
    }
}
