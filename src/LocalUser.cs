namespace ModIO
{
    /// <summary>Structure for storing data about a user specific to this device.</summary>
    public struct LocalUser
    {
        // ---------[ FIELDS ]---------
        /// <summary>mod.io User Profile.</summary>
        public UserProfile profile;

        /// <summary>User authentication token to send with API requests identifying the user.</summary>
        public string oAuthToken;

        /// <summary>A flag to indicate that the auth token has been rejected.</summary>
        public bool wasTokenRejected;

        /// <summary>Mods the user has enabled on this device.</summary>
        public int[] enabledModIds;

        /// <summary>Mods the user is subscribed to.</summary>
        public int[] subscribedModIds;

        // ---------[ ACCESSORS ]---------
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
    }
}
