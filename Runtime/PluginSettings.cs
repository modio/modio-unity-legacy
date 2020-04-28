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
                #if UNITY_EDITOR
                if(!Application.isPlaying)
                {
                    PluginSettings._loaded = false;
                }
                #endif

                if(!PluginSettings._loaded)
                {
                    PluginSettings.LoadDataInstance(PluginSettings.FILE_PATH);
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

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Loads the Data from the asset instance.</summary>
        private static void LoadDataInstance(string assetPath)
        {
            PluginSettings.Data settings = PluginSettings.LoadDataFromAsset(assetPath);

            PluginSettings._dataInstance = settings;
            PluginSettings._loaded = true;
        }

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
        /// <summary>Locates the PluginSettings asset used by the plugin.</summary>
        [UnityEditor.MenuItem("Tools/mod.io/Edit Settings", false)]
        public static void FocusAsset()
        {
            PluginSettings settings = Resources.Load<PluginSettings>(PluginSettings.FILE_PATH);

            if(settings == null)
            {
                settings = PluginSettings.InitializeAsset();
            }

            UnityEditor.EditorGUIUtility.PingObject(settings);
            UnityEditor.Selection.activeObject = settings;
        }

        /// <summary>Creates the asset instance that the plugin will use.</summary>
        private static PluginSettings InitializeAsset()
        {
            PluginSettings.Data data = new PluginSettings.Data()
            {
                apiURL = APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION,
                gameId = 0,
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

            return SetGlobalValues(data);
        }

        /// <summary>Sets the values of the Plugin Settings.</summary>
        public static PluginSettings SetGlobalValues(PluginSettings.Data data)
        {
            string assetPath = "Assets/Resources/" + PluginSettings.FILE_PATH + ".asset";
            PluginSettings settings = ScriptableObject.CreateInstance<PluginSettings>();

            if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }

            settings.m_data = data;

            UnityEditor.AssetDatabase.CreateAsset(settings, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return settings;
        }

        /// <summary>Copies a Plugin Settings' values into the main asset.</summary>
        public static PluginSettings SetAssetAsGlobal(string assetPath)
        {
            PluginSettings valuesToCopy = UnityEditor.AssetDatabase.LoadAssetAtPath<PluginSettings>(assetPath);
            if(valuesToCopy == null)
            {
                Debug.LogError("[mod.io] PluginSettings at " + assetPath + " could not be found and"
                               + " thus the globally used PluginSettings asset was unchanged.");
                return null;
            }

            return PluginSettings.SetGlobalValues(valuesToCopy.m_data);
        }
        #endif
    }
}
