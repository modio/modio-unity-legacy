using System;

namespace ModIO
{
    [Serializable]
    public class ModMediaInfo : IEquatable<ModMediaInfo>, IAPIObjectWrapper<API.ModMediaObject>
    {
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.ModMediaObject apiObject)
        {
            this._data = apiObject;

            // - Load Images -
            this.images = new ImageInfo[apiObject.images.Length];
            for(int i = 0;
                i < apiObject.images.Length;
                ++i)
            {
                this.images[i] = new ImageInfo();
                this.images[i].WrapAPIObject(apiObject.images[i]);
            }
        }
        public API.ModMediaObject GetAPIObject()
        {
            return this._data;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModMediaObject _data;

        public string[] youtubeURLs     { get { return _data.youtube; } }
        public string[] sketchfabURLS   { get { return _data.sketchfab; } }
        public ImageInfo[] images       { get; private set; }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ModMediaInfo);
        }

        public bool Equals(ModMediaInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
