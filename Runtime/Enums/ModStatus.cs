namespace ModIO
{
    public enum ModStatus
    {
        /// <summary>
        /// Mod is not accepted and not returned when browsing.
        /// Mods will be returned if requested directly provided the
        /// user is an admin or subscribed to the content. All
        /// resources are always returned via the /me endpoints.
        /// </summary>
        NotAccepted = 0,

        /// <summary>
        /// Mod is accepted and returned via all endpoints.
        /// </summary>
        Accepted = 1,

        /// <summary>
        /// Resource is deleted and only returned via the /me
        /// endpoints.
        /// </summary>
        Deleted = 3,

        /// <summary>
        /// [Obsolete] Resource is accepted and returned via all endpoints
        /// (but flagged as out of date/incompatible).
        /// </summary>
        [System.Obsolete(
            "No longer used. All mods previously Archived are now Accepted.")] Archived = 2,
    }
}
