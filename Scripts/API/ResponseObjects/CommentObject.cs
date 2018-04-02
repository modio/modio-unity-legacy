namespace ModIO.API
{
    [System.Serializable]
    public struct CommentObject
    {
        // Unique id of the comment.
        public readonly int id;
        // Unique id of the parent mod.
        public readonly int mod_id;
        // Unix timestamp of date the comment was posted.
        public readonly int date_added;
        // Id of the parent comment this comment is replying to (can be 0 if the comment is not a reply).
        public readonly int reply_id;
        // Levels of nesting in a comment thread. How it works:
        public readonly string reply_position;
        // Karma received for the comment (can be postive or negative).
        public readonly int karma;
        // Karma received for guest comments (can be postive or negative).
        public readonly int karma_guest;
        // Contents of the comment.
        public readonly string content;
        // Contains user data.
        public readonly UserObject submitted_by;
    }
}