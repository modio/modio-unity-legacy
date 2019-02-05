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

                            cacheDirParts[i] = cacheDirParts[i].Replace("$GAME_ID$", settings.gameId.ToString());
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

                            installDirParts[i] = installDirParts[i].Replace("$GAME_ID$", settings.gameId.ToString());
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

        /// <summary>Loads the PluginSettings from disk.</summary>
        [System.Obsolete]
        public static PluginSettingsData LoadDefaults()
        {
            return PluginSettings._data;
        }

        #if UNITY_EDITOR
        [UnityEditor.MenuItem("mod.io/Edit Plugin Settings", false)]
        public static void FocusAsset()
        {
            string assetPath = "Assets/Resources/" + PluginSettings.FILE_PATH + ".asset";
            PluginSettings settings = Resources.Load<PluginSettings>(PluginSettings.FILE_PATH);

            if(settings == null)
            {
                settings = PluginSettings.InitializeAsset();
            }

            UnityEditor.EditorGUIUtility.PingObject(settings);
            UnityEditor.Selection.activeObject = settings;
        }

        private static PluginSettings InitializeAsset()
        {
            string assetPath = "Assets/Resources/" + PluginSettings.FILE_PATH + ".asset";
            PluginSettings settings = ScriptableObject.CreateInstance<PluginSettings>();

            if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }
            settings.values.apiURL = APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION;
            settings.values.gameId = 0;
            settings.values.gameAPIKey = string.Empty;
            settings.values.cacheDirectory = "$PERSISTENT_DATA_PATH$/modio-$GAME_ID$";
            settings.values.installDirectory = "$PERSISTENT_DATA_PATH$/modio-$GAME_ID$/_installedMods";

            UnityEditor.AssetDatabase.CreateAsset(settings, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return settings;
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
