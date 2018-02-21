#define USING_TEST_SERVER
#define LOG_ALL_REQUESTS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


// TODO(@jackson): Assert apiKey, and oAuthToken length to avoid user error
namespace ModIO
{
    public class BinaryDataField
    {
        public string key = "";
        public byte[] contents = null;
        public string fileName = null;
        public string mimeType = null;
    }

    public class StringValueField
    {
        public string key = "";
        public string value = "";

        public static StringValueField Create(string k, object v)
        {
            Debug.Assert(!String.IsNullOrEmpty(k) && v != null);

            StringValueField retVal = new StringValueField();
            retVal.key = k;
            retVal.value = v.ToString();
            return retVal;
        }
    }

    public class APIClient
    {
        // ---------[ CONSTANTS ]---------
        public const string API_VERSION = "v1";

        #if USING_TEST_SERVER
        public const string API_URL = "https://api.test.mod.io/" + API_VERSION + "/";
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
        public static void IgnoreResponse(object result) {}
        public static void LogError(ErrorInfo errorInfo)
        {
            string errorMessage = "API ERROR";
            errorMessage += "\nURL: " + errorInfo.url;
            errorMessage += "\nCode: " + errorInfo.httpStatusCode;
            errorMessage += "\nMessage: " + errorInfo.message;
            errorMessage += "\n";

            Debug.LogWarning(errorMessage);
        }

        // ---------[ REQUEST HANDLING ]---------
        public static UnityWebRequest GenerateQuery(string endpointURL,
                                                    string apiKey,
                                                    Filter queryFilter)
        {
            string queryURL = (endpointURL
                               + "?api_key=" + apiKey
                               + "&" + queryFilter.GenerateQueryString());

            #if LOG_ALL_REQUESTS
            Debug.Log("GENERATING QUERY"
                      + "\nQuery: " + queryURL
                      + "\n");
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

            #if LOG_ALL_REQUESTS
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
                                                            StringValueField[] valueFields)
        {
            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueField valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.method = UnityWebRequest.kHttpVerbPUT;
            webRequest.SetRequestHeader("Authorization", "Bearer " + oAuthToken);

            #if LOG_ALL_REQUESTS
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
                foreach(StringValueField svf in valueFields)
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
                                                             StringValueField[] valueFields,
                                                             BinaryDataField[] dataFields)
        {
            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueField valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }
            if(dataFields != null)
            {
                foreach(BinaryDataField dataField in dataFields)
                {
                    form.AddBinaryData(dataField.key, dataField.contents, dataField.fileName, dataField.mimeType);
                }
            }


            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + oAuthToken);

