using System;
using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    // - Enums -
    public enum UserEventType
    {
        _UNKNOWN = -1,
        TeamJoined,
        TeamLeft,
        ModSubscribed,
        ModUnsubscribed,
    }

    [Serializable]
    public class UserEvent
    {
        private const string APIOBJECT_TYPESTRING_TEAMJOINED        = "USER_TEAM_JOIN";
        private const string APIOBJECT_TYPESTRING_TEAMLEFT          = "USER_TEAM_LEAVE";
        private const string APIOBJECT_TYPESTRING_MODSUBSCRIBED     = "USER_SUBSCRIBE";
        private const string APIOBJECT_TYPESTRING_MODUNSUBSCRIBED   = "USER_UNSUBSCRIBE";

        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _id;
        [SerializeField] private int _modId;
        [SerializeField] private int _userId;
        [SerializeField] private TimeStamp _dateAdded;
        [SerializeField] private UserEventType _eventType;

        // ---------[ FIELDS ]---------
        public int id                   { get { return this._id; } }
        public int modId                { get { return this._modId; } }
        public int userId               { get { return this._userId; } }
        public TimeStamp dateAdded      { get { return this._dateAdded; } }
        public UserEventType eventType  { get { return this._eventType; } }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyEventObjectValues(API.EventObject apiObject)
        {
            this._id = apiObject.id;
            this._modId = apiObject.mod_id;
            this._userId = apiObject.user_id;
            this._dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this._eventType = UserEvent.ParseAPITypeStringAsEventType(apiObject.event_type);
        }

        public static UserEvent CreateFromEventObject(API.EventObject apiObject)
        {
            var retVal = new UserEvent();
            retVal.ApplyEventObjectValues(apiObject);
            return retVal;
        }

        public static UserEventType ParseAPITypeStringAsEventType(string apiObjectValue)
        {
            switch(apiObjectValue.ToUpper())
            {
                case APIOBJECT_TYPESTRING_TEAMJOINED:
                {
                    return UserEventType.TeamJoined;
                }
                case APIOBJECT_TYPESTRING_TEAMLEFT:
                {
                    return UserEventType.TeamLeft;
                }
                case APIOBJECT_TYPESTRING_MODSUBSCRIBED:
                {
                    return UserEventType.ModSubscribed;
                }
                case APIOBJECT_TYPESTRING_MODUNSUBSCRIBED:
                {
                    return UserEventType.ModUnsubscribed;
                }
                default:
                {
                    return UserEventType._UNKNOWN;
                }
            }
        }

        public static string EventTypeToAPIString(UserEventType eventType)
        {
            switch(eventType)
            {
                case UserEventType.TeamJoined:
                {
                    return APIOBJECT_TYPESTRING_TEAMJOINED;
                }
                case UserEventType.TeamLeft:
                {
                    return APIOBJECT_TYPESTRING_TEAMLEFT;
                }
                case UserEventType.ModSubscribed:
                {
                    return APIOBJECT_TYPESTRING_MODSUBSCRIBED;
                }
                case UserEventType.ModUnsubscribed:
                {
                    return APIOBJECT_TYPESTRING_MODUNSUBSCRIBED;
                }
                default:
                {
                    return string.Empty;
                }
            }
        }
    }
}
