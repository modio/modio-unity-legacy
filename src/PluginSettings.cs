using UnityEngine;

namespace ModIO
{
    /// <summary>Wrapper object for the PluginSettingsData.</summary>
    public class PluginSettings : ScriptableObject
    {
        /// <summary>Location of the settings file.</summary>
        public static readonly string FILE_PATH = "modio_settings";

        /// <summary>Has the asset been loaded.</summary>
        private static bool _loaded = false;

        /// <summary>Singleton instance.</summary>
        private static PluginSettingsData _data;

        /// <summary>The values that the plugin should use.</summary>
        public static PluginSettingsData data
        {
            get
            {
                if(!PluginSettings._loaded)
                {
                    PluginSettings wrapper = Resources.Load<PluginSettings>(PluginSettings.FILE_PATH);

                    if(wrapper == null)
                    {
                        PluginSettings._data = new PluginSettingsData();
                    }
                    else
                    {
                        PluginSettingsData settings = wrapper.values;

                        // - CacheDirectory Building -
                        string[] cacheDirParts = settings.cacheDirectory.Split('\\', '/');
                        for(int i = 0; i < cacheDirParts.Length; ++i)
                        {
                            if(cacheDirParts[i].ToUpper().Equals("$PERSISTENT_DATA_PATH$"))
                            {
                                cacheDirParts[i] = Application.persistentDataPath;
                            }
                        }
                        settings.cacheDirectory = IOUtilities.CombinePath(cacheDirParts);

                        // - Installation Building -
                        string[] installDirParts = settings.installDirectory.Split('\\', '/');
                        for(int i = 0; i < installDirParts.Length; ++i)
                        {
                            if(installDirParts[i].ToUpper().Equals("$PERSISTENT_DATA_PATH$"))
                            {
                                installDirParts[i] = Application.persistentDataPath;
                            }
                        }
                        settings.installDirectory = IOUtilities.CombinePath(installDirParts);

                        PluginSettings._data = settings;
                    }

                    PluginSettings._loaded = true;
                }

                return PluginSettings._data;
            }
        }

        /// <summary>Settings data.</summary>
        public PluginSettingsData values;

        /// <summary>Writes a PluginSettings file to disk.</summary>
        [System.Obsolete]
        public static void SaveDefaults(PluginSettingsData settings)
        {
            WriteSettingsAsset(settings);
        }

        /// <summary>Loads the PluginSettings from disk.</summary>
        [System.Obsolete]
        public static PluginSettingsData LoadDefaults()
        {
            return PluginSettings._data;
        }


        #if UNITY_EDITOR
        public static void WriteSettingsAsset(PluginSettingsData settings)
        {
            // TODO(@jackson)
            Debug.LogWarning("Ensure we save with the vars (Application.persistentDataPath for example)");

            PluginSettings asset = ScriptableObject.CreateInstance<PluginSettings>();
            asset.values = settings;

            if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }

            UnityEditor.AssetDatabase.CreateAsset(asset,
                                                  "Assets/Resources/" + PluginSettings.FILE_PATH + ".asset");
            UnityEditor.AssetDatabase.SaveAssets();
        }
        #endif
    }

    [System.Serializable]
    public struct PluginSettingsData
    {
        // ---------[ FIELDS ]---------
        public string   apiURL;
        public int      gameId;
        public string   gameAPIKey;
        public string   cacheDirectory;
        public string   installDirectory;
    }
}
