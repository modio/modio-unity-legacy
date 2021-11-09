using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModEvent
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
        /// Type of event was 'MODFILE_CHANGED', 'MOD_AVAILABLE', 'MOD_UNAVAILABLE', 'MOD_EDITED',
        /// 'MOD_DELETED'.
        /// </summary>
        [JsonProperty("mod_event_type")]
        public ModEventType eventType;


        // ---------[ API DESERIALIZATION ]---------
        private const string APIOBJECT_VALUESTRING_MODAVAILABLE = "MOD_AVAILABLE";
        private const string APIOBJECT_VALUESTRING_MODUNAVAILABLE = "MOD_UNAVAILABLE";
        private const string APIOBJECT_VALUESTRING_MODEDITED = "MOD_EDITED";
        private const string APIOBJECT_VALUESTRING_MODFILECHANGED = "MODFILE_CHANGED";

        /// <summary>
        /// An optional event_type field, which is only deserialized from API responses
        /// </summary>
        [JsonProperty("event_type")]
        private string _eventTypeString;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if(string.IsNullOrEmpty(this._eventTypeString))
            {
                return;
            }

            switch(this._eventTypeString.ToUpper())
            {
                case APIOBJECT_VALUESTRING_MODAVAILABLE:
                {
                    this.eventType = ModEventType.ModAvailable;
                }
                break;
                case APIOBJECT_VALUESTRING_MODUNAVAILABLE:
                {
                    this.eventType = ModEventType.ModUnavailable;
                }
                break;
                case APIOBJECT_VALUESTRING_MODEDITED:
                {
                    this.eventType = ModEventType.ModEdited;
                }
                break;
                case APIOBJECT_VALUESTRING_MODFILECHANGED:
                {
                    this.eventType = ModEventType.ModfileChanged;
                }
                break;
                default:
                {
                    this.eventType = ModEventType._UNKNOWN;
                }
                break;
            }

            this._eventTypeString = null;
        }
    }
}
