using System;

namespace ModIO
{
    [Serializable]
    public class ModTag : IEquatable<ModTag>, IAPIObjectWrapper<API.ModTagObject>
    {
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.ModTagObject apiObject)
        {
            this._data = apiObject;

            this.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
        }

        public API.ModTagObject GetAPIObject()
        {
            return this._data;
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
