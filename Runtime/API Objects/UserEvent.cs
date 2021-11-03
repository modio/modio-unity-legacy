using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class UserEvent
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// Unique id of the event object.
        /// </summary>
        [JsonProperty("id")]
        public int id;

        /// <summary>
        /// Unique id of the parent mod.
        /// </summary>
        [JsonProperty("mod_id")]
        public int modId;

        /// <summary>
        /// Unique id of the user who performed the action.
        /// </summary>
        [JsonProperty("user_id")]
        public int userId;

        /// <summary>
        /// Unix timestamp of date the event occurred.
        /// </summary>
        [JsonProperty("date_added")]
        public int dateAdded;

        /// <summary>
        /// Type of event was 'USER_TEAM_JOIN', 'USER_TEAM_LEAVE', 'USER_SUBSCRIBE',
        /// 'USER_UNSUBSCRIBE'.
        /// </summary>
        [JsonProperty("user_event_type")]
        public UserEventType eventType;

        // ---------[ API DESERIALIZATION ]---------
        private const string APIOBJECT_VALUESTRING_TEAMJOINED = "USER_TEAM_JOIN";
        private const string APIOBJECT_VALUESTRING_TEAMLEFT = "USER_TEAM_LEAVE";
        private const string APIOBJECT_VALUESTRING_MODSUBSCRIBED = "USER_SUBSCRIBE";
        private const string APIOBJECT_VALUESTRING_MODUNSUBSCRIBED = "USER_UNSUBSCRIBE";

        /// <summary>
        /// An optional event_type field, which is only deserialized from API responses
        /// </summary>
        [JsonProperty("event_type")]
        public string _eventTypeString;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if(string.IsNullOrEmpty(this._eventTypeString))
            {
                return;
            }

            switch(this._eventTypeString.ToUpper())
            {
                case APIOBJECT_VALUESTRING_TEAMJOINED:
                {
                    this.eventType = UserEventType.TeamJoined;
                }
                break;
                case APIOBJECT_VALUESTRING_TEAMLEFT:
                {
                    this.eventType = UserEventType.TeamLeft;
                }
                break;
                case APIOBJECT_VALUESTRING_MODSUBSCRIBED:
                {
                    this.eventType = UserEventType.ModSubscribed;
                }
                break;
                case APIOBJECT_VALUESTRING_MODUNSUBSCRIBED:
                {
                    this.eventType = UserEventType.ModUnsubscribed;
                }
                break;
                default:
                {
                    this.eventType = UserEventType._UNKNOWN;
                }
                break;
            }

            this._eventTypeString = null;
        }
    }
}
