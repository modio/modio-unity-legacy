using System;

namespace ModIO
{
    [Serializable]
    public class ModfileDownload : IEquatable<ModfileDownload>, IAPIObjectWrapper<API.ModfileDownloadObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModfileDownloadObject _data;

        public string binaryURL         { get { return _data.binary_url; } }
        public TimeStamp dateExpires    { get; private set; }

        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.ModfileDownloadObject apiObject)
        {
            this._data = apiObject;
            
            this.dateExpires = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_expires);
        }

        public API.ModfileDownloadObject GetAPIObject()
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
            return this.Equals(obj as ModfileDownload);
        }

        public bool Equals(ModfileDownload other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
