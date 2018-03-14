using System;

namespace ModIO
{
    [Serializable]
    public class ModEvent : IEquatable<ModEvent>, IAPIObjectWrapper<API.EventObject>, UnityEngine.ISerializationCallbackReceiver
    {
        // - Enums -
        public enum EventType
        {
            ModfileChanged,
            ModAvailable,
            ModUnavailable,
            ModEdited,
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.EventObject _data;

        public int id               { get { return _data.id; } }
        public int modId            { get { return _data.mod_id; } }
        public int userId           { get { return _data.user_id; } }
        public TimeStamp dateAdded  { get; private set; }
        public EventType eventType  { get; private set; }

        // - Event Type Parsing -
        public static string GetNameForType(EventType eventType)
        {
            switch(eventType)
            {
                case EventType.ModfileChanged:
                {
                    return "MODFILE_CHANGED";
                }
                case EventType.ModAvailable:
                {
                    return "MOD_AVAILABLE";
                }
                case EventType.ModUnavailable:
                {
                    return "MOD_UNAVAILABLE";
                }
                case EventType.ModEdited:
                {
                    return "MOD_EDITED";
                }
            }
            return "";
        }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.EventObject apiObject)
        {
            this._data = apiObject;

            this.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);

            // - Parse EventType -
            switch(apiObject.event_type.ToUpper())
            {
                case "MODFILE_CHANGED":
                {
                    this.eventType = EventType.ModfileChanged;
                }
                break;
                case "MOD_AVAILABLE":
                {
                    this.eventType = EventType.ModAvailable;
                }
                break;
                case "MOD_UNAVAILABLE":
                {
                    this.eventType = EventType.ModUnavailable;
                }
                break;
                case "MOD_EDITED":
                {
                    this.eventType = EventType.ModEdited;
                }
                break;
            }
        }

        public API.EventObject GetAPIObject()
        {
            return this._data;
        }

        // - ISerializationCallbackReceiver -
        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            this.WrapAPIObject(this._data);
        }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ModEvent);
        }

        public bool Equals(ModEvent other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
