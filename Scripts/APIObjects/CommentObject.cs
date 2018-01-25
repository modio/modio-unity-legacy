using System;

namespace ModIO.API
{
    [Serializable]
    public struct CommentObject : IEquatable<CommentObject>
    {
        // - Fields -
        public int id;  // Unique id of the comment.
        public int mod_id;  // Unique id of the parent mod.
        public UserObject submitted_by; // Contains user data.
        public int date_added;  // Unix timestamp of date the comment was posted.
        public int reply_id;    // Id of the parent comment this comment is replying to (can be 0 if the comment is not a reply).
        public string reply_position; // Levels of nesting in a comment thread. How it works:
        public int karma;   // Karma received for the comment (can be postive or negative).
        public int karma_guest; // Karma received for guest comments (can be postive or negative).
        public string content; // Contents of the comment.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is CommentObject
                    && this.Equals((CommentObject)obj));
        }

        public bool Equals(CommentObject other)
        {
            return(this.id.Equals(other.id)
                   && this.mod_id.Equals(other.mod_id)
                   && this.submitted_by.Equals(other.submitted_by)
                   && this.date_added.Equals(other.date_added)
                   && this.reply_id.Equals(other.reply_id)
                   && this.reply_position.Equals(other.reply_position)
                   && this.karma.Equals(other.karma)
                   && this.karma_guest.Equals(other.karma_guest)
                   && this.content.Equals(other.content));
        }
    }
}
