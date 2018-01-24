using System;

namespace ModIO
{
    [Serializable]
    public class ImageInfo : IEquatable<ImageInfo>
    {
        // - Constructors - 
        public static ImageInfo GenerateFromAPIObject(API.ImageObject apiObject)
        {
            ImageInfo newImage = new ImageInfo();
            newImage._data = apiObject;
            return newImage;
        }

        public static ImageInfo[] GenerateFromAPIObjectArray(API.ImageObject[] apiObjectArray)
        {
            ImageInfo[] objectArray = new ImageInfo[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = ImageInfo.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.ImageObject _data;

        public string filename          { get { return _data.filename; } }
        public string original_URL      { get { return _data.original; } }
        public string thumb320x180_URL  { get { return _data.thumb_320x180; } }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ImageInfo);
        }

        public bool Equals(ImageInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
