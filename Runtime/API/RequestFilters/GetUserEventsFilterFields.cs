namespace ModIO.API
{
    public static class GetUserEventsFilterFields
    {
        // (integer) Unique id of the event object.
        public const string id = "id";
        // (integer) Unique id of the game.
        public const string gameId = "game_id";
        // (integer) Unique id of the parent mod.
        public const string modId = "mod_id";
        // (integer) Unique id of the user who performed the action.
        public const string userId = "user_id";
        // (integer) Unix timestamp of date mod was updated.
        public const string dateAdded = "date_added";
        // (string)  Type of change that occurred:
        public const string eventType = "event_type";
    }
}
