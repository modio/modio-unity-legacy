namespace ModIO.API
{
    public static class GetModEventsFilterFields
    {
        // (integer) Unique id of the event object.
        public const string id = "id";

        // (integer) Unique id of the user who performed the action.
        public const string userId = "user_id";

        // (integer) Unix timestamp of date mod was added.
        public const string dateAdded = "date_added";

        // (string)  Type of change that occurred:
        public const string eventType = "event_type";

        // (boolean) Default value is true. Returns only the latest unique events, which is useful
        // for checking if the primary modfile has changed.
        public const string isLatestEventOfType = "latest";
    }
}
