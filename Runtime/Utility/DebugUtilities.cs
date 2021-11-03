using System.Collections.Generic;

using UnityEngine;

using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using UnityWebRequestAsyncOperation = UnityEngine.Networking.UnityWebRequestAsyncOperation;

namespace ModIO
{
    /// <summary>Tools to assist in debugging.</summary>
    public static class DebugUtilities
    {
        // ---------[ Web Requests ]---------
        /// <summary>Data state at the time of the request being sent.</summary>
        public struct RequestDebugData
        {
            /// <summary>User identifier to display in logs.</summary>
            public string userIdString;

            /// <summary>ServerTimeStamp at which the request was sent.</summary>
            public int timeSent;

            /// <summary>FilePath to which the request is saving data.</summary>
            public string downloadLocation;
        }

        /// <summary>Mapping of tracked WebRequests with their sent data.</summary>
        public static Dictionary<UnityWebRequest, RequestDebugData> webRequestDebugData =
            new Dictionary<UnityWebRequest, RequestDebugData>();

        /// <summary>Tracks and logs a request upon it completing.</summary>
        public static void DebugWebRequest(UnityWebRequestAsyncOperation operation,
                                           LocalUser userData, int timeSent = -1)
        {
#if DEBUG
            DebugUtilities.DebugDownload(operation, userData, null, timeSent);
#endif // DEBUG
        }

