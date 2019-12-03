namespace ModIO
{
    /// <summary>Structure for storing data about a user specific to this device.</summary>
    public struct LocalUserData
    {
        /// <summary>mod.io User Profile.</summary>
        public UserProfile profile;

        /// <summary>Mods the user has enabled on this device.</summary>
        public int[] enabledModIds;
    }
}
