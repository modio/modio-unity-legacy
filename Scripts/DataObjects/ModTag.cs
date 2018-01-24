using System;

namespace ModIO
{
    [Serializable]
    public class ModTag : IEquatable<ModTag>
    {
        // - Constructors - 
        public static ModTag GenerateFromAPIObject(API.ModTagObject apiObject)
        {
            ModTag newModTag = new ModTag();
            newModTag._data = apiObject;

            newModTag.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);

            return newModTag;
        }

        public static ModTag[] GenerateFromAPIObjectArray(API.ModTagObject[] apiObjectArray)
        {
            ModTag[] objectArray = new ModTag[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = ModTag.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModTagObject _data;

        public string name          { get { return _data.name; } }
        public TimeStamp dateAdded  { get; private set; }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ModTag);
        }

        public bool Equals(ModTag other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
