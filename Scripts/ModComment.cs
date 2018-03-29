using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    [System.Serializable]
    public class ModComment
    {
        // - Inner Classes -
        [System.Serializable]
        public class CommentThreadPosition
        {
            // ---------[ SERIALIZED MEMBERS ]---------
            [SerializeField] private int _depth = -1;
            [SerializeField] private int _mainThread = -1;
            [SerializeField] private int _replyThread = -1;
            [SerializeField] private int _subReplyThread = -1;

            // ---------[ FIELDS ]---------
            public int depth            { get { return this._depth; } }
            public int mainThread       { get { return this._mainThread; } }
            public int replyThread      { get { return this._replyThread; } }
            public int subReplyThread   { get { return this._subReplyThread; } }

            // ---------[ INITIALIZATION ]---------
            public void ApplyAPIObjectValues(string replyPosition)
            {
                string[] positionElements = replyPosition.Split('.');

                this._depth = 0;
                if(positionElements.Length > 0)
                {
                    this._depth = 1;
                    if(int.TryParse(positionElements[0], out this._mainThread)
                       && positionElements.Length > 1)
                    {
                        this._depth = 2;
                        if(int.TryParse(positionElements[1], out this._replyThread)
                           && positionElements.Length > 2)
                        {
                            this._depth = 3;
                            int.TryParse(positionElements[2], out this._subReplyThread);
                        }
                    }
                }
            }
        }

        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _id;
        [SerializeField] private int _modId;
        [SerializeField] private int _parentId;
        [SerializeField] private int _submittedById;
        [SerializeField] private TimeStamp _dateAdded;
        [SerializeField] private CommentThreadPosition _threadPosition;
        [SerializeField] private int _karma;
        [SerializeField] private int _karmaGuest;
        [SerializeField] private string _content;

        // ---------[ FIELDS ]---------
        public int id                               { get { return this._id; } }
        public int modId                            { get { return this._modId; } }
        public int submittedById                    { get { return this._submittedById; } }
        public TimeStamp dateAdded                  { get { return this._dateAdded; } }
        public int parentId                         { get { return this._parentId; } }
        public CommentThreadPosition threadPosition { get { return this._threadPosition; } }
        public int karma                            { get { return this._karma; } }
        public int karmaGuest                       { get { return this._karmaGuest; } }
        public string content                       { get { return this._content; } }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyAPIObjectValues(API.CommentObject apiObject)
        {
            this._id = apiObject.id;
            this._modId = apiObject.mod_id;
            this._submittedById = apiObject.submitted_by.id;
            this._dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this._parentId = apiObject.reply_id;
            this._karma = apiObject.karma;
            this._karmaGuest = apiObject.karma_guest;
            this._content = apiObject.content;

            // - Parse Thread Position -
            if(this._threadPosition == null)
            {
                this._threadPosition = new CommentThreadPosition();
            }
            this._threadPosition.ApplyAPIObjectValues(apiObject.reply_position);
        }

        public static ModComment CreateFromAPIObject(API.CommentObject apiObject)
        {
            var retVal = new ModComment();
            retVal.ApplyAPIObjectValues(apiObject);
            return retVal;
        }
    }
}
