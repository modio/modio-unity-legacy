namespace ModIO
{
    // TODO(@jackson): Missing MOD_DELETED, MOD_TEAM_CHANGED
    public enum ModEventType
    {
        _UNKNOWN = -1,
        ModAvailable,
        ModUnavailable,
        ModEdited,
        ModfileChanged,
    }
}
