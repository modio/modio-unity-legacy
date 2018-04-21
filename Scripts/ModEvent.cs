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
        public void ApplyEventObjectValues(API.EventObject apiObject)
        {
            this._id = apiObject.id;
            this._modId = apiObject.mod_id;
            this._userId = apiObject.user_id;
            this._dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this._eventType = ModEvent.ParseAPITypeStringAsEventType(apiObject.event_type);
        }

        public static ModEvent CreateFromEventObject(API.EventObject apiObject)
        {
            var retVal = new ModEvent();
            retVal.ApplyEventObjectValues(apiObject);
            return retVal;
        }

        public static ModEventType ParseAPITypeStringAsEventType(string apiObjectValue)
        {
            switch(apiObjectValue.ToUpper())
            {
                case APIOBJECT_TYPESTRING_MODAVAILABLE:
                {
                    return ModEventType.ModAvailable;
                }
                case APIOBJECT_TYPESTRING_MODUNAVAILABLE:
                {
                    return ModEventType.ModUnavailable;
                }
                case APIOBJECT_TYPESTRING_MODEDITED:
                {
                    return ModEventType.ModEdited;
                }
                case APIOBJECT_TYPESTRING_MODFILECHANGED:
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
                    return APIOBJECT_TYPESTRING_MODAVAILABLE;
                }
                case ModEventType.ModUnavailable:
                {
                    return APIOBJECT_TYPESTRING_MODUNAVAILABLE;
                }
                case ModEventType.ModEdited:
                {
                    return APIOBJECT_TYPESTRING_MODEDITED;
                }
                case ModEventType.ModfileChanged:
                {
                    return APIOBJECT_TYPESTRING_MODFILECHANGED;
                }
                default:
                {
                    return string.Empty;
                }
            }
        }
    }
}
