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
namespace ModIO
{
    /// <summary>
    /// This class provides a native wrapper for each of the endpoints available via the mod.io web
    /// API.
    /// </summary>
    public static class APIClient
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>
        /// Denotes which version of the mod.io web API that this class is compatible with.
        /// </summary>
        /// <remarks>
        /// This value forms part of the web API URL and should not be changed.
        /// </remarks>
        public const string API_VERSION = "v1";

        /// <summary>
        /// The base URL for the web API
        /// </summary>
        #if DEBUG
        public static readonly string API_URL = (GlobalSettings.USE_TEST_SERVER
                                                 ? "https://api.test.mod.io/"
                                                 : "https://api.mod.io/") + API_VERSION;
        #else
        public const string API_URL = "https://api.mod.io/" + API_VERSION;
        #endif

        /// <summary>
        /// Collection of the HTTP request header keys used by Unity
        /// </summary>
        /// <remarks>
        /// Used almost exclusively for debugging requests.
        /// </remarks>
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
        /// <summary>
        /// Collection of the HTTP request header keys used by mod.io
        /// </summary>
        /// <remarks>
        /// Used almost exclusively for debugging requests.
        /// </remarks>
        public static readonly string[] MODIO_REQUEST_HEADER_KEYS = new string[]
        {
            "Authorization",
            "Accept-Language",
        };

        // ---------[ MEMBERS ]---------
        /// <summary>
        /// Game ID that the APIClient should use when contacting the API
        /// </summary>
        public static int gameId = GlobalSettings.GAME_ID;

        /// <summary>
        /// Game API that the APIClient should use when contacting the API
        /// </summary>
        public static string gameAPIKey = GlobalSettings.GAME_APIKEY;

        /// <summary>
        /// The user's OAuthToken that the APIClient should include when contacting the API
        /// </summary>
        public static string userAuthorizationToken = null;

        /// <summary>
        /// The language code that designates requested language for the API response messages
        /// </summary>
        /// <remarks>
        /// Currently supported languages and codes are listed in the mod.io documentation under
        /// <a href="https://docs.mod.io/#localization">Localization</a>.
        /// </remarks>
        public static string languageCode = "en";

        // ---------[ DEBUG ASSERTS ]---------
        /// <summary>
        /// Asserts that the required authorization data for making API requests is set.
        /// </summary>
        /// <remarks>
        /// Only asserts that the values have been set, but **does not check the correctness** of
        /// those values.
        /// </remarks>
        /// <param name="isUserTokenRequired">Whether to assert that [[ModIO.APIClient.userAuthorizationToken]] is set</param>
        /// <returns>True if all the neccessary authorization details for an API request have been set</returns>
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
        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint request that requires no user authentication
        /// </summary>
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

        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint 'GET' request
        /// </summary>
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

        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint 'PUT' request
        /// </summary>
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

        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint 'POST' request
        /// </summary>
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

        /// <summary>
        /// Generates a prefilled <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequest.html">
        /// UnityWebRequest</a> for a mod.io API endpoint 'DELETE' request
        /// </summary>
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

        /// <summary>
        /// Sends the request and attaches the callbacks to the
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequestAsyncOperation.html">
        /// UnityWebRequestAsyncOperation</a> that is created.
        /// </summary>
        /// <param name="webRequest">The request to send and attach the callback functions to</param>
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

        /// <summary>
        /// Sends the request and attaches the callbacks to the
        /// <a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Networking.UnityWebRequestAsyncOperation.html">
        /// UnityWebRequestAsyncOperation</a> that is created, and attempts to parse the response.
        /// </summary>
        /// <param name="webRequest">The request to send and attach the callback functions to</param>
        /// <param name="successCallback">Action to execute if the request succeeds</param>
        /// <param name="errorCallback">Action to execute if the request returns an error</param>
        /// <remarks>See also: [[ModIO.APIClient.SendRequest]]</remarks>
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
        /// <summary>
        /// Requests a login code be sent to the user's email address.
        /// </summary>
        /// <remarks>
        /// <para>For further information see the <a href="https://docs.mod.io/#authentication"> mod.io
        /// filtering documentation</a>.</para>
        /// <para>See also: [[ModIO.APIClient.GetOAuthToken]]</para>
        /// </remarks>
        /// <param name="emailAddress">The user's email address to receive the security code</param>
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

