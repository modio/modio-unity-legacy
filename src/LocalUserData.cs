namespace ModIO
{
    /// <summary>Structure for storing data about a user specific to this device.</summary>
    public struct LocalUserData
    {
        /// <summary>mod.io user id</summary>
        public int modioUserId;

        /// <summary>Data for how the user is identified locally.</summary>
        public string localUserId;

        /// <summary>Mods the user has enabled on this device.</summary>
        public int[] enabledModIds;
    }
}