            #if LOG_ALL_REQUESTS
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
                    foreach(StringValueField valueField in valueFields)
                    {
                        formFields += "\n" + valueField.key + "=" + valueField.value;
                    }
                    
                }
                if(dataFields != null)
                {
                    foreach(BinaryDataField dataField in dataFields)
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
                                                               StringValueField[] valueFields)
        {
            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueField valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.method = UnityWebRequest.kHttpVerbDELETE;
            webRequest.SetRequestHeader("Authorization", "Bearer " + oAuthToken);

            #if LOG_ALL_REQUESTS
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
                foreach(StringValueField kvp in valueFields)
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
                                                 Action<ErrorInfo> errorCallback)
        {
            // - Start Request -
            UnityWebRequestAsyncOperation requestOperation = webRequest.SendWebRequest();
            requestOperation.completed += (operation) =>
            {
                APIClient.ProcessWebResponse<T_APIObj>(webRequest,
                                                       successCallback,
                                                       errorCallback);
            };
        }

        public static void ProcessWebResponse<T>(UnityWebRequest webRequest,
                                                 System.Action<T> successCallback,
                                                 System.Action<ErrorInfo> errorCallback)
        {
            Debug.Log("Full API Response"
                      + "\nURL: " + webRequest.url
                      + "\nBody:" + webRequest.downloadHandler.text);

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                ErrorInfo errorInfo = ErrorInfo.GenerateFromWebRequest(webRequest);

                errorCallback(errorInfo);

                #if LOG_ALL_REQUESTS
                if(errorCallback != APIClient.LogError)
                {
                    APIClient.LogError(errorInfo);
                }
                #endif

                return;
            }

            #if LOG_ALL_REQUESTS
            Debug.Log("API REQUEST SUCEEDED"
                      + "\nQuery: " + webRequest.url
                      + "\nResponse: " + webRequest.downloadHandler.text
                      + "\n");
            #endif

            // TODO(@jackson): Handle as a T == null?
            if(webRequest.responseCode == 204)
            {
                if(typeof(T) == typeof(API.MessageObject))
                {
                    API.MessageObject response = new API.MessageObject();
                    response.code = 204;
                    response.message = "Succeeded";
                    successCallback((T)(object)response);
                }
                else
                {
                    successCallback(default(T));
                }
            }
            else
            {
                // TODO(@jackson): Add error handling (where FromJson fails)
                T response = JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                successCallback(response);
            }
        }

        // ---------[ OBJECT WRAPPING ]---------
        private void OnSuccessWrapper<T_APIObj, T>(T_APIObj apiObject,
                                                   Action<T> successCallback)
                                                   where T_APIObj : struct
                                                   where T : IAPIObjectWrapper<T_APIObj>, new()
        {
            T wrapperObject = new T();
            wrapperObject.WrapAPIObject(apiObject);
            successCallback(wrapperObject);
        }
        private void OnSuccessWrapper<T_APIObj, T>(API.ObjectArray<T_APIObj> apiObjectArray,
                                                   Action<T[]> successCallback)
                                                   where T_APIObj : struct
                                                   where T : IAPIObjectWrapper<T_APIObj>, new()
        {
            T[] wrapperObjectArray = new T[apiObjectArray.data.Length];
            for(int i = 0;
                i < apiObjectArray.data.Length;
                ++i)
            {
                T newObject = new T();
                newObject.WrapAPIObject(apiObjectArray.data[i]);
                
                wrapperObjectArray[i] = newObject;
            }

            successCallback(wrapperObjectArray);
        }


        // ---------[ AUTHENTICATION ]---------
        public void RequestSecurityCode(string apiKey,
                                        string emailAddress,
                                        Action<APIMessage> successCallback,
                                        Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "oauth/emailrequest";
            StringValueField[] valueFields = new StringValueField[]
            {
                StringValueField.Create("api_key", apiKey),
                StringValueField.Create("email", emailAddress),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL, "",
                                                                                          valueFields,
                                                                                          null);

            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }

        // NOTE(@jackson): Untested
        public void RequestOAuthToken(string apiKey,
                                      string securityCode,
                                      Action<string> successCallback,
                                      Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "oauth/emailexchange";
            StringValueField[] valueFields = new StringValueField[]
            {
                StringValueField.Create("api_key", apiKey),
                StringValueField.Create("security_code", securityCode),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.AccessTokenObject>(endpointURL,
                                                                                    "",
                                                                                    valueFields,
                                                                                    null);
            Action<API.AccessTokenObject> onSuccess = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ GAME ENDPOINTS ]---------
        // Get All Games
        public void GetAllGames(string apiKey,
                                GetAllGamesFilter filter,
                                Action<GameInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.GameObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameObject, GameInfo>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }

        // Get GameInfo
        public void GetGame(string apiKey, int gameId,
                            Action<GameInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId;
            
            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, Filter.None);
            
            
            Action<API.GameObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameObject, GameInfo>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }

        // Edit GameInfo
        public void EditGame(string oAuthToken,
                             EditableGameInfo gameInfo,
                             Action<GameInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameInfo.id;
            StringValueField[] valueFields = gameInfo.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            
            Action<API.GameObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameObject, GameInfo>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ MOD ENDPOINTS ]---------
        // Get All Mods
        public void GetAllMods(string apiKey, int gameId,
                               GetAllModsFilter filter,
                               Action<ModInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get Mod
        public void GetMod(string apiKey, int gameId,
                           int modId,
                           Action<ModInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId;
            
            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, Filter.None);
            
            
            Action<API.ModObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod
        public void AddMod(string oAuthToken, int gameId,
                           EditableModInfo modInfo,
                           Action<ModInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods";
            StringValueField[] valueFields = modInfo.GetAddValueFields();
            BinaryDataField[] dataFields = modInfo.GetAddDataFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.ModObject>(endpointURL,
                                                                            oAuthToken,
                                                                            valueFields,
                                                                            dataFields);
            
            Action<API.ModObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Edit Mod
        public void EditMod(string oAuthToken,
                            EditableModInfo modInfo,
                            Action<ModInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + modInfo.gameId + "/mods/" + modInfo.id;
            StringValueField[] valueFields = modInfo.GetEditValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            
            Action<API.ModObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod
        public void DeleteMod(string oAuthToken, int gameId,
                              int modId,
                              Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        // Get All Modfiles
        public void GetAllModfiles(string apiKey, int gameId,
                                   int modId, GetAllModfilesFilter filter,
                                   Action<Modfile[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/files";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModfileObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get Modfile
        public void GetModfile(string apiKey, int gameId,
                               int modId, int modfileId,
                               Action<Modfile> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, Filter.None);

            
            Action<API.ModfileObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Modfile
        public void AddModfile(string oAuthToken, int gameId,
                               int modId, UnsubmittedModfile modfile,
                               Action<Modfile> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/files";
            StringValueField[] valueFields = modfile.GetValueFields();
            BinaryDataField[] dataFields = modfile.GetDataFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.ModfileObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                dataFields);
            
            Action<API.ModfileObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Edit Modfile
        public void EditModfile(string oAuthToken, int gameId,
                                EditableModfile modfile,
                                Action<Modfile> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modfile.modId + "/files/" + modfile.id;
            StringValueField[] valueFields = modfile.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            
            Action<API.ModfileObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        // Add GameInfo Media
        public void AddGameMedia(string oAuthToken, int gameId,
                                 UnsubmittedGameMedia gameMedia,
                                 Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/media";
            BinaryDataField[] dataFields = gameMedia.GetDataFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                null,
                                                                                dataFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod Media
        public void AddModMedia(string oAuthToken, int gameId,
                                int modId, UnsubmittedModMedia modMedia,
                                Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/media";
            StringValueField[] valueFields = modMedia.GetValueFields();
            BinaryDataField[] dataFields = modMedia.GetDataFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                dataFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Media
        public void DeleteModMedia(string oAuthToken, int gameId,
                                   int modId, ModMediaToDelete modMediaToDelete,
                                   Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/media";
            StringValueField[] valueFields = modMediaToDelete.GetValueFields();

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        public void SubscribeToMod(string oAuthToken, int gameId,
                                   int modId,
                                   Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                null,
                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        public void UnsubscribeFromMod(string oAuthToken, int gameId,
                                       int modId,
                                       Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ EVENT ENDPOINTS ]---------
        // Get Mod Events
        public void GetModEvents(string apiKey, int gameId,
                                 int modId, GetModEventFilter filter,
                                 Action<ModEvent[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModEventObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModEventObject, ModEvent>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get All Mod Events
        public void GetAllModEvents(string apiKey, int gameId,
                                    GetAllModEventsFilter filter,
                                    Action<ModEvent[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModEventObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModEventObject, ModEvent>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ TAG ENDPOINTS ]---------
        // Get All Game Tag Options
        public void GetAllGameTagOptions(string apiKey, int gameId,
                                         Action<GameTagOption[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, Filter.None);

            Action<API.ObjectArray<API.GameTagOptionObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameTagOptionObject, GameTagOption>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }

        // Add Game Tag Option
        public void AddGameTagOption(string oAuthToken, int gameId,
                                     UnsubmittedGameTagOption tagOption,
                                     Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/tags";
            StringValueField[] valueFields = tagOption.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }

        // Delete Game Tag Option
        public void DeleteGameTagOption(string oAuthToken, int gameId,
                                        GameTagOptionToDelete gameTagOptionToDelete,
                                        Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/tags";
            StringValueField[] valueFields = gameTagOptionToDelete.GetValueFields();

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }

        // Get All Mod Tags
        public void GetAllModTags(string apiKey, int gameId,
                                  int modId, GetAllModTagsFilter filter,
                                  Action<ModTag[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModTagObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModTagObject, ModTag>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod Tags
        public void AddModTags(string oAuthToken, int gameId,
                               int modId, string[] tagNames,
                               Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/tags";
            StringValueField[] valueFields = new StringValueField[tagNames.Length];

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Tags
        public void DeleteModTags(string oAuthToken, int gameId,
                                  int modId, string[] tagsToDelete,
                                  Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/tags";
            StringValueField[] valueFields = new StringValueField[tagsToDelete.Length];
            for(int i = 0; i < tagsToDelete.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("tags[]", tagsToDelete[i]);
            }

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ RATING ENDPOINTS ]---------
        // Add Mod Rating
        public void AddModRating(string oAuthToken, int gameId,
                                 int modId, int ratingValue,
                                 Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/ratings";
            StringValueField[] valueFields = new StringValueField[]
            {
                StringValueField.Create("rating", ratingValue),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ METADATA ENDPOINTS ]---------
        // Get All Mod KVP Metadata
        public void GetAllModKVPMetadata(string apiKey, int gameId,
                                         int modId,
                                         Action<MetadataKVP[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, Filter.None);

            Action<API.ObjectArray<API.MetadataKVPObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MetadataKVPObject, MetadataKVP>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod KVP Metadata
        public void AddModKVPMetadata(string oAuthToken, int gameId,
                                      int modId, UnsubmittedMetadataKVP[] metadataKVPs,
                                      Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/metadatakvp";
            StringValueField[] valueFields = new StringValueField[metadataKVPs.Length];
            for(int i = 0; i < metadataKVPs.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("metadata[]",
                                                             metadataKVPs[i].key + ":" + metadataKVPs[i].value);
            }

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod KVP Metadata
        public void DeleteModKVPMetadata(string oAuthToken, int gameId,
                                         int modId, UnsubmittedMetadataKVP[] metadataKVPsToRemove,
                                         Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/metadatakvp";
            StringValueField[] valueFields = new StringValueField[metadataKVPsToRemove.Length];
            for(int i = 0; i < metadataKVPsToRemove.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("metadata[]",
                                                             metadataKVPsToRemove[i].key + ":" + metadataKVPsToRemove[i].value);
            }

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
        public void GetAllModDependencies(string apiKey, int gameId,
                                          int modId, GetAllModDependenciesFilter filter,
                                          Action<ModDependency[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModDependencyObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModDependencyObject, ModDependency>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod Dependencies
        public void AddModDependencies(string oAuthToken, int gameId,
                                       int modId, int[] requiredModIds,
                                       Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/dependencies";
            StringValueField[] valueFields = new StringValueField[requiredModIds.Length];
            for(int i = 0; i < requiredModIds.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("dependencies[]", requiredModIds[i]);
            }

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Dependencies
        public void DeleteModDependencies(string oAuthToken, int gameId,
                                          int modId, int[] modIdsToRemove,
                                          Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/dependencies";
            StringValueField[] valueFields = new StringValueField[modIdsToRemove.Length];
            for(int i = 0; i < modIdsToRemove.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("dependencies[]", modIdsToRemove[i]);
            }

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ TEAM ENDPOINTS ]---------
        // Get All Mod Team Members
        public void GetAllModTeamMembers(string apiKey, int gameId,
                                         int modId, GetAllModTeamMembersFilter filter,
                                         Action<TeamMember[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.TeamMemberObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.TeamMemberObject, TeamMember>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod Team Member
        public void AddModTeamMember(string oAuthToken, int gameId,
                                     int modId, UnsubmittedTeamMember teamMember,
                                     Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team";
            StringValueField[] valueFields = teamMember.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Update Mod Team Member
        // NOTE(@jackson): Untested
        public void UpdateModTeamMember(string oAuthToken, int gameId,
                                        int modId, EditableTeamMember teamMember,
                                        Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team/" + teamMember.id;
            StringValueField[] valueFields = teamMember.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Team Member
        public void DeleteModTeamMember(string oAuthToken, int gameId,
                                        int modId, int teamMemberId,
                                        Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
        // NOTE(@jackson): Untested
        public void GetAllModComments(string apiKey, int gameId,
                                      int modId, GetAllModCommentsFilter filter,
                                      Action<UserComment[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/comments";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.CommentObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.CommentObject, UserComment>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get Mod Comment
        // NOTE(@jackson): Untested
        public void GetModComment(string apiKey, int gameId,
                                  int modId, int commentId,
                                  Action<UserComment> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, Filter.None);

            
            Action<API.CommentObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.CommentObject, UserComment>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Comment
        // NOTE(@jackson): Untested
        public void DeleteModComment(string oAuthToken, int gameId,
                                     int modId, int commentId,
                                     Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
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
        public void GetResourceOwner(string oAuthToken,
                                     ResourceType resourceType, int resourceID,
                                     Action<User> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "general/owner";
            StringValueField[] valueFields = new StringValueField[]
            {
                StringValueField.Create("resource_type", resourceType.ToString().ToLower()),
                StringValueField.Create("resource_id", resourceID),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.UserObject>(endpointURL,
                                                                             oAuthToken,
                                                                             valueFields,
                                                                             null);
            
            Action<API.UserObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.UserObject, User>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get All Users
        public void GetAllUsers(string apiKey,
                                GetAllUsersFilter filter,
                                Action<User[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "users";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.UserObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.UserObject, User>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User
        public void GetUser(string apiKey,
                            int userID,
                            Action<User> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "users/" + userID;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, apiKey, Filter.None);

            
            Action<API.UserObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.UserObject, User>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // Submit Report
        // NOTE(@jackson): Untested
        public void SubmitReport(string oAuthToken,
                                 UnsubmittedReport report,
                                 Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "report";
            StringValueField[] valueFields = report.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        

        // ---------[ ME ENDPOINTS ]---------
        // Get Authenticated User
        public void GetAuthenticatedUser(string oAuthToken,
                                         Action<User> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<API.UserObject>(endpointURL,
                                                                                            oAuthToken,
                                                                                            Filter.None);
            
            Action<API.UserObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.UserObject, User>(result, successCallback);
            };

            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User Subscriptions
        public void GetUserSubscriptions(string oAuthToken,
                                         GetUserSubscriptionsFilter filter,
                                         Action<ModInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/subscribed";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<API.ModObject[]>(endpointURL,
                                                                                             oAuthToken,
                                                                                             filter);
            Action<API.ObjectArray<API.ModObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User Games
        public void GetUserGames(string oAuthToken,
                                 Action<GameInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/games";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<API.GameObject[]>(endpointURL,
                                                                                              oAuthToken,
                                                                                              Filter.None);
            Action<API.ObjectArray<API.GameObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameObject, GameInfo>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User Mods
        public void GetUserMods(string oAuthToken,
                                Action<ModInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/mods";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<API.ModObject[]>(endpointURL,
                                                                                             oAuthToken,
                                                                                             Filter.None);
            Action<API.ObjectArray<API.ModObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User Files
        public void GetUserModfiles(string oAuthToken,
                                    Action<Modfile[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/files";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<API.ModfileObject[]>(endpointURL,
                                                                                                 oAuthToken,
                                                                                                 Filter.None);
            Action<API.ObjectArray<API.ModfileObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };
            APIClient.SendRequest(webRequest, onSuccess, errorCallback);
        }
    }
}
