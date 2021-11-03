namespace ModIO.API
{
    public static class GetAllModCommentsFilterFields
    {
        // (integer) Unique id of the comment.
        public const string id = "id";
        // (integer) Unique id of the mod.
        public const string modId = "mod_id";
        // (integer) Unique id of the user who posted the comment.
        public const string submittedByUserId = "submitted_by";
        // (integer) Unix timestamp of date comment was posted.
        public const string dateAdded = "date_added";
        // (integer) Id of the parent comment this comment is replying to (can be 0 if the comment
        // is not a reply).
        public const string replyId = "reply_id";
        // (string)  Levels of nesting in a comment thread. You should order by this field, to
        // maintain comment grouping. How it works:
        public const string replyPosition = "reply_position";
        // (integer) Karma received for the comment (can be postive or negative).
        public const string karma = "karma";
        // (string)  Contents of the comment.
        public const string summary = "summary";
    }
}
