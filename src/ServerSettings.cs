using UnityEngine;

namespace ModIO
{
    [System.Serializable]
    public struct ServerSettings
    {
        // ---------[ FIELDS ]---------
        public string   apiURL;
        public int      gameId;
        public string   gameAPIKey;
        public string   cacheDirectory;
        public string   installDirectory;

        // ---------[ SAVE/LOAD ]---------
        /// <summary>Instance for removing need to load.</summary>
        private static ServerSettings _instance;

        /// <summary>Location of the settings file.</summary>
        public static readonly string FILE_LOCATION = IOUtilities.CombinePath(Application.persistentDataPath,
                                                                              "modio",
                                                                              "settings.data");

        /// <summary>Writes a ServerSettings file to disk.</summary>
        public static void Save(ServerSettings settings)
        {
            #if DEBUG
            if(Application.isPlaying)
            {
                if((Application.identifier.ToUpper().Contains("PRODUCTNAME")
                    && Application.identifier.ToUpper().Contains("COMPANY"))
                   || (ServerSettings.FILE_LOCATION.ToUpper().Contains("PRODUCTNAME")
                       && ServerSettings.FILE_LOCATION.ToUpper().Contains("COMPANY")))
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

            if(!ServerSettings._instance.Equals(settings))
            {
                ServerSettings._instance = settings;
                IOUtilities.WriteJsonObjectFile(FILE_LOCATION, settings);
            }
        }

        /// <summary>Loads the ServerSettings from disk.</summary>
        public static ServerSettings Load()
        {
            #if DEBUG
            if(Application.isPlaying)
            {
                if((Application.identifier.ToUpper().Contains("PRODUCTNAME")
                    && Application.identifier.ToUpper().Contains("COMPANY"))
                   || (ServerSettings.FILE_LOCATION.ToUpper().Contains("PRODUCTNAME")
                       && ServerSettings.FILE_LOCATION.ToUpper().Contains("COMPANY")))
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

            if(ServerSettings._instance.Equals(default(ServerSettings)))
            {
                ServerSettings._instance = IOUtilities.ReadJsonObjectFile<ServerSettings>(ServerSettings.FILE_LOCATION);
            }

            return ServerSettings._instance;
        }
    }
}
