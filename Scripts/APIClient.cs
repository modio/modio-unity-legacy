using System;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

using Debug = UnityEngine.Debug;
using WWWForm = UnityEngine.WWWForm;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using UnityWebRequestAsyncOperation = UnityEngine.Networking.UnityWebRequestAsyncOperation;

using ModIO.API;

// TODO(@jackson): Add filter references
// TODO(@jackson): Add SeeAlso: gameID and userAuthorizationToken where necessary
// TODO(@jackson): Add See also: docs.mod.io ref
// TODO(@jackson): search ")[", "][", and "[["
// TODO(@jackson): Examples - https://www.stack.nl/~dimitri/doxygen/manual/commands.html#cmdexample
namespace ModIO
{
    /// <summary>An interface for sending requests to the mod.io servers.</summary>
    public static class APIClient
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>Denotes the version of the mod.io web API that this class is compatible with.</summary>
        /// <para>This value forms part of the web API URL and should not be changed.</para>
        public const string API_VERSION = "v1";

        /// <summary>The base URL for the web API.</summary>
        #if DEBUG
        public static readonly string API_URL = (GlobalSettings.USE_TEST_SERVER
                                                 ? "https://api.test.mod.io/"
                                                 : "https://api.mod.io/") + API_VERSION;
        #else
        public const string API_URL = "https://api.mod.io/" + API_VERSION;
        #endif

        /// <summary>Collection of the HTTP request header keys used by Unity.</summary>
        /// <para>Used almost exclusively for debugging requests.</para>
        public static readonly string[] UNITY_REQUEST_HEADER_KEYS = new string[]
        {
            // - UNIVERSAL -
            "accept-charset",
            "access-control-request-headers",
            "access-control-request-method",
            "connection",
            "cookie",
            "cookie2",
            "date",
            "dnt",
            "expect",
            "host",
            "keep-alive",
            "origin",
            "referer",
            "te",
            "trailer",
            "transfer-encoding",
            "upgrade",
            "via",
            // - UNITY -
            "accept-encoding",
            "Content-Type",
            "content-length",
            "x-unity-version",
            "user-agent",
        };

        /// <summary>Collection of the HTTP request header keys used by mod.io.</summary>
        /// <para>Used almost exclusively for debugging requests.</para>
        public static readonly string[] MODIO_REQUEST_HEADER_KEYS = new string[]
        {
            "Authorization",
            "Accept-Language",
        };

        // ---------[ MEMBERS ]---------
        /// <summary>Game ID that the APIClient should use when contacting the API.</summary>
        /// <para>Game details can be found under the API Key Management page on both the
        /// <a href="https://mod.io/apikey/">production server</a> and
        /// <a href="https://test.mod.io/apikey/">test server</a>.</para>
        /// <para>See [Authentication and Security](Authentication-And-Security#game-profile-api-key-and-id)
        /// for more information.</para>
        /// <para>See also: [[ModIO.APIClient.gameAPIKey]]</para>
        public static int gameId = GlobalSettings.GAME_ID;

        /// <summary>Game API Key that the APIClient should use when contacting the API.</summary>
        /// <para>Game details can be found under the API Key Management page on both the
        /// <a href="https://mod.io/apikey/">production server</a> and
        /// <a href="https://test.mod.io/apikey/">test server</a>.</para>
        /// <para>See [Authentication and Security](Authentication-And-Security#game-profile-api-key-and-id)
        /// for more information.</para>
        /// <para>See also: [[ModIO.APIClient.gameId]]</para>
        public static string gameAPIKey = GlobalSettings.GAME_APIKEY;

        /// <summary>User OAuthToken that the APIClient submits in requests.</summary>
        /// <para>This value uniquely identifies the user and their access rights for a specific
        /// game or app, and allows the authentication of the user's credentials in
        /// update/submission requests to the mod.io servers and query the authenticated user's
        /// details.</para>
        /// <para>See [Authentication and Security](Authentication-And-Security#user-authentication)
        /// for more information.</para>
        /// <para>See also: [[ModIO.APIClient.SendSecurityCode]], [[ModIO.APIClient.GetOAuthToken]]</para>
        public static string userAuthorizationToken = null;

        /// <summary>Requested language for the API response messages.</summary>
        /// <para>Currently supported languages and the corresponding codes are listed in the mod.io
        /// documentation under <a href="https://docs.mod.io/#localization">Localization</a>.</para>
        public static string languageCode = "en";

        // ---------[ DEBUG ASSERTS ]---------
        /// <summary>Asserts that the required authorization data for making API requests is set.</summary>
        /// <para>**NOTE:** This function only asserts that the values have been set, but **does
        /// not check the correctness** of those values.</remarks>
        /// <param name="isUserTokenRequired">If true, will assert that [[ModIO.APIClient.userAuthorizationToken]]
        /// is set</param>
        /// <returns>True if all the neccessary authorization details for an API request have been
        /// set</returns>
        public static bool AssertAuthorizationDetails(bool isUserTokenRequired)
        {
            if(APIClient.gameId <= 0
               || String.IsNullOrEmpty(APIClient.gameAPIKey))
            {
                Debug.LogError("[mod.io] No API requests can be executed without a"
                               + " valid Game Id and Game API Key. These need to be"
                               + " saved in ModIO.GlobalSettings or"
                               + " set directly on the ModIO.APIClient before"
                               + " any requests can be sent to the API.");
                return false;
            }

            if(isUserTokenRequired
               && String.IsNullOrEmpty(APIClient.userAuthorizationToken))
            {
                Debug.LogError("[mod.io] API request to modification or User-specific"
                               + " endpoints cannot be made without first setting the"
                               + " User Authorization Token on the ModIO.APIClient.");
                return false;
            }

            return true;
        }

