using ModIO.API;

using System;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

using Debug = UnityEngine.Debug;
using WWWForm = UnityEngine.WWWForm;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using UnityWebRequestAsyncOperation = UnityEngine.Networking.UnityWebRequestAsyncOperation;


namespace ModIO
{
    /// <summary>An interface for sending requests to the mod.io servers.</summary>
    public static class APIClient
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>Denotes the version of the mod.io web API that this class is compatible with.</summary>
        public const string API_VERSION = "v1";

        /// <summary>URL for the test server</summary>
        public const string API_URL_TESTSERVER = "https://api.test.mod.io/";

        /// <summary>URL for the production server</summary>
        public const string API_URL_PRODUCTIONSERVER = "https://api.mod.io/";


        /// <summary>Collection of the HTTP request header keys used by Unity.</summary>
        public static readonly string[] UNITY_REQUEST_HEADER_KEYS = new string[]
        {
            // - UNIVERSAL -
            "accept-charset",
            "access-control-request-headers",
            "access-control-request-method",
            "connection",
            "content-length",
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
            "content-type",
            "x-unity-version",
            "user-agent",
        };

        /// <summary>Collection of the HTTP request header keys used by mod.io.</summary>
        public static readonly string[] MODIO_REQUEST_HEADER_KEYS = new string[]
        {
            "authorization",
            "accept-language",
        };

        // ---------[ SETTINGS ]---------
        /// <summary>Requested language for the API response messages.</summary>
        public static string languageCode = "en";

        // ---------[ DEBUGGING ]---------
        /// <summary>Pairing of the WWWForm field types.</summary>
        private struct DebugFormData
        {
            public IEnumerable<StringValueParameter> strings;
            public IEnumerable<BinaryDataParameter> binaryData;
        }

        /// <summary>Mapping of UnityWebRequests to their form data.</summary>
        private static Dictionary<UnityWebRequest, DebugFormData> webRequestFormData = new Dictionary<UnityWebRequest, DebugFormData>();

        // ---------[ DEBUG FUNCTIONALITY ]---------
        /// <summary>Asserts that the required authorization data for making API requests is set.</summary>
        public static bool AssertAuthorizationDetails(bool isUserTokenRequired)
        {
            if(PluginSettings.data.gameId <= 0
               || String.IsNullOrEmpty(PluginSettings.data.gameAPIKey))
            {
                Debug.LogError("[mod.io] No API requests can be executed without a"
                               + " valid Game Id and Game API Key. These need to be"
                               + " saved into the Plugin Settings (mod.io > Edit Settings"
                               + " before any requests can be sent to the API.");
                return false;
            }

            if(isUserTokenRequired)
            {
                if(String.IsNullOrEmpty(UserAuthenticationData.instance.token))
                {
                    Debug.LogError("[mod.io] API request to modification or User-specific"
                                   + " endpoints cannot be made without first setting the"
                                   + " User Authorization Data instance with a valid token.");
                    return false;
                }
                else if(UserAuthenticationData.instance.wasTokenRejected)
                {
                    Debug.LogWarning("[mod.io] An API request is being made with a UserAuthenticationData"
                                     + " token that has been flagged as previously rejected."
                                     + " A check on UserAuthenticationData.instance.IsTokenValid"
                                     + " should be made prior to making user-authrization calls.");
                }
            }

            return true;
        }