        /// <summary>
        /// Wrapper object for [[ModIO.APIClient.GetOAuthToken]] requests
        /// </summary>
        [System.Serializable]
        private struct AccessTokenObject { public string access_token; }

        /// <summary>
        /// Requests the user's application OAuthToken that matches the single use security code.
        /// </summary>
        /// <remarks>
        /// <para>For further information see the <a href="https://docs.mod.io/#authentication"> mod.io
        /// filtering documentation</a>.</para>
        /// <para>See also: [[ModIO.APIClient.SendSecurityCode]]</para>
        /// </remarks>
        /// <param name="securityCode">The security code sent to the user's email address.</param>
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
        /// <summary>
        /// Fetches all games profiles from the mod.io servers matching the filter and pagination
        /// parameters.
        /// </summary>
        /// <remarks>
        /// <para>Successful requests return an [[ModIO.API.ResponseArray]] of [[ModIO.GameProfile]].</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation
        /// </a> for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// </remarks>
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

        /// <summary>
        /// Gets the game profile matching the stored id.
        /// </summary>
        /// <remarks>
        /// The profile returned will be the one with the id stored in [[ModIO.APIClient.gameId]].
        /// Successful request will return a single [[ModIO.GameProfile]].
        /// </remarks>
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

        /// <summary>
        /// Update the game profile for the game id stored in [[ModIO.APIClient.gameId]].
        /// </summary>
        /// <remarks>
        /// <para>This function only supports the game profile fields listed in the
        /// [[ModIO.API.EditGameParameters]] class.  To update the icon, logo or header fields use
        /// [[ModIO.APIClient.AddGameMedia]]. A successful request will return updated
        /// [[ModIO.GameProfile]].</para>
        /// <para>**NOTE:** You can also edit a game profile directly via the mod.io web interface.
        /// This is the recommended approach.</para>
        /// </remarks>
        /// <param name="parameters">The updated values for the game profile</param>
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
        /// <summary>
        /// Fetches all mods for the game id stored in [[ModIO.APIClient.gameId]] matching the
        /// filtering and pagination parameters supplied.
        /// </summary>
        /// <remarks>
        /// <para>Successful request will the results of the query as a [[ModIO.API.ResponseArray]]
        /// of [[ModIO.ModProfile]]</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// </remarks>
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

        /// <summary>
        /// Fetches the [[ModIO.ModProfile]] for the mod with the given id.
        /// </summary>
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

        /// <summary>
        /// Creates a new mod profile on the mod.io servers.
        /// </summary>
        /// <remarks>
        /// <para>The new mod profile is created as a mod belonging to the game id stored in
        /// [[ModIO.APIClient.gameId]]. Successful requests will return the newly created
        /// [[ModIO.ModProfile]].</para>
        /// <para>By default new mods are [NotAccepted](ModIO.ModStatus.NotAccepted) and
        /// [Public](ModIO.ModVisibility.Public). They can only be [accepted](ModIO.ModStatus.Accepted)
        /// and made available via the API once a [Modfile](ModIO.Modfile) has been uploaded.
        /// Media, Metadata Key Value Pairs and Dependencies can also be added after a mod profile
        /// is created.</para>
        /// </remarks>
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

        /// <summary>
        /// Submits changes to an existing mod profile.
        /// </summary>
        /// <remarks>
        /// <para>This function is only able to update the parameters found in the
        /// [EditModParameters](ModIO.API.EditModParameters) class. To update the logo or media
        /// associated with a mod, use [[ModIO.APIClient.AddModMedia]]. The same applies to
        /// Modfiles, Metadata Key Value Pairs and Dependencies which are all managed via other
        /// endpoints. A successful request will return the updated [ModProfile](ModIO.ModProfile).</para>
        /// </remarks>
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

