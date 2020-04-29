using UnityEngine;

namespace ModIO
{
    /// <summary>Stores the settings used by various classes that are unique to the game/app.</summary>
    public class PluginSettings : ScriptableObject
    {
        // ---------[ NESTED CLASSES ]---------
        /// <summary>Attribute for denoting a field as containing directory variables.</summary>
        public class VariableDirectoryAttribute : PropertyAttribute {}

        /// <summary>Request logging options.</summary>
        [System.Serializable]
        public struct RequestLoggingOptions
        {
            [Tooltip("Should failed requests be logged as warnings")]
            public bool errorsAsWarnings;

            [Tooltip("Log all web request responses made received")]
            public bool logAllResponses;

            [Tooltip("Should the sending of a request be logged separately")]
            public bool logOnSend;
        }

        /// <summary>Data struct that is wrapped by the ScriptableObject.</summary>
        [System.Serializable]
        public struct Data
        {
            // ---------[ Fields ]---------
            [Tooltip("API URL to use when making requests")]
            public string apiURL;

            [Tooltip("Game Id assigned to your game profile")]
            public int gameId;

            [Tooltip("API Key assigned to your game profile")]
            public string gameAPIKey;

            [Tooltip("Directory to use for mod installations")]
            [VariableDirectory]
            public string installationDirectory;

            [Tooltip("Directory to use for cached server data")]
            [VariableDirectory]
            public string cacheDirectory;

            /// <summary>Request logging options.</summary>
            public RequestLoggingOptions requestLogging;

            // ---------[ Obsolete ]---------
            [System.Obsolete("Use requestLogging.logAllResponses instead.")]
            [HideInInspector]
            public bool logAllRequests
            {
                get { return this.requestLogging.logAllResponses; }
                set { this.requestLogging.logAllResponses = value; }
            }
        }

        // ---------[ CONSTANTS & STATICS ]---------
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
                    PluginSettings._dataInstance = PluginSettings.LoadDataFromAsset(PluginSettings.FILE_PATH);

                    #if UNITY_EDITOR
                        // If Application isn't playing, we reload every time
                        PluginSettings._loaded = Application.isPlaying;
                    #else
                        PluginSettings._loaded = true;
                    #endif
                }

