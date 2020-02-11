using Newtonsoft.Json;

using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>A singleton struct that is referenced by multiple classes for user authentication.</summary>
    [System.Serializable]
    public struct UserAuthenticationData
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>An instance of UserAuthenticationData with zeroed fields.</summary>
        public static readonly UserAuthenticationData NONE = new UserAuthenticationData()
        {
            userId = UserProfile.NULL_ID,
            token = null,
            wasTokenRejected = false,
            steamTicket = null,
            gogTicket = null,
        };

        /// <summary>Location of the settings file.</summary>
        public static readonly string FILE_LOCATION = IOUtilities.CombinePath(PluginSettings.data.cacheDirectory,
                                                                              "user.data");

        // ---------[ FIELDS ]---------
        /// <summary>User Id associated with the stored OAuthToken.</summary>
        public int userId;

        /// <summary>User authentication token to send with API requests identifying the user.</summary>
        public string token;

        /// <summary>A flag to indicate that the auth token has been rejected.</summary>
        public bool wasTokenRejected;

        /// <summary>Steam ticket (if applicable).</summary>
        public string steamTicket;

        /// <summary>GOG ticket (if applicable).</summary>
        public string gogTicket;

        // --- ACCESSORS ---
        [JsonIgnore]
        public bool IsTokenValid
        { get { return !this.wasTokenRejected && !string.IsNullOrEmpty(this.token); } }

        // ---------[ SINGLETON ]---------
        /// <summary>Singleton instance to be used as the current/active data.</summary>
        private static UserAuthenticationData m_instance;

        /// <summary>Singleton instance to be used as the current/active data.</summary>
        public static UserAuthenticationData instance
        {
            get
            {
                #if UNITY_EDITOR
                if(!Application.isPlaying)
                {
                    LoadInstance();
                }
                #endif

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
            else
            {
                UserAuthenticationData.m_instance = UserAuthenticationData.NONE;
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
