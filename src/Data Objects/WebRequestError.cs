using System.Collections.Generic;
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
        public IDictionary<string, string> fieldValidationMessages;

        public string method;
        public string url;
        public int timeStamp;
        public int responseCode;
        public string processingException;

        public Dictionary<string, string> responseHeaders;
        public string responseBody;

        // ---------[ INITIALIZATION ]---------
        public static WebRequestError GenerateFromWebRequest(UnityEngine.Networking.UnityWebRequest webRequest)
        {
            UnityEngine.Debug.Assert(webRequest != null);
            UnityEngine.Debug.Assert(webRequest.isNetworkError || webRequest.isHttpError);

            string responseBody = null;
            WebRequestError.APIWrapper errorWrapper = null;
            string processingException = null;

            try
            {
                responseBody = webRequest.downloadHandler.text;
            }
            catch(System.Exception e)
            {
                responseBody = null;
                processingException = e.Message;
            }

            if(responseBody != null)
            {
                try
                {
                    errorWrapper = JsonConvert.DeserializeObject<APIWrapper>(responseBody);
                }
                catch(System.Exception e)
                {
                    errorWrapper = null;
                    processingException = e.Message;
                }
            }

            WebRequestError error = null;
            if(errorWrapper != null)
            {
                // NOTE(@jackson): Can be null
                error = errorWrapper.error;
            }

            if(error == null)
            {
                error = new WebRequestError();
                error.message = webRequest.error;
            }

            if(processingException != null)
            {
                error.processingException = processingException;
            }

            error.responseBody = responseBody;
            error.responseCode = (int)webRequest.responseCode;
            error.responseHeaders = webRequest.GetResponseHeaders();

            error.method = webRequest.method.ToUpper();
            error.url = webRequest.url;
            error.timeStamp = ServerTimeStamp.Now;

            return error;
        }

        public static WebRequestError GenerateLocal(string errorMessage)
        {
            WebRequestError error = new WebRequestError()
            {
                message = errorMessage,
                fieldValidationMessages = null,
                method = "LOCAL",
                url = "null",
                timeStamp = ServerTimeStamp.Now,
                responseCode = 0,
                processingException = null,
                responseHeaders = null,
                responseBody = null,
            };

            return error;
        }

        // ---------[ HELPER FUNCTIONS ]---------
        public string ToUnityDebugString()
        {
            var debugString = new System.Text.StringBuilder();

            debugString.AppendLine(this.method + " REQUEST FAILED");
            debugString.AppendLine("URL: " + this.url);
            debugString.AppendLine("Received At: [" + this.timeStamp + "] "
                                   + ServerTimeStamp.ToLocalDateTime(this.timeStamp));
            debugString.AppendLine("Response Code: " + this.responseCode.ToString());
            debugString.AppendLine("Message: " + this.message);

            if(this.fieldValidationMessages != null
               && this.fieldValidationMessages.Count > 0)
            {
                debugString.AppendLine("Field Validation Messages:");
                foreach(var kvp in fieldValidationMessages)
                {
                    debugString.AppendLine("- [" + kvp.Key + "] " + kvp.Value);
                }
            }

            if(this.responseHeaders.Count > 0)
            {
                debugString.AppendLine("Response Headers:");
                foreach(var kvp in responseHeaders)
                {
                    debugString.AppendLine("- [" + kvp.Key + "] " + kvp.Value);
                }
            }

            if(this.processingException != null)
            {
                debugString.AppendLine("Processing Exception: " + processingException);
            }

            debugString.AppendLine("Response Body:" + responseBody);

            return debugString.ToString();
        }

        public static void LogAsWarning(WebRequestError error)
        {
            Debug.LogWarning(error.ToUnityDebugString());
        }
    }
}
