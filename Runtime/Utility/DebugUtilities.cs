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
            public string userIdString;
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

                RequestInfo info = new RequestInfo()
                {
                    userIdString = DebugUtilities.GenerateUserIdString(userData.profile),
                    stringFields = stringFields,
                    binaryFields = binaryFields,
                };
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
                RequestInfo info;
                if(!DebugUtilities.webRequestInfo.TryGetValue(webRequest, out info))
                {
                    info = new RequestInfo()
                    {
                        userIdString = "NONE_RECORDED",
                        stringFields = null,
                        binaryFields = null,
                    };
                }

                // generate strings
                string requestString = DebugUtilities.GenerateRequestDebugString(webRequest,
                                                                                 info.userIdString,
                                                                                 info.stringFields,
                                                                                 info.binaryFields);
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
                    foreach(var kvp in responseHeaders)
                    {
                        headerString.AppendLine();
                        headerString.Append('\t');
                        headerString.Append(kvp.Key);
                        headerString.Append(':');
                        headerString.Append(kvp.Value);
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

        /// <summary>Generates a debug-friendly string of a web request.</summary>
        public static string GenerateRequestDebugString(UnityWebRequest webRequest,
                                                        string userIdString,
                                                        IEnumerable<API.StringValueParameter> stringFields,
                                                        IEnumerable<API.BinaryDataParameter> binaryFields)
        {
            // check user string
            if(userIdString == null)
            {
                userIdString = "[NOT RECORDED]";
            }

            // build string
            var requestString = new System.Text.StringBuilder();

            requestString.Append("URL: ");
            requestString.Append(webRequest.url);
            requestString.Append(" (");
            requestString.Append(webRequest.method.ToUpper());
            requestString.AppendLine(")");

            requestString.Append("User: ");
            requestString.AppendLine(userIdString);

            // add request headers
            requestString.Append("Headers: ");
            foreach(string headerKey in APIClient.MODIO_REQUEST_HEADER_KEYS)
            {
                string headerValue = webRequest.GetRequestHeader(headerKey);
                if(headerValue != null)
                {
                    requestString.AppendLine();
                    requestString.Append('\t');
                    requestString.Append(headerKey);
                    requestString.Append(':');

                    if(headerKey.ToUpper() == "AUTHORIZATION")
                    {
                        if(headerValue != null
                           && headerValue.StartsWith("Bearer ")
                           && headerValue.Length > 8)
                        {
                            requestString.Append("Bearer [OAUTH TOKEN]");
                        }
                        else // NULL
                        {
                            requestString.Append(headerValue);
                        }
                    }
                    else
                    {
                        requestString.Append(headerValue);
                    }
                }
            }

            // add string fields
            requestString.Append("String Fields: ");
            if(stringFields == null)
            {
                requestString.Append(" [NONE]");
            }
            else
            {
                foreach(var svp in stringFields)
                {
                    requestString.AppendLine();
                    requestString.Append('\t');
                    requestString.Append(svp.key);
                    requestString.Append(':');
                    requestString.Append(svp.value);
                }
            }

            // add binary fields
            requestString.Append("Binary Fields: ");
            if(binaryFields == null)
            {
                requestString.Append(" [NONE]");
            }
            else
            {
                foreach(var bdp in binaryFields)
                {
                    requestString.AppendLine();
                    requestString.Append('\t');
                    requestString.Append(bdp.key);
                    requestString.Append(':');
                    requestString.Append(bdp.fileName);
                    requestString.Append(" (");
                    requestString.Append(bdp.contents == null
                                         ? "NULL DATA"
                                         : ValueFormatting.ByteCount(bdp.contents.Length, null));
                    requestString.Append(")");
                }
            }

            return requestString.ToString();
        }

        /// <summary>Generates a user identifying string to debug with.</summary>
        public static string GenerateUserIdString(UserProfile profile)
        {
            if(profile == null)
            {
                return "NULL_USER_PROFILE";
            }

            string username = profile.username;
            if(string.IsNullOrEmpty(username))
            {
                username = "NO_USERNAME";
            }

            return ("[" + profile.id.ToString() + "] " + username);
        }
    }
}
