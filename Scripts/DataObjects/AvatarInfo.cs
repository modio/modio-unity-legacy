using System;

namespace ModIO
{
    [Serializable]
    public class AvatarURLInfo : IEquatable<AvatarURLInfo>, IAPIObjectWrapper<API.AvatarObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.AvatarObject _data;

        public string filename      { get { return _data.filename; } }
        public string original      { get { return _data.original; } }
        public string thumb50x50    { get { return _data.thumb_50x50; } }
        public string thumb100x100  { get { return _data.thumb_100x100; } }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.AvatarObject apiObject)
        {
            this._data = apiObject;
        }

        public API.AvatarObject GetAPIObject()
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
            return this.Equals(obj as AvatarURLInfo);
        }

        public bool Equals(AvatarURLInfo other)
        {
            return(Object.ReferenceEquals(this, other)
                   || (this._data.Equals(other._data)));
        }
    }   
}