        /// <summary>
        /// Deletes a mod profile from the mod.io servers.
        /// </summary>
        /// <remarks>
        /// <para>Sets the status of a mod profile to [Deleted](ModIO.ModStatus.Deleted).
        /// This will close the mod profile meaning it cannot be viewed or retrieved via API
        /// requests but will still exist on the servers, allowing it to be restored at a later date.
        /// A successful request will return an [APIMessage](ModIO.APIMessage) with a [code](ModIO.APIMessage.code)
        /// of 204 create a [ModUnavailable](ModIO.ModEventType.ModUnavailable)
        /// [ModEvent](ModIO.ModEvent).</para>
        /// <para>**NOTE:** A mod may only be permanently removed from the mod.io servers via the
        /// website interface.</para>
        /// </remarks>
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
        /// <summary>
        /// Fetches the modfiles for a given mod.
        /// </summary>
        /// <remarks>
        /// <para>Returns all the modfiles associated matching the filter and pagination paramters
        /// for the the given mod id. Successful request will return a
        /// [ResponseArray](ModIO.API.ResponseArray) of [Modfiles](ModIO.Modfile).</para>
        /// <para>See the <a href="https://docs.mod.io/#filtering">mod.io filtering documentation</a>
        /// for more comprehensive explanation of the filtering and pagination parameters.</para>
        /// <para>**NOTE:** If the game requires mod downloads to be initiated via the API, the
        /// [download url](ModIO.ModfileLocator.binaryURL) contained in the
        /// [download locator](ModIO.Modfile.downloadLocator) will expire at the time indicated by
        /// the value of `Modfile.downloadLocator.dateExpires` and is thus unwise to cache.</para>
        /// </remarks>
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

        /// <summary>
        /// Fetch the a modfile's data from the server.
        /// </summary>
        /// <remarks>
        /// <para>Successful request will return a single [Modfile](ModIO.Modfile) matching the
        /// given mod and modfile ids.</para>
        /// <para>**NOTE:** If the game requires mod downloads to be initiated via the API, the
        /// [download url](ModIO.ModfileLocator.binaryURL) contained in the
        /// [download locator](ModIO.Modfile.downloadLocator) will expire at the time indicated by
        /// the value of `Modfile.downloadLocator.dateExpires` and is thus unwise to cache.</para>
        /// </remarks>
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

        /// <summary>
        /// Uploads a new modfile to the mod.io servers.
        /// </summary>
        /// <remarks>
        /// Successful requests will return the newly created [Modfile](ModIO.Modfile).
        /// It is recommended that an upload tool check mods are stable and free from any critical
        /// issues.
        /// </remarks>
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

        /// <summary>
        /// Submits changes to an existing modfile.
        /// </summary>
        /// <remarks>
        /// <para>This function is only able to update the parameters found in the
        /// [EditModfileParameters](ModIO.API.EditModfileParameters) class. To update a binary,
        /// submitting a new modfile is necessary. A successful request will return the updated
        /// [Modfile](ModIO.Modfile).</para>
        /// </remarks>
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
        /// <summary>
        /// Submit a positive or negative rating for a mod. Each user can supply only one rating for
        /// a mod, subsequent ratings will override the old value. Successful request will return an
        /// [[ModIO.APIMessage]].
        /// <remarks>
        /// You can order mods by their rating, and view their rating in the
        /// [[ModIO.ModProfile]].
        /// </remarks>
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
        /// <summary>
        /// Get all metadata stored by the game developer for this mod as searchable key value
        /// pairs. Successful request will return a [[ModIO.API.ResponseArray]] of
        /// [[ModIO.MetadataKVP]].
        /// <remarks>
        /// Metadata can also be stored to [[ModIO.ModProfile.metadataBlob]].
        /// </remarks>
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

