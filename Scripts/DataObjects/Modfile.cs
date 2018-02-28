using System;
using System.Collections.Generic;
using ModIO.API;

namespace ModIO
{
    [Serializable]
    public class Modfile : IEquatable<Modfile>, IAPIObjectWrapper<ModfileObject>, UnityEngine.ISerializationCallbackReceiver
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
        protected ModfileObject _data;

        public int id                           { get { return _data.id; } }
        public int modId                        { get { return _data.mod_id; } }
        public TimeStamp dateAdded              { get; protected set; }
        public TimeStamp dateScanned            { get; protected set; }
        public VirusScanStatus virusScanStatus  { get { return (VirusScanStatus)_data.virus_status; } }
        public VirusScanResult virusScanResult  { get { return (VirusScanResult)_data.virus_positive; } }
        public string virustotalHash            { get { return _data.virustotal_hash; } }
        public int filesize                     { get { return _data.filesize; } }
        public Filehash filehash                { get; protected set; }
        public string filename                  { get { return _data.filename; } }
        public string version                   { get { return _data.version; } }
        public string changelog                 { get { return _data.changelog; } }
        public string metadataBlob              { get { return _data.metadata_blob; } }
        public ModfileDownload download         { get; protected set; }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(ModfileObject apiObject)
        {
            this._data = apiObject;

            this.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this.dateScanned = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_scanned);
            this.filehash = new Filehash();
            this.filehash.WrapAPIObject(apiObject.filehash);
            this.download = new ModfileDownload();
            this.download.WrapAPIObject(apiObject.download);
        }

        public ModfileObject GetAPIObject()
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
            return this.Equals(obj as Modfile);
        }

        public bool Equals(Modfile other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }

    [Serializable]
    public class ModfileProfile
    {
        // --- FIELDS ---
        public int modId = 0;
        public int modfileId = 0;

        // Version of the file release.
        public string version;
        // Changelog of this release.
        public string changelog;
        // Metadata stored by the game developer which may include properties such as what version of the game this file is compatible with. Metadata can also be stored as searchable key value pairs, and to the mod object.
        public string metadataBlob;
    }
}
