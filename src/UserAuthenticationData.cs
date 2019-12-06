using System.Collections.Generic;

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
            externalAuthToken = null,
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

        /// <summary>External authentication service token.</summary>
        public string externalAuthToken;

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
                UserProfile p = UserAccountManagement.ActiveUserProfile;

                UserAuthenticationData data = new UserAuthenticationData()
                {
                    userId = (p == null ? UserProfile.NULL_ID : p.id),
                    token = UserAccountManagement.ActiveUserToken,
                    wasTokenRejected = UserAccountManagement.WasTokenRejected,
                    externalAuthToken = UserAccountManagement.ExternalAuthTicket,
                };

                return data;
            }
            set
            {
                // get existing values
                List<int> enabled = UserAccountManagement.GetEnabledMods();
                List<int> subscribed = UserAccountManagement.GetSubscribedMods();

                UserProfile profile = UserAccountManagement.ActiveUserProfile;
                if(profile == null
                   || profile.id != value.userId)
                {
                    profile = new UserProfile()
                    {
                        id = value.userId,
                    };
                }

                // create data
                LocalUser userData = new LocalUser()
                {
                    profile = profile,
                    oAuthToken = value.token,
                    wasTokenRejected = value.wasTokenRejected,

                    externalAuthTicket = new ExternalAuthenticationTicket()
                    {
                        value = value.externalAuthToken,
                        provider = ExternalAuthenticationProvider.None,
                    },

                    enabledModIds = enabled.ToArray(),
                    subscribedModIds = subscribed.ToArray(),
                };

                // set
                UserAccountManagement.SetLocalUserData(userData);
                UserAccountManagement.SaveActiveUser();
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

        // ---------[ OBSOLETE ]---------
        /// <summary>[Obsolete] Steam ticket (if applicable).</summary>
        [System.Obsolete("Use externalAuthToken instead.")]
        public string steamTicket
        {
            get { return this.externalAuthToken; }
            set { this.externalAuthToken = value; }
        }

        /// <summary>[Obsolete] GOG ticket (if applicable).</summary>
        [System.Obsolete("Use externalAuthToken instead.")]
        public string gogTicket
        {
            get { return this.externalAuthToken; }
            set { this.externalAuthToken = value; }
        }
    }
}
