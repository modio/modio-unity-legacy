using UnityEngine;

namespace ModIO
{
    /// <summary>Wrapper object for the PluginSettings.Data.</summary>
    public class PluginSettings : ScriptableObject
    {
        [System.Serializable]
        public struct Data
        {
            // ---------[ FIELDS ]---------
            [Tooltip("API URL to use when making requests")]
            public string   apiURL;
            [Tooltip("Game Id assigned to your game profile")]
            public int      gameId;
            [Tooltip("API Key assigned to your game profile")]
            public string   gameAPIKey;
            // [Tooltip("Directory to use for persistent data")]
            // public string   storageDirectory;
            [Tooltip("Directory to use for mod installations")]
            public string   installationDirectory;
            [Tooltip("Directory to use for cached server data")]
            public string   cacheDirectory;
        }

        /// <summary>Location of the settings file.</summary>
        public static readonly string FILE_PATH = "modio_settings";

        /// <summary>Has the asset been loaded.</summary>
        private static bool _loaded = false;

        /// <summary>Singleton instance.</summary>
        private static Data _dataInstance;

        /// <summary>The values that the plugin should use.</summary>
        public static Data data
        {
            get
            {
                if(!PluginSettings._loaded)
                {
                    PluginSettings.LoadDataInstance();
                }

                return PluginSettings._dataInstance;
            }
        }

        /// <summary>Settings data.</summary>
        [SerializeField]
        private Data m_data;

        /// <summary>Loads the Data from the asset instance.</summary>
        private static void LoadDataInstance()
        {
            PluginSettings wrapper = Resources.Load<PluginSettings>(PluginSettings.FILE_PATH);

            if(wrapper == null)
            {
                PluginSettings._dataInstance = new Data();
            }
            else
            {
                Data settings = wrapper.m_data;

                // - Path variable replacement -
                // cachedir
                if(settings.cacheDirectory != null)
                {
                    string[] cacheDirParts = settings.cacheDirectory.Split(System.IO.Path.AltDirectorySeparatorChar,
                                                                           System.IO.Path.DirectorySeparatorChar);
                    for(int i = 0; i < cacheDirParts.Length; ++i)
                    {
                        if(cacheDirParts[i].ToUpper().Equals("$PERSISTENT_DATA_PATH$"))
                        {
                            cacheDirParts[i] = Application.persistentDataPath;
                        }

                        cacheDirParts[i] = cacheDirParts[i].Replace("$GAME_ID$", settings.gameId.ToString());
                    }
                    settings.cacheDirectory = IOUtilities.CombinePath(cacheDirParts);
                }

                // installdir
                if(settings.installationDirectory != null)
                {
                    string[] installDirParts = settings.installationDirectory.Split(System.IO.Path.AltDirectorySeparatorChar,
                                                                                    System.IO.Path.DirectorySeparatorChar);
                    for(int i = 0; i < installDirParts.Length; ++i)
                    {
                        if(installDirParts[i].ToUpper().Equals("$PERSISTENT_DATA_PATH$"))
                        {
                            installDirParts[i] = Application.persistentDataPath;
                        }

                        installDirParts[i] = installDirParts[i].Replace("$GAME_ID$", settings.gameId.ToString());
                    }

                    settings.installationDirectory = IOUtilities.CombinePath(installDirParts);
                }

                // apply to data instance
                PluginSettings._dataInstance = settings;
            }

            PluginSettings._loaded = true;
        }


        /// <summary>Loads the PluginSettings from disk.</summary>
        [System.Obsolete]
        public static Data LoadDefaults()
        {
            return PluginSettings._dataInstance;
        }

        #if UNITY_EDITOR
        [UnityEditor.MenuItem("mod.io/Edit Settings", false)]
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
            settings.m_data.apiURL = APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION;
            settings.m_data.gameId = 0;
            settings.m_data.gameAPIKey = string.Empty;
            settings.m_data.cacheDirectory = "$PERSISTENT_DATA_PATH$/modio-$GAME_ID$";
            settings.m_data.installationDirectory = "$PERSISTENT_DATA_PATH$/modio-$GAME_ID$/_installedMods";

            UnityEditor.AssetDatabase.CreateAsset(settings, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return settings;
        }
        #endif
    }
}
