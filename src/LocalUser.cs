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
        public static LocalUser instance;

        // ---------[ Static Fields ]---------
        /// <summary>Is the instance loaded?</summary>
        public static bool isLoaded;

        // ---------[ Fields ]---------
        /// <summary>mod.io User Profile.</summary>
        public UserProfile profile;

        /// <summary>User authentication token to send with API requests identifying the user.</summary>
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

        // --- Accessor ---
        /// <summary>Returns the summarised authentication state.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public AuthenticationState AuthenticationState
        {
            get
            {
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
        /// <summary>[Singleton Instance Accessor] mod.io User Profile.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public static UserProfile Profile
        {
            get { return LocalUser.instance.profile; }
            set { LocalUser.instance.profile = value; }
        }

        /// <summary>[Singleton Instance Accessor] User authentication token to send with API requests identifying the user.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public static string OAuthToken
        {
            get { return LocalUser.instance.oAuthToken; }
            set { LocalUser.instance.oAuthToken = value; }
        }

        /// <summary>[Singleton Instance Accessor] A flag to indicate that the auth token has been rejected.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public static bool WasTokenRejected
        {
            get { return LocalUser.instance.wasTokenRejected; }
            set { LocalUser.instance.wasTokenRejected = value; }
        }

        /// <summary>[Singleton Instance Accessor] Mods the user has enabled on this device.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public static List<int> EnabledModIds
        {
            get { return LocalUser.instance.enabledModIds; }
            set { LocalUser.instance.enabledModIds = value; }
        }

        /// <summary>[Singleton Instance Accessor] Mods the user is subscribed to.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public static List<int> SubscribedModIds
        {
            get { return LocalUser.instance.subscribedModIds; }
            set { LocalUser.instance.subscribedModIds = value; }
        }

        /// <summary>[Singleton Instance Accessor] Queued subscribe actions.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public static List<int> QueuedSubscribes
        {
            get { return LocalUser.instance.queuedSubscribes; }
            set { LocalUser.instance.queuedSubscribes = value; }
        }

        /// <summary>[Singleton Instance Accessor] Queued unsubscribe actions</summary>
        [Newtonsoft.Json.JsonIgnore]
        public static List<int> QueuedUnsubscribes
        {
            get { return LocalUser.instance.queuedUnsubscribes; }
            set { LocalUser.instance.queuedUnsubscribes = value; }
        }

        /// <summary>[Singleton Instance Accessor] External authentication data for the session.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public static ExternalAuthenticationData ExternalAuthentication
        {
            get { return LocalUser.instance.externalAuthentication; }
            set { LocalUser.instance.externalAuthentication = value; }
        }

        // ---------[ Initialization ]---------
        /// <summary>Sets the initial Singleton values.</summary>
        static LocalUser()
        {
            LocalUser.isLoaded = false;
            LocalUser.instance = new LocalUser();
        }

        /// <summary>Loads the LocalUser instance.</summary>
        public static System.Collections.IEnumerator Load(System.Action callback)
        {
            bool isDone = false;

            LocalUser.isLoaded = false;

            UserDataStorage.TryReadJSONFile<LocalUser>(LocalUser.FILENAME, (success, fileData) =>
            {
                LocalUser.AssertListsNotNull(ref fileData);

                LocalUser.instance = fileData;
                LocalUser.isLoaded = success;

                if(callback != null) { callback.Invoke(); }
            });

            while(!isDone) { yield return null; }
        }

        /// <summary>Saves the LocalUser instance.</summary>
        public static System.Collections.IEnumerator Save(System.Action callback)
        {
            UserDataStorage.TryWriteJSONFile(UserAccountManagement.USER_DATA_FILENAME,
                                             UserAccountManagement.activeUser);

            if(callback != null) { callback.Invoke(); }

            yield return null;
        }

        /// <summary>Asserts that the list fields are not null.</summary>
        public static void AssertListsNotNull(ref LocalUser userData)
        {
            if(userData.enabledModIds == null
               || userData.subscribedModIds == null
               || userData.queuedSubscribes == null
               || userData.queuedUnsubscribes == null)
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
