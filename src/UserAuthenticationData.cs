using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

namespace ModIO
{
    [System.Serializable]
    public struct UserAuthenticationData
    {
        // ---------[ CONSTANTS ]---------
        public static readonly UserAuthenticationData NONE = new UserAuthenticationData()
        {
            userId = UserProfile.NULL_ID,
            token = null,
        };

        /// <summary>Location of the settings file.</summary>
        public static readonly string FILE_LOCATION = IOUtilities.CombinePath(Application.persistentDataPath,
                                                                              "modio",
                                                                              "user.data");

        // ---------[ FIELDS ]---------
        public int userId;
        public string token;

        // ---------[ SINGLETON ]---------
        /// <summary>Instance for removing need to load.</summary>
        private static UserAuthenticationData m_instance;

        public static UserAuthenticationData instance
        {
            get
            {
                if(m_instance.Equals(default(UserAuthenticationData)))
                {
                    LoadInstance();
                }
                return m_instance;
            }
            set
            {
                if(!UserAuthenticationData.m_instance.Equals(value))
                {
                    m_instance = value;
                    SaveInstance();
                }
            }
        }

        // ---------[ SAVE/LOAD ]---------
        /// <summary>Writes the instance instance to disk.</summary>
        private static void SaveInstance()
        {
            #if DEBUG
            if(Application.isPlaying)
            {
                if((Application.identifier.ToUpper().Contains("PRODUCTNAME")
                    && Application.identifier.ToUpper().Contains("COMPANY"))
                   || (UserAuthenticationData.FILE_LOCATION.ToUpper().Contains("PRODUCTNAME")
                       && UserAuthenticationData.FILE_LOCATION.ToUpper().Contains("COMPANY")))
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

            IOUtilities.WriteJsonObjectFile(FILE_LOCATION, UserAuthenticationData.m_instance);
        }

        /// <summary>Loads the UserAuthenticationData from disk.</summary>
        private static void LoadInstance()
        {
            #if DEBUG
            if(Application.isPlaying)
            {
                if((Application.identifier.ToUpper().Contains("PRODUCTNAME")
                    && Application.identifier.ToUpper().Contains("COMPANY"))
                   || (UserAuthenticationData.FILE_LOCATION.ToUpper().Contains("PRODUCTNAME")
                       && UserAuthenticationData.FILE_LOCATION.ToUpper().Contains("COMPANY")))
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

            UserAuthenticationData cachedData;
            if(IOUtilities.TryReadJsonObjectFile(FILE_LOCATION, out cachedData))
            {
                UserAuthenticationData.m_instance = cachedData;
            }
        }
    }
}
