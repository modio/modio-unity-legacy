using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    [System.Serializable]
    public class ModComment
    {
        // - Inner Classes -
        [System.Serializable]
        public struct CommentThreadPosition
        {
            // ---------[ SERIALIZED MEMBERS ]---------
            [SerializeField] internal int _depth;
            [SerializeField] internal int _mainThread;
            [SerializeField] internal int _replyThread;
            [SerializeField] internal int _subReplyThread;

            // ---------[ FIELDS ]---------
            public int depth            { get { return this._depth; } }
            public int mainThread       { get { return this._mainThread; } }
            public int replyThread      { get { return this._replyThread; } }
            public int subReplyThread   { get { return this._subReplyThread; } }
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
        public void ApplyCommentObjectValues(API.CommentObject apiObject)
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
            string[] positionElements = apiObject.reply_position.Split('.');

            this._threadPosition._depth = 0;
            this._threadPosition._mainThread = -1;
            this._threadPosition._replyThread = -1;
            this._threadPosition._subReplyThread = -1;
            
            if(positionElements.Length > 0)
            {
                this._threadPosition._depth = 1;
                if(int.TryParse(positionElements[0], out this._threadPosition._mainThread)
                   && positionElements.Length > 1)
                {
                    this._threadPosition._depth = 2;
                    if(int.TryParse(positionElements[1], out this._threadPosition._replyThread)
                       && positionElements.Length > 2)
                    {
                        this._threadPosition._depth = 3;
                        int.TryParse(positionElements[2], out this._threadPosition._subReplyThread);
                    }
                }
            }
        }

        public static ModComment CreateFromCommentObject(API.CommentObject apiObject)
        {
            var retVal = new ModComment();
            retVal.ApplyCommentObjectValues(apiObject);
            return retVal;
        }
    }
}
