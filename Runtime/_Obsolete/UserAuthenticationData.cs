using Newtonsoft.Json;

namespace ModIO
{
    /// <summary>[Obsolete] A singleton struct that is referenced by multiple classes for user
    /// authentication.</summary>
    [System.Obsolete("Replaced by LocalUser.")]
    [System.Serializable]
    public struct UserAuthenticationData
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>An instance of UserAuthenticationData with zeroed fields.</summary>
        public static readonly UserAuthenticationData NONE = new UserAuthenticationData() {
            userId = UserProfile.NULL_ID, token = null,     wasTokenRejected = false,
            steamTicket = null,           gogTicket = null,
        };

        /// <summary>Location of the settings file.</summary>
        public static readonly string FILE_LOCATION =
            IOUtilities.CombinePath(DataStorage.CACHE_DIRECTORY, "user.data");

        // ---------[ FIELDS ]---------
        /// <summary>User Id associated with the stored OAuthToken.</summary>
        public int userId;

        /// <summary>User authentication token to send with API requests identifying the
        /// user.</summary>
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
        {
            get {
                return !this.wasTokenRejected && !string.IsNullOrEmpty(this.token);
            }
        }

        // ---------[ SINGLETON ]---------
        /// <summary>Singleton instance to be used as the current/active data.</summary>
        public static UserAuthenticationData instance
        {
            get {
                LocalUser userData = LocalUser.instance;
                UserProfile p = userData.profile;
                string steamTicket = null;
                string gogTicket = null;

                switch(LocalUser.ExternalAuthentication.provider)
                {
                    case ExternalAuthenticationProvider.Steam:
                    {
                        steamTicket = LocalUser.ExternalAuthentication.ticket;
                    }
                    break;

                    case ExternalAuthenticationProvider.GOG:
                    {
                        gogTicket = LocalUser.ExternalAuthentication.ticket;
                    }
                    break;
                }

                UserAuthenticationData data = new UserAuthenticationData() {
                    userId = (p == null ? UserProfile.NULL_ID : p.id),
                    token = userData.oAuthToken,
                    wasTokenRejected = userData.wasTokenRejected,
                    steamTicket = steamTicket,
                    gogTicket = gogTicket,
                };

                return data;
            }
            set {
                // get existing values
                LocalUser userData = LocalUser.instance;

                // profile data
                if(userData.profile == null || userData.profile.id != value.userId)
                {
                    if(value.userId == UserProfile.NULL_ID)
                    {
                        userData.profile = null;
                    }
                    else
                    {
                        userData.profile = new UserProfile() {
                            id = value.userId,
                        };
                    }
                }

                // copy auth data
                userData.oAuthToken = value.token;
                userData.wasTokenRejected = value.wasTokenRejected;

                // externalAuthData
                var externalAuth = new ExternalAuthenticationData() {
                    ticket = null,
                    provider = ExternalAuthenticationProvider.None,
                };

                if(!string.IsNullOrEmpty(value.steamTicket))
                {
                    externalAuth.ticket = value.steamTicket;
                    externalAuth.provider = ExternalAuthenticationProvider.Steam;
                }
                else if(!string.IsNullOrEmpty(value.gogTicket))
                {
                    externalAuth.ticket = value.gogTicket;
                    externalAuth.provider = ExternalAuthenticationProvider.GOG;
                }

                // set
                LocalUser.AssertListsNotNull(ref userData);
                LocalUser.instance = userData;
                LocalUser.isLoaded = true;
                LocalUser.Save();

                LocalUser.ExternalAuthentication = externalAuth;
            }
        }

        // ---------[ SAVE/LOAD ]---------
        /// <summary>Clears the instance and deletes the data on disk.</summary>
        public static void Clear()
        {
            UserAuthenticationData.instance = UserAuthenticationData.NONE;
        }
    }
}
