namespace ModIO
{
    [System.Serializable]
    public class Modfile
    {
        // ---------[ INNERS ]---------
        [System.Serializable]
        public class EditableFields
        {
            public string fileName;
            public string version;
            public string changelog;
            public string metadataBlob;
        }

        // ---------[ MEMBERS ]---------
        [UnityEngine.SerializeField] private int _id;
        [UnityEngine.SerializeField] private int _modId;
        [UnityEngine.SerializeField] private TimeStamp _dateAdded;
        [UnityEngine.SerializeField] private int _fileSize;
        [UnityEngine.SerializeField] private string _md5;
        [UnityEngine.SerializeField] private string _fileName;
        [UnityEngine.SerializeField] private string _version;
        [UnityEngine.SerializeField] private string _changelog;
        [UnityEngine.SerializeField] private string _metadataBlob;
        [UnityEngine.SerializeField] private EditableFields _localChanges;

        // TODO(@jackson): public downloadexpand  Download Object Contains download data.

        // ---------[ FIELDS ]---------
        public int id               { get { return this._id; } }
        public int modId            { get { return this._modId; } }
        public TimeStamp dateAdded  { get { return this._dateAdded; } }
        public int fileSize         { get { return this._fileSize; } }
        public string md5           { get { return this._md5; } }
        public string fileName      { get { return this._fileName; } }
        public string version       { get { return this._version; } }
        public string changelog     { get { return this._changelog; } }
        public string metadataBlob  { get { return this._metadataBlob; } }
        public EditableFields localChanges
        {
            get { return this._localChanges; }
            set { this._localChanges = value; }
        }

        // ---------[ INITIALIZATION ]---------
        public void UpdateUsingAPIObjectValues(API.ModfileObject apiObject)
        {
            UnityEngine.Debug.Assert(this._id == 0 || this._id == apiObject.id);

            // --- Update Changes ---
            if(this._localChanges.fileName == this._fileName)
            {
                this._localChanges.fileName = apiObject.filename;
            }
            if(this._localChanges.version == this._version)
            {
                this._localChanges.version = apiObject.version;
            }
            if(this._localChanges.changelog == this._changelog)
            {
                this._localChanges.changelog = apiObject.changelog;
            }
            if(this._localChanges.metadataBlob == this._metadataBlob)
            {
                this._localChanges.metadataBlob = apiObject.metadata_blob;
            }

            // --- Update Fields ---
            this._id = apiObject.id;
            this._modId = apiObject.mod_id;
            this._dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this._fileSize = apiObject.filesize;
            this._md5 = apiObject.filehash.md5;
            this._fileName = apiObject.filename;
            this._version = apiObject.version;
            this._changelog = apiObject.changelog;
            this._metadataBlob = apiObject.metadata_blob;
        }

        public static Modfile CreateFromAPIObject(API.ModfileObject apiObject)
        {
            Modfile newModfile = new Modfile()
            {
                _id = apiObject.id,
                _modId = apiObject.mod_id,
                _dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added),
                _fileSize = apiObject.filesize,
                _md5 = apiObject.filehash.md5,
                _fileName = apiObject.filename,
                _version = apiObject.version,
                _changelog = apiObject.changelog,
                _metadataBlob = apiObject.metadata_blob,

                _localChanges = new EditableFields()
                {
                    fileName = apiObject.filename,
                    version = apiObject.version,
                    changelog = apiObject.changelog,
                    metadataBlob = apiObject.metadata_blob,
                }
            };
            return newModfile;
        }
    }
}
