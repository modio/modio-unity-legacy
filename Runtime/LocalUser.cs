using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Structure for storing data about a user specific to this device.</summary>
    [System.Serializable]
    public struct LocalUser
    {
        // ---------[ Constants ]---------
        /// <summary>File that this class uses to store user data.</summary>
        public static readonly string FILENAME = "user.data";

        // ---------[ Singleton ]---------
        /// <summary>Singleton instance.</summary>
        private static LocalUser _instance;

        /// <summary>Singleton instance accessor.</summary>
        public static LocalUser instance
        {
            get {
                return LocalUser._instance;
            }
            set {
                LocalUser._instance = value;
                LocalUser.AssertListsNotNull(ref LocalUser._instance);
            }
        }

        // ---------[ Static Fields ]---------
        /// <summary>Is the instance loaded?</summary>
        public static bool isLoaded;

        // ---------[ Fields ]---------
        /// <summary>mod.io User Profile.</summary>
        public UserProfile profile;

        /// <summary>User authentication token to send with API requests identifying the
        /// user.</summary>
        public string oAuthToken;

        /// <summary>A flag to indicate that the auth token has been rejected.</summary>
        public bool wasTokenRejected;

        /// <summary>Mods the user has enabled on this device.</summary>
        public List<int> enabledModIds;

        /// <summary>Mods the user is subscribed to.</summary>
        public List<int> subscribedModIds;

        /// <summary>Queued subscribe actions.</summary>
        public List<int> queuedSubscribes;

        /// <summary>Queued unsubscribe actions</summary>
        public List<int> queuedUnsubscribes;

        /// <summary>External authentication data for the session.</summary>
        /// <remarks>This data is **not** stored between sessions.</remarks>
        [Newtonsoft.Json.JsonIgnore]
        public ExternalAuthenticationData externalAuthentication;

        // --- Accessors ---
        /// <summary>Returns the summarised authentication state.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public AuthenticationState authenticationState
        {
            get {
                if(string.IsNullOrEmpty(this.oAuthToken))
                {
                    return AuthenticationState.NoToken;
                }
                else if(this.wasTokenRejected)
                {
                    return AuthenticationState.RejectedToken;
                }
                else
                {
                    return AuthenticationState.ValidToken;
                }
            }
        }

        // --- Static Accessors ---
        /// <summary>[Singleton Instance Accessor] mod.io User Id.</summary>
        public static int UserId
        {
            get {
                return (LocalUser._instance.profile != null ? LocalUser._instance.profile.id
                                                            : UserProfile.NULL_ID);
            }
        }

        /// <summary>[Singleton Instance Accessor] mod.io User Profile.</summary>
        public static UserProfile Profile
        {
            get {
                return LocalUser._instance.profile;
            }
            set {
                LocalUser._instance.profile = value;
            }
        }

        /// <summary>[Singleton Instance Accessor] User authentication token to send with API
        /// requests identifying the user.</summary>
        public static string OAuthToken
        {
            get {
                return LocalUser._instance.oAuthToken;
            }
            set {
                LocalUser._instance.oAuthToken = value;
            }
        }

        /// <summary>[Singleton Instance Accessor] A flag to indicate that the auth token has been
        /// rejected.</summary>
        public static bool WasTokenRejected
        {
            get {
                return LocalUser._instance.wasTokenRejected;
            }
            set {
                LocalUser._instance.wasTokenRejected = value;
            }
        }

        /// <summary>[Singleton Instance Accessor] Mods the user has enabled on this
        /// device.</summary>
        public static List<int> EnabledModIds
        {
            get {
                return LocalUser._instance.enabledModIds;
            }
            set {
                if(value == null)
                {
                    value = new List<int>();
                }
                LocalUser._instance.enabledModIds = value;
            }
        }

        /// <summary>[Singleton Instance Accessor] Mods the user is subscribed to.</summary>
        public static List<int> SubscribedModIds
        {
            get {
                return LocalUser._instance.subscribedModIds;
            }
            set {
                if(value == null)
                {
                    value = new List<int>();
                }
                LocalUser._instance.subscribedModIds = value;
            }
        }

        /// <summary>[Singleton Instance Accessor] Queued subscribe actions.</summary>
        public static List<int> QueuedSubscribes
        {
            get {
                return LocalUser._instance.queuedSubscribes;
            }
            set {
                if(value == null)
                {
                    value = new List<int>();
                }
                LocalUser._instance.queuedSubscribes = value;
            }
        }

        /// <summary>[Singleton Instance Accessor] Queued unsubscribe actions</summary>
        public static List<int> QueuedUnsubscribes
        {
            get {
                return LocalUser._instance.queuedUnsubscribes;
            }
            set {
                if(value == null)
                {
                    value = new List<int>();
                }
                LocalUser._instance.queuedUnsubscribes = value;
            }
        }

        /// <summary>[Singleton Instance Accessor] External authentication data for the
        /// session.</summary>
        public static ExternalAuthenticationData ExternalAuthentication
        {
            get {
                return LocalUser._instance.externalAuthentication;
            }
            set {
                LocalUser._instance.externalAuthentication = value;
            }
        }

        /// <summary>[Singleton Instance Accessor] Returns the summarised authentication
        /// state.</summary>
        public static AuthenticationState AuthenticationState
        {
            get {
                return LocalUser._instance.authenticationState;
            }
        }

        // ---------[ Initialization ]---------
        /// <summary>Sets the initial Singleton values.</summary>
        static LocalUser()
        {
            LocalUser._instance = new LocalUser();
            LocalUser.AssertListsNotNull(ref LocalUser._instance);
            LocalUser.isLoaded = false;
        }

        // ---------[ Data I/O ]---------
        /// <summary>Loads the LocalUser instance.</summary>
        public static void Load(System.Action callback = null)
        {
            LocalUser.isLoaded = false;

            UserDataStorage.ReadJSONFile<LocalUser>(LocalUser.FILENAME,
                                                    (path, success, fileData) => {
                                                        LocalUser.AssertListsNotNull(ref fileData);

                                                        LocalUser._instance = fileData;
                                                        LocalUser.isLoaded = success;

                                                        if(callback != null)
                                                        {
                                                            callback.Invoke();
                                                        }
                                                    });
        }

        /// <summary>Saves the LocalUser instance.</summary>
        public static void Save(System.Action callback = null)
        {
            UserDataStorage.WriteJSONFile(LocalUser.FILENAME, LocalUser._instance,
                                          (path, success) => {
                                              if(callback != null)
                                              {
                                                  callback.Invoke();
                                              }
                                          });
        }

        // ---------[ Utility ]---------
        /// <summary>Asserts that the list fields are not null.</summary>
        public static void AssertListsNotNull(ref LocalUser userData)
        {
            if(userData.enabledModIds == null || userData.subscribedModIds == null
               || userData.queuedSubscribes == null || userData.queuedUnsubscribes == null)
            {
                if(userData.enabledModIds == null)
                {
                    userData.enabledModIds = new List<int>();
                }
                if(userData.subscribedModIds == null)
                {
                    userData.subscribedModIds = new List<int>();
                }
                if(userData.queuedSubscribes == null)
                {
                    userData.queuedSubscribes = new List<int>();
                }
                if(userData.queuedUnsubscribes == null)
                {
                    userData.queuedUnsubscribes = new List<int>();
                }
            }
        }
    }
}
