using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

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

            WebRequestError error = null;

            try
            {
                WebRequestError.APIWrapper errorWrapper = JsonConvert.DeserializeObject<APIWrapper>(webRequest.downloadHandler.text);

                if(errorWrapper != null
                   && errorWrapper.error != null)
                {
                    error = errorWrapper.error;
                }
            }
            #pragma warning disable 0168
            catch(System.Exception e)
            {
                Debug.LogError("[mod.io] The error response was unable to be parsed:\n"
                               + webRequest.downloadHandler.text);
            }
            #pragma warning restore 0168

            if(error == null)
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

        public static WebRequestError GenerateLocal(string errorMessage)
        {
            WebRequestError error = new WebRequestError()
            {
                method = "LOCAL",
                url = "null",
                timeStamp = ServerTimeStamp.Now,
                responseCode = 0,
                message = errorMessage,
            };

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

        public static void LogAsWarning(WebRequestError error)
        {
            Debug.LogWarning(error.ToUnityDebugString());
        }
    }
}
