using System;

namespace ModIO
{
    [Serializable]
    public class LogoURLInfo : IEquatable<LogoURLInfo>, IAPIObjectWrapper<API.LogoObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.LogoObject _data;

        public string filename          { get { return _data.filename; } }
        public string original          { get { return _data.original; } }
        public string thumb320x180      { get { return _data.thumb_320x180; } }
        public string thumb640x360      { get { return _data.thumb_640x360; } }
        public string thumb1280x720     { get { return _data.thumb_1280x720; } }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.LogoObject apiObject)
        {
            this._data = apiObject;
        }

        public API.LogoObject GetAPIObject()
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
            return this.Equals(obj as LogoURLInfo);
        }

        public bool Equals(LogoURLInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }

        public ModImageURLCollection AsModImageURLCollection()
        {
            var muc = new ModImageURLCollection();
            muc.filename = this.filename;
            muc.original = this.original;
            muc.thumb320x180 = this.thumb320x180;
            muc.thumb640x360 = this.thumb640x360;
            muc.thumb1280x720 = this.thumb1280x720;
            return muc;
        }
    }
}
