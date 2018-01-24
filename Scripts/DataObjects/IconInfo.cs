using System;

namespace ModIO
{
    [Serializable]
    public class IconURLInfo : IEquatable<IconURLInfo>, IAPIObjectWrapper<API.IconObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.IconObject _data;

        public string filename      { get { return _data.filename; } }
        public string original      { get { return _data.original; } }
        public string thumb64x64    { get { return _data.thumb_64x64; } }
        public string thumb128x128  { get { return _data.thumb_128x128; } }
        public string thumb256x256  { get { return _data.thumb_256x256; } }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.IconObject apiObject)
        {
            this._data = apiObject;
        }

        public API.IconObject GetAPIObject()
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
            return this.Equals(obj as IconURLInfo);
        }

        public bool Equals(IconURLInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
