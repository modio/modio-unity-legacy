using System;

namespace ModIO
{
    [Serializable]
    public class IconInfo : IEquatable<IconInfo>
    {
        // - Constructors - 
        public static IconInfo GenerateFromAPIObject(API.IconObject apiObject)
        {
            IconInfo newIcon = new IconInfo();
            newIcon._data = apiObject;
            return newIcon;
        }

        public static IconInfo[] GenerateFromAPIObjectArray(API.IconObject[] apiObjectArray)
        {
            IconInfo[] objectArray = new IconInfo[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = IconInfo.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.IconObject _data;

        public string filename          { get { return _data.filename; } }
        public string original_URL      { get { return _data.original; } }
        public string thumb64x64_URL    { get { return _data.thumb_64x64; } }
        public string thumb128x128_URL  { get { return _data.thumb_128x128; } }
        public string thumb256x256_URL  { get { return _data.thumb_256x256; } }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as IconInfo);
        }

        public bool Equals(IconInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
