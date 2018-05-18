using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class WebRequestError
    {
        // ---------[ NESTED CLASSES ]---------
        [System.Serializable]
        private class APIWrapper
        {
            public WebRequestError error = null;
        }

        // ---------[ FIELDS ]---------
        /// <summary>
        /// The server response to your request.
        /// Responses will vary depending on the endpoint,
        /// but the object structure will persist.
        /// </summary>
        [JsonProperty("message")]
        public string message;

        // TODO(@jackson): Add Hyperlink
        /// <summary>
        /// Optional Validation errors object.
        /// This field is only supplied if the response is
        /// a validation error 422 Unprocessible Entity.
        /// </summary>
        /// <remarks>
        /// See errors documentation for more information.
        /// </remarks>
        [JsonProperty("errors")]
        public System.Collections.Generic.IDictionary<string, string> fieldValidationMessages;

        public string method;
        public string url;
        public int timeStamp;
        public int responseCode;

        // ---------[ INITIALIZATION ]---------
        public static WebRequestError GenerateFromWebRequest(UnityEngine.Networking.UnityWebRequest webRequest)
        {
            UnityEngine.Debug.Assert(webRequest.isNetworkError || webRequest.isHttpError);

            WebRequestError.APIWrapper errorWrapper;

            Utility.TryParseJsonString(webRequest.downloadHandler.text, out errorWrapper);

            WebRequestError error;

            if(errorWrapper != null
               && errorWrapper.error != null)
            {
                error = errorWrapper.error;
            }
            else
            {
                error = new WebRequestError();
                error.message = webRequest.error;
            }

            error.responseCode = (int)webRequest.responseCode;
            error.method = webRequest.method.ToUpper();
            error.url = webRequest.url;
            error.timeStamp = ServerTimeStamp.Now;

            return error;
        }

        // ---------[ HELPER FUNCTIONS ]---------
        public string ToUnityDebugString()
        {
            string debugString = (this.method + " REQUEST FAILED"
                                  + "\nResponse received at: [" + this.timeStamp + "] "
                                  + ServerTimeStamp.ToLocalDateTime(this.timeStamp)
                                  + "\nURL: " + this.url
                                  + "\nCode: " + this.responseCode
                                  + "\nMessage: " + this.message
                                  + "\n");

            if(this.fieldValidationMessages != null
               && this.fieldValidationMessages.Count > 0)
            {
                debugString += "Field Validation Messages:\n";
                foreach(var kvp in fieldValidationMessages)
                {
                    debugString += " [" + kvp.Key + "] " + kvp.Value + "\n";
                }
            }

            return debugString;
        }
    }
}
