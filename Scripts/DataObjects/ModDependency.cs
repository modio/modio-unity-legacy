using System;

namespace ModIO
{
    [Serializable]
    public class ModDependency : IEquatable<ModDependency>, IAPIObjectWrapper<API.ModDependencyObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModDependencyObject _data;

        public int modId            { get { return _data.mod_id; } }
        public TimeStamp dateAdded  { get; private set; }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.ModDependencyObject apiObject)
        {
            this._data = apiObject;
            
            this.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
        }

        public API.ModDependencyObject GetAPIObject()
        {
            return this._data;
        }

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