        /// <summary>Tracks and logs a download upon it completing.</summary>
        public static void DebugDownload(UnityWebRequestAsyncOperation operation,
                                         LocalUser userData, string downloadLocation,
                                         int timeSent = -1)
        {
#if DEBUG

            Debug.Assert(operation != null);

            UnityWebRequest webRequest = operation.webRequest;
            string userIdString = DebugUtilities.GenerateUserIdString(userData.profile);

            if(timeSent < 0)
            {
                timeSent = ServerTimeStamp.Now;
            }

            RequestDebugData debugData;
            if(DebugUtilities.webRequestDebugData.TryGetValue(webRequest, out debugData))
            {
                var logString = new System.Text.StringBuilder();
                logString.AppendLine("[mod.io] Duplicate Web Request Sent");
                logString.Append("URL: ");
                logString.Append(webRequest.url);
                logString.Append(" (");
                logString.Append(webRequest.method.ToUpper());
                logString.AppendLine(")");

                if(!string.IsNullOrEmpty(downloadLocation))
                {
                    logString.Append("Download Location: ");
                    logString.AppendLine(downloadLocation);
                }

                if(debugData.timeSent >= 0)
                {
                    logString.Append("Sent: ");
                    logString.Append(
                        ServerTimeStamp.ToLocalDateTime(debugData.timeSent).ToString());
                    logString.Append(" [");
                    logString.Append(debugData.timeSent.ToString());
                    logString.AppendLine("]");
                }

                if(timeSent >= 0)
                {
                    logString.Append("Re-sent: ");
                    logString.Append(ServerTimeStamp.ToLocalDateTime(timeSent).ToString());
                    logString.Append(" [");
                    logString.Append(timeSent.ToString());
                    logString.AppendLine("]");
                }

                Debug.Log(logString.ToString());
            }
            else
            {
                debugData = new RequestDebugData() {
                    userIdString = userIdString,
                    timeSent = timeSent,
                    downloadLocation = downloadLocation,
                };

                if(PluginSettings.REQUEST_LOGGING.logOnSend)
                {
                    var logString = new System.Text.StringBuilder();
                    logString.AppendLine("[mod.io] Web Request Sent");
                    logString.Append("URL: ");
                    logString.Append(webRequest.url);
                    logString.Append(" (");
                    logString.Append(webRequest.method.ToUpper());
                    logString.AppendLine(")");

                    if(!string.IsNullOrEmpty(debugData.downloadLocation))
                    {
                        logString.Append("Download Location: ");
                        logString.AppendLine(debugData.downloadLocation);
                    }

                    if(debugData.timeSent >= 0)
                    {
                        logString.Append("Sent: ");
                        logString.Append(
                            ServerTimeStamp.ToLocalDateTime(debugData.timeSent).ToString());
                        logString.Append(" [");
                        logString.Append(debugData.timeSent.ToString());
                        logString.AppendLine("]");
                    }

                    logString.AppendLine();

                    string requestString =
                        DebugUtilities.GetRequestInfo(webRequest, debugData.userIdString);

                    logString.AppendLine("------[ Request ]------");
                    logString.AppendLine(requestString);

                    Debug.Log(logString.ToString());
                }

                if(PluginSettings.REQUEST_LOGGING.logAllResponses
                   || PluginSettings.REQUEST_LOGGING.errorsAsWarnings)
                {
                    DebugUtilities.webRequestDebugData.Add(webRequest, debugData);

                    // handle completion
                    if(operation.isDone)
                    {
                        DebugUtilities.OnOperationCompleted(operation);
                    }
                    else
                    {
                        operation.completed += DebugUtilities.OnOperationCompleted;
                    }
                }
            }

#endif // DEBUG
        }

#if DEBUG
        /// <summary>Callback upon request operation completion.</summary>
        private static void OnOperationCompleted(AsyncOperation operation)
        {
            if(operation == null)
            {
                return;
            }

            // get vars
            UnityWebRequestAsyncOperation o = operation as UnityWebRequestAsyncOperation;
            UnityWebRequest webRequest = o.webRequest;
            var now = ServerTimeStamp.Now;

            // should we log?
            if(PluginSettings.REQUEST_LOGGING.logAllResponses || webRequest.IsError())
            {
                RequestDebugData debugData;
                if(!DebugUtilities.webRequestDebugData.TryGetValue(webRequest, out debugData))
                {
                    debugData = new RequestDebugData() {
                        userIdString = "NONE_RECORDED",
                        timeSent = -1,
                        downloadLocation = null,
                    };
                }

                // generate strings
                string requestString =
                    DebugUtilities.GetRequestInfo(webRequest, debugData.userIdString);

                string responseString = DebugUtilities.GetResponseInfo(webRequest);

                // generate log string
                var logString = new System.Text.StringBuilder();
                if(!webRequest.IsError())
                {
                    logString.AppendLine("[mod.io] Web Request Succeeded");
                }
                else
                {
                    logString.AppendLine("[mod.io] Web Request Failed");
                }

                logString.Append("URL: ");
                logString.Append(webRequest.url);
                logString.Append(" (");
                logString.Append(webRequest.method.ToUpper());
                logString.AppendLine(")");

                if(!string.IsNullOrEmpty(debugData.downloadLocation))
                {
                    logString.Append("Download Location: ");
                    logString.AppendLine(debugData.downloadLocation);
                }

                if(debugData.timeSent >= 0)
                {
                    logString.Append("Sent: ");
                    logString.Append(
                        ServerTimeStamp.ToLocalDateTime(debugData.timeSent).ToString());
                    logString.Append(" [");
                    logString.Append(debugData.timeSent.ToString());
                    logString.AppendLine("]");
                }

                logString.Append("Completed: ");
                logString.Append(ServerTimeStamp.ToLocalDateTime(now).ToString());
                logString.Append(" [");
                logString.Append(now.ToString());
                logString.AppendLine("]");

                logString.AppendLine();

                logString.AppendLine("------[ Request ]------");
                logString.AppendLine(requestString);

                logString.AppendLine("------[ Response ]------");
                logString.AppendLine(responseString);

                // log
                if(webRequest.IsError() && PluginSettings.REQUEST_LOGGING.errorsAsWarnings)
                {
                    Debug.LogWarning(logString.ToString());
                }
                else
                {
                    Debug.Log(logString.ToString());
                }
            }

            DebugUtilities.webRequestDebugData.Remove(webRequest);
        }
#endif // DEBUG

