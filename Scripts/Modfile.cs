namespace ModIO
{
    [System.Serializable]
    public class Modfile
    {
        // ---------[ MEMBERS ]---------
        [UnityEngine.SerializeField] protected int _id;
        [UnityEngine.SerializeField] protected int _modId;
        [UnityEngine.SerializeField] protected TimeStamp _dateAdded;
        [UnityEngine.SerializeField] protected int _fileSize;
        [UnityEngine.SerializeField] protected string _md5;
        [UnityEngine.SerializeField] protected string _fileName;
        [UnityEngine.SerializeField] protected string _version;
        [UnityEngine.SerializeField] protected string _changelog;
        [UnityEngine.SerializeField] protected string _metadataBlob;

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

        // ---------[ INITIALIZATION ]---------
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
            };
            return newModfile;
        }
    }
}
