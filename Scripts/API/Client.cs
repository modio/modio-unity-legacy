using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO.API
{
    public class BinaryUpload
    {
        public string fileName = string.Empty;
        public byte[] data = null;

        public static BinaryUpload Create(string fileName, byte[] data)
        {
            BinaryUpload retVal = new BinaryUpload();
            retVal.fileName = fileName;
            retVal.data = data;
            return retVal;
        }
    }

    public class BinaryDataParameter
    {
        public string key = "";
        public string fileName = null;
        public string mimeType = null;
        public byte[] contents = null;

        public static BinaryDataParameter Create(string key, string fileName, string mimeType, byte[] contents)
        {
            Debug.Assert(!String.IsNullOrEmpty(key) && contents != null);

            BinaryDataParameter retVal = new BinaryDataParameter();
            retVal.key = key;
            retVal.fileName = fileName;
            retVal.mimeType = mimeType;
            retVal.contents = contents;
            return retVal;
        }
    }

    public class StringValueParameter
    {
        public string key = "";
        public string value = "";

        public static StringValueParameter Create(string k, object v)
        {
            Debug.Assert(!String.IsNullOrEmpty(k) && v != null);

            StringValueParameter retVal = new StringValueParameter();
            retVal.key = k;
            retVal.value = v.ToString();
            return retVal;
        }
    }

    public static class Client
    {
        // ---------[ CONSTANTS ]---------
        public const string API_VERSION = "v1";

        #if DEBUG
        public static readonly string API_URL = (GlobalSettings.USE_TEST_SERVER ? "https://api.test.mod.io/" : "https://api.mod.io/") + API_VERSION + "/";
        #else
        public const string API_URL = "https://api.mod.io/" + API_VERSION + "/";
        #endif

        public static readonly string[] UNITY_REQUEST_HEADER_KEYS = new string[]
        {
            // RESERVED
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
            // UNITY
            "accept-encoding",
            "Content-Type",
            "content-length",
            "x-unity-version",
            "user-agent",
        };
        public static readonly string[] MODIO_REQUEST_HEADER_KEYS = new string[]
        {
            "Authorization",
        };

        // ---------[ DEFAULT SUCCESS/ERROR FUNCTIONS ]---------
        public static void LogError(WebRequestError errorInfo)
        {
            string errorMessage = errorInfo.method + " REQUEST FAILED";
            errorMessage += "\nURL: " + errorInfo.url;
            errorMessage += "\nCode: " + errorInfo.responseCode;
            errorMessage += "\nMessage: " + errorInfo.message;
            errorMessage += "\n";

            Debug.LogWarning(errorMessage);
        }

        // ---------[ REQUEST HANDLING ]---------
        public static UnityWebRequest GenerateQuery(string endpointURL,
                                                    Filter queryFilter)
        {
            Debug.Assert(!String.IsNullOrEmpty(GlobalSettings.GAME_APIKEY),
                         "Please save your game's API Key into GlobalSettings.cs before using this plugin");

            string queryURL = (endpointURL
                               + "?api_key=" + GlobalSettings.GAME_APIKEY
                               + "&" + queryFilter.GenerateQueryString());

            #if DEBUG
            if(GlobalSettings.LOG_ALL_WEBREQUESTS)
            {
                Debug.Log("GENERATING QUERY"
                          + "\nQuery: " + queryURL
                          + "\n");
            }
            #endif

            UnityWebRequest webRequest = UnityWebRequest.Get(queryURL);
            return webRequest;
        }

        public static UnityWebRequest GenerateGetRequest<T>(string endpointURL,
                                                            string oAuthToken,
                                                            Filter filter)
        {
            string constructedURL = endpointURL + "?" + filter.GenerateQueryString();
            
            UnityWebRequest webRequest = UnityWebRequest.Get(constructedURL);
            webRequest.SetRequestHeader("Authorization", "Bearer " + oAuthToken);

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

        public static UnityWebRequest GeneratePutRequest<T>(string endpointURL,
                                                            string oAuthToken,
                                                            StringValueParameter[] valueFields)
        {
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
            webRequest.SetRequestHeader("Authorization", "Bearer " + oAuthToken);

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

        public static UnityWebRequest GeneratePostRequest<T>(string endpointURL,
                                                             string oAuthToken,
                                                             StringValueParameter[] valueFields,
                                                             BinaryDataParameter[] dataFields)
        {
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
            webRequest.SetRequestHeader("Authorization", "Bearer " + oAuthToken);

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

        public static UnityWebRequest GenerateDeleteRequest<T>(string endpointURL,
                                                               string oAuthToken,
                                                               StringValueParameter[] valueFields)
        {
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
            webRequest.SetRequestHeader("Authorization", "Bearer " + oAuthToken);

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
                foreach(StringValueParameter kvp in valueFields)
                {
                    formFields += "\n" + kvp.key + "=" + kvp.value;
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
        
        public static void SendRequest<T_APIObj>(UnityWebRequest webRequest,
                                                 Action<T_APIObj> successCallback,
                                                 Action<WebRequestError> errorCallback)
        {
            // - Start Request -
            UnityWebRequestAsyncOperation requestOperation = webRequest.SendWebRequest();
            requestOperation.completed += (operation) =>
            {
                Client.ProcessWebResponse<T_APIObj>(webRequest,
                                                       successCallback,
                                                       errorCallback);
            };
        }

        public static void ProcessWebResponse<T>(UnityWebRequest webRequest,
                                                 System.Action<T> successCallback,
                                                 System.Action<WebRequestError> errorCallback)
        {
            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                #if DEBUG
                    WebRequestError errorInfo = WebRequestError.GenerateFromWebRequest(webRequest);

                    if(GlobalSettings.LOG_ALL_WEBREQUESTS
                       && errorCallback != Client.LogError)
                    {
                        Client.LogError(errorInfo);
                    }

                    if(errorCallback != null)
                    {
                        errorCallback(errorInfo);
                    }
                #else
                    if(errorCallback != null)
                    {
                        errorCallback(WebRequestError.GenerateFromWebRequest(webRequest));
                    }
                #endif
            }
            else
            {
                #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS)
                {
                    Debug.Log(webRequest.method.ToUpper() + " REQUEST SUCEEDED"
                              + "\nURL: " + webRequest.url
                              + "\nResponse: " + webRequest.downloadHandler.text
                              + "\n");
                }
                #endif

                if(successCallback != null)
                {
                    // TODO(@jackson): Handle as a T == null?
                    if(webRequest.responseCode == 204)
                    {
                        // if(typeof(T) == typeof(MessageObject))
                        // {
                        //     MessageObject response = new MessageObject();
                        //     response.code = 204;
                        //     response.message = "Succeeded";
                        //     successCallback((T)(object)response);
                        // }
                        // else
                        // {
                            successCallback(default(T));
                        // }
                    }
                    else
                    {
                        // TODO(@jackson): Add error handling (where FromJson fails)
                        T response = JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                        successCallback(response);
                    }
                }
            }
        }


        // ---------[ AUTHENTICATION ]---------
        public static void RequestSecurityCode(string emailAddress,
                                               Action<MessageObject> successCallback,
                                               Action<WebRequestError> errorCallback)
        {
            Debug.Assert(!String.IsNullOrEmpty(GlobalSettings.GAME_APIKEY),
                         "Please save your game's API Key into GlobalSettings.cs before using this plugin");

            string endpointURL = API_URL + "oauth/emailrequest";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("api_key", GlobalSettings.GAME_APIKEY),
                StringValueParameter.Create("email", emailAddress),
            };

            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL, "",
                                                                                          valueFields,
                                                                                          null);


            Client.SendRequest(webRequest, successCallback, errorCallback);
        }

        public static void RequestOAuthToken(string securityCode,
                                             Action<string> successCallback,
                                             Action<WebRequestError> errorCallback)
        {
            Debug.Assert(!String.IsNullOrEmpty(GlobalSettings.GAME_APIKEY),
                         "Please save your game's API Key into GlobalSettings.cs before using this plugin");

            string endpointURL = API_URL + "oauth/emailexchange";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("api_key", GlobalSettings.GAME_APIKEY),
                StringValueParameter.Create("security_code", securityCode),
            };

            UnityWebRequest webRequest = Client.GeneratePostRequest<AccessTokenObject>(endpointURL,
                                                                                              "",
                                                                                              valueFields,
                                                                                              null);
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            Client.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }


        // ---------[ GAME ENDPOINTS ]---------
        // Get All Games
        public static void GetAllGames(GetAllGamesFilter filter,
                                       Action<ObjectArray<API.GameObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Get GameProfile
        public static void GetGame(Action<API.GameObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID;
            
            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, Filter.None);
            
            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Edit GameProfile
        public static void EditGame(string oAuthToken,
                                    EditGameParameters parameters,
                                    Action<API.GameObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID;

            UnityWebRequest webRequest = Client.GeneratePutRequest<MessageObject>(endpointURL,
                                                                                  oAuthToken,
                                                                                  parameters.stringValues.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MOD ENDPOINTS ]---------
        // Get All Mods
        public static void GetAllMods(GetAllModsFilter filter,
                                      Action<ObjectArray<ModObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get Mod
        public static void GetMod(int modId,
                                  Action<ModObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId;
            
            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, Filter.None);
            
            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod
        public static void AddMod(string oAuthToken,
                                  AddModParameters parameters,
                                  Action<ModObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods";

            UnityWebRequest webRequest = Client.GeneratePostRequest<ModObject>(endpointURL,
                                                                                  oAuthToken,
                                                                                  parameters.stringValues.ToArray(),
                                                                                  parameters.binaryData.ToArray());
            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Edit Mod
        public static void EditMod(string oAuthToken, int modId,
                                   EditModParameters parameters,
                                   Action<ModObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId;

            UnityWebRequest webRequest = Client.GeneratePutRequest<MessageObject>(endpointURL,
                                                                                  oAuthToken,
                                                                                  parameters.stringValues.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod
        public static void DeleteMod(string oAuthToken,
                                     int modId,
                                     Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId;

            UnityWebRequest webRequest = Client.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                     oAuthToken,
                                                                                     null);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        // Get All Modfiles
        public static void GetAllModfiles(int modId, GetAllModfilesFilter filter,
                                          Action<ObjectArray<ModfileObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/files";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get Modfile
        public static void GetModfile(int modId, int modfileId,
                                      Action<ModfileObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, Filter.None);

            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Modfile
        public static void AddModfile(string oAuthToken,
                                      int modId,
                                      AddModfileParameters parameters,
                                      Action<ModfileObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/files";
            
            UnityWebRequest webRequest = Client.GeneratePostRequest<ModfileObject>(endpointURL,
                                                                                   oAuthToken,
                                                                                   parameters.stringValues.ToArray(),
                                                                                   parameters.binaryData.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Edit Modfile
        public static void EditModfile(string oAuthToken,
                                       int modId, int modfileId,
                                       EditModfileParameters parameters,
                                       Action<ModfileObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = Client.GeneratePutRequest<MessageObject>(endpointURL,
                                                                                  oAuthToken,
                                                                                  parameters.stringValues.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        // Add GameProfile Media
        public static void AddGameMedia(string oAuthToken,
                                        AddGameMediaParameters parameters,
                                        Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/media";

            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                   oAuthToken,
                                                                                   parameters.stringValues.ToArray(),
                                                                                   parameters.binaryData.ToArray());
            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod Media
        public static void AddModMedia(string oAuthToken, int modId,
                                       AddModMediaParameters parameters,
                                       Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/media";
            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                   oAuthToken,
                                                                                   parameters.stringValues.ToArray(),
                                                                                   parameters.binaryData.ToArray());
            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Media
        public static void DeleteModMedia(string oAuthToken, int modId,
                                          DeleteModMediaParameters parameters,
                                          Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/media";
            
            UnityWebRequest webRequest = Client.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                     oAuthToken,
                                                                                     parameters.stringValues.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        public static void SubscribeToMod(string oAuthToken,
                                          int modId,
                                          Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                null,
                                                                                null);
            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        public static void UnsubscribeFromMod(string oAuthToken,
                                              int modId,
                                              Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = Client.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ EVENT ENDPOINTS ]---------
        // Get Mod Events
        public static void GetModEvents(int modId, GetModEventFilter filter,
                                        Action<ObjectArray<EventObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/events";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get All Mod Events
        public static void GetAllModEvents(GetAllModEventsFilter filter,
                                           Action<ObjectArray<EventObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/events";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TAG ENDPOINTS ]---------
        // Get All Game Tag Options
        public static void GetAllGameTagOptions(Action<ObjectArray<GameTagOptionObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/tags";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, Filter.None);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Add Game Tag Option
        public static void AddGameTagOption(string oAuthToken,
                                            AddGameTagOptionParameters parameters,
                                            Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/tags";

            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                parameters.stringValues.ToArray(),
                                                                                parameters.binaryData.ToArray());
            Client.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Delete Game Tag Option
        public static void DeleteGameTagOption(string oAuthToken,
                                               DeleteGameTagOptionParameters parameters,
                                               Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/tags";

            UnityWebRequest webRequest = Client.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                     oAuthToken,
                                                                                     parameters.stringValues.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Get All Mod Tags
        public static void GetAllModTags(int modId, GetAllModTagsFilter filter,
                                         Action<ObjectArray<ModTagObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod Tags
        public static void AddModTags(string oAuthToken,
                                      int modId, AddModTagsParameters parameters,
                                      Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                   oAuthToken,
                                                                                   parameters.stringValues.ToArray(),
                                                                                   parameters.binaryData.ToArray());
            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Tags
        public static void DeleteModTags(string oAuthToken,
                                         int modId,
                                         DeleteModTagsParameters parameters,
                                         Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = Client.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                     oAuthToken,
                                                                                     parameters.stringValues.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ RATING ENDPOINTS ]---------
        // Add Mod Rating
        public static void AddModRating(string oAuthToken,
                                        int modId, AddModRatingParameters parameters,
                                        Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/ratings";

            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                   oAuthToken,
                                                                                   parameters.stringValues.ToArray(),
                                                                                   parameters.binaryData.ToArray());
            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ METADATA ENDPOINTS ]---------
        // Get All Mod KVP Metadata
        public static void GetAllModKVPMetadata(int modId,
                                                Action<ObjectArray<MetadataKVPObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, Filter.None);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod KVP Metadata
        public static void AddModKVPMetadata(string oAuthToken,
                                             int modId, AddModKVPMetadataParameters parameters,
                                             Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/metadatakvp";
            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                   oAuthToken,
                                                                                   parameters.stringValues.ToArray(),
                                                                                   parameters.binaryData.ToArray());
            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod KVP Metadata
        public static void DeleteModKVPMetadata(string oAuthToken,
                                                int modId, DeleteModKVPMetadataParameters parameters,
                                                Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = Client.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                     oAuthToken,
                                                                                     parameters.stringValues.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
        public static void GetAllModDependencies(int modId, GetAllModDependenciesFilter filter,
                                                 Action<ObjectArray<ModDependenciesObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod Dependencies
        public static void AddModDependencies(string oAuthToken,
                                              int modId, AddModDependenciesParameters parameters,
                                              Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                   oAuthToken,
                                                                                   parameters.stringValues.ToArray(),
                                                                                   parameters.binaryData.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Dependencies
        public static void DeleteModDependencies(string oAuthToken,
                                                 int modId, DeleteModDependenciesParameters parameters,
                                                 Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = Client.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                     oAuthToken,
                                                                                     parameters.stringValues.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TEAM ENDPOINTS ]---------
        // Get All Mod Team Members
        public static void GetAllModTeamMembers(int modId, GetAllModTeamMembersFilter filter,
                                                Action<ObjectArray<TeamMemberObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod Team Member
        public static void AddModTeamMember(string oAuthToken,
                                            int modId, AddModTeamMemberParameters parameters,
                                            Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                   oAuthToken,
                                                                                   parameters.stringValues.ToArray(),
                                                                                   parameters.binaryData.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Update Mod Team Member
        // NOTE(@jackson): Untested
        public static void UpdateModTeamMember(string oAuthToken,
                                               int modId, int teamMemberId,
                                               UpdateModTeamMemberParameters parameters,
                                               Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = Client.GeneratePutRequest<MessageObject>(endpointURL,
                                                                                  oAuthToken,
                                                                                  parameters.stringValues.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Team Member
        public static void DeleteModTeamMember(string oAuthToken,
                                               int modId, int teamMemberId,
                                               Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = Client.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                     oAuthToken,
                                                                                     null);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
        // NOTE(@jackson): Untested
        public static void GetAllModComments(int modId, GetAllModCommentsFilter filter,
                                             Action<ObjectArray<CommentObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/comments";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get Mod Comment
        // NOTE(@jackson): Untested
        public static void GetModComment(int modId, int commentId,
                                         Action<CommentObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, Filter.None);

            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Comment
        // NOTE(@jackson): Untested
        public static void DeleteModComment(string oAuthToken,
                                            int modId, int commentId,
                                            Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = Client.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                     oAuthToken,
                                                                                     null);
            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ USER ENDPOINTS ]---------
        // Get Resource Owner
        public enum ResourceType
        {
            Games,
            Mods,
            Files,
            Tags
        }
        public static void GetResourceOwner(string oAuthToken,
                                            ResourceType resourceType, int resourceID,
                                            Action<UserObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "general/owner";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("resource_type", resourceType.ToString().ToLower()),
                StringValueParameter.Create("resource_id", resourceID),
            };

            UnityWebRequest webRequest = Client.GeneratePostRequest<UserObject>(endpointURL,
                                                                             oAuthToken,
                                                                             valueFields,
                                                                             null);
            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get All Users
        public static void GetAllUsers(GetAllUsersFilter filter,
                                       Action<ObjectArray<UserObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "users";

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, filter);

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User
        public static void GetUser(int userID,
                                   Action<UserObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "users/" + userID;

            UnityWebRequest webRequest = Client.GenerateQuery(endpointURL, Filter.None);

            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // Submit Report
        // NOTE(@jackson): Untested
        public static void SubmitReport(string oAuthToken,
                                        SubmitReportParameters parameters,
                                        Action<MessageObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "report";

            UnityWebRequest webRequest = Client.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                   oAuthToken,
                                                                                   parameters.stringValues.ToArray(),
                                                                                   parameters.binaryData.ToArray());

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        

        // ---------[ ME ENDPOINTS ]---------
        // Get Authenticated User
        public static void GetAuthenticatedUser(string oAuthToken,
                                                Action<UserObject> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "me";

            UnityWebRequest webRequest = Client.GenerateGetRequest<UserObject>(endpointURL,
                                                                                            oAuthToken,
                                                                                            Filter.None);
            

            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User Subscriptions
        public static void GetUserSubscriptions(string oAuthToken,
                                                GetUserSubscriptionsFilter filter,
                                                Action<ObjectArray<ModObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "me/subscribed";

            UnityWebRequest webRequest = Client.GenerateGetRequest<ObjectArray<ModObject>>(endpointURL,
                                                                                             oAuthToken,
                                                                                             filter);
            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User Games
        public static void GetUserGames(string oAuthToken,
                                        Action<ObjectArray<API.GameObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "me/games";

            UnityWebRequest webRequest = Client.GenerateGetRequest<ObjectArray<API.GameObject>>(endpointURL,
                                                                                              oAuthToken,
                                                                                              Filter.None);
            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User Mods
        public static void GetUserMods(string oAuthToken,
                                       Action<ObjectArray<ModObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "me/mods";

            UnityWebRequest webRequest = Client.GenerateGetRequest<ObjectArray<ModObject>>(endpointURL,
                                                                                             oAuthToken,
                                                                                             Filter.None);
            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User Files
        public static void GetUserModfiles(string oAuthToken,
                                           Action<ObjectArray<ModfileObject>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = API_URL + "me/files";

            UnityWebRequest webRequest = Client.GenerateGetRequest<ObjectArray<ModfileObject>>(endpointURL,
                                                                                                 oAuthToken,
                                                                                                 Filter.None);
            Client.SendRequest(webRequest, successCallback, errorCallback);
        }
    }
}