        // ---------[ REQUEST HANDLING ]---------
        /// <summary>Generates the object for a basic mod.io server request.</summary>
        /// <para>The
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">UnityWebRequest</a>
        /// that is created is initialized with the values for a mod.io server endpoint request that
        /// **does not require** a user authentication token and can be sent as-is.</para>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="filterString">Filter string to be appended to the endpoint URL</param>
        /// <param name="pagination">Pagination data for the request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GenerateQuery(string endpointURL,
                                                    string filterString,
                                                    PaginationParameters pagination)
        {
            APIClient.AssertAuthorizationDetails(false);

            string paginationString;
            if(pagination == null)
            {
                paginationString = string.Empty;
            }
            else
            {
                paginationString = ("&_limit=" + pagination.limit
                                    + "&_offset=" + pagination.offset);
            }

            string queryURL = (endpointURL
                               + "?" + filterString
                               + paginationString);

            if(APIClient.userAuthorizationToken == null)
            {
                queryURL += "&api_key=" + APIClient.gameAPIKey;
            }

            UnityWebRequest webRequest = UnityWebRequest.Get(queryURL);
            if(APIClient.userAuthorizationToken != null)
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            }
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                Debug.Log("GENERATING QUERY"
                          + "\nEndpoint: " + queryURL
                          + "\nHeaders: " + requestHeaders
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io GET request.</summary>
        /// <para>The
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">UnityWebRequest</a>
        /// that is created is initialized with the values for a mod.io server endpoint request that
        /// **requires a user authentication token** and can be sent as-is.</para>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="filterString">Filter string to be appended to the endpoint URL</param>
        /// <param name="pagination">Pagination data for the request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GenerateGetRequest(string endpointURL,
                                                         string filterString,
                                                         PaginationParameters pagination)
        {
            APIClient.AssertAuthorizationDetails(true);

            string paginationString;
            if(pagination == null)
            {
                paginationString = string.Empty;
            }
            else
            {
                paginationString = ("&_limit=" + pagination.limit
                                    + "&_offset=" + pagination.offset);
            }

            string constructedURL = (endpointURL
                                     + "?" + filterString
                                     + paginationString);

            UnityWebRequest webRequest = UnityWebRequest.Get(constructedURL);
            webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                Debug.Log("GENERATING GET REQUEST"
                          + "\nEndpoint: " + constructedURL
                          + "\nHeaders: " + requestHeaders
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io PUT request.</summary>
        /// <para>The
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">UnityWebRequest</a>
        /// that is created is initialized with the values for a mod.io server endpoint request that
        /// **requires a user authentication token** and can be sent as-is.</para>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="valueFields">The string values to be submitted with the PUT request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GeneratePutRequest(string endpointURL,
                                                         StringValueParameter[] valueFields)
        {
            APIClient.AssertAuthorizationDetails(true);

            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueParameter valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.method = UnityWebRequest.kHttpVerbPUT;
            webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                string formFields = "";
                foreach(StringValueParameter svf in valueFields)
                {
                    formFields += "\n" + svf.key + "=" + svf.value;
                }

                Debug.Log("GENERATING PUT REQUEST"
                          + "\nEndpoint: " + endpointURL
                          + "\nHeaders: " + requestHeaders
                          + "\nFields: " + formFields
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io POST request.</summary>
        /// <para>The
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">UnityWebRequest</a>
        /// that is created is initialized with the values for a mod.io server endpoint request that
        /// **requires a user authentication token** and can be sent as-is.</para>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="valueFields">The string values to be submitted with the POST request</param>
        /// <param name="dataFields">The binary data to be submitted with the POST request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GeneratePostRequest(string endpointURL,
                                                          StringValueParameter[] valueFields,
                                                          BinaryDataParameter[] dataFields)
        {
            APIClient.AssertAuthorizationDetails(true);

            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueParameter valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }
            if(dataFields != null)
            {
                foreach(BinaryDataParameter dataField in dataFields)
                {
                    form.AddBinaryData(dataField.key, dataField.contents, dataField.fileName, dataField.mimeType);
                }
            }


            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                string formFields = "";
                if(valueFields != null)
                {
                    foreach(StringValueParameter valueField in valueFields)
                    {
                        formFields += "\n" + valueField.key + "=" + valueField.value;
                    }

                }
                if(dataFields != null)
                {
                    foreach(BinaryDataParameter dataField in dataFields)
                    {
                        formFields += "\n" + dataField.key + "= [BINARY DATA]: "
                                    + dataField.fileName + "("
                                    + (dataField.contents.Length/1000f).ToString("0.00") + "KB)\n";
                    }
                }

                Debug.Log("GENERATING POST REQUEST"
                          + "\nEndpoint: " + endpointURL
                          + "\nHeaders: " + requestHeaders
                          + "\nFields: " + formFields
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io DELETE request.</summary>
        /// <para>The
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">UnityWebRequest</a>
        /// that is created is initialized with the values for a mod.io server endpoint request that
        /// **requires a user authentication token** and can be sent as-is.</para>
        /// <param name="endpointURL">Endpoint URL for the request</param>
        /// <param name="valueFields">The string values to be submitted with the DELETE request</param>
        /// <returns>
        /// A <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> initialized with the data for sending the API request
        /// </returns>
        public static UnityWebRequest GenerateDeleteRequest(string endpointURL,
                                                            StringValueParameter[] valueFields)
        {
            APIClient.AssertAuthorizationDetails(true);

            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueParameter valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.method = UnityWebRequest.kHttpVerbDELETE;
            webRequest.SetRequestHeader("Authorization", "Bearer " + APIClient.userAuthorizationToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        if(headerKey == "Authorization"
                           && headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 6) + " [OAUTH TOKEN]";
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                string formFields = "";
                if(valueFields != null)
                {
                    foreach(StringValueParameter kvp in valueFields)
                    {
                        formFields += "\n" + kvp.key + "=" + kvp.value;
                    }
                }
                // foreach(KeyValuePair<string, Request.BinaryData> kvp in dataFields)
                // {
                //     formFields += "\n" + kvp.Key + "= [BINARY DATA] " + kvp.Value.fileName + "\n";
                // }

                Debug.Log("GENERATING DELETE REQUEST"
                          + "\nEndpoint: " + endpointURL
                          + "\nHeaders: " + requestHeaders
                          + "\nFields: " + formFields
                          + "\n"
                          );
            }
            #endif

            return webRequest;
        }

        /// <summary>A wrapper for sending a UnityWebRequest and attaching callbacks.</summary>
        /// <para>Calls
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.SendWebRequest.html">SendWebRequest</a>
        /// on the
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">UnityWebRequest</a>
        /// and attaches the callbacks to the
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequestAsyncOperation.html">UnityWebRequestAsyncOperation</a>
        /// that is created.</para>
        /// <param name="webRequest">Request to send and attach the callback functions to</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void SendRequest(UnityWebRequest webRequest,
                                       Action successCallback,
                                       Action<WebRequestError> errorCallback)
        {
            // - Start Request -
            UnityWebRequestAsyncOperation requestOperation = webRequest.SendWebRequest();
            requestOperation.completed += (operation) =>
            {
                #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log(webRequest.method.ToUpper() + " REQUEST RESPONSE"
                              + "\nResponse received at: " + ServerTimeStamp.ToLocalDateTime(responseTimeStamp)
                              + " [" + responseTimeStamp + "]"
                              + "\nURL: " + webRequest.url
                              + "\nResponse Code: " + webRequest.responseCode
                              + "\nResponse Error: " + webRequest.error
                              + "\nResponse: " + webRequest.downloadHandler.text
                              + "\n");
                }
                #endif

                if(webRequest.isNetworkError || webRequest.isHttpError)
                {
                    if(errorCallback != null)
                    {
                        errorCallback(WebRequestError.GenerateFromWebRequest(webRequest));
                    }
                }
                else
                {
                    if(successCallback != null) { successCallback(); }
                }
            };
        }

        /// <summary>A wrapper for sending a web request to mod.io and parsing the result.</summary>
        /// <para>Calls
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.SendWebRequest.html">SendWebRequest</a>
        /// on the
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">UnityWebRequest</a>
        /// and attaches the callbacks to the
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequestAsyncOperation.html">UnityWebRequestAsyncOperation</a>
        /// that is created (via [[ModIO.APIClient.SendRequest]]) and additionally attempts to parse
        /// the response from the mod.io server as an object of type `T`.</para>
        /// <param name="webRequest">Request to send and attach the callback functions to</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void SendRequest<T>(UnityWebRequest webRequest,
                                          Action<T> successCallback,
                                          Action<WebRequestError> errorCallback)
        {
            Action processResponse = () =>
            {
                if(successCallback != null)
                {
                    try
                    {
                        T response = default(T);
                        response = JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
                        successCallback(response);
                    }
                    catch(Exception e)
                    {
                        Debug.LogError("[mod.io] Failed to convert response into " + typeof(T).ToString() + " representation\n\n"
                                       + Utility.GenerateExceptionDebugString(e));

                        // TODO(@jackson): Error!
                    }
                }
            };

            APIClient.SendRequest(webRequest,
                                  processResponse,
                                  errorCallback);
        }


        // ---------[ AUTHENTICATION ]---------
        /// <summary>Requests a login code be sent to an email address.</summary>
        /// <para>This request is the first step of authenticating a user account for the game/app.</para>
        /// <para>See [Authentication and Security](Authentication-And-Security#game-profile-api-key-and-id)
        /// for more information, and the [mod.io docs](https://docs.mod.io/#authentication) for an
        /// in-depth explanation of the authentication process.</para>
        /// <para>See also: [[ModIO.APIClient.GetOAuthToken]]</para>
        /// <param name="emailAddress">Email address for a new or existing account</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void SendSecurityCode(string emailAddress,
                                            Action<APIMessage> successCallback,
                                            Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/oauth/emailrequest";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("api_key", APIClient.gameAPIKey),
                StringValueParameter.Create("email", emailAddress),
            };

            // NOTE(@jackson): APIClient post requests _always_ require
            // the userAuthorizationToken to be set, and so we just use
            // a dummy value here.
            string oldToken = APIClient.userAuthorizationToken;
            APIClient.userAuthorizationToken = "NONE";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       valueFields,
                                                                       null);

            APIClient.userAuthorizationToken = oldToken;

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Wrapper object for [[ModIO.APIClient.GetOAuthToken]] requests.</summary>
        [System.Serializable]
        private struct AccessTokenObject { public string access_token; }

        /// <summary>Requests a user OAuthToken in exchange for a security code.</summary>
        /// <para>This request is the second step of authenticating a user account for the game/app.</para>
        /// <para>See [Authentication and Security](Authentication-And-Security#game-profile-api-key-and-id)
        /// for more information, and the [mod.io docs](https://docs.mod.io/#authentication) for an
        /// in-depth explanation of the authentication process.</para>
        /// <para>See also: [[ModIO.APIClient.SendSecurityCode]]</para>
        /// </remarks>
        /// <param name="securityCode">Security code sent to the user's email address</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetOAuthToken(string securityCode,
                                         Action<string> successCallback,
                                         Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/oauth/emailexchange";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("api_key", APIClient.gameAPIKey),
                StringValueParameter.Create("security_code", securityCode),
            };

            // NOTE(@jackson): APIClient post requests _always_ require
            // the userAuthorizationToken to be set, and so we just use
            // a dummy value here.
            string oldToken = APIClient.userAuthorizationToken;
            APIClient.userAuthorizationToken = "NONE";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       valueFields,
                                                                       null);

            APIClient.userAuthorizationToken = oldToken;

            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }


        // ---------[ GAME ENDPOINTS ]---------
        /// <summary>Fetches all the game profiles from the mod.io servers.</summary>
        /// <para>A successful request returns a [ResponseArray](ModIO.API.ResponseArray) of
        /// [GameProfiles](ModIO.GameProfile) that match the filtering and pagination parameters
        /// supplied.</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllGames(RequestFilter filter, PaginationParameters pagination,
                                       Action<ResponseArray<GameProfile>> successCallback,
                                       Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the game's/app's profile from the mod.io servers.</summary>
        /// <para>A successful request will return the [GameProfile](ModIO.GameProfile) matching the
        /// id stored in [[ModIO.APIClient.gameId]].</para>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetGame(Action<GameProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Updates the game's profile on the mod.io servers.</summary>
        /// <para>Updates the game profile for the game id stored in [[ModIO.APIClient.gameId]].
        /// This function only supports the game profile fields listed in the
        /// [EditGameParameters](ModIO.API.EditGameParameters) class. To update the icon, logo, or
        /// header fields use [AddGameMedia](ModIO.APIClient.AddGameMedia). A successful request
        /// will return the updated [GameProfile](ModIO.GameProfile).</para>
        /// <para>**NOTE:** You can also edit a game profile directly via the mod.io web interface.
        /// This is the recommended approach.</para>
        /// <param name="parameters">Updated values for the game profile</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void EditGame(EditGameParameters parameters,
                                    Action<GameProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MOD ENDPOINTS ]---------
        /// <summary>Fetches all mod profiles from the mod.io servers.</summary>
        /// <para>A successful request returns a [ResponseArray](ModIO.API.ResponseArray) of
        /// [ModProfiles](ModIO.ModProfile) that match the filtering and pagination parameters
        /// supplied.</para>
        /// <para>**NOTE:** As with all requests send via [APIClient](ModIO.APIClient), the results
        /// of this query are limited to mods belonging to the game matching [[ModIO.APIClient.gameId]].</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllMods(RequestFilter filter, PaginationParameters pagination,
                                      Action<ResponseArray<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches a mod profile from the mod.io servers.</summary>
        /// <param name="modId">The id of the mod profile to retrieve</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetMod(int modId,
                                  Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a new mod profile to the mod.io servers.</summary>
        /// <para>The new mod profile is created as a mod belonging to the game id stored in
        /// [[ModIO.APIClient.gameId]]. Successful requests will return the newly created
        /// [[ModIO.ModProfile]].</para>
        /// <para>By default new mods are [NotAccepted](ModIO.ModStatus.NotAccepted) and
        /// [Public](ModIO.ModVisibility.Public). They can only be [accepted](ModIO.ModStatus.Accepted)
        /// and made available via the API once a [Modfile](ModIO.Modfile) has been uploaded.
        /// Media, Metadata Key Value Pairs and Dependencies can also be added after a mod profile
        /// is created.</para>
        /// <param name="parameters">The values to be uploaded with to the new mod profile</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddMod(AddModParameters parameters,
                                  Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits changes to an existing mod profile.</summary>
        /// <para>This function is only able to update the parameters found in the
        /// [EditModParameters](ModIO.API.EditModParameters) class. To update the logo or media
        /// associated with a mod, use [[ModIO.APIClient.AddModMedia]]. The same applies to
        /// Modfiles, Metadata Key Value Pairs and Dependencies which are all managed via other
        /// endpoints. A successful request will return the updated [ModProfile](ModIO.ModProfile).</para>
        /// <param name="modId">Id of the mod profile to update</param>
        /// <param name="parameters">Altered values to submit in the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void EditMod(int modId,
                                   EditModParameters parameters,
                                   Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes a mod profile from the mod.io servers.</summary>
        /// <para>Sets the status of a mod profile to [Deleted](ModIO.ModStatus.Deleted).
        /// This will close the mod profile meaning it cannot be viewed or retrieved via API
        /// requests but will still exist on the servers, allowing it to be restored at a later date.
        /// A successful request will generate a [ModUnavailable](ModIO.ModEventType.ModUnavailable)
        /// [ModEvent](ModIO.ModEvent), and return a confirmation [APIMessage](ModIO.APIMessage)
        /// with the code `204 No Content`. .</para>
        /// <para>**NOTE:** A mod may be permanently removed from the mod.io servers via the
        /// website interface.</para>
        /// <param name="modId">Id of the mod to be deleted</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void DeleteMod(int modId,
                                     Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        /// <summary>Fetches all modfiles for a given mod from the mod.io servers.</summary>
        /// <para>Returns all the modfiles associated matching the filter and pagination paramters
        /// for the the given mod id. A successful request will return a
        /// [ResponseArray](ModIO.API.ResponseArray) of [Modfiles](ModIO.Modfile).</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <para>**NOTE:** If the game requires mod downloads to be initiated via the API, the
        /// address stored in [[ModIO.ModfileLocator.binaryURL]] contained in the
        /// [download locator](ModIO.Modfile.downloadLocator) will expire at the time indicated by
        /// the value of [[Modfile.downloadLocator.dateExpires]] and is thus unwise to cache.</para>
        /// <param name="modId">Id of the mod to retrieve modfiles for</param>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllModfiles(int modId,
                                          RequestFilter filter, PaginationParameters pagination,
                                          Action<ResponseArray<Modfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/files";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetch the a modfile from the mod.io servers.</summary>
        /// <para>Successful request will return a single [Modfile](ModIO.Modfile) matching the
        /// given mod and modfile ids.</para>
        /// <para>**NOTE:** If the game requires mod downloads to be initiated via the API, the
        /// [download url](ModIO.ModfileLocator.binaryURL) contained in the
        /// [download locator](ModIO.Modfile.downloadLocator) will expire at the time indicated by
        /// the value of `Modfile.downloadLocator.dateExpires` and is thus unwise to cache.</para>
        /// <param name="modId">Id of the mod for the request modfile</param>
        /// <param name="modfileId">Id of the requested modfile</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetModfile(int modId, int modfileId,
                                      Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a new modfile and binary to the mod.io servers.</summary>
        /// <para>Successful requests will return the newly created [Modfile](ModIO.Modfile).
        /// It is recommended that an upload tool check mods are stable and free from any critical
        /// issues.</para>
        /// <param name="modId">Destination mod for the new modfile</param>
        /// <param name="parameters">The values to be uploaded with to the new modfile</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddModfile(int modId,
                                      AddModfileParameters parameters,
                                      Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/files";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits changes to an existing modfile.</summary>
        /// <para>This function is only able to update the parameters found in the
        /// [EditModfileParameters](ModIO.API.EditModfileParameters) class. To update a binary,
        /// submitting a new modfile is necessary. A successful request will return the updated
        /// [Modfile](ModIO.Modfile).</para>
        /// <param name="modId">Mod that the modfile belongs to</param>
        /// <param name="modfileId">Modfile that will receive the updated values</param>
        /// <param name="parameters">The values to be updated</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void EditModfile(int modId, int modfileId,
                                       EditModfileParameters parameters,
                                       Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        /// <summary>Upload new media to the game profile on the mod.io servers.</summary>
        /// <remarks>
        /// <para>The profile returned will be the one with the id stored in [[ModIO.APIClient.gameId]].
        /// A successful request will return an [APIMessage](ModIO.APIMessage).</para>
        /// <para>**NOTE:** You can also edit game media directly via the mod.io web interface.
        /// This is the recommended approach.</para>
        /// </remarks>
        /// <param name="parameters">The game media to be added</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddGameMedia(AddGameMediaParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/media";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Adds media to the given mod on the mod.io servers.</summary>
        /// <remarks>
        /// A successful request will return an [[ModIO.APIMessage]].
        /// </remarks>
        /// <param name="modId">Mod to add the media to</param>
        /// <param name="parameters">Media to be added</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddModMedia(int modId,
                                       AddModMediaParameters parameters,
                                       Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/media";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes mod media from a mod on the mod.io servers.</summary>
        /// <remarks>
        /// Deletes images, sketchfab or youtube links from a mod profile as per the values provided.
        /// A successful request will an [APIMessage](ModIO.APIMessage) with the code `204 No
        /// Content`.
        /// </remarks>
        /// <param name="modId">Mod to remove the media from</param>
        /// <param name="parameters">Values to be removed from the mod</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void DeleteModMedia(int modId,
                                          DeleteModMediaParameters parameters,
                                          Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/media";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        /// <summary>Subscribes the authenticated user to a mod.</summary>
        /// <remarks>
        /// <para>A successful request will return the [ModProfile](ModIO.ModProfile) of the newly
        /// subscribed mod.
        /// As users can subscribe to mods via the mod.io web interface it is recommended that any
        /// cached records are updated via [[ModIO.APIClient.GetUserEvents]].</para>
        /// <para>See also: [[ModIO.APIClient.userAuthorizationToken]],
        /// [[ModIO.APIClient.UnsubscribeFromMod]]</para>
        /// </remarks>
        /// <param name="modId">Mod to be subscribed to</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void SubscribeToMod(int modId,
                                          Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    null,
                                                                    null);


            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Unsubscribes the authenticated user from a mod.</summary>
        /// <remarks>
        /// <para>A successful request will return an [APIMessage](ModIO.APIMessage) with the code
        /// `204 No Content`.
        /// As users can unsubscribe from mods via the mod.io web interface it is recommended that
        /// any cached records are updated via [[ModIO.APIClient.GetUserEvents]].</para>
        /// <para>See also: [[ModIO.APIClient.userAuthorizationToken]],
        /// [[ModIO.APIClient.SubscribeToMod]]</para>
        /// </remarks>
        /// <param name="modId">Mod to be unsubscribed from</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void UnsubscribeFromMod(int modId,
                                              Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ EVENT ENDPOINTS ]---------
        /// <summary>Fetches the update events for a given mod.</summary>
        /// <remarks>
        /// <para>A successful request will return a [ResponseArray](ModIO.API.ResponseArray) of
        /// [ModEvents](ModIO.ModEvent).</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// </remarks>
        /// <param name="modId">Mod to fetch events for</param>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetModEvents(int modId,
                                        RequestFilter filter, PaginationParameters pagination,
                                        Action<ResponseArray<ModEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches all the mod update events for the game profile</summary>
        /// <remarks>
        /// <para>Successful request will return a [ResponseArray](ModIO.API.ResponseArray) of
        /// [ModEvents](ModIO.ModEvent).</para>
        /// <para>It is recommended that games and apps poll this endpoint as a method of ensuring
        /// cached data is current. Consider caching the timestamp for the last request sent via
        /// this endpoint to allow its application in this filter parameter in future calls,
        /// return only unacquired updates.</para>
        /// <para>See also: [[ModIO.API.GetModEventsFilterFields]]</para>
        /// </remarks>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllModEvents(RequestFilter filter, PaginationParameters pagination,
                                           Action<ResponseArray<ModEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TAG ENDPOINTS ]---------
        /// <summary>Fetches the tag categories specified by the game profile.</summary>
        /// <remarks>
        /// <para>The response will be a [ResponseArray](ModIO.API.ResponseArray) of
        /// [ModTagCategories](ModIO.ModTagCategory) that define the tagging options for the mod
        /// profiles belonging to the game matching the id stored in [[ModIO.APIClient.gameId]].</para>
        /// <para>See also: [[ModIO.APIClient.gameId]]</para>
        /// </remarks>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllGameTagOptions(Action<ResponseArray<ModTagCategory>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Adds mod tag categories to the game profile.</summary>
        /// <remarks>
        /// <para>A successful request will return an (APIMessage)[ModIO.APIMessage].</para>
        /// <para>**NOTE:** You can also modify the mod tags available via the mod.io web interface.
        /// This is the recommended approach.</para>
        /// <para>See also: [[ModIO.APIClient.gameId]]</para>
        /// </remarks>
        /// <param name="parameters">The mod tags and categories to add</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddGameTagOption(AddGameTagOptionParameters parameters,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/tags";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes mod tags from the game profile.</summary>
        /// <remarks>
        /// <para>This function can delete individual tags or entire categories.
        /// Successful requests will return an (APIMessage)[ModIO.APIMessage] with a code of
        /// `204 No Content`.</para>
        /// <para>**NOTE:** You can also modify the mod tags available via the mod.io web interface.
        /// This is the recommended approach.</para>
        /// <para>See also: [[ModIO.APIClient.gameId]]</para>
        /// </remarks>
        /// <param name="parameters">The mod tags and categories to remove</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void DeleteGameTagOption(DeleteGameTagOptionParameters parameters,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the tags applied to the given mod.</summary>
        /// <remarks>
        /// <para>This is a filterable endpoint that returns all of the tags matching the filter and
        /// pagination parameters. A successful request will return a
        /// (ResponseArray)[ModIO.API.ResponseArray] of the (ModTags)[ModIO.ModTag] applied to the
        /// given mod.</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// </remarks>
        /// <param name="modId">Mod to fetch tags for</param>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetModTags(int modId,
                                      RequestFilter filter, PaginationParameters pagination,
                                      Action<ResponseArray<ModTag>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Adds tags to the given mod.</summary>
        /// <remarks>
        /// <para>The tags added will be matched to the mod tag categories available in the game
        /// profile whereby non-allowed tags will produce an error.
        /// A successful request will return a confirmation [APIMessage](ModIO.APIMessage).</para>
        /// <para>See also: [[ModIO.APIClient.GetAllGameTagOptions]]</para>
        /// </remarks>
        /// <param name="modId">Mod to add tags to</param>
        /// <param name="parameters">Mod tags to be added</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddModTags(int modId, AddModTagsParameters parameters,
                                      Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Removes tags from the given mod.</param>
        /// <remarks>
        /// <para>A successful request will return a confirmation [APIMessage](ModIO.APIMessage)
        /// with the code `204 No Content`.</para>
        /// </remarks>
        /// <param name="modId">Mod to remove tags from</param>
        /// <param name="parameters">Mod tags to be removed</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void DeleteModTags(int modId,
                                         DeleteModTagsParameters parameters,
                                         Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ RATING ENDPOINTS ]---------
        /// <summary>Submits a user's rating for a mod.</summary>
        /// <remarks>
        /// <para>Each user can supply either one postiive or one negative rating for a mod.
        /// Subsequent ratings will override any previous ratings. Successful request will return a
        /// confirmation [APIMessage](ModIO.APIMessage).</para>
        /// <para>See also: [[ModIO.ModProfile.ratingSummary]]</para>
        /// </remarks>
        /// <param name="modId">Mod to submit the rating for</param>
        /// <param name="parameters">Rating data to be submitted</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddModRating(int modId, AddModRatingParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/ratings";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ METADATA ENDPOINTS ]---------
        /// <summary>Fetches all the KVP metadata for a mod.</summary>
        /// <remarks>
        /// <para>Any key-value pair metadata that has been stored with a mod profile will be
        /// retrieved by this function. A successful request will return a
        /// [ResponseArray](ModIO.API.ResponseArray) of (MetadataKVPs)[[ModIO.MetadataKVP]].</para>
        /// <para>See also: [[ModIO.ModProfile.metadataBlob]]</para>
        /// </remarks>
        /// <param name="modId">Mod to retrieve metadata KVP for</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllModKVPMetadata(int modId,
                                                PaginationParameters pagination,
                                                Action<ResponseArray<MetadataKVP>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submit KVP Metadata to a mod.</summary>
        /// <para>A successful request will return a confirmation [APIMessage](ModIO.APIMessage).
        /// It is recommended that the mod upload tool developed defines and submits metadata behind
        /// the scenes, to avoid incorrect values affecting gameplay.</para>
        /// <param name="modId">Mod to submit tags from</param>
        /// <param name="parameters">KVP data to add to the mod</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddModKVPMetadata(int modId, AddModKVPMetadataParameters parameters,
                                             Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes KVP metadata from a mod.</summary>
        /// <para>Calling this function submits the delete request to the servers and if successful
        /// returns an [APIMessage](ModIO.APIMessage) with the code: `204 No Content`.</para>
        /// <param name="modId">Mod to submit tags from</param>
        /// <param name="parameters">KVP data to remove from the mod</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void DeleteModKVPMetadata(int modId, DeleteModKVPMetadataParameters parameters,
                                                Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        /// <summary>Fetches all the dependencies for a mod.</summary>
        /// <param name="modId">Mod to fetch dependencies for</param>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllModDependencies(int modId,
                                                 RequestFilter filter, PaginationParameters pagination,
                                                 Action<ResponseArray<ModDependency>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits new dependencides for a mod.</summary>
        /// <param name="modId">Mod to add dependencies to</param>
        /// <param name="parameters">Dependency data to add</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddModDependencies(int modId, AddModDependenciesParameters parameters,
                                              Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Removes dependencides from a mod.</summary>
        /// <param name="modId">Mod to add dependencies to</param>
        /// <param name="parameters">Dependency data to remove</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void DeleteModDependencies(int modId, DeleteModDependenciesParameters parameters,
                                                 Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TEAM ENDPOINTS ]---------
        /// <summary>Fetches the team members for a mod.</summary>
        /// <para>Any users that are listed as team members for the given mod will be retrieved.
        /// A successful request will return a [ResponseArray](ModIO.API.ResponseArray) of
        /// [ModTeamMembers](ModIO.ModTeamMembe).</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <param name="modId">Mod to team members for</param>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllModTeamMembers(int modId,
                                                RequestFilter filter, PaginationParameters pagination,
                                                Action<ResponseArray<ModTeamMember>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a new team member to a mod.</summary>
        /// <para>Calling this function submits the data to the mod.io servers and if successful,
        /// will generate a [ModTeamChanged](ModIO.ModEventType.ModTeamChanged)
        /// [ModEvent](ModIO.ModEvent).</para>
        /// <param name="modId">Mod to add the team member to</param>
        /// <param name="parameters">The values to be uploaded with to the new mod profile</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void AddModTeamMember(int modId, AddModTeamMemberParameters parameters,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits changes to a mod team member.</summary>
        /// <para>Calling this function submits updates to the mod.io servers for the given team
        /// member id and if successful returns a confirmation [APIMessage](ModIO.APIMessage).</para>
        /// <param name="modId">Mod the team member belongs to</param>
        /// <param name="teamMemberId">Team member to be updated</param>
        /// <param name="parameters">The values to be updated in the team member profile</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void UpdateModTeamMember(int modId, int teamMemberId,
                                               UpdateModTeamMemberParameters parameters,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                   parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a delete request for a mod team member.</summary>
        /// <para>A successful request will revoke a user's access rights to the mod, generate a
        /// [ModTeamChanged](ModIO.ModEventType.ModTeamChanged) [ModEvent](ModIO.ModEvent), and
        /// return an [APIMessage](ModIO.APIMessage) with the code: `204 No Content`.</para>
        /// <param name="modId">Mod the team member belongs to</param>
        /// <param name="teamMemberId">Team member to be deleted</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void DeleteModTeamMember(int modId, int teamMemberId,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        /// <summary>Fetches all the comments for a mod.</summary>
        /// <param name="modId">Mod to fetch comments for</param>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllModComments(int modId,
                                             RequestFilter filter, PaginationParameters pagination,
                                             Action<ResponseArray<ModComment>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/comments";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches a mod comment by id.</summary>
        /// <param name="modId">Mod the comment belongs to</param>
        /// <param name="commentId">Comment to retrieve</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetModComment(int modId, int commentId,
                                         Action<ModComment> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // NOTE(@jackson): Untested
        /// <summary>Submits a delete request for a mod comment.</summary>
        /// <param name="modId">Mod the comment belongs to</param>
        /// <param name="commentId">Comment to remove</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void DeleteModComment(int modId, int commentId,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ USER ENDPOINTS ]---------
        /// <summary>Fetches the owner for a mod resource.</summary>
        /// <para>A successful request returns the [UserProfile](ModIO.UserProfile) for the original
        /// submitter of a resource. Because the mods and games can be managed by teams of users,
        /// and the team endpoints are a more reliable source of information.</para>
        /// <param name="resourceType">The type of resource to query</param>
        /// <param name="resourceID">Resource to fetch the owner of</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetResourceOwner(APIResourceType resourceType, int resourceID,
                                            Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/general/owner";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("resource_type", resourceType.ToString().ToLower()),
                StringValueParameter.Create("resource_id", resourceID),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    valueFields,
                                                                    null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches all the user profiles on mod.io.</summary>
        /// <para>A successful request will return a [ResponseArray](ModIO.API.ResponseArray) of
        /// [UserProfiles](ModIO.UserProfile) that match the filtering and pagination parameters
        /// supplied.</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAllUsers(RequestFilter filter, PaginationParameters pagination,
                                       Action<ResponseArray<UserProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/users";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches a user profile from the mod.io servers.</summary>
        /// <para>A successful request will return the [UserProfile](ModIO.UserProfile) that matches
        /// the given id.</para>
        /// <param name="userId">Id of the user profile to retrieve</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetUser(int userId,
                                   Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/users/" + userId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // NOTE(@jackson): Untested
        /// <summary>Submits a report against a mod/resource on mod.io.</summary>
        /// <para>**NOTE:** Reports can also be <a href="https://mod.io/report/widget">submitted
        /// online</a>. Additionally, the <a href="https://mod.io/terms/widget">mod.io terms of use</a>
        /// contain details on which content is and isn't acceptable.</para>
        /// <param name="parameters">Report data to submit</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void SubmitReport(SubmitReportParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/report";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ ME ENDPOINTS ]---------
        /// <summary>Fetches the user profile for the authenticated user.</summary>
        /// <para>A successful request will return the [UserProfile](ModIO.UserProfile) that matches
        /// the token stored in [[ModIO.APIClient.userAuthorizationToken]].</para>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetAuthenticatedUser(Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the subscriptions for the authenticated user.</summary>
        /// <para>A successful request will return a [ResponseArray](ModIO.ResponseArray) of
        /// [ModProfiles](ModIO.ModProfile) that the authenticated user is subscribed to and matches
        /// the filter and pagination parameters.</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <para>See also: [[ModIO.APIClient.userAuthorizationToken]].</para>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetUserSubscriptions(RequestFilter filter, PaginationParameters pagination,
                                                Action<ResponseArray<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/subscribed";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetch the update events for the authenticated user.</summary>
        /// <para>A successful request returns a [ResponseArray](ModIO.API.ResponseArray) of
        /// [UserEvents](ModIO.UserEvent) that summarize the updates made to the authenticated user,
        /// and that match the filter and pagination parameters.</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <para>See also: [[ModIO.APIClient.userAuthorizationToken]].</para>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetUserEvents(RequestFilter filter, PaginationParameters pagination,
                                         Action<ResponseArray<UserEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/events";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the games that the authenticated user is a team member of.</summary>
        /// <para>A successful request returns a [ResponseArray](ModIO.API.ResponseArray) of all the
        /// [GameProfiles](ModIO.GameProfile) that the authenticated user added to the mod.io
        /// servers or is a team member of.</para>
        /// <para>See also: [[ModIO.APIClient.userAuthorizationToken]].</para>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetUserGames(Action<ResponseArray<GameProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/games";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the mods that the authenticated user is a team member of.</summary>
        /// <para>A successful request returns a [ResponseArray](ModIO.API.ResponseArray) of all the
        /// [ModProfiles](ModIO.ModProfile) that the authenticated user added to the mod.io
        /// servers or is a team member of, and matches the filter and pagination parameters.</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <para>See also: [[ModIO.APIClient.userAuthorizationToken]].</para>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetUserMods(RequestFilter filter, PaginationParameters pagination,
                                       Action<ResponseArray<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/mods";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the modfiles that the authenticated user uploaded.</summary>
        /// <para>A successful request returns a [ResponseArray](ModIO.API.ResponseArray) of all the
        /// [Modfiles](ModIO.Modfile) that the authenticated user uploaded to the mod.io
        /// servers, and matches the filter and pagination parameters.</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <para>See also: [[ModIO.APIClient.userAuthorizationToken]].</para>
        /// <param name="filter">The filter parameters to be applied to the request</param>
        /// <param name="pagination">The pagination parameters to be applied to the request</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        public static void GetUserModfiles(RequestFilter filter, PaginationParameters pagination,
                                           Action<ResponseArray<Modfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/files";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
    }
}