        /// <summary>
        /// Add metadata for this mod as searchable key value pairs. Metadata is useful to define
        /// how a mod works, or other information you need to display and manage the mod. Successful
        /// request will return an [[ModIO.APIMessage]].
        /// </summary>
        /// <example>
        /// A mod might change gravity and the rate of fire of weapons, you could define these
        /// properties as key value pairs.
        /// </example>
        /// <remarks>
        /// We recommend the mod upload tool you create defines and submits metadata behind the
        /// scenes, because if these settings affect gameplay, invalid information may cause
        /// problems.
        /// </remarks>
        public static void AddModKVPMetadata(int modId, AddModKVPMetadataParameters parameters,
                                             Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Delete key value pairs metadata defined for this mod. Successful request will return
        /// 204 No Content.
        /// </summary>
        public static void DeleteModKVPMetadata(int modId, DeleteModKVPMetadataParameters parameters,
                                                Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
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
        // Add Mod Dependencies
        public static void AddModDependencies(int modId, AddModDependenciesParameters parameters,
                                              Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Dependencies
        public static void DeleteModDependencies(int modId, DeleteModDependenciesParameters parameters,
                                                 Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TEAM ENDPOINTS ]---------
        /// <summary>
        /// Get all users that are part of a mod team. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.ModTeamMember]]. We
        /// recommend reading the <a href="https://docs.mod.io/#filtering">filtering documentation
        /// </a> to return only the records you want.
        /// </summary>
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

        /// <summary>
        /// Add a user to a mod team. Successful request will return an
        /// [[ModIO.APIMessage]] and fire a
        /// [[ModIO.ModEventType.ModTeamChanged]] [[ModIO.ModEvent]].
        /// </summary>
        public static void AddModTeamMember(int modId, AddModTeamMemberParameters parameters,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Update a mod team members details. Successful request will return an
        /// [[ModIO.APIMessage]].
        /// </summary>
        public static void UpdateModTeamMember(int modId, int teamMemberId,
                                               UpdateModTeamMemberParameters parameters,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                   parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Delete a user from a mod team. This will revoke their access rights if they are not the
        /// original creator of the resource. Successful request will return 204 No Content and fire
        /// a [[ModIO.ModEventType.ModTeamChanged]] [[ModIO.ModEvent]].
        public static void DeleteModTeamMember(int modId, int teamMemberId,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
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
        // Get Mod Comment
        public static void GetModComment(int modId, int commentId,
                                         Action<ModComment> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Comment
        // NOTE(@jackson): Untested
        public static void DeleteModComment(int modId, int commentId,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/games/" + APIClient.gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ USER ENDPOINTS ]---------
        /// <summary>
        /// Get the user that is the original submitter of a resource. Successful request will
        /// return a single [[ModIO.UserProfile]].
        /// </summary>
        /// <remarks>
        /// Mods and games can be managed by teams of users, for the most accurate information you
        /// should use the Team endpoints.
        /// </remarks>
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

        /// <summary>
        /// Get all users registered on mod.io. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.UserProfile]]. We recommend
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        public static void GetAllUsers(RequestFilter filter, PaginationParameters pagination,
                                       Action<ResponseArray<UserProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/users";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get a user. Successful request will return a single [[ModIO.UserProfile]].
        /// </summary>
        public static void GetUser(int userID,
                                   Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/users/" + userID;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // Submit Report
        // NOTE(@jackson): Untested
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
        /// <summary>
        /// Get the authenticated user details. Successful request will return a single
        /// [[ModIO.UserProfile]].
        /// </summary>
        public static void GetAuthenticatedUser(Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all mod's the authenticated user is subscribed to. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.ModProfile]]. We recommend
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        public static void GetUserSubscriptions(RequestFilter filter, PaginationParameters pagination,
                                                Action<ResponseArray<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/subscribed";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get events that have been fired specific to the user. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.UserEvent]]. We recommend
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        public static void GetUserEvents(RequestFilter filter, PaginationParameters pagination,
                                         Action<ResponseArray<UserEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/events";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all games the authenticated user added or is a team member of. Successful request
        /// will return a [[ModIO.API.ResponseArray]] of [[ModIO.GameProfile]].
        /// We recommend reading the <a href="https://docs.mod.io/#filtering">filtering
        /// documentation</a> to return only the records you want.
        /// </summary>
        public static void GetUserGames(Action<ResponseArray<GameProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/games";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all mods the authenticated user added or is a team member of. Successful request
        /// will return a [[ModIO.API.ResponseArray]] of [[ModIO.ModProfile]].
        /// We recommended reading the <a href="https://docs.mod.io/#filtering">filtering
        /// documentation</a> to return only the records you want.
        /// </summary>
        public static void GetUserMods(RequestFilter filter, PaginationParameters pagination,
                                       Action<ResponseArray<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "/me/mods";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>
        /// Get all modfiles the authenticated user uploaded. Successful request will return a
        /// [[ModIO.API.ResponseArray]] of [[ModIO.Modfile]]. We recommend
        /// reading the <a href="https://docs.mod.io/#filtering">filtering documentation</a> to
        /// return only the records you want.
        /// </summary>
        /// <param name="filter">The filter to be applied to the request</param>
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
