using System.Collections.Generic;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        /// Can multiple tags be selected via 'checkboxes' or should only a single tag
        /// be selected via a 'dropdown'.
        /// </summary>
        [JsonProperty("multiple_tags")]
        public bool isMultiTagCategory;
        
        /// <summary>
        /// Groups of tags flagged as 'admin only' should only be used for filtering, and should not be displayed to users.
        /// </summary>
        [JsonProperty("hidden")]
        public bool isHidden;
        
        /// <summary>
        /// Array of tags in this group.
        /// </summary>
        [JsonProperty("tags")]
        public string[] tags;


        // ---------[ API DESERIALIZATION ]---------
        private const string APIOBJECT_TYPESTRING_ISMULTIVALUE_ENABLED = "CHECKBOXES";
        private const string APIOBJECT_TYPESTRING_ISMULTIVALUE_DISABLED = "DROPDOWN";

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            JToken token;
            if(_additionalData.TryGetValue("type", out token))
            {
                this.isMultiTagCategory = APIOBJECT_TYPESTRING_ISMULTIVALUE_ENABLED.Equals((string)token);
            }
        }
    }
}