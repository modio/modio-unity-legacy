using System;

namespace ModIO
{
    [Serializable]
    public class HeaderImageURLInfo : IEquatable<HeaderImageURLInfo>, IAPIObjectWrapper<API.HeaderImageObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.HeaderImageObject _data;

        public string filename  { get { return _data.filename; } }
        public string original  { get { return _data.original; } }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.HeaderImageObject apiObject)
        {
            this._data = apiObject;
        }

        public API.HeaderImageObject GetAPIObject()
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
            return this.Equals(obj as HeaderImageURLInfo);
        }

        public bool Equals(HeaderImageURLInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
