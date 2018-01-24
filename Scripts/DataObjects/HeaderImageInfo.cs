using System;

namespace ModIO
{
    [Serializable]
    public class HeaderImageInfo : IEquatable<HeaderImageInfo>
    {
        // - Constructors - 
        public static HeaderImageInfo GenerateFromAPIObject(API.HeaderImageObject apiObject)
        {
            HeaderImageInfo newHeaderImage = new HeaderImageInfo();
            newHeaderImage._data = apiObject;
            return newHeaderImage;
        }

        public static HeaderImageInfo[] GenerateFromAPIObjectArray(API.HeaderImageObject[] apiObjectArray)
        {
            HeaderImageInfo[] objectArray = new HeaderImageInfo[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = HeaderImageInfo.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.HeaderImageObject _data;

        public string filename      { get { return _data.filename; } }
        public string original_URL  { get { return _data.original; } }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as HeaderImageInfo);
        }

        public bool Equals(HeaderImageInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
