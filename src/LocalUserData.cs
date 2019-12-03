namespace ModIO
{
    /// <summary>Structure for storing data about a user specific to this device.</summary>
    public struct LocalUserData
    {
        // ---------[ FIELDS ]---------
        /// <summary>mod.io User Profile.</summary>
        public UserProfile profile;

        /// <summary>User authentication token to send with API requests identifying the user.</summary>
        public string oAuthToken;

        /// <summary>A flag to indicate that the auth token has been rejected.</summary>
        public bool wasTokenRejected;

        /// <summary>External authentication service token.</summary>
        public string externalAuthToken;

        /// <summary>External authentication service user id.</summary>
        public string externalAuthId;

        /// <summary>Mods the user has enabled on this device.</summary>
        public int[] enabledModIds;
    }
}
