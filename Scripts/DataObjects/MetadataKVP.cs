using System;

namespace ModIO
{
    [Serializable]
    public class MetadataKVP : IEquatable<MetadataKVP>, IAPIObjectWrapper<API.MetadataKVPObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.MetadataKVPObject _data;

        public string key   { get { return _data.metakey; } }
        public string value { get { return _data.metavalue; } }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.MetadataKVPObject apiObject)
        {
            this._data = apiObject;
        }

        public API.MetadataKVPObject GetAPIObject()
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
            return this.Equals(obj as MetadataKVP);
        }

        public bool Equals(MetadataKVP other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }

    [Serializable]
    public class UnsubmittedMetadataKVP
    {
        // --- FIELDS ---
        public string key;
        public string value;
    }
}
