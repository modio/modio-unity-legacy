namespace ModIO
{
    // - Enums -
    public enum ModStatus
    {
        NotAccepted = 0,
        Accepted = 1,
        Archived = 2,
        Deleted = 3,
    }
    public enum ModVisibility
    {
        Hidden = 0,
        Public = 1,
    }

    // TODO(@jackson): rename - ModProfile?
    [System.Serializable]
    public class Mod
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [UnityEngine.SerializeField] private int _id;
        [UnityEngine.SerializeField] private int _gameId;
        [UnityEngine.SerializeField] private ModStatus _status;
        [UnityEngine.SerializeField] private ModVisibility _visibility;
        [UnityEngine.SerializeField] private int _submittedById;
        [UnityEngine.SerializeField] private TimeStamp _dateAdded;
        [UnityEngine.SerializeField] private TimeStamp _dateUpdated;
        [UnityEngine.SerializeField] private TimeStamp _dateLive;
        [UnityEngine.SerializeField] private string _homepageURL;
        [UnityEngine.SerializeField] private string _name;
        [UnityEngine.SerializeField] private string _nameId;
        [UnityEngine.SerializeField] private string _summary;
        [UnityEngine.SerializeField] private string _description;
        [UnityEngine.SerializeField] private string _metadataBlob;
        [UnityEngine.SerializeField] private string _profileURL;
        [UnityEngine.SerializeField] private int _primaryModfileId;
        // TODO(@jackson): Update this
        [UnityEngine.SerializeField] private RatingSummary _ratingSummary;
        [UnityEngine.SerializeField] private string[] _tags;
        // TODO(@jackson): ModMedia
        // TODO(@jackson): TeamMembers
        [UnityEngine.SerializeField] private string[] _youtubeURLs;
        [UnityEngine.SerializeField] private string[] _sketchfabURLs;

        // ---------[ FIELDS ]---------
        public int id                       { get { return this._id; } }
        public int gameId                   { get { return this._gameId; } }
        public ModStatus status             { get { return this._status; } }
        public ModVisibility visibility     { get { return this._visibility; } }
        public int submittedById            { get { return this._submittedById; } }
        public TimeStamp dateAdded          { get { return this._dateAdded; } }
        public TimeStamp dateUpdated        { get { return this._dateUpdated; } }
        public TimeStamp dateLive           { get { return this._dateLive; } }
        public string homepageURL           { get { return this._homepageURL; } }
        public string name                  { get { return this._name; } }
        public string nameId                { get { return this._nameId; } }
        public string summary               { get { return this._summary; } }
        public string description           { get { return this._description; } }
        public string metadataBlob          { get { return this._metadataBlob; } }
        public string profileURL            { get { return this._profileURL; } }
        public int primaryModfileId         { get { return this._primaryModfileId; } }
        public string[] youtubeURLs         { get { return this._youtubeURLs; } }
        public string[] sketchfabURLs       { get { return this._sketchfabURLs; } }
        public RatingSummary ratingSummary  { get { return this._ratingSummary; } }
    }
}