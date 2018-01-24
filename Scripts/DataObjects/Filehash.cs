using System;

namespace ModIO
{
    [Serializable]
    public class Filehash : IEquatable<Filehash>, IAPIObjectWrapper<API.FilehashObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.FilehashObject _data;

        public string md5 { get { return _data.md5; } }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.FilehashObject apiObject)
        {
            this._data = apiObject;
        }

        public API.FilehashObject GetAPIObject()
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
            return this.Equals(obj as Filehash);
        }

        public bool Equals(Filehash other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
