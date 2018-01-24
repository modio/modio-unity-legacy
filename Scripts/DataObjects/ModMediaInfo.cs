using System;

namespace ModIO
{
    [Serializable]
    public class ModMediaInfo : IEquatable<ModMediaInfo>
    {
        // - Constructors - 
        public static ModMediaInfo GenerateFromAPIObject(API.ModMediaObject apiObject)
        {
            ModMediaInfo newModMedia = new ModMediaInfo();
            newModMedia._data = apiObject;

            // - Load Images -
            newModMedia.images = new ImageInfo[apiObject.images.Length];
            for(int i = 0;
                i < apiObject.images.Length;
                ++i)
            {
                newModMedia.images[i] = ImageInfo.GenerateFromAPIObject(apiObject.images[i]);
            }

            return newModMedia;
        }

        public static ModMediaInfo[] GenerateFromAPIObjectArray(API.ModMediaObject[] apiObjectArray)
        {
            ModMediaInfo[] objectArray = new ModMediaInfo[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = ModMediaInfo.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
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
