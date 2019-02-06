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
        public static readonly string FILE_LOCATION = IOUtilities.CombinePath(PluginSettings.data.cacheDirectory,
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
        /// <summary>Writes the UserAuthenticationData to disk.</summary>
        private static void SaveInstance()
        {
            IOUtilities.WriteJsonObjectFile(FILE_LOCATION, UserAuthenticationData.m_instance);
        }

        /// <summary>Loads the UserAuthenticationData from disk.</summary>
        private static void LoadInstance()
        {
            UserAuthenticationData cachedData;
            if(IOUtilities.TryReadJsonObjectFile(FILE_LOCATION, out cachedData))
            {
                UserAuthenticationData.m_instance = cachedData;
            }
        }

        /// <summary>Clears the instance and deletes the data on disk.</summary>
        public static void Clear()
        {
            UserAuthenticationData.m_instance = UserAuthenticationData.NONE;
            IOUtilities.DeleteFile(UserAuthenticationData.FILE_LOCATION);
        }
    }
}
