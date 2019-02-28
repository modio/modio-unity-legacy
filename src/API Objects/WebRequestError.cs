using System.Collections.Generic;
using Newtonsoft.Json;

using Debug = UnityEngine.Debug;
using DownloadHandlerFile = UnityEngine.Networking.DownloadHandlerFile;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;

namespace ModIO
{
    public class WebRequestError
    {
        // ---------[ NESTED CLASSES ]---------
        [System.Serializable]
        private class APIWrapper
        {
            [System.Serializable]
            public class APIError
            {
                public string message;
                public Dictionary<string, string> errors;
            }

            public APIError error = null;
        }

        // ---------[ FIELDS ]---------
        /// <summary>UnityWebRequest that generated the data for the error.</summary>
        public UnityWebRequest webRequest;

        /// <summary>The message returned by the API explaining the error.</summary>
        public string apiMessage;

        /// <summary>Errors pertaining to specific POST data fields.</summary>
        public IDictionary<string, string> fieldValidationMessages;

        /// <summary>The ServerTimeStamp at which the request was received.</summary>
        public int timeStamp;






        // --- OBSOLETE FIELDS ---
        [System.Obsolete("Use webRequest.responseCode instead")]
        public int responseCode
        {
            get { return (webRequest != null ? (int)webRequest.responseCode : -1); }
        }
        [System.Obsolete("Use webRequest.method instead")]
        public string method
        {
            get { return (webRequest != null ? webRequest.method : "LOCAL"); }
        }
        [System.Obsolete("Use webRequest.url instead")]
        public string url
        {
            get { return (webRequest != null ? webRequest.url : string.Empty); }
        }
        [System.Obsolete("Use webRequest.GetResponseHeaders() instead")]
        public Dictionary<string, string> responseHeaders
        {
            get { return (webRequest != null ? webRequest.GetResponseHeaders() : null); }
        }

        [System.Obsolete("Use webRequest.downloadHandler.text instead")]
        public string responseBody
        {
            get
            {
                if(webRequest != null
                   && webRequest.downloadHandler != null
                   && !(webRequest.downloadHandler is DownloadHandlerFile))
                {
                    return webRequest.downloadHandler.text;
                }
                return string.Empty;
            }
        }

        /// <summary>[Obsolete] The message returned by the API explaining the error.</summary>
        [System.Obsolete("Use WebRequestError.apiMessage instead")]
        public string message
        {
            get { return this.apiMessage; }
            set { this.apiMessage = value; }
        }


        // ---------[ INITIALIZATION ]---------
        public static WebRequestError GenerateFromWebRequest(UnityWebRequest webRequest)
        {
            UnityEngine.Debug.Assert(webRequest != null);
            UnityEngine.Debug.Assert(webRequest.isNetworkError || webRequest.isHttpError);

            WebRequestError error = new WebRequestError();
            error.webRequest = webRequest;

            error.timeStamp = ServerTimeStamp.Now;

            error.ApplyAPIErrorValues();

            return error;
        }

        public static WebRequestError GenerateLocal(string errorMessage)
        {
            WebRequestError error = new WebRequestError()
            {
                webRequest = null,
                apiMessage = errorMessage,
                timeStamp = ServerTimeStamp.Now,
            };

            return error;
        }

        // ---------[ VALUE INTERPRETATION AND APPLICATION ]---------
        private void ApplyAPIErrorValues()
        {
            this.apiMessage = null;
            this.fieldValidationMessages = null;

            // null-ref and type-check
            if(this.webRequest.downloadHandler != null
               && !(this.webRequest.downloadHandler is DownloadHandlerFile))
            {
                try
                {
                    // get the request content
                    string requestContent = this.webRequest.downloadHandler.text;
                    if(string.IsNullOrEmpty(requestContent)) { return; }

                    // deserialize into an APIError
                    WebRequestError.APIWrapper errorWrapper = JsonConvert.DeserializeObject<APIWrapper>(requestContent);
                    if(errorWrapper == null
                       || errorWrapper.error == null)
                    {
                        return;
                    }

                    // extract values
                    this.apiMessage = errorWrapper.error.message;
                    this.fieldValidationMessages = errorWrapper.error.errors;
                }
                catch(System.Exception e)
                {
                    Debug.LogWarning("[mod.io] Error deserializing API Error:\n"
                                     + e.Message);
                }
            }
        }


        // ---------[ HELPER FUNCTIONS ]---------
        public string ToUnityDebugString()
        {
            var debugString = new System.Text.StringBuilder();

            string headerString = (this.webRequest == null ? "REQUEST FAILED LOCALLY"
                                   : this.webRequest.method.ToUpper() + " REQUEST FAILED");
            debugString.AppendLine(headerString);
            debugString.AppendLine("TimeStamp: " + this.timeStamp + " ("
                                   + ServerTimeStamp.ToLocalDateTime(this.timeStamp) + ")");

            if(this.webRequest != null)
            {
                debugString.AppendLine("URL: " + this.webRequest.url);

                var responseHeaders = webRequest.GetResponseHeaders();
                if(responseHeaders != null
                   && responseHeaders.Count > 0)
                {
                    debugString.AppendLine("Response Headers:");
                    foreach(var kvp in responseHeaders)
                    {
                        debugString.AppendLine("- [" + kvp.Key + "] " + kvp.Value);
                    }
                }

                debugString.AppendLine("APIMessage: " + this.apiMessage);

                if(this.fieldValidationMessages != null
                   && this.fieldValidationMessages.Count > 0)
                {
                    debugString.AppendLine("Field Validation Messages:");
                    foreach(var kvp in fieldValidationMessages)
                    {
                        debugString.AppendLine("- [" + kvp.Key + "] " + kvp.Value);
                    }
                }
            }

            return debugString.ToString();
        }

        public static void LogAsWarning(WebRequestError error)
        {
            Debug.LogWarning(error.ToUnityDebugString());
        }
    }
}
