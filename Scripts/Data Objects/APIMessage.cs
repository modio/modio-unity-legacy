using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class APIMessage
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// <a href="https://docs.mod.io/#response-codes">HTTP status code</a> of response.
        /// </summary>
        [JsonProperty("code")]
        public int code;

        /// <summary>
        /// The server response to your request. Responses will vary depending on the
        /// endpoint, but the object structure will persist.
        /// </summary>
        [JsonProperty("message")]
        public string message;
    }
}
