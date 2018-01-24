using System;

namespace ModIO
{
    [Serializable]
    public class ModfileDownload : IEquatable<ModfileDownload>
    {
        // - Constructors - 
        public static ModfileDownload GenerateFromAPIObject(API.ModfileDownloadObject apiObject)
        {
            ModfileDownload newModfileDownload = new ModfileDownload();
            newModfileDownload._data = apiObject;

            newModfileDownload.dateExpires = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_expires);

            return newModfileDownload;
        }

        public static ModfileDownload[] GenerateFromAPIObjectArray(API.ModfileDownloadObject[] apiObjectArray)
        {
            ModfileDownload[] objectArray = new ModfileDownload[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = ModfileDownload.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModfileDownloadObject _data;

        public string binaryURL         { get { return _data.binary_url; } }
        public TimeStamp dateExpires    { get; private set; }

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
