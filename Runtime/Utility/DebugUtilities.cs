using System.Collections.Generic;

using UnityEngine;

using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using UnityWebRequestAsyncOperation = UnityEngine.Networking.UnityWebRequestAsyncOperation;

namespace ModIO
{
    /// <summary>Tools to assist in debugging.</summary>
    public static class DebugUtilities
    {
        // ---------[ Nested Data-Types ]---------
        /// <summary>Pairing of the WWWForm field types.</summary>
        public struct RequestInfo
        {
            public int userId;
            public string username;

            public IEnumerable<API.StringValueParameter> stringFields;
            public IEnumerable<API.BinaryDataParameter> binaryFields;
        }

        // ---------[ Web Requests ]---------
        /// <summary>Mapping of tracked WebRequests with their sent data.</summary>
        public static Dictionary<UnityWebRequest, RequestInfo> webRequestInfo = new Dictionary<UnityWebRequest, RequestInfo>();

        /// <summary>Tracks and logs a request upon it completing.</summary>
        public static void DebugRequestOperation(UnityWebRequestAsyncOperation operation,
                                                 LocalUser userData,
                                                 IEnumerable<API.StringValueParameter> stringFields,
                                                 IEnumerable<API.BinaryDataParameter> binaryFields)
        {
            #if DEBUG
                Debug.Assert(operation != null);

                RequestInfo info = new RequestInfo();

                if(userData.profile == null)
                {
                    info.userId = UserProfile.NULL_ID;
                    info.username = "No User Profile";
                }
                else
                {
                    info.userId = userData.profile.id;
                    info.username = userData.profile.username;
                }
                info.stringFields = stringFields;
                info.binaryFields = binaryFields;

                DebugUtilities.webRequestInfo.Add(operation.webRequest, info);

                if(operation.isDone)
                {
                    DebugUtilities.OnOperationCompleted(operation);
                }
                else
                {
                    operation.completed += DebugUtilities.OnOperationCompleted;
                }
            #endif // DEBUG
        }

        /// <summary>Callback upon request operation completion.</summary>
        private static void OnOperationCompleted(AsyncOperation operation)
        {
            #if DEBUG
                // get vars
                UnityWebRequestAsyncOperation o = operation as UnityWebRequestAsyncOperation;
                UnityWebRequest webRequest = o.webRequest;

                // simple string
                string requestString = DebugUtilities.GenerateRequestDebugString(webRequest);
                var timeStampString = (ServerTimeStamp.Now.ToString()
                                       + " ["
                                       + ServerTimeStamp.ToLocalDateTime(ServerTimeStamp.Now)
                                       + "]");

                // complex strings
                var responseHeaders = webRequest.GetResponseHeaders();
                var headerString = new System.Text.StringBuilder();
                if(responseHeaders != null
                   && responseHeaders.Count > 0)
                {
                    headerString.AppendLine();
                    foreach(var kvp in responseHeaders)
                    {
                        headerString.AppendLine("- [" + kvp.Key + "]:" + kvp.Value);
                    }
                }
                else
                {
                    headerString.Append(" NONE");
                }

                // generate log string
                var logString = new System.Text.StringBuilder();
                logString.AppendLine("[mod.io] Web Request Completed: " + webRequest.url);

                logString.AppendLine("------[ Request ]------");
                logString.AppendLine(requestString);

                logString.AppendLine("------[ Response ]------");
                logString.Append("Time Stamp: ");
                logString.AppendLine(timeStampString);
                logString.Append("Response Code: ");
                logString.AppendLine(webRequest.responseCode.ToString());
                logString.Append("Response Headers: ");
                logString.AppendLine(headerString.ToString());
                logString.Append("Response Error: ");
                logString.AppendLine(webRequest.error);
                logString.Append("Response Body: ");
                logString.AppendLine(webRequest.downloadHandler == null
                                     ? " NULL DOWNLOAD HANDLER"
                                     : webRequest.downloadHandler.text);
                logString.AppendLine();

                // log
                Debug.Log(requestString);
            #endif // DEBUG
        }

        /// <summary>Generates a debug-friendly string of web request details.</summary>
        public static string GenerateRequestDebugString(UnityWebRequest webRequest)
        {
            var requestHeaders = new System.Text.StringBuilder();
            foreach(string headerKey in APIClient.MODIO_REQUEST_HEADER_KEYS)
            {
                string headerValue = webRequest.GetRequestHeader(headerKey);
                if(headerValue != null)
                {
                    if(headerKey.ToUpper() == "AUTHORIZATION")
                    {
                        requestHeaders.Append("\n  " + headerKey + ": " + headerValue.Substring(0, 6));

                        if(headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders.Append(" [OAUTH TOKEN]");
                        }
                        else // NULL
                        {
                            requestHeaders.Append(" [NULL]");
                        }
                    }
                    else
                    {
                        requestHeaders.Append("\n  " + headerKey + ": " + headerValue);
                    }
                }
            }

            RequestInfo info;
            if(webRequestInfo.TryGetValue(webRequest, out info))
            {
                var infoString = new System.Text.StringBuilder();

                if(info.stringFields != null)
                {
                    foreach(var svp in info.stringFields)
                    {
                        infoString.Append("\n  " + svp.key + ": " + svp.value);
                    }
                }

                if(info.binaryFields != null)
                {
                    foreach(var bdp in info.binaryFields)
                    {
                        infoString.Append("\n  " + bdp.key
                                              + ": " + bdp.fileName);

                        if(bdp.contents == null)
                        {
                            infoString.Append(" [NULL DATA]");
                        }
                        else
                        {
                            infoString.Append(" [" + (bdp.contents.Length/1000).ToString("0.00")
                                                  + "KB]");
                        }
                    }
                }

                return("URL: " + webRequest.url
                       + "\nMethod: " + webRequest.method.ToUpper()
                       + "\nHeaders: " + requestHeaders.ToString()
                       + "\nUser: [" + info.userId.ToString() + "] " + info.username
                       + "\nForm Data: " + infoString.ToString());

            }
            else
            {
                return("URL: " + webRequest.url
                       + "\nMethod: " + webRequest.method.ToUpper()
                       + "\nHeaders: " + requestHeaders.ToString());
            }
        }
    }
}
