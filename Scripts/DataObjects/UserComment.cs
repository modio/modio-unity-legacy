using System;

namespace ModIO
{
    [Serializable]
    public class UserComment : IEquatable<UserComment>
    {
        // - Inner Classes -
        [Serializable]
        public class CommentPosition
        {
            public int depth = -1;

            public int mainThread = -1;
            public int replyThread = -1;
            public int subReplyThread = -1;
        }

        // - Constructors - 
        public static UserComment GenerateFromAPIObject(API.CommentObject apiObject)
        {
            UserComment newUserComment = new UserComment();
            newUserComment._data = apiObject;

            newUserComment.submittedBy = User.GenerateFromAPIObject(apiObject.submitted_by);
            newUserComment.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);

            // - Parse the position of the comment -
            newUserComment.position = new CommentPosition();

            int positionValue;
            string[] positionStrings = apiObject.reply_position.Split('.');

            newUserComment.position.depth = positionStrings.Length;
            if(positionStrings.Length > 0
               && int.TryParse(positionStrings[0], out positionValue))
            {
                newUserComment.position.mainThread = positionValue;

                if(positionStrings.Length > 1 
                   && int.TryParse(positionStrings[1], out positionValue))
                {
                    newUserComment.position.replyThread = positionValue;

                    if(positionStrings.Length > 2
                       && int.TryParse(positionStrings[2], out positionValue))
                    {
                        newUserComment.position.subReplyThread = positionValue;
                    }

                }
            }

            return newUserComment;
        }

        public static UserComment[] GenerateFromAPIObjectArray(API.CommentObject[] apiObjectArray)
        {
            UserComment[] objectArray = new UserComment[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = UserComment.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.CommentObject _data;

        public int id                   { get { return _data.id; } }
        public int modId                { get { return _data.mod_id; }}
        public User submittedBy         { get; private set; }
        public TimeStamp dateAdded      { get; private set; }
        public int parentCommentId      { get { return _data.reply_id; } }
        public CommentPosition position { get; private set; }
        public int karma                { get { return _data.karma; } }
        public int karmaGuest           { get { return _data.karma_guest; } }
        public string content           { get { return _data.content; } }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as UserComment);
        }

        public bool Equals(UserComment other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
