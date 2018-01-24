using System;

namespace ModIO
{
    [Serializable]
    public class ModEvent : IEquatable<ModEvent>
    {
        // - Enums -
        public enum EventType
        {
            ModfileChanged,
            ModAvailable,
            ModUnavailable,
            ModEdited,
        }

        // - Constructors - 
        public static ModEvent GenerateFromAPIObject(API.ModEventObject apiObject)
        {
            ModEvent newModEvent = new ModEvent();
            newModEvent._data = apiObject;

            newModEvent.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);

            // - Parse EventType -
            switch(apiObject.event_type.ToUpper())
            {
                case "MODFILE_CHANGED":
                {
                    newModEvent.eventType = EventType.ModfileChanged;
                }
                break;
                case "MOD_AVAILABLE":
                {
                    newModEvent.eventType = EventType.ModAvailable;
                }
                break;
                case "MOD_UNAVAILABLE":
                {
                    newModEvent.eventType = EventType.ModUnavailable;
                }
                break;
                case "MOD_EDITED":
                {
                    newModEvent.eventType = EventType.ModEdited;
                }
                break;
            }

            return newModEvent;
        }

        public static ModEvent[] GenerateFromAPIObjectArray(API.ModEventObject[] apiObjectArray)
        {
            ModEvent[] objectArray = new ModEvent[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = ModEvent.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModEventObject _data;

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