                return PluginSettings._dataInstance;
            }
        }

        // ---------[ FIELDS ]---------
        /// <summary>Settings data.</summary>
        [SerializeField]
        #pragma warning disable 0649
        private Data m_data;
        #pragma warning restore 0649

        // --- Accessors ---
        public string API_URL
        {
            get { return PluginSettings.data.apiURL; }
        }
        public int GAME_ID
        {
            get { return PluginSettings.data.gameId; }
        }
        public string GAME_API_KEY
        {
            get { return PluginSettings.data.gameAPIKey; }
        }
        public RequestLoggingOptions REQUEST_LOGGING
        {
            get { return PluginSettings.data.requestLogging; }
        }
        public string INSTALLATION_DIRECTORY
        {
            get { return PluginSettings.data.installationDirectory; }
        }
        public string CACHE_DIRECTORY
        {
            get { return PluginSettings.data.cacheDirectory; }
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Loads the data from a PluginSettings asset.</summary>
        public static PluginSettings.Data LoadDataFromAsset(string assetPath)
        {
            PluginSettings wrapper = Resources.Load<PluginSettings>(assetPath);
            PluginSettings.Data settings;

            if(wrapper == null)
            {
                settings = new Data();
            }
            else
            {
                settings = wrapper.m_data;

                bool isTestServer = settings.apiURL.Contains("api.test.mod.io");

                // - Path variable replacement -
                // cachedir
                if(settings.cacheDirectory != null)
                {
                    settings.cacheDirectory = ReplaceDirectoryVariables(settings.cacheDirectory,
                                                                        settings.gameId,
                                                                        isTestServer);
                }

                // installdir
                if(settings.installationDirectory != null)
                {
                    settings.installationDirectory = ReplaceDirectoryVariables(settings.installationDirectory,
                                                                               settings.gameId,
                                                                               isTestServer);
                }
            }

            return settings;
        }

        /// <summary>Replaces variables in the directory values.</summary>
        public static string ReplaceDirectoryVariables(string directory, int gameId, bool isTestServer)
        {
            // straight replaces
            directory = (directory
                         .Replace("$PERSISTENT_DATA_PATH$", Application.persistentDataPath)
                         .Replace("$DATA_PATH$", Application.dataPath)
                         .Replace("$BUILD_GUID$", Application.buildGUID)
                         .Replace("$COMPANY_NAME$", Application.companyName)
                         .Replace("$PRODUCT_NAME$", Application.productName)
                         .Replace("$TEMPORARY_CACHE_PATH$", Application.temporaryCachePath)
                         .Replace("$APPLICATION_IDENTIFIER", Application.identifier)
                         .Replace("$GAME_ID$", gameId.ToString())
                         );

            // boolean replacements
            string testString = null;
            int testStringIndex = -1;

            testString = "$IS_TEST_SERVER?";
            testStringIndex = directory.IndexOf(testString);
            if(testStringIndex >= 0)
            {
                directory = ReplaceTestValueString(directory, testStringIndex, isTestServer);
            }

            return directory;
        }

        /// <summary>Processes a test variable string.</summary>
        private static string ReplaceTestValueString(string directory,
                                                     int testStringIndex,
                                                     bool testValue)
        {
            // get vars
            int trueStart = -1;
            int trueEnd = -1;
            int falseStart = -1;
            int testEnd = -1;

            trueStart = directory.IndexOf('?', testStringIndex+1) + 1;

            testEnd = directory.IndexOf('$', testStringIndex+1);

            #if UNITY_EDITOR
            if(!Application.isPlaying && testEnd < 0)
            {
                return "Missing \'$\': Directory contains an unclosed test string.";
            }
            #endif

            Debug.Assert(testEnd > 0, ("[mod.io] Unclosed test string \'"
                                       + directory.Substring(testStringIndex, trueStart-testStringIndex)
                                       + "\' in Plugin Settings directory. A closing \'$\' is required."));

            ++testEnd;

            falseStart = directory.IndexOf(':', trueStart);
            if(falseStart > testEnd
               || falseStart == -1)
            {
                falseStart = -1;
                trueEnd = testEnd-1;
            }
            else
            {
                trueEnd = falseStart;
                ++falseStart;
            }

            // replace
            string insertString = string.Empty;

            if(testValue)
            {
                insertString = directory.Substring(trueStart, trueEnd-trueStart);
            }
            else if(falseStart > -1)
            {
                insertString = directory.Substring(falseStart, testEnd-1-falseStart);
            }

            return directory
                .Remove(testStringIndex, testEnd-testStringIndex)
                .Insert(testStringIndex, insertString);
        }

        // ---------[ EDITOR CODE ]---------
        #if UNITY_EDITOR
        /// <summary>Locates the PluginSettings asset used at runtime.</summary>
        [UnityEditor.MenuItem("Tools/mod.io/Edit Settings", false)]
        public static void FocusAsset()
        {
            PluginSettings settings = Resources.Load<PluginSettings>(PluginSettings.FILE_PATH);

            if(settings == null)
            {
                PluginSettings.Data defaultData = PluginSettings.GenerateRuntimeDefaults();
                settings = PluginSettings.SetRuntimeData(defaultData);
            }

            UnityEditor.EditorGUIUtility.PingObject(settings);
            UnityEditor.Selection.activeObject = settings;
        }

        /// <summary>Generates a PluginSettings.Data instance with runtime defaults.</summary>
        public static PluginSettings.Data GenerateRuntimeDefaults()
        {
            PluginSettings.Data data = new PluginSettings.Data()
            {
                apiURL = APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION,
                gameId = GameProfile.NULL_ID,
                gameAPIKey = string.Empty,
                cacheDirectory = "$PERSISTENT_DATA_PATH$/modio-$GAME_ID$",
                installationDirectory = "$PERSISTENT_DATA_PATH$/modio-$GAME_ID$/_installedMods",
                requestLogging = new RequestLoggingOptions()
                {
                    errorsAsWarnings = true,
                    logAllResponses = false,
                    logOnSend = false,
                },
            };

            return data;
        }

        /// <summary>Stores the given values to the Runtime asset.</summary>
        public static PluginSettings SetRuntimeData(PluginSettings.Data data)
        {
            return PluginSettings.SaveToAsset(PluginSettings.FILE_PATH, data);
        }

        /// <summary>Sets/saves the settings for the runtime instance.</summary>
        public static PluginSettings SaveToAsset(string path,
                                                 PluginSettings.Data data)
        {
            string assetPath = IOUtilities.CombinePath("Assets", "Resources", path + ".asset");

            // creates the containing folder
            string assetFolder = System.IO.Path.GetDirectoryName(assetPath);
            LocalDataStorage.CreateDirectory(assetFolder);

            // create asset
            PluginSettings settings = ScriptableObject.CreateInstance<PluginSettings>();
            settings.m_data = data;

            // save
            UnityEditor.AssetDatabase.CreateAsset(settings, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return settings;
        }

        // ---------[ Obsolete ]---------
        /// <summary>[Obsolete] Sets the values of the Plugin Settings.</summary>
        [System.Obsolete("Use PluginSettings.SetRuntimeData() instead.")]
        public static PluginSettings SetGlobalValues(PluginSettings.Data data)
        {
            return PluginSettings.SetRuntimeData(data);
        }

        /// <summary>[Obsolete] Creates the asset instance that the plugin will use.</summary>
        [System.Obsolete("Use PluginSettings.GenerateRuntimeDefaults() and PluginSettings.SetRuntimeData() instead.")]
        private static PluginSettings InitializeAsset()
        {
            PluginSettings.Data data = PluginSettings.GenerateRuntimeDefaults();
            return PluginSettings.SetRuntimeData(data);
        }
        #endif
    }
}
