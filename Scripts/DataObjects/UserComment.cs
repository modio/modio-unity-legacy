using System;

namespace ModIO
{
    [Serializable]
    public class UserComment : IEquatable<UserComment>, IAPIObjectWrapper<API.CommentObject>, UnityEngine.ISerializationCallbackReceiver
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

        // - Fields -
        [UnityEngine.SerializeField]
        private API.CommentObject _data;

        public int id                   { get { return _data.id; } }
        public int modId                { get { return _data.mod_id; }}
        public UserProfile submittedBy         { get; private set; }
        public TimeStamp dateAdded      { get; private set; }
        public int parentCommentId      { get { return _data.reply_id; } }
        public CommentPosition position { get; private set; }
        public int karma                { get { return _data.karma; } }
        public int karmaGuest           { get { return _data.karma_guest; } }
        public string content           { get { return _data.content; } }

        // - IAPIObjectWrapper -
        public void WrapAPIObject(API.CommentObject apiObject)
        {
            this._data = apiObject;

            // this.submittedBy = new User();
            // this.submittedBy.WrapAPIObject(apiObject.submitted_by);
            this.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);

            // - Parse the position of the comment -
            this.position = new CommentPosition();

            int positionValue;
            string[] positionStrings = apiObject.reply_position.Split('.');

            this.position.depth = positionStrings.Length;
            if(positionStrings.Length > 0
               && int.TryParse(positionStrings[0], out positionValue))
            {
                this.position.mainThread = positionValue;

                if(positionStrings.Length > 1
                   && int.TryParse(positionStrings[1], out positionValue))
                {
                    this.position.replyThread = positionValue;

                    if(positionStrings.Length > 2
                       && int.TryParse(positionStrings[2], out positionValue))
                    {
                        this.position.subReplyThread = positionValue;
                    }

                }
            }
        }

        public API.CommentObject GetAPIObject()
        {
            return this._data;
        }

        // - ISerializationCallbackReceiver -
        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            this.WrapAPIObject(this._data);
        }

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
