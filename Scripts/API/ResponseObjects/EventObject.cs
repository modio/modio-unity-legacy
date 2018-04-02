namespace ModIO.API
{
    [System.Serializable]
    public struct EventObject
    {
        // Unique id of the event object.
        public int id;
        // Unique id of the parent mod.
        public int mod_id;
        // Unique id of the user who performed the action.
        public int user_id;
        // Unix timestamp of date the event occurred.
        public int date_added;
        // Type of event was 'MODFILE_CHANGED', 'MOD_AVAILABLE', 'MOD_UNAVAILABLE', 'MOD_EDITED'.
        public string event_type;
    }
}
