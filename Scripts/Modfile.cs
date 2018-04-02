using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    [System.Serializable]
    public class Modfile
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _id;
        [SerializeField] private int _modId;
        [SerializeField] private TimeStamp _dateAdded;
        [SerializeField] private int _fileSize;
        [SerializeField] private string _md5;
        [SerializeField] private string _fileName;
        [SerializeField] private string _version;
        [SerializeField] private string _changelog;
        [SerializeField] private string _metadataBlob;

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
        public void ApplyModfileObjectValues(API.ModfileObject apiObject)
        {
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

        public static Modfile CreateFromModfileObject(API.ModfileObject apiObject)
        {
            var retVal = new Modfile();
            retVal.ApplyModfileObjectValues(apiObject);
            return retVal;
        }
    }
}
