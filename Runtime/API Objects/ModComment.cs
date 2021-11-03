using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public struct ModCommentPosition
    {
        // ---------[ FIELDS ]---------
        public int depth;
        public int mainThread;
        public int replyThread;
        public int subReplyThread;
    }

    [System.Serializable]
    public class ModComment
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// Unique id of the comment.
        /// </summary>
        [JsonProperty("id")]
        public int id;

        /// <summary>
        /// Unique id of the parent mod.
        /// </summary>
        [JsonProperty("mod_id")]
        public int modId;

        /// <summary>
        /// Contains user data.
        /// </summary>
        [JsonProperty("submitted_by")]
        public UserProfile submittedBy;

        /// <summary>
        /// Unix timestamp of date the comment was posted.
        /// </summary>
        [JsonProperty("date_added")]
        public int dateAdded;

        /// <summary>
        /// Id of the parent comment this comment is replying to (can be 0 if the comment is not a
        /// reply).
        /// </summary>
        [JsonProperty("reply_id")]
        public int replyId;

        /// <summary>
        /// Levels of nesting in a comment thread.
        /// </summary>
        [JsonProperty("position")]
        public ModCommentPosition position;

        /// <summary>
        /// Karma received for the comment (can be postive or negative).
        /// </summary>
        [JsonProperty("karma")]
        public int karma;

        /// <summary>
        /// Karma received for guest comments (can be postive or negative).
        /// </summary>
        [JsonProperty("karma_guest")]
        public int karmaGuest;

        /// <summary>
        /// Contents of the comment.
        /// </summary>
        [JsonProperty("content")]
        public string content;

        // ---------[ API DESERIALIZATION ]---------
        /// <summary>
        /// An optional thread_position field, which is only deserialized from API responses
        /// </summary>
        [JsonProperty("thread_position")]
        private string _threadPositionString;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if(string.IsNullOrEmpty(this._threadPositionString))
            {
                return;
            }

            this.position = new ModCommentPosition();

            // - Parse Thread Position -
            string[] positionElements = this._threadPositionString.Split('.');

            this.position.depth = 0;
            this.position.mainThread = -1;
            this.position.replyThread = -1;
            this.position.subReplyThread = -1;

            if(positionElements.Length > 0)
            {
                this.position.depth = 1;
                if(int.TryParse(positionElements[0], out this.position.mainThread)
                   && positionElements.Length > 1)
                {
                    this.position.depth = 2;
                    if(int.TryParse(positionElements[1], out this.position.replyThread)
                       && positionElements.Length > 2)
                    {
                        this.position.depth = 3;
                        int.TryParse(positionElements[2], out this.position.subReplyThread);
                    }
                }
            }

            this._threadPositionString = null;
        }
    }
}
