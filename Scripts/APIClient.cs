#define USING_TEST_SERVER
#define LOG_ALL_QUERIES

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public delegate void ErrorCallback(ErrorInfo errorInfo);
    public delegate void ObjectCallback<T>(T requestedObject); 
    public delegate void DownloadCallback(byte[] data);

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
            StringValueField retVal = new StringValueField();
            retVal.key = k;
            retVal.value = v.ToString();
            return retVal;
        }
    }

    public class APIClient : MonoBehaviour
    {
        private static ErrorInfo GenerateNotImplementedError(string url)
        {
            ErrorInfo retVal = new ErrorInfo();
            retVal.httpStatusCode = -1;
            retVal.message = "This APIClient function has not yet been implemented";
            retVal.url = url;
            return retVal;
        }

        private static void OnSuccessWrapper<T, T_APIObj>(ObjectCallback<T> onSuccess, 
                                                          T_APIObj apiResult) 
                                                          where T_APIObj : struct 
                                                          where T : IAPIObjectWrapper<T_APIObj>, new()
        {
            T retVal = new T();
            retVal.WrapAPIObject(apiResult);

            onSuccess(retVal);
        }

        private static T[] WrapArray<T, T_APIObj>(T_APIObj[] apiObjectArray) 
                                                  where T_APIObj : struct 
                                                  where T : IAPIObjectWrapper<T_APIObj>, new()
        {
            T[] retVal = new T[apiObjectArray.Length];
            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                T newObject = new T();
                newObject.WrapAPIObject(apiObjectArray[i]);

                retVal[i] = newObject;
            }
            return retVal;
        }

        // ---------[ INNER CLASSES ]---------
        public class GetRequest
        {
            public string endpoint = "";
            public string oAuthToken = "";
            public Filter filter = Filter.None;
        }
        public class PutRequest
        {
            public string endpoint = "";
            public string oAuthToken = "";

            public StringValueField[] valueFields = new StringValueField[0];

            public void AddFieldsToForm(WWWForm form)
            {
                foreach(StringValueField valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }
        }
        public class PostRequest
        {
            public string endpoint = "";
            public string oAuthToken = "";
            public StringValueField[] valueFields = new StringValueField[0];
            public BinaryDataField[] dataFields = new BinaryDataField[0];

            public void AddFieldsToForm(WWWForm form)
            {
                foreach(StringValueField valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }

                foreach(BinaryDataField dataField in dataFields)
                {
                    form.AddBinaryData(dataField.key, dataField.contents, dataField.fileName, dataField.mimeType);
                }
            }
        }
        public class DeleteRequest
        {
            public string endpoint = "";
            public string oAuthToken = "";
        }

        // ---------[ CONSTANTS ]---------
        public const string VERSION = "v1";

        #if USING_TEST_SERVER
        public const string URL = "https://api.test.mod.io/" + VERSION + "/";
        #else
        public const string URL = "https://api.mod.io/" + VERSION + "/";
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
        public static void IgnoreSuccess(object result) {}
        public static void IgnoreError(ErrorInfo errorInfo) {}
        public static void LogError(ErrorInfo errorInfo)
        {
            string errorMessage = "API ERROR";
            errorMessage += "\nURL: " + errorInfo.url;
            errorMessage += "\nCode: " + errorInfo.httpStatusCode;
            errorMessage += "\nMessage: " + errorInfo.message;
            // errorMessage += "\nHeaders:";
            // foreach(KeyValuePair<string, string> header in errorInfo.headers)
            // {
            //     errorMessage += "\n\t" + header.Key + ": " + header.Value;
            // }
            errorMessage += "\n";

            Debug.LogWarning(errorMessage);
        }

        // ---------[ CORE FUNCTIONS ]---------
        public static IEnumerator ExecuteQuery<T>(string endpoint,
                                                  string apiKey,
                                                  Filter queryFilter,
                                                  ObjectCallback<T> onSuccess,
                                                  ErrorCallback onError)
        {
            string queryURL = URL + endpoint
                + "?api_key=" + apiKey
                + "&" + queryFilter.GenerateQueryString();

            #if LOG_ALL_QUERIES
            Debug.Log("EXECUTING QUERY"
                      + "\nQuery: " + queryURL
                      + "\n");
            #endif

            UnityWebRequest webRequest = UnityWebRequest.Get(queryURL);
            yield return webRequest.SendWebRequest();
            
            ProcessJSONResponse<T>(webRequest, onSuccess, onError);
        }

        public static IEnumerator ExecuteGetRequest<T>(GetRequest request,
                                                       ObjectCallback<T> onSuccess,
                                                       ErrorCallback onError)
        {
            string constructedURL = URL + request.endpoint + "?" + request.filter.GenerateQueryString();
            
            UnityWebRequest webRequest = UnityWebRequest.Get(constructedURL);
            webRequest.SetRequestHeader("Authorization", "Bearer " + request.oAuthToken);

            #if LOG_ALL_QUERIES
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

                Debug.Log("EXECUTING GET REQUEST"
                          + "\nEndpoint: " + constructedURL
                          + "\nHeaders: " + requestHeaders
                          + "\n"
                          );
            }
            #endif

            yield return webRequest.SendWebRequest();

            ProcessJSONResponse<T>(webRequest, onSuccess, onError);
        }

        public static IEnumerator ExecutePutRequest<T>(PutRequest request,
                                                       ObjectCallback<T> onSuccess,
                                                       ErrorCallback onError)
        {
            string constructedURL = URL + request.endpoint;
            
            WWWForm form = new WWWForm();
            request.AddFieldsToForm(form);

            UnityWebRequest webRequest = UnityWebRequest.Post(constructedURL, form);
            webRequest.method = UnityWebRequest.kHttpVerbPUT;
            webRequest.SetRequestHeader("Authorization", "Bearer " + request.oAuthToken);

            #if LOG_ALL_QUERIES
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
                foreach(StringValueField svf in request.valueFields)
                {
                    formFields += "\n" + svf.key + "=" + svf.value;
                }

                Debug.Log("EXECUTING PUT REQUEST"
                          + "\nEndpoint: " + constructedURL
                          + "\nHeaders: " + requestHeaders
                          + "\nFields: " + formFields
                          + "\n"
                          );
            }
            #endif

            yield return webRequest.SendWebRequest();

            ProcessJSONResponse<T>(webRequest, onSuccess, onError);
        }

        public static IEnumerator ExecutePostRequest<T>(PostRequest request,
                                                        ObjectCallback<T> onSuccess,
                                                        ErrorCallback onError)
        {
            string constructedURL = URL + request.endpoint;

            WWWForm form = new WWWForm();
            request.AddFieldsToForm(form);

            UnityWebRequest webRequest = UnityWebRequest.Post(constructedURL, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + request.oAuthToken);

            #if LOG_ALL_QUERIES
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
                foreach(StringValueField valueField in request.valueFields)
                {
                    formFields += "\n" + valueField.key + "=" + valueField.value;
                }
                foreach(BinaryDataField dataField in request.dataFields)
                {
                    formFields += "\n" + dataField.key + "= [BINARY DATA]: "
                                + dataField.fileName + "("
                                + (dataField.contents.Length/1000f).ToString("0.00") + "KB)\n";
                }

                Debug.Log("EXECUTING POST REQUEST"
                          + "\nEndpoint: " + constructedURL
                          + "\nHeaders: " + requestHeaders
                          + "\nFields: " + formFields
                          + "\n"
                          );
            }
            #endif

            yield return webRequest.SendWebRequest();

            ProcessJSONResponse<T>(webRequest, onSuccess, onError);
        }

        public static IEnumerator ExecuteDeleteRequest<T>(DeleteRequest request,
                                                          ObjectCallback<T> onSuccess,
                                                          ErrorCallback onError)
        {
            string constructedURL = URL + request.endpoint;// + "?" + request.filter.GenerateQueryString();

            // WWWForm form = new WWWForm();
            // request.AddFieldsToForm(form);

            UnityWebRequest webRequest = UnityWebRequest.Post(constructedURL, "");
            webRequest.method = UnityWebRequest.kHttpVerbDELETE;
            webRequest.SetRequestHeader("Authorization", "Bearer " + request.oAuthToken);

            #if LOG_ALL_QUERIES
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

                // string formFields = "";
                // foreach(StringValueField kvp in request.valueFields)
                // {
                //     formFields += "\n" + kvp.Key + "=" + kvp.Value;
                // }
                // foreach(KeyValuePair<string, Request.BinaryData> kvp in request.dataFields)
                // {
                //     formFields += "\n" + kvp.Key + "= [BINARY DATA] " + kvp.Value.fileName + "\n";
                // }

                Debug.Log("EXECUTING DELETE REQUEST"
                          + "\nEndpoint: " + constructedURL
                          + "\nHeaders: " + requestHeaders
                          // + "\nFields: " + formFields
                          + "\n"
                          );
            }
            #endif

            yield return webRequest.SendWebRequest();

            ProcessJSONResponse<T>(webRequest, onSuccess, onError);
        }

        private static void ProcessJSONResponse<T>(UnityWebRequest webRequest,
                                                   ObjectCallback<T> onSuccess,
                                                   ErrorCallback onError)
        {
            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                ErrorInfo errorInfo = ErrorInfo.GenerateFromWebRequest(webRequest);

                #if LOG_ALL_QUERIES
                if(onError != APIClient.LogError)
                {
                    APIClient.LogError(errorInfo);
                }
                #endif

                onError(errorInfo);

                return;
            }

            #if LOG_ALL_QUERIES
            Debug.Log("API REQUEST SUCEEDED"
                      + "\nQuery: " + webRequest.url
                      + "\nResponse: " + webRequest.downloadHandler.text
                      + "\n");
            #endif

            T response = JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
            onSuccess(response);
        }

        // ---------[ ACCESS CONTEXT ]---------
        public int gameId { get; private set; }
        private string apiKey = "";

        public void SetAccessContext(int gameId, string apiKey)
        {
            this.gameId = gameId;
            this.apiKey = apiKey;
        }


        // ---------[ AUTHENTICATION ]---------
        public void RequestSecurityCode(string emailAddress,
                                        ObjectCallback<APIMessage> onSuccess,
                                        ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "oauth/emailrequest";
            request.valueFields = new StringValueField[2];
            request.valueFields[0] = StringValueField.Create("api_key", apiKey);
            request.valueFields[1] = StringValueField.Create("email", emailAddress);

            StartCoroutine(ExecutePostRequest<API.MessageObject>(request, 
                                                                 result => OnSuccessWrapper(onSuccess, result),
                                                                 onError));
        }
        public void RequestOAuthToken(string securityCode,
                                      ObjectCallback<string> onSuccess,
                                      ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "oauth/emailexchange";
            request.valueFields = new StringValueField[2];
            request.valueFields[0] = StringValueField.Create("api_key", apiKey);
            request.valueFields[1] = StringValueField.Create("security_code", securityCode);

            StartCoroutine(ExecutePostRequest<API.AccessTokenObject>(request, 
                                                                     data => onSuccess(data.access_token),
                                                                     onError));
        }


        // ---------[ GAME ENDPOINTS ]---------
        // Get All Games
        public void GetAllGames(GetAllGamesFilter filter,
                                ObjectCallback<GameInfo[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games";

            ObjectCallback<API.ObjectArray<API.GameObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<GameInfo, API.GameObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        filter,
                                        onSuccessArrayWrapper,
                                        onError));
        }

        // Get GameInfo
        public void GetGame(ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId;
            
            StartCoroutine(ExecuteQuery<API.GameObject>(endpoint,
                                                        apiKey,
                                                        Filter.None,
                                                        result => OnSuccessWrapper(onSuccess, result),
                                                        onError));
        }

        // Edit GameInfo
        public void EditGame(string oAuthToken,
                             EditableGameInfo gameInfo,
                             ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            PutRequest request = new PutRequest();

            request.endpoint = "games/" + gameInfo.id;
            request.oAuthToken = oAuthToken;
            request.valueFields = gameInfo.GetValueFields();

            StartCoroutine(ExecutePutRequest<API.GameObject>(request, 
                                                             result => OnSuccessWrapper(onSuccess, result), 
                                                             onError));
        }


        // ---------[ MOD ENDPOINTS ]---------
        // Get All Mods
        public void GetAllMods(GetAllModsFilter filter,
                               ObjectCallback<ModInfo[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods";

            ObjectCallback<API.ObjectArray<API.ModObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<ModInfo, API.ModObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        filter,
                                        onSuccessArrayWrapper,
                                        onError));
        }
        // Get Mod
        public void GetMod(int modId,
                           ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId;
            
            StartCoroutine(ExecuteQuery<API.ModObject>(endpoint, 
                                                       apiKey, 
                                                       Filter.None, 
                                                       result => OnSuccessWrapper(onSuccess, result), 
                                                       onError));
        }
        // Add Mod
        public void AddMod(string oAuthToken,
                           AddableModInfo modInfo,
                           ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "games/" + gameId + "/mods";
            request.oAuthToken = oAuthToken;
            request.valueFields = modInfo.GetValueFields();
            request.dataFields = modInfo.GetDataFields();

            StartCoroutine(ExecutePostRequest<API.ModObject>(request, 
                                                             result => OnSuccessWrapper(onSuccess, result),
                                                             onError));
        }
        // Edit Mod
        public void EditMod(string oAuthToken,
                            EditableModInfo modInfo,
                            ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            PutRequest request = new PutRequest();

            request.endpoint = "games/" + gameId + "/mods/" + modInfo.id;
            request.oAuthToken = oAuthToken;
            request.valueFields = modInfo.GetValueFields();

            StartCoroutine(ExecutePutRequest<API.ModObject>(request, 
                                                            result => OnSuccessWrapper(onSuccess, result),
                                                            onError));
        }
        // Delete Mod
        public void DeleteMod(int modId,
                              ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId;
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        // Get All Modfiles
        public void GetAllModfiles(int modId, GetAllModfilesFilter filter,
                                   ObjectCallback<Modfile[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/files";

            ObjectCallback<API.ObjectArray<API.ModfileObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<Modfile, API.ModfileObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        filter,
                                        onSuccessArrayWrapper,
                                        onError));
        }
        // Get Modfile
        public void GetModfile(int modId, int modfileId,
                               ObjectCallback<Modfile> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/files/" + modfileId;

            StartCoroutine(ExecuteQuery<API.ModfileObject>(endpoint, 
                                                           apiKey, 
                                                           Filter.None, 
                                                           result => OnSuccessWrapper(onSuccess, result), 
                                                           onError));
        }
        // Add Modfile
        public void AddModfile(string oAuthToken,
                               int modId, UnsubmittedModfile modfile,
                               ObjectCallback<Modfile> onSuccess, ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "games/" + gameId + "/mods/" + modId + "/files";
            request.oAuthToken = oAuthToken;
            request.valueFields = modfile.GetValueFields();
            request.dataFields = modfile.GetDataFields();

            StartCoroutine(ExecutePostRequest<API.ModfileObject>(request,
                                                                 result => OnSuccessWrapper(onSuccess, result),
                                                                 onError));
        }
        // Edit Modfile
        public void EditModfile(string oAuthToken,
                                EditableModfile modfile,
                                ObjectCallback<Modfile> onSuccess, ErrorCallback onError)
        {
            PutRequest request = new PutRequest();

            request.endpoint = "games/" + gameId + "/mods/" + modfile.modId + "/files/" + modfile.id;
            request.oAuthToken = oAuthToken;
            request.valueFields = modfile.GetValueFields();

            StartCoroutine(ExecutePutRequest<API.ModfileObject>(request, 
                                                                result => OnSuccessWrapper(onSuccess, result), 
                                                                onError));
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        // Add GameInfo Media
        public void AddGameMedia(string oAuthToken,
                                 UnsubmittedGameMedia gameMedia,
                                 ObjectCallback<string> onSuccess, ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "games/" + gameId + "/media";
            request.oAuthToken = oAuthToken;
            request.dataFields = gameMedia.GetDataFields();

            StartCoroutine(ExecutePostRequest<API.MessageObject>(request,
                                                                 result => onSuccess(result.message),
                                                                 onError));
        }
        // Add Mod Media
        public void AddModMedia(string oAuthToken,
                                int modId, UnsubmittedModMedia modMedia,
                                ObjectCallback<string> onSuccess, ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "games/" + gameId + "/mods/" + modId + "/media";
            request.oAuthToken = oAuthToken;
            // request.valueFields = modMedia.GetValueFields();
            request.dataFields = modMedia.GetDataFields();

            StartCoroutine(ExecutePostRequest<API.MessageObject>(request, 
                                                                 result => onSuccess(result.message), 
                                                                 onError));
        }
        // Delete Mod Media
        public void DeleteModMedia(int modId,
                                   ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/media";
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        public void SubscribeToMod(string oAuthToken,
                                   int modId,
                                   ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "games/" + gameId + "/mods/" + modId + "/subscribe";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecutePostRequest<API.MessageObject>(request,
                                                                 result => OnSuccessWrapper(onSuccess, result),
                                                                 onError));
        }
        public void UnsubscribeFromMod(string oAuthToken,
                                       int modId,
                                       ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            DeleteRequest request = new DeleteRequest();
            request.endpoint = "games/" + gameId + "/mods/" + modId + "/subscribe";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecuteDeleteRequest<API.MessageObject>(request,
                                                                   result => OnSuccessWrapper(onSuccess, result),
                                                                   onError));
        }


        // ---------[ EVENT ENDPOINTS ]---------
        // Get Mod Events
        public void GetModEvents(int modId, GetModEventFilter filter, 
                                 ObjectCallback<ModEvent[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/events";

            ObjectCallback<API.ObjectArray<API.ModEventObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<ModEvent, API.ModEventObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        filter,
                                        onSuccessArrayWrapper,
                                        onError));
        }
        // Get All Mod Events
        public void GetAllModEvents(GetAllModEventsFilter filter,
                                    ObjectCallback<ModEvent[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/events";

            ObjectCallback<API.ObjectArray<API.ModEventObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<ModEvent, API.ModEventObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        filter,
                                        onSuccessArrayWrapper,
                                        onError));
        }


        // ---------[ TAG ENDPOINTS ]---------
        // Get All Mod Tags
        public void GetAllModTags(int modId, GetAllModTagsFilter filter,
                                  ObjectCallback<ModTag[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/tags";

            ObjectCallback<API.ObjectArray<API.ModTagObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<ModTag, API.ModTagObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        filter,
                                        onSuccessArrayWrapper,
                                        onError));
        }
        // Add Mod Tag
        public void AddModTag(int modId,
                                ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/tags";

            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Delete Mod Tag
        public void DeleteModTag(int modId,
                                 ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/tags";

            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }


        // ---------[ RATING ENDPOINTS ]---------
        // Add Mod Rating
        public void AddModRating(int modId, int ratingValue,
                                 ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/ratings";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }


        // ---------[ METADATA ENDPOINTS ]---------
        // Get All Mod KVP Metadata
        public void GetAllModKVPMetadata(int modId,
                                         ObjectCallback<MetadataKVP[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/metadatakvp";

            ObjectCallback<API.ObjectArray<API.MetadataKVPObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<MetadataKVP, API.MetadataKVPObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        Filter.None,
                                        onSuccessArrayWrapper,
                                        onError));
        }
        // Add Mod KVP Metadata
        public void AddModKVPMetadata(int modId,
                                      ObjectCallback<MetadataKVP> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/metadatakvp";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Delete Mod KVP Metadata
        public void DeleteModKVPMetadata(int modId,
                                         ObjectCallback<MetadataKVP> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/metadatakvp";
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
        public void GetAllModDependencies(int modId, GetAllModDependenciesFilter filter,
                                          ObjectCallback<ModDependency[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/dependencies";

            ObjectCallback<API.ObjectArray<API.ModDependencyObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<ModDependency, API.ModDependencyObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        filter,
                                        onSuccessArrayWrapper,
                                        onError));
        }
        // Add Mod Dependency
        public void AddModDependency(int modId,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/dependencies";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Delete Mod Dependencies
        public void DeleteModDependencies(int modId,
                                          ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/dependencies";
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }


        // ---------[ TEAM ENDPOINTS ]---------
        // Get All Mod Team Members
        public void GetAllModTeamMembers(int modId, GetAllModTeamMembersFilter filter,
                                         ObjectCallback<TeamMember[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/team";

            ObjectCallback<API.ObjectArray<API.TeamMemberObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<TeamMember, API.TeamMemberObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        filter,
                                        onSuccessArrayWrapper,
                                        onError));
        }
        // Add GameInfo Team Member
        public void AddGameTeamMember(ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/team";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Add Mod Team Member
        public void AddModTeamMember(int modId,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/team";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Update GameInfo Team Member
        public void UpdateGameTeamMember(int teamMemberID,
                                         ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/team/" + teamMemberID;
            onError(GenerateNotImplementedError(endpoint + ":PUT"));
        }
        // Update Mod Team Member
        public void UpdateModTeamMember(int modId, int teamMemberID,
                                        ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/team/" + teamMemberID;
            onError(GenerateNotImplementedError(endpoint + ":PUT"));
        }
        // Delete GameInfo Team Member
        public void DeleteGameTeamMember(int teamMemberID,
                                         ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/team/" + teamMemberID;
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }
        // Delete Mod Team Member
        public void DeleteModTeamMember(int modId, int teamMemberID,
                                        ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/team/" + teamMemberID;
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
        public void GetAllModComments(int modId, GetAllModCommentsFilter filter,
                                      ObjectCallback<UserComment[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/comments";

            ObjectCallback<API.ObjectArray<API.CommentObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<UserComment, API.CommentObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint,
                                        apiKey,
                                        filter,
                                        onSuccessArrayWrapper,
                                        onError));
        }
        // Get Mod Comment
        public void GetModComment(int modId, int commentID,
                                  ObjectCallback<UserComment> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/comments/" + commentID;

            StartCoroutine(ExecuteQuery<API.CommentObject>(endpoint, 
                                                           apiKey, 
                                                           Filter.None, 
                                                           result => OnSuccessWrapper(onSuccess, result),
                                                           onError));
        }
        // Delete Mod Comment
        public void DeleteModComment(int modId, int commentID,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameId + "/mods/" + modId + "/comments/" + commentID;
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
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
                                     ObjectCallback<User> onSuccess, ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "general/owner";
            request.oAuthToken = oAuthToken;
            request.valueFields = new StringValueField[2];
            request.valueFields[0] = StringValueField.Create("resource_type", resourceType.ToString().ToLower());
            request.valueFields[1] = StringValueField.Create("resource_id", resourceID);

            StartCoroutine(ExecutePostRequest<API.UserObject>(request, 
                                                              result => OnSuccessWrapper(onSuccess, result), 
                                                              onError));
        }
        // Get All Users
        public void GetAllUsers(GetAllUsersFilter filter,
                                ObjectCallback<User[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "users";

            ObjectCallback<API.ObjectArray<API.UserObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<User, API.UserObject>(results.data));
            };

            StartCoroutine(ExecuteQuery(endpoint, 
                                        apiKey, 
                                        filter, 
                                        onSuccessArrayWrapper,
                                        onError));
        }
        // Get User
        public void GetUser(int userID,
                            ObjectCallback<User> onSuccess, ErrorCallback onError)
        {
            string endpoint = "users/" + userID;

            StartCoroutine(ExecuteQuery<API.UserObject>(endpoint, 
                                                        apiKey, 
                                                        Filter.None, 
                                                        result => OnSuccessWrapper(onSuccess, result), 
                                                        onError));
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // Submit Report
        public void SubmitReport(ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "report";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        

        // ---------[ ME ENDPOINTS ]---------
        // Get Authenticated User
        public void GetAuthenticatedUser(string oAuthToken,
                                         ObjectCallback<User> onSuccess, ErrorCallback onError)
        {
            GetRequest request = new GetRequest();
            request.endpoint = "me";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecuteGetRequest<API.UserObject>(request, 
                                                             result => OnSuccessWrapper(onSuccess, result), 
                                                             onError)); 
        }
        // Get User Subscriptions
        public void GetUserSubscriptions(string oAuthToken,
                                         GetUserSubscriptionsFilter filter,
                                         ObjectCallback<ModInfo[]> onSuccess, ErrorCallback onError)
        {
            GetRequest request = new GetRequest();
            request.endpoint = "me/subscribed";
            request.oAuthToken = oAuthToken;
            request.filter = filter;

            ObjectCallback<API.ObjectArray<API.ModObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<ModInfo, API.ModObject>(results.data));
            };

            StartCoroutine(ExecuteGetRequest(request, 
                                             onSuccessArrayWrapper,
                                             onError));
        }
        // Get User Games
        public void GetUserGames(string oAuthToken,
                                 ObjectCallback<GameInfo[]> onSuccess, ErrorCallback onError)
        {
            GetRequest request = new GetRequest();
            request.endpoint = "me/games";
            request.oAuthToken = oAuthToken;

            ObjectCallback<API.ObjectArray<API.GameObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<GameInfo, API.GameObject>(results.data));
            };

            StartCoroutine(ExecuteGetRequest(request, 
                                             onSuccessArrayWrapper, 
                                             onError));
        }
        // Get User Mods
        public void GetUserMods(string oAuthToken,
                                ObjectCallback<ModInfo[]> onSuccess, ErrorCallback onError)
        {
            GetRequest request = new GetRequest();
            request.endpoint = "me/mods";
            request.oAuthToken = oAuthToken;

            ObjectCallback<API.ObjectArray<API.ModObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<ModInfo, API.ModObject>(results.data));
            };

            StartCoroutine(ExecuteGetRequest(request,
                                             onSuccessArrayWrapper,
                                             onError));
        }
        // Get User Files
        public void GetUserModfiles(string oAuthToken,
                                    ObjectCallback<Modfile[]> onSuccess, ErrorCallback onError)
        {
            GetRequest request = new GetRequest();
            request.endpoint = "me/files";
            request.oAuthToken = oAuthToken;

            ObjectCallback<API.ObjectArray<API.ModfileObject>> onSuccessArrayWrapper = results =>
            {
                onSuccess(WrapArray<Modfile, API.ModfileObject>(results.data));
            };

            StartCoroutine(ExecuteGetRequest(request, 
                                             onSuccessArrayWrapper,
                                             onError));
        }
    }
}