        /// <summary>Generates a debug-friendly string of a web request.</summary>
        public static string GetRequestInfo(UnityWebRequest webRequest, string userIdString)
        {
            if(webRequest == null)
            {
                return "NULL_WEB_REQUEST";
            }

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
            requestString.AppendLine("Headers:");
            foreach(string headerKey in APIClient.MODIO_REQUEST_HEADER_KEYS)
            {
                string headerValue = webRequest.GetRequestHeader(headerKey);
                if(headerValue != null)
                {
                    requestString.Append("  ");
                    requestString.Append(headerKey);
                    requestString.Append('=');

                    if(headerKey.ToUpper() == "AUTHORIZATION")
                    {
                        if(headerValue != null && headerValue.StartsWith("Bearer ")
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

                    requestString.AppendLine();
                }
            }

            // add uploaded data
            var uploadHandler = webRequest.uploadHandler;
            if(uploadHandler != null)
            {
                List<API.StringValueParameter> stringFields = null;
                List<API.BinaryDataParameter> binaryFields = null;

                string contentType = webRequest.GetRequestHeader("content-type");
                if(contentType.ToLower() == "application/x-www-form-urlencoded")
                {
                    DebugUtilities.ParseURLEncodedFormData(uploadHandler.data, out stringFields);
                }
                else if(contentType.Contains("multipart/form-data"))
                {
                    DebugUtilities.ParseMultipartFormData(uploadHandler.data, out stringFields,
                                                          out binaryFields);
                }
                else
                {
                    Debug.Log("[mod.io] Unable to parse upload data for content-type \'"
                              + contentType + "\'");
                }

                // add string fields
                if(stringFields != null)
                {
                    requestString.AppendLine("String Fields:");

                    int countInsertIndex = requestString.Length - 1;
                    int count = 0;

                    foreach(var svp in stringFields)
                    {
                        requestString.Append("  ");
                        requestString.Append(svp.key);
                        requestString.Append('=');
                        requestString.Append(svp.value);
                        requestString.AppendLine();
                        ++count;
                    }

                    requestString.Insert(countInsertIndex, " [" + count.ToString() + "]");
                }

                // add binary fields
                if(binaryFields != null)
                {
                    requestString.AppendLine("Binary Fields:");

                    int countInsertIndex = requestString.Length - 1;
                    int count = 0;

                    foreach(var bdp in binaryFields)
                    {
                        requestString.Append("  ");
                        requestString.Append(bdp.key);
                        requestString.Append('=');
                        requestString.Append(bdp.fileName);
                        requestString.Append(" (");
                        requestString.Append(bdp.contents == null ? "NULL_DATA"
                                                                  : ValueFormatting.ByteCount(
                                                                      bdp.contents.Length, null));
                        requestString.Append(")");
                        requestString.AppendLine();
                        ++count;
                    }

                    requestString.Insert(countInsertIndex, " [" + count.ToString() + "]");
                }
            }

            return requestString.ToString();
        }

        /// <summary>Generates a debug-friendly string of a web request response.</summary>
        public static string GetResponseInfo(UnityWebRequest webRequest)
        {
            if(webRequest == null)
            {
                return "NULL_WEB_REQUEST";
            }

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
            if(string.IsNullOrEmpty(webRequest.error))
            {
                responseString.AppendLine("NO_ERROR");
            }
            else
            {
                responseString.AppendLine(webRequest.error);
            }

            // add request headers
            responseString.AppendLine("Headers:");

            var responseHeaders = webRequest.GetResponseHeaders();
            if(responseHeaders == null || responseHeaders.Count == 0)
            {
                responseString.AppendLine("  NONE");
            }
            else
            {
                foreach(var kvp in responseHeaders)
                {
                    responseString.Append("  ");
                    responseString.Append(kvp.Key);
                    responseString.Append('=');
                    responseString.Append(kvp.Value);
                    responseString.AppendLine();
                }
            }

            // add error information
            if(webRequest.IsError())
            {
                var error = WebRequestError.GenerateFromWebRequest(webRequest);

                responseString.AppendLine("mod.io Error Details:");

                // add flags
                responseString.Append("  flags=");

                if(error.isAuthenticationInvalid)
                {
                    responseString.Append("[AuthenticationInvalid]");
                }

                if(error.isServerUnreachable)
                {
                    responseString.Append("[ServerUnreachable]");
                }

                if(error.isRequestUnresolvable)
                {
                    responseString.Append("[RequestUnresolvable]");
                }

                if(!error.isAuthenticationInvalid && !error.isServerUnreachable
                   && !error.isRequestUnresolvable)
                {
                    responseString.Append("[NONE]");
                }

                responseString.AppendLine();

                // add rate limiting
                responseString.Append("  limitedUntilTimeStamp=");
                responseString.AppendLine(error.limitedUntilTimeStamp.ToString());

                // add messages
                responseString.Append("  errorReference=");
                responseString.AppendLine(error.errorReference.ToString());

                responseString.Append("  errorMessage=");
                responseString.AppendLine(error.errorMessage);

                if(error.fieldValidationMessages != null && error.fieldValidationMessages.Count > 0)
                {
                    responseString.AppendLine("  fieldValidation:");

                    foreach(var kvp in error.fieldValidationMessages)
                    {
                        responseString.Append("    [");
                        responseString.Append(kvp.Key);
                        responseString.Append("]=");
                        responseString.Append(kvp.Value);
                        responseString.AppendLine();
                    }
                }

                responseString.Append("  displayMessage=");
                responseString.AppendLine(error.displayMessage);
            }

            // body
            responseString.AppendLine("Body:");

            string bodyText = null;
            try
            {
                if(webRequest.downloadHandler == null)
                {
                    bodyText = "  NULL_DOWNLOAD_HANDLER";
                }
                else
                {
                    bodyText = webRequest.downloadHandler.text;
                }
            }
            catch
            {
                bodyText = "  TEXT_ACCESS_NOT_SUPPORTED";
            }
            responseString.AppendLine(bodyText);

            return responseString.ToString();
        }

        /// <summary>Parses an UploadHandler's payload for content-type =
        /// "application/x-www-form-urlencoded".</summary>
        public static void ParseURLEncodedFormData(byte[] data,
                                                   out List<API.StringValueParameter> stringFields)
        {
            stringFields = null;

            // early out
            if(data == null || data.Length == 0)
            {
                return;
            }

            // parse
            stringFields = new List<API.StringValueParameter>();

            string dataString = System.Text.Encoding.UTF8.GetString(data);

            string[] pairs =
                dataString.Split(new char[] { '&' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach(string pairString in pairs)
            {
                string[] elements = pairString.Split(new char[] { '=' });

                if(elements.Length == 0)
                {
                    continue;
                }
                else if(elements.Length != 2)
                {
                    stringFields.Add(
                        API.StringValueParameter.Create(pairString, "BADLY_FORMATTED_FORMDATA"));
                }
                else
                {
                    stringFields.Add(API.StringValueParameter.Create(elements[0], elements[1]));
                }
            }
        }

        /// <summary>Parses an UploadHandler's payload for content-type =
        /// "multipart/form-data".</summary>
        public static void ParseMultipartFormData(byte[] data,
                                                  out List<API.StringValueParameter> stringFields,
                                                  out List<API.BinaryDataParameter> binaryFields)
        {
            stringFields = null;
            binaryFields = null;

            // early out
            if(data == null || data.Length == 0)
            {
                return;
            }

            // get dataString and delimiter
            string dataString = System.Text.Encoding.UTF8.GetString(data);
            string lineEnd = "\r\n";
            int lineEndIndex = -1;

            lineEndIndex = dataString.IndexOf(lineEnd, 1);
            if(lineEndIndex < 0)
            {
                return;
            }

            string delimiter = dataString.Substring(0, lineEndIndex).Trim();
            string[] sections = dataString.Split(new string[] { delimiter },
                                                 System.StringSplitOptions.RemoveEmptyEntries);
            stringFields = new List<API.StringValueParameter>();
            binaryFields = new List<API.BinaryDataParameter>();

            foreach(string s in sections)
            {
                string searchString = null;
                int searchIndex = 0;
                int elementStartIndex = 0;
                int elementEndIndex = 0;

                // Content-Type
                searchString = "Content-Type: ";
                searchIndex = s.IndexOf(searchString);
                if(searchIndex < 0)
                {
                    continue;
                }

                elementStartIndex = searchIndex + searchString.Length;
                elementEndIndex = s.IndexOf(lineEnd, elementStartIndex);

                string contentType =
                    s.Substring(elementStartIndex, elementEndIndex - elementStartIndex);

                // process text
                if(contentType.Contains(@"text/plain"))
                {
                    var newStringParam = new API.StringValueParameter();

                    // get key
                    searchString = "name=\"";
                    searchIndex = s.IndexOf(searchString);

                    if(searchIndex < 0)
                    {
                        newStringParam.key = "KEY_NOT_FOUND";
                    }
                    else
                    {
                        elementStartIndex = searchIndex + searchString.Length;
                        elementEndIndex = s.IndexOf("\"", elementStartIndex);

                        newStringParam.key =
                            s.Substring(elementStartIndex, elementEndIndex - elementStartIndex);
                    }

                    // get value
                    searchString = lineEnd + lineEnd;
                    searchIndex = s.IndexOf(searchString);

                    if(searchIndex < 0)
                    {
                        newStringParam.value = "VALUE_NOT_FOUND";
                    }
                    else
                    {
                        elementStartIndex = searchIndex + searchString.Length;
                        newStringParam.value = s.Substring(elementStartIndex).Trim();
                    }

                    stringFields.Add(newStringParam);
                }
                // process literally anything else
                else
                {
                    var newBinaryParam = new API.BinaryDataParameter() {
                        mimeType = contentType,
                    };

                    // get key
                    searchString = "name=\"";
                    searchIndex = s.IndexOf(searchString);

                    if(searchIndex < 0)
                    {
                        newBinaryParam.key = "KEY_NOT_FOUND";
                    }
                    else
                    {
                        elementStartIndex = searchIndex + searchString.Length;
                        elementEndIndex = s.IndexOf("\"", elementStartIndex);

                        newBinaryParam.key =
                            s.Substring(elementStartIndex, elementEndIndex - elementStartIndex);
                    }

                    // get fileName
                    searchString = "filename=\"";
                    searchIndex = s.IndexOf(searchString);

                    if(searchIndex < 0)
                    {
                        newBinaryParam.fileName = "FILENAME_NOT_FOUND";
                    }
                    else
                    {
                        elementStartIndex = searchIndex + searchString.Length;
                        elementEndIndex = s.IndexOf("\"", elementStartIndex);

                        newBinaryParam.fileName =
                            s.Substring(elementStartIndex, elementEndIndex - elementStartIndex);
                    }

                    // get contents
                    searchString = lineEnd + lineEnd;
                    searchIndex = s.IndexOf(searchString);

                    if(searchIndex < 0)
                    {
                        newBinaryParam.contents = null;
                    }
                    else
                    {
                        elementStartIndex = searchIndex + searchString.Length;

                        int byteCount = (s.Length - elementStartIndex - lineEnd.Length);
                        newBinaryParam.contents = System.Text.Encoding.UTF8.GetBytes(
                            s.Substring(elementStartIndex, byteCount));
                    }

                    binaryFields.Add(newBinaryParam);
                }
            }
        }

        // ---------[ General ]---------
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

            return ("[" + profile.id.ToString() + "]:" + username);
        }
    }
}
