using UnityEngine;

namespace ModIO
{
    [System.Serializable]
    public struct PluginSettings
    {
        // ---------[ FIELDS ]---------
        public string   apiURL;
        public int      gameId;
        public string   gameAPIKey;
        public string   cacheDirectory;
        public string   installDirectory;
        [HideInInspector]
        public string   authenticationToken;

        // ---------[ SAVE/LOAD ]---------
        /// <summary>Instance for removing need to load.</summary>
        private static PluginSettings _defaults;

        /// <summary>Location of the settings file.</summary>
        public static readonly string FILE_LOCATION = IOUtilities.CombinePath(Application.persistentDataPath,
                                                                              "modio",
                                                                              "settings.data");

        /// <summary>Writes a PluginSettings file to disk.</summary>
        public static void SaveDefaults(PluginSettings settings)
        {
            #if DEBUG
            if(Application.isPlaying)
            {
                if((Application.identifier.ToUpper().Contains("PRODUCTNAME")
                    && Application.identifier.ToUpper().Contains("COMPANY"))
                   || (PluginSettings.FILE_LOCATION.ToUpper().Contains("PRODUCTNAME")
                       && PluginSettings.FILE_LOCATION.ToUpper().Contains("COMPANY")))
                {
                    Debug.LogError("[mod.io] Implementing ModIO in a project that uses the default"
                                   + " bundle identifier will cause conflicts with other projects"
                                   + " using mod.io. Please open \'Build Settings' > \'Player Settings\'"
                                   + " and assign a unique Company Name, Project Name, and Bundle"
                                   + " Identifier (under \'Other Settings\') to utilize the mod.io "
                                   + " Unity Plugin.");
                }
            }
            #endif

            if(!PluginSettings._defaults.Equals(settings))
            {
                PluginSettings._defaults = settings;
                IOUtilities.WriteJsonObjectFile(FILE_LOCATION, settings);
            }
        }

        /// <summary>Loads the PluginSettings from disk.</summary>
        public static PluginSettings LoadDefaults()
        {
            #if DEBUG
            if(Application.isPlaying)
            {
                if((Application.identifier.ToUpper().Contains("PRODUCTNAME")
                    && Application.identifier.ToUpper().Contains("COMPANY"))
                   || (PluginSettings.FILE_LOCATION.ToUpper().Contains("PRODUCTNAME")
                       && PluginSettings.FILE_LOCATION.ToUpper().Contains("COMPANY")))
                {
                    Debug.LogError("[mod.io] Implementing ModIO in a project that uses the default"
                                   + " bundle identifier will cause conflicts with other projects"
                                   + " using mod.io. Please open \'Build Settings' > \'Player Settings\'"
                                   + " and assign a unique Company Name, Project Name, and Bundle"
                                   + " Identifier (under \'Other Settings\') to utilize the mod.io "
                                   + " Unity Plugin.");
                }
            }
            #endif

            if(PluginSettings._defaults.Equals(default(PluginSettings)))
            {
                PluginSettings._defaults = IOUtilities.ReadJsonObjectFile<PluginSettings>(PluginSettings.FILE_LOCATION);
            }

            return PluginSettings._defaults;
        }
    }
}
