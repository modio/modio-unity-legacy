namespace ModIO
{
    /// <summary>Structure for storing data about a user specific to this device.</summary>
    public struct LocalUserData
    {
        /// <summary>mod.io user id</summary>
        public int modioUserId;

        /// <summary>Mods the user has enabled on this device.</summary>
        public int[] enabledModIds;
    }
}
