using System;
using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    // - Enums -
    public enum ModEventType
    {
        _UNKNOWN = -1,
        ModAvailable,
        ModUnavailable,
        ModEdited,
        ModfileChanged,
    }

    [Serializable]
    public class ModEvent
    {
        private const string APIOBJECT_TYPESTRING_MODAVAILABLE      = "MOD_AVAILABLE";
        private const string APIOBJECT_TYPESTRING_MODUNAVAILABLE    = "MOD_UNAVAILABLE";
        private const string APIOBJECT_TYPESTRING_MODEDITED         = "MOD_EDITED";
        private const string APIOBJECT_TYPESTRING_MODFILECHANGED    = "MODFILE_CHANGED";

        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _id;
        [SerializeField] private int _modId;
        [SerializeField] private int _userId;
        [SerializeField] private TimeStamp _dateAdded;
        [SerializeField] private ModEventType _eventType;

        // ---------[ FIELDS ]---------
        public int id                   { get { return this._id; } }
        public int modId                { get { return this._modId; } }
        public int userId               { get { return this._userId; } }
        public TimeStamp dateAdded      { get { return this._dateAdded; } }
        public ModEventType eventType   { get { return this._eventType; } }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyAPIObjectValues(API.EventObject apiObject)
        {
            this._id = apiObject.id;
            this._modId = apiObject.mod_id;
            this._userId = apiObject.user_id;
            this._dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this._eventType = ModEvent.ParseAPITypeStringAsEventType(apiObject.event_type);
        }

        public static ModEvent CreateFromAPIObject(API.EventObject apiObject)
        {
            var retVal = new ModEvent();
            retVal.ApplyAPIObjectValues(apiObject);
            return retVal;
        }

        public static ModEventType ParseAPITypeStringAsEventType(string apiObjectValue)
        {
            switch(apiObjectValue.ToUpper())
            {
                case "MOD_AVAILABLE":
                {
                    return ModEventType.ModAvailable;
                }
                case "MOD_UNAVAILABLE":
                {
                    return ModEventType.ModUnavailable;
                }
                case "MOD_EDITED":
                {
                    return ModEventType.ModEdited;
                }
                case "MODFILE_CHANGED":
                {
                    return ModEventType.ModfileChanged;
                }
                default:
                {
                    return ModEventType._UNKNOWN;
                }
            }
        }

        public static string EventTypeToAPIString(ModEventType eventType)
        {
            switch(eventType)
            {
                case ModEventType.ModAvailable:
                {
                    return "MOD_AVAILABLE";
                }
                case ModEventType.ModUnavailable:
                {
                    return "MOD_UNAVAILABLE";
                }
                case ModEventType.ModEdited:
                {
                    return "MOD_EDITED";
                }
                case ModEventType.ModfileChanged:
                {
                    return "MODFILE_CHANGED";
                }
                default:
                {
                    return string.Empty;
                }
            }
        }
    }
}
