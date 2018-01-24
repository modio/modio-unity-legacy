using System;

namespace ModIO
{
    [Serializable]
    public class Modfile : IEquatable<Modfile>, IAPIObjectWrapper<API.ModfileObject>
    {
        // - Enums -
        public enum VirusScanStatus
        {
            NotScanned = 0,
            ScanComplete = 1,
            InProgress = 2,
            TooLargeToScan = 3,
            FileNotFound = 4,
            ErrorScanning = 5,
        }
        public enum VirusScanResult
        {
            NoThreatsDetected = 0,
            FlaggedAsMalicious = 1,
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModfileObject _data;

        public int id                           { get { return _data.id; } }
        public int modId                        { get { return _data.mod_id; } }
        public TimeStamp dateAdded              { get; private set; }
        public TimeStamp dateScanned            { get; private set; }
        public VirusScanStatus virusScanStatus  { get { return (VirusScanStatus)_data.virus_status; } }
        public VirusScanResult virusScanResult  { get { return (VirusScanResult)_data.virus_positive; } }
        public string virustotalHash            { get { return _data.virustotal_hash; } }
        public int filesize                     { get { return _data.filesize; } }
        public Filehash filehash                { get; private set; }
        public string filename                  { get { return _data.filename; } }
        public string version                   { get { return _data.version; } }
        public string changelog                 { get { return _data.changelog; } }
        public string metadataBlob              { get { return _data.metadata_blob; } }
        public ModfileDownload download         { get; private set; }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.ModfileObject apiObject)
        {
            this._data = apiObject;

            this.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this.dateScanned = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_scanned);
            this.filehash = new Filehash();
            this.filehash.WrapAPIObject(apiObject.filehash);
            this.download = new ModfileDownload();
            this.download.WrapAPIObject(apiObject.download);
        }

        public API.ModfileObject GetAPIObject()
        {
            return this._data;
        }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Modfile);
        }

        public bool Equals(Modfile other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
