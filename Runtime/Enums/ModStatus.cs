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
    }
}
