using System;

namespace ModIO
{
    [Serializable]
    public class ModDependency : IEquatable<ModDependency>
    {
        // - Constructors - 
        public static ModDependency GenerateFromAPIObject(API.ModDependencyObject apiObject)
        {
            ModDependency newModDependency = new ModDependency();
            newModDependency._data = apiObject;

            newModDependency.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);

            return newModDependency;
        }

        public static ModDependency[] GenerateFromAPIObjectArray(API.ModDependencyObject[] apiObjectArray)
        {
            ModDependency[] objectArray = new ModDependency[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = ModDependency.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModDependencyObject _data;

        public int modId            { get { return _data.mod_id; } }
        public TimeStamp dateAdded  { get; private set; }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ModDependency);
        }

        public bool Equals(ModDependency other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
