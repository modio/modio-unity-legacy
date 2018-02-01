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
    public class EditableModfile : Modfile
    {
        public static EditableModfile FromModfile(Modfile modfile)
        {
            EditableModfile newEMI = new EditableModfile();

            newEMI.WrapAPIObject(modfile.GetAPIObject());

            return newEMI;
        }

        // - Put Request Values -
        private Dictionary<string, string> putValues = new Dictionary<string, string>();
        public StringValueField[] GetValueFields()
        {
            List<StringValueField> retVal = new List<StringValueField>();
            
            foreach(KeyValuePair<string, string> kvp in putValues)
            {
                retVal.Add(StringValueField.Create(kvp.Key, kvp.Value));
            }

            return retVal.ToArray();
        }

        // --- SETTERS ---
        public void SetVersion(string value)
        {
            _data.version = value;
            putValues["version"] = value;
        }
        public void SetChangelog(string value)
        {
            _data.changelog = value;
            putValues["changelog"] = value;
        }
        public void SetMetadataBlob(string value)
        {
            _data.metadata_blob = value;
            putValues["metadata_blob"] = value;
        }
        public void SetAsPrimaryRelease(bool value)
        {
            putValues["active"] = value.ToString();
        }
    }

    [Serializable]
    public class UnsubmittedModfile
    {
        // --- FIELDS ---
        [UnityEngine.SerializeField]
        private UnsubmittedModfileObject _data;
        
        // The binary file for the release. For compatibility you should ZIP the base folder of your mod, or if it is a collection of files which live in a pre-existing game folder, you should ZIP those files. Your file must meet the following conditions:
        public string binaryFilepath = "";

        // Version of the file release.
        public string version
        {
            get { return _data.version; }
            set { _data.version = value; }
        }
        // Changelog of this release.
        public string changelog
        {
            get { return _data.changelog; }
            set { _data.changelog = value; }
        }
        // Default value is true. Label this upload as the current release, this will change the modfile field on the parent mod to the id of this file after upload.
        public bool active
        {
            get { return _data.active; }
            set { _data.active = value; }
        }
        // MD5 of the submitted file. When supplied the MD5 will be compared against the uploaded files MD5. If they don't match a 422 Unprocessible Entity error will be returned.
        public string filehash
        {
            get { return _data.filehash; }
            set { _data.filehash = value; }
        }
        // Metadata stored by the game developer which may include properties such as what version of the game this file is compatible with. Metadata can also be stored as searchable key value pairs, and to the mod object.
        public string metadataBlob
        {
            get { return _data.metadata_blob; }
            set { _data.metadata_blob = value; }
        }


        // --- ACCESSORS ---
        public StringValueField[] GetValueFields()
        {
            List<StringValueField> retVal = new List<StringValueField>(5);

            retVal.Add(StringValueField.Create("version", _data.version));
            retVal.Add(StringValueField.Create("changelog", _data.changelog));
            retVal.Add(StringValueField.Create("active", (_data.active ? "1" : "0")));
            retVal.Add(StringValueField.Create("filehash", _data.filehash));
            retVal.Add(StringValueField.Create("metadata_blob", _data.metadata_blob));

            return retVal.ToArray();
        }
        
        public BinaryDataField[] GetDataFields()
        {
            List<BinaryDataField> retVal = new List<BinaryDataField>(1);

            if(System.IO.File.Exists(binaryFilepath))
            {
                BinaryDataField newData = new BinaryDataField();
                newData.key = "filedata";
                newData.contents = System.IO.File.ReadAllBytes(binaryFilepath);
                newData.fileName = System.IO.Path.GetFileName(binaryFilepath);

                retVal.Add(newData);
            }
            return retVal.ToArray();
        }
    }
}
