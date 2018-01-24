using System;

namespace ModIO
{
    [Serializable]
    public class LogoURLInfo : IEquatable<LogoURLInfo>
    {
        // - Constructors - 
        public static LogoURLInfo GenerateFromAPIObject(API.LogoObject apiObject)
        {
            LogoURLInfo newLogo = new LogoURLInfo();
            newLogo._data = apiObject;
            return newLogo;
        }

        public static LogoURLInfo[] GenerateFromAPIObjectArray(API.LogoObject[] apiObjectArray)
        {
            LogoURLInfo[] objectArray = new LogoURLInfo[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = LogoURLInfo.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.LogoObject _data;

        public string filename          { get { return _data.filename; } }
        public string original          { get { return _data.original; } }
        public string thumb320x180      { get { return _data.thumb_320x180; } }
        public string thumb640x360      { get { return _data.thumb_640x360; } }
        public string thumb1280x720     { get { return _data.thumb_1280x720; } }

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
    }
}