        /// <summary>Generates a debug-friendly string of web request details.</summary>
        public static string GenerateRequestDebugString(UnityWebRequest webRequest)
        {
            string requestHeaders = "";
            List<string> requestKeys = new List<string>(UNITY_REQUEST_HEADER_KEYS);
            requestKeys.AddRange(MODIO_REQUEST_HEADER_KEYS);

            foreach(string headerKey in requestKeys)
            {
                string headerValue = webRequest.GetRequestHeader(headerKey);
                if(headerValue != null)
                {
                    if(headerKey.ToUpper() == "AUTHORIZATION")
                    {
                        requestHeaders += "\n  " + headerKey + ": " + headerValue.Substring(0, 6);

                        if(headerValue.Length > 8) // Contains more than "Bearer "
                        {
                            requestHeaders += " [OAUTH TOKEN]";
                        }
                        else // NULL
                        {
                            requestHeaders += " [NULL]";
                        }
                    }
                    else
                    {
                        requestHeaders += "\n  " + headerKey + ": " + headerValue;
                    }
                }
            }

            DebugFormData formData;
            if(webRequestFormData.TryGetValue(webRequest, out formData))
            {
                var formDataString = new System.Text.StringBuilder();

                if(formData.strings != null)
                {
                    foreach(StringValueParameter svp in formData.strings)
                    {
                        formDataString.Append("\n  " + svp.key + ": " + svp.value);
                    }
                }

                if(formData.binaryData != null)
                {
                    foreach(BinaryDataParameter bdp in formData.binaryData)
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

                return("Endpoint: " + webRequest.url
                       + "\nMethod: " + webRequest.method.ToUpper()
                       + "\nHeaders: " + requestHeaders
                       + "\nForm Data: " + formDataString.ToString());
            }
            else
            {
                return("Endpoint: " + webRequest.url
                       + "\nMethod: " + webRequest.method.ToUpper()
                       + "\nHeaders: " + requestHeaders);
            }
        }

        // ---------[ REQUEST HANDLING ]---------
        /// <summary>Generates the object for a basic mod.io server request.</summary>
        public static UnityWebRequest GenerateQuery(string endpointURL,
                                                    string filterString,
                                                    APIPaginationParameters pagination)
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

            UnityWebRequest webRequest = UnityWebRequest.Get(queryURL);

            if(UserAuthenticationData.instance.IsTokenValid)
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + UserAuthenticationData.instance.token);
            }
            else
            {
                webRequest.url += "&api_key=" + PluginSettings.data.gameAPIKey;
            }

            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io GET request.</summary>
        public static UnityWebRequest GenerateGetRequest(string endpointURL,
                                                         string filterString,
                                                         APIPaginationParameters pagination)
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
            webRequest.SetRequestHeader("Authorization", "Bearer " + UserAuthenticationData.instance.token);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io PUT request.</summary>
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
            webRequest.SetRequestHeader("Authorization", "Bearer " + UserAuthenticationData.instance.token);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(PluginSettings.data.logAllRequests)
            {
                // Setup form data logging
                DebugFormData formData = new DebugFormData()
                {
                    strings = valueFields,
                    binaryData = null,
                };
                webRequestFormData.Add(webRequest, formData);
            }
            #endif

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io POST request.</summary>
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
            webRequest.SetRequestHeader("Authorization", "Bearer " + UserAuthenticationData.instance.token);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(PluginSettings.data.logAllRequests)
            {
                // Setup form data logging
                DebugFormData formData = new DebugFormData()
                {
                    strings = valueFields,
                    binaryData = dataFields,
                };
                webRequestFormData.Add(webRequest, formData);

            }
            #endif

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io DELETE request.</summary>
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
            webRequest.SetRequestHeader("Authorization", "Bearer " + UserAuthenticationData.instance.token);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(PluginSettings.data.logAllRequests)
            {
                // Setup form data logging
                DebugFormData formData = new DebugFormData()
                {
                    strings = valueFields,
                    binaryData = null,
                };
                webRequestFormData.Add(webRequest, formData);
            }
            #endif

            return webRequest;
        }

        /// <summary>A wrapper for sending a UnityWebRequest and attaching callbacks.</summary>
        public static void SendRequest(UnityWebRequest webRequest,
                                       Action successCallback,
                                       Action<WebRequestError> errorCallback)
        {
            // - Start Request -
            UnityWebRequestAsyncOperation requestOperation = webRequest.SendWebRequest();

            #if DEBUG
            if(PluginSettings.data.logAllRequests)
            {
                Debug.Log("REQUEST SENT\n"
                          + APIClient.GenerateRequestDebugString(webRequest)
                          + "\n");
            }
            #endif

            requestOperation.completed += (operation) =>
            {
                #if DEBUG
                if(PluginSettings.data.logAllRequests)
                {
                    if(webRequest.isNetworkError || webRequest.isHttpError)
                    {
                        WebRequestError.LogAsWarning(WebRequestError.GenerateFromWebRequest(webRequest));
                    }
                    else
                    {
                        var headerString = new System.Text.StringBuilder();
                        var responseHeaders = webRequest.GetResponseHeaders();
                        if(responseHeaders != null
                           && responseHeaders.Count > 0)
                        {
                            headerString.Append("\n");
                            foreach(var kvp in responseHeaders)
                            {
                                headerString.AppendLine("- [" + kvp.Key + "] " + kvp.Value);
                            }
                        }
                        else
                        {
                            headerString.Append(" NONE");
                        }

                        var responseTimeStamp = ServerTimeStamp.Now;
                        string logString = ("RESPONSE RECEIVED\n"
                                            + "------[ Request Data ]------\n"
                                            + APIClient.GenerateRequestDebugString(webRequest)
                                            + "\n------[ Response Data ]------"
                                            + "\nTime Stamp: " + responseTimeStamp + " ("
                                            + ServerTimeStamp.ToLocalDateTime(responseTimeStamp) + ")"
                                            + "\nResponse Headers: " + headerString.ToString()
                                            + "\nResponse Code: " + webRequest.responseCode
                                            + "\nResponse Error: " + webRequest.error
                                            + "\nResponse Raw: " + webRequest.downloadHandler.text
                                            + "\n");
                        Debug.Log(logString);
                    }
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

                #if DEBUG
                APIClient.webRequestFormData.Remove(webRequest);
                #endif
            };
        }

        /// <summary>A wrapper for sending a web request to mod.io and parsing the result.</summary>
        public static void SendRequest<T>(UnityWebRequest webRequest,
                                          Action<T> successCallback,
                                          Action<WebRequestError> errorCallback)
        {
            Action processResponse = () =>
            {
                // TODO(@jackson): Don't call success on exception
                if(successCallback != null)
                {
                    T response = default(T);

                    try
                    {
                        response = JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
                    }
                    catch(Exception e)
                    {
                        // TODO(@jackson): Error!
                        Debug.LogWarning("[mod.io] Failed to convert response into " + typeof(T).ToString() + " representation\n\n"
                                         + Utility.GenerateExceptionDebugString(e));
                    }

                    successCallback(response);
                }
            };

            APIClient.SendRequest(webRequest,
                                  processResponse,
                                  errorCallback);
        }


        // ---------[ AUTHENTICATION ]---------
        /// <summary>Wrapper object for [[ModIO.APIClient.GetOAuthToken]] requests.</summary>
        [System.Serializable]
        #pragma warning disable 0649
        private struct AccessTokenObject { public string access_token; }
        #pragma warning restore 0649

        /// <summary>Generates the object for a mod.io Authentication request.</summary>
        public static UnityWebRequest GenerateAuthenticationRequest(string endpointURL,
                                                                    string authenticationKey,
                                                                    string authenticationValue)
        {
            APIClient.AssertAuthorizationDetails(false);

            WWWForm form = new WWWForm();
            form.AddField("api_key", PluginSettings.data.gameAPIKey);
            form.AddField(authenticationKey, authenticationValue);

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);

            #if DEBUG
            if(PluginSettings.data.logAllRequests)
            {
                // Setup form data logging
                DebugFormData formData = new DebugFormData()
                {
                    strings = new StringValueParameter[]
                    {
                        StringValueParameter.Create("api_key", PluginSettings.data.gameAPIKey),
                        StringValueParameter.Create(authenticationKey, authenticationValue),
                    },
                    binaryData = null,
                };
                webRequestFormData.Add(webRequest, formData);
            }
            #endif

            return webRequest;
        }

        /// <summary>Requests a login code be sent to an email address.</summary>
        public static void SendSecurityCode(string emailAddress,
                                            Action<APIMessage> successCallback,
                                            Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/oauth/emailrequest";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "email",
                                                                                 emailAddress);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Requests a user OAuthToken in exchange for a security code.</summary>
        public static void GetOAuthToken(string securityCode,
                                         Action<string> successCallback,
                                         Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/oauth/emailexchange";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "security_code",
                                                                                 securityCode);
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }

        /// <summary>Request an OAuthToken using a Steam User authentication ticket.</summary>
        public static void RequestSteamAuthentication(byte[] pTicket, uint pcbTicket,
                                                      Action<string> successCallback,
                                                      Action<WebRequestError> errorCallback)
        {
            if(pTicket == null
               || pTicket.Length == 0
               || pTicket.Length > 1024)
            {
                Debug.LogWarning("[mod.io] Steam Ticket is invalid. Ensure that the"
                                 + " pTicket is not null, and is less than 1024 bytes.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("Steam Ticket is invalid. Ensure"
                        + " that the pTicket is not null, and is less than 1024 bytes."));
                }
            }
            else
            {
                // create vars
                string encodedTicket = Utility.EncodeEncryptedAppTicket(pTicket, pcbTicket);

                if(string.IsNullOrEmpty(encodedTicket))
                {
                    if(errorCallback != null)
                    {
                        string message = ("Failed to convert Steam ticket"
                                          + " and so authentication cannot"
                                          + " be attempted.");
                        errorCallback(WebRequestError.GenerateLocal(message));
                    }
                }
                else
                {
                    APIClient.RequestSteamAuthentication(encodedTicket,
                                                         successCallback,
                                                         errorCallback);
                }
            }
        }

        /// <summary>Request an OAuthToken using an encoded Steam User authentication ticket.</summary>
        public static void RequestSteamAuthentication(string base64EncodedTicket,
                                                      Action<string> successCallback,
                                                      Action<WebRequestError> errorCallback)
        {
            // create vars
            string endpointURL = PluginSettings.data.apiURL + @"/external/steamauth";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "appdata",
                                                                                 base64EncodedTicket);

            // send request
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }

        /// <summary>Request an OAuthToken using a GOG user authentication ticket.</summary>
        public static void RequestGOGAuthentication(byte[] data, uint dataSize,
                                                    Action<string> successCallback,
                                                    Action<WebRequestError> errorCallback)
        {
            if(data == null
               || data.Length == 0
               || data.Length > 1024)
            {
                Debug.LogWarning("[mod.io] GOG Ticket is invalid. Ensure that the"
                                 + " data is not null, and is less than 1024 bytes.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("GOG Ticket is invalid. Ensure"
                        + " that the data is not null, and is less than 1024 bytes."));
                }
            }
            else
            {
                // create vars
                string encodedTicket = Utility.EncodeEncryptedAppTicket(data, dataSize);

                if(string.IsNullOrEmpty(encodedTicket))
                {
                    if(errorCallback != null)
                    {
                        string message = ("Failed to convert GOG ticket"
                                          + " and so authentication cannot"
                                          + " be attempted.");
                        errorCallback(WebRequestError.GenerateLocal(message));
                    }
                }
                else
                {
                    APIClient.RequestGOGAuthentication(encodedTicket,
                                                       successCallback,
                                                       errorCallback);
                }
            }
        }

        /// <summary>Request an OAuthToken using a GOG Galaxy App ticket.</summary>
        public static void RequestGOGAuthentication(string encryptedAppTicket,
                                                    Action<string> successCallback,
                                                    Action<WebRequestError> errorCallback)
        {
            if(encryptedAppTicket == null
               || encryptedAppTicket.Length == 0
               || encryptedAppTicket.Length > 1024)
            {
                Debug.LogWarning("[mod.io] GOG Galaxy Ticket is invalid. Ensure that the"
                                 + " encryptedAppTicket is not null, and is less than 1024 bytes.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("GOG Galaxy Ticket is invalid. Ensure"
                        + " that the encryptedAppTicket is not null, and is less than 1024 bytes."));
                }
            }

            // create vars
            string endpointURL = PluginSettings.data.apiURL + @"/external/galaxyauth";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "appdata",
                                                                                 encryptedAppTicket);

            // send request
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }

        // ---------[ GAME ENDPOINTS ]---------
        /// <summary>Fetches all the game profiles from the mod.io servers.</summary>
        public static void GetAllGames(RequestFilter filter, APIPaginationParameters pagination,
                                       Action<RequestPage<GameProfile>> successCallback,
                                       Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the game's/app's profile from the mod.io servers.</summary>
        public static void GetGame(Action<GameProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Updates the game's profile on the mod.io servers.</summary>
        public static void EditGame(EditGameParameters parameters,
                                    Action<GameProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MOD ENDPOINTS ]---------
        /// <summary>Fetches all mod profiles from the mod.io servers.</summary>
        public static void GetAllMods(RequestFilter filter, APIPaginationParameters pagination,
                                      Action<RequestPage<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches a mod profile from the mod.io servers.</summary>
        public static void GetMod(int modId,
                                  Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a new mod profile to the mod.io servers.</summary>
        public static void AddMod(AddModParameters parameters,
                                  Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits changes to an existing mod profile.</summary>
        public static void EditMod(int modId,
                                   EditModParameters parameters,
                                   Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes a mod profile from the mod.io servers.</summary>
        public static void DeleteMod(int modId,
                                     Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        /// <summary>Fetches all modfiles for a given mod from the mod.io servers.</summary>
        public static void GetAllModfiles(int modId,
                                          RequestFilter filter, APIPaginationParameters pagination,
                                          Action<RequestPage<Modfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/files";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetch the a modfile from the mod.io servers.</summary>
        public static void GetModfile(int modId, int modfileId,
                                      Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a new modfile and binary to the mod.io servers.</summary>
        public static void AddModfile(int modId,
                                      AddModfileParameters parameters,
                                      Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/files";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits changes to an existing modfile.</summary>
        public static void EditModfile(int modId, int modfileId,
                                       EditModfileParameters parameters,
                                       Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        /// <summary>Submit new game media to the mod.io servers.</summary>
        public static void AddGameMedia(AddGameMediaParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/media";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits new mod media to the mod.io servers.</summary>
        public static void AddModMedia(int modId,
                                       AddModMediaParameters parameters,
                                       Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/media";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes mod media from a mod on the mod.io servers.</summary>
        public static void DeleteModMedia(int modId,
                                          DeleteModMediaParameters parameters,
                                          Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/media";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        /// <summary>Subscribes the authenticated user to a mod.</summary>
        public static void SubscribeToMod(int modId,
                                          Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/subscribe";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    null,
                                                                    null);


            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Unsubscribes the authenticated user from a mod.</summary>
        public static void UnsubscribeFromMod(int modId,
                                              Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/subscribe";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ EVENT ENDPOINTS ]---------
        /// <summary>Fetches the update events for a given mod.</summary>
        public static void GetModEvents(int modId,
                                        RequestFilter filter, APIPaginationParameters pagination,
                                        Action<RequestPage<ModEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches all the mod update events for the game profile</summary>
        public static void GetAllModEvents(RequestFilter filter, APIPaginationParameters pagination,
                                           Action<RequestPage<ModEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ STATS ENDPOINTS ]---------
        /// <summary>Fetches the statistics for all mods.</summary>
        public static void GetAllModStats(RequestFilter filter, APIPaginationParameters pagination,
                                          Action<RequestPage<ModStatistics>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/stats";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the statics for a mod.</summary>
        public static void GetModStats(int modId,
                                       Action<ModStatistics> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/stats";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // ---------[ TAG ENDPOINTS ]---------
        /// <summary>Fetches the tag categories specified by the game profile.</summary>
        public static void GetGameTagOptions(Action<RequestPage<ModTagCategory>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits new mod tag categories to the mod.io servers.</summary>
        public static void AddGameTagOption(AddGameTagOptionParameters parameters,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/tags";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Removes mod tag options from the mod.io servers.</summary>
        public static void DeleteGameTagOption(DeleteGameTagOptionParameters parameters,
                                               Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/tags";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the tags applied to the given mod.</summary>
        public static void GetModTags(int modId,
                                      RequestFilter filter, APIPaginationParameters pagination,
                                      Action<RequestPage<ModTag>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits new mod tags to the mod.io servers.</summary>
        public static void AddModTags(int modId, AddModTagsParameters parameters,
                                      Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/tags";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Removes tags from the given mod.</param>
        public static void DeleteModTags(int modId,
                                         DeleteModTagsParameters parameters,
                                         Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/tags";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ RATING ENDPOINTS ]---------
        /// <summary>Submits a user's rating for a mod.</summary>
        public static void AddModRating(int modId, AddModRatingParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/ratings";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ METADATA ENDPOINTS ]---------
        /// <summary>Fetches all the KVP metadata for a mod.</summary>
        public static void GetAllModKVPMetadata(int modId,
                                                APIPaginationParameters pagination,
                                                Action<RequestPage<MetadataKVP>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submit KVP Metadata to a mod.</summary>
        public static void AddModKVPMetadata(int modId, AddModKVPMetadataParameters parameters,
                                             Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/metadatakvp";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes KVP metadata from a mod.</summary>
        public static void DeleteModKVPMetadata(int modId, DeleteModKVPMetadataParameters parameters,
                                                Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        /// <summary>Fetches all the dependencies for a mod.</summary>
        public static void GetAllModDependencies(int modId,
                                                 RequestFilter filter, APIPaginationParameters pagination,
                                                 Action<RequestPage<ModDependency>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits new dependencides for a mod.</summary>
        public static void AddModDependencies(int modId, AddModDependenciesParameters parameters,
                                              Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/dependencies";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Removes dependencides from a mod.</summary>
        public static void DeleteModDependencies(int modId, DeleteModDependenciesParameters parameters,
                                                 Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TEAM ENDPOINTS ]---------
        /// <summary>Fetches the team members for a mod.</summary>
        public static void GetAllModTeamMembers(int modId,
                                                RequestFilter filter, APIPaginationParameters pagination,
                                                Action<RequestPage<ModTeamMember>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/team";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a new team member to a mod.</summary>
        public static void AddModTeamMember(int modId, AddModTeamMemberParameters parameters,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/team";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits changes to a mod team member.</summary>
        public static void UpdateModTeamMember(int modId, int teamMemberId,
                                               UpdateModTeamMemberParameters parameters,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                   parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a delete request for a mod team member.</summary>
        public static void DeleteModTeamMember(int modId, int teamMemberId,
                                               Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        /// <summary>Fetches all the comments for a mod.</summary>
        public static void GetAllModComments(int modId,
                                             RequestFilter filter, APIPaginationParameters pagination,
                                             Action<RequestPage<ModComment>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/comments";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches a mod comment by id.</summary>
        public static void GetModComment(int modId, int commentId,
                                         Action<ModComment> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a delete request for a mod comment.</summary>
        public static void DeleteModComment(int modId, int commentId,
                                            Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/games/" + PluginSettings.data.gameId + @"/mods/" + modId + @"/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ USER ENDPOINTS ]---------
        /// <summary>Fetches the owner for a mod resource.</summary>
        public static void GetResourceOwner(APIResourceType resourceType, int resourceID,
                                            Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/general/owner";
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

        // ---------[ REPORT ENDPOINTS ]---------
        /// <summary>Submits a report against a mod/resource on mod.io.</summary>
        public static void SubmitReport(SubmitReportParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/report";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ ME ENDPOINTS ]---------
        /// <summary>Fetches the user profile for the authenticated user.</summary>
        public static void GetAuthenticatedUser(Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/me";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the subscriptions for the authenticated user.</summary>
        public static void GetUserSubscriptions(RequestFilter filter, APIPaginationParameters pagination,
                                                Action<RequestPage<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/me/subscribed";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetch the update events for the authenticated user.</summary>
        public static void GetUserEvents(RequestFilter filter, APIPaginationParameters pagination,
                                         Action<RequestPage<UserEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/me/events";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the games that the authenticated user is a team member of.</summary>
        public static void GetUserGames(Action<RequestPage<GameProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/me/games";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the mods that the authenticated user is a team member of.</summary>
        public static void GetUserMods(RequestFilter filter, APIPaginationParameters pagination,
                                       Action<RequestPage<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/me/mods";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the modfiles that the authenticated user uploaded.</summary>
        public static void GetUserModfiles(RequestFilter filter, APIPaginationParameters pagination,
                                           Action<RequestPage<Modfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/me/files";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches _all_ the ratings submitted by the authenticated user.</summary>
        public static void GetUserRatings(RequestFilter filter, APIPaginationParameters pagination,
                                          Action<RequestPage<ModRating>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.data.apiURL + @"/me/ratings";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // ---------[ OBSOLETE FUNCTIONALITY ]---------
        /// <summary>The base URL for the web API that the APIClient should use.</summary>
        [Obsolete("Use PluginSettings.data.apiURL instead")]
        public static string apiURL
        {
            get { return PluginSettings.data.apiURL; }
        }

        /// <summary>Game ID that the APIClient should use when contacting the API.</summary>
        [Obsolete("Use PluginSettings.data.gameId instead")]
        public static int gameId
        {
            get { return PluginSettings.data.gameId; }
        }

        /// <summary>Game API Key that the APIClient should use when contacting the API.</summary>
        [Obsolete("Use PluginSettings.data.gameAPIKey instead")]
        public static string gameAPIKey
        {
            get { return PluginSettings.data.gameAPIKey; }
        }

        /// <summary>Enable logging of all web requests.</summary>
        [Obsolete("Use PluginSettings.data.logAllRequests instead.")]
        public static bool logAllRequests
        {
            get { return PluginSettings.data.logAllRequests; }
        }

        /// <summary>[Obsolete] Fetches the tag categories specified by the game profile.</summary>
        [Obsolete("Use APIClient.GetGameTagOptions() instead.")]
        public static void GetAllGameTagOptions(Action<RequestPage<ModTagCategory>> successCallback, Action<WebRequestError> errorCallback)
        {
            APIClient.GetGameTagOptions(successCallback, errorCallback);
        }

        /// <summary>[Obsolete] Deletes a mod profile from the mod.io servers.</summary>
        [Obsolete("This function no longer returns an APIMessage on success")]
        public static void DeleteMod(int modId,
                                     Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            Action onSuccess = null;
            if(successCallback != null)
            {
                onSuccess = () =>
                {
                    APIMessage message = new APIMessage();
                    successCallback(message);
                };
            }

            APIClient.DeleteMod(modId, onSuccess, errorCallback);
        }

        /// <summary>[Obsolete] Removes mod tag options from the mod.io servers.</summary>
        [Obsolete("This function no longer returns an APIMessage on success")]
        public static void DeleteGameTagOption(DeleteGameTagOptionParameters parameters,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            Action onSuccess = null;
            if(successCallback != null)
            {
                onSuccess = () =>
                {
                    APIMessage message = new APIMessage();
                    successCallback(message);
                };
            }

            APIClient.DeleteGameTagOption(parameters, onSuccess, errorCallback);
        }

        /// <summary>[Obsolete] Removes tags from the given mod.</param>
        [Obsolete("This function no longer returns an APIMessage on success")]
        public static void DeleteModTags(int modId,
                                         DeleteModTagsParameters parameters,
                                         Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            Action onSuccess = null;
            if(successCallback != null)
            {
                onSuccess = () =>
                {
                    APIMessage message = new APIMessage();
                    successCallback(message);
                };
            }

            APIClient.DeleteModTags(modId, parameters, onSuccess, errorCallback);
        }

        /// <summary>[Obsolete] Deletes KVP metadata from a mod.</summary>
        [Obsolete("This function no longer returns an APIMessage on success")]
        public static void DeleteModKVPMetadata(int modId, DeleteModKVPMetadataParameters parameters,
                                                Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            Action onSuccess = null;
            if(successCallback != null)
            {
                onSuccess = () =>
                {
                    APIMessage message = new APIMessage();
                    successCallback(message);
                };
            }

            APIClient.DeleteModKVPMetadata(modId, parameters, onSuccess, errorCallback);
        }

        /// <summary>[Obsolete] Removes dependencides from a mod.</summary>
        [Obsolete("This function no longer returns an APIMessage on success")]
        public static void DeleteModDependencies(int modId, DeleteModDependenciesParameters parameters,
                                                 Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            Action onSuccess = null;
            if(successCallback != null)
            {
                onSuccess = () =>
                {
                    APIMessage message = new APIMessage();
                    successCallback(message);
                };
            }

            APIClient.DeleteModDependencies(modId, parameters, onSuccess, errorCallback);
        }

        /// <summary>[Obsolete] Submits a delete request for a mod team member.</summary>
        [Obsolete("This function no longer returns an APIMessage on success")]
        public static void DeleteModTeamMember(int modId, int teamMemberId,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            Action onSuccess = null;
            if(successCallback != null)
            {
                onSuccess = () =>
                {
                    APIMessage message = new APIMessage();
                    successCallback(message);
                };
            }

            APIClient.DeleteModTeamMember(modId, teamMemberId, onSuccess, errorCallback);
        }

        /// <summary>[Obsolete] Submits a delete request for a mod comment.</summary>
        [Obsolete("This function no longer returns an APIMessage on success")]
        public static void DeleteModComment(int modId, int commentId,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            Action onSuccess = null;
            if(successCallback != null)
            {
                onSuccess = () =>
                {
                    APIMessage message = new APIMessage();
                    successCallback(message);
                };
            }

            APIClient.DeleteModComment(modId, commentId, onSuccess, errorCallback);
        }

        /// <summary>Fetches all the user profiles on mod.io.</summary>
        [Obsolete("This endpoint has been removed from the mod.io API.", true)]
        public static void GetAllUsers(RequestFilter filter, APIPaginationParameters pagination,
                                       Action<RequestPage<UserProfile>> successCallback, Action<WebRequestError> errorCallback) {}

        /// <summary>Fetches a user profile from the mod.io servers.</summary>
        [Obsolete("This endpoint has been removed from the mod.io API.", true)]
        public static void GetUser(int userId,
                                   Action<UserProfile> successCallback, Action<WebRequestError> errorCallback) {}

    }
}
