using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModTagCategory
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// Name of the tag group.
        /// </summary>
        [JsonProperty("name")]
        public string name;

        /// <summary>
        /// Can multiple tags be selected (ie. checkboxes) or should only a single tag be selectable
        /// (ie. dropdown).
        /// </summary>
        [JsonProperty("multiple_tags")]
        public bool isMultiTagCategory;

        /// <summary>
        /// Groups of tags flagged as 'admin only' should only be used for filtering, and should not
        /// be displayed to users.
        /// </summary>
        [JsonProperty("hidden")]
        public bool isHidden;

        /// <summary>
        /// Array of tags in this group.
        /// </summary>
        [JsonProperty("tags")]
        public string[] tags;

        // ---------[ API DESERIALIZATION ]---------
        public const string APIOBJECT_VALUESTRING_ISSINGLETAG = "DROPDOWN";
        public const string APIOBJECT_VALUESTRING_ISMULTITAG = "CHECKBOXES";

        /// <summary>
        /// An optional type field, which is only deserialized from API responses
        /// </summary>
        [JsonProperty("type")]
        private string _typeString;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if(string.IsNullOrEmpty(this._typeString))
            {
                return;
            }

            this.isMultiTagCategory =
                APIOBJECT_VALUESTRING_ISMULTITAG.Equals(this._typeString.ToUpper());
            this._typeString = null;
        }
    }
}
