using System;

namespace ModIO
{
    [Serializable]
    public class AvatarInfo : IEquatable<AvatarInfo>
    {
        // - Constructors - 
        public static AvatarInfo GenerateFromAPIObject(API.AvatarObject apiObject)
        {
            AvatarInfo newAvatar = new AvatarInfo();
            newAvatar._data = apiObject;
            return newAvatar;
        }

        public static AvatarInfo[] GenerateFromAPIObjectArray(API.AvatarObject[] apiObjectArray)
        {
            AvatarInfo[] objectArray = new AvatarInfo[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = AvatarInfo.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.AvatarObject _data;

        public string filename                  { get { return _data.filename; } }
        public string original_URL              { get { return _data.original; } }
        public string thumb50x50_URL            { get { return _data.thumb_50x50; } }
        public string thumb100x100_URL          { get { return _data.thumb_100x100; } }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as AvatarInfo);
        }

        public bool Equals(AvatarInfo other)
        {
            return(Object.ReferenceEquals(this, other)
                   || (this._data.Equals(other._data)));
        }
    }   
}
