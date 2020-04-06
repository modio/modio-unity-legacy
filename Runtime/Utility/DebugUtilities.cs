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
            public IEnumerable<API.StringValueParameter> stringFields;
            public IEnumerable<API.BinaryDataParameter> binaryFields;
        }

        // ---------[ Web Requests ]---------
        /// <summary>Mapping of tracked WebRequests with their sent data.</summary>
        public static Dictionary<UnityWebRequest, RequestInfo> webRequestInfo = new Dictionary<UnityWebRequest, RequestInfo>();

        /// <summary>Tracks and logs a request upon it completing.</summary>
        public static void DebugRequestOperation(UnityWebRequestAsyncOperation operation,
                                                 IEnumerable<API.StringValueParameter> stringFields,
                                                 IEnumerable<API.BinaryDataParameter> binaryFields)
        {
            #if DEBUG
                Debug.Assert(operation != null);

                RequestInfo info = new RequestInfo()
                {
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
                UnityWebRequestAsyncOperation o = operation as UnityWebRequestAsyncOperation;

                DebugUtilities.GenerateRequestDebugString(o.webRequest);
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

            RequestInfo formData;
            if(webRequestInfo.TryGetValue(webRequest, out formData))
            {
                var formDataString = new System.Text.StringBuilder();

                if(formData.stringFields != null)
                {
                    foreach(var svp in formData.stringFields)
                    {
                        formDataString.Append("\n  " + svp.key + ": " + svp.value);
                    }
                }

                if(formData.binaryFields != null)
                {
                    foreach(var bdp in formData.binaryFields)
                    {
                        formDataString.Append("\n  " + bdp.key
                                              + ": " + bdp.fileName);

                        if(bdp.contents == null)
                        {
                            formDataString.Append(" [NULL DATA]");
                        }
                        else
                        {
                            formDataString.Append(" [" + (bdp.contents.Length/1000).ToString("0.00")
                                                  + "KB]");
                        }
                    }
                }

                return("URL: " + webRequest.url
                       + "\nMethod: " + webRequest.method.ToUpper()
                       + "\nHeaders: " + requestHeaders.ToString()
                       + "\nForm Data: " + formDataString.ToString());
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
