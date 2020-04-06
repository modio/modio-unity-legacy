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
                var timeStampString = (ServerTimeStamp.Now.ToString()
                                       + " ["
                                       + ServerTimeStamp.ToLocalDateTime(ServerTimeStamp.Now)
                                       + "]");

                string requestString = DebugUtilities.GenerateRequestDebugString(webRequest,
                                                                                 info.userIdString,
                                                                                 info.stringFields,
                                                                                 info.binaryFields);

                string responseString = DebugUtilities.GenerateResponseDebugString(webRequest);

                // generate log string
                var logString = new System.Text.StringBuilder();
                logString.AppendLine("[mod.io] Web Request Completed");
                logString.Append("URL: ");
                logString.Append(webRequest.url);
                logString.Append(" (");
                logString.Append(webRequest.method.ToUpper());
                logString.AppendLine(")");

                logString.AppendLine("------[ Request ]------");
                logString.AppendLine(requestString);

                logString.AppendLine("------[ Response ]------");
                logString.Append("Time Stamp: ");
                logString.AppendLine(timeStampString);
                logString.AppendLine(responseString);

                logString.AppendLine();

                // log
                Debug.Log(logString.ToString());

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
                            requestString.Append("Bearer [OAUTH_TOKEN]");
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
                requestString.Append(" NONE");
            }
            else
            {
                int count = 0;
                foreach(var svp in stringFields)
                {
                    requestString.AppendLine();
                    requestString.Append('\t');
                    requestString.Append(svp.key);
                    requestString.Append(':');
                    requestString.Append(svp.value);
                    ++count;
                }

                requestString.AppendLine();
                requestString.Append('\t');
                requestString.Append("Field Count = ");
                requestString.AppendLine(count.ToString());
            }

            // add binary fields
            requestString.Append("Binary Fields: ");
            if(binaryFields == null)
            {
                requestString.Append(" NONE");
            }
            else
            {
                int count = 0;

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
                    ++count;
                }

                requestString.AppendLine();
                requestString.Append('\t');
                requestString.Append("Field Count = ");
                requestString.AppendLine(count.ToString());
            }

            return requestString.ToString();
        }

        /// <summary>Generates a debug-friendly string of a web request response.</summary>
        public static string GenerateResponseDebugString(UnityWebRequest webRequest)
        {
            // get info
            var responseString = new System.Text.StringBuilder();

            responseString.Append("URL: ");
            responseString.Append(webRequest.url);
            responseString.Append(" (");
            responseString.Append(webRequest.method.ToUpper());
            responseString.AppendLine(")");

            responseString.Append("Response Code: ");
            responseString.AppendLine(webRequest.responseCode.ToString());

            responseString.Append("Response Error: ");
            responseString.AppendLine(webRequest.error);

            // add request headers
            var responseHeaders = webRequest.GetResponseHeaders();

            responseString.Append("Headers: ");

            if(responseHeaders == null
               || responseHeaders.Count == 0)
            {
                responseString.Append(" NONE");
            }
            else
            {
                foreach(var kvp in responseHeaders)
                {
                    responseString.AppendLine();
                    responseString.Append('\t');
                    responseString.Append(kvp.Key);
                    responseString.Append(':');
                    responseString.Append(kvp.Value);
                }
            }

            // body
            responseString.Append("Body: ");
            responseString.AppendLine(webRequest.downloadHandler == null
                                      ? " NULL_DOWNLOAD_HANDLER"
                                      : webRequest.downloadHandler.text);

            return responseString.ToString();
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
