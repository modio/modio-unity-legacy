#define USE_TEST_API
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

            // public byte[] data;
            public string data = "";
        }
        public class PostRequest
        {
            public class BinaryData
            {
                public byte[] contents = null;
                public string fileName = null;
                public string mimeType = null;
            }

            public string endpoint = "";
            public string oAuthToken = "";
            public Dictionary<string, string> fieldValues = new Dictionary<string, string>();
            public Dictionary<string, BinaryData> fieldData = new Dictionary<string, BinaryData>();

            public void AddFieldsToForm(WWWForm form)
            {
                foreach(KeyValuePair<string, string> kvp in fieldValues)
                {
                    form.AddField(kvp.Key, kvp.Value);
                }
                foreach(KeyValuePair<string, PostRequest.BinaryData> kvp in fieldData)
                {
                    form.AddBinaryData(kvp.Key, kvp.Value.contents, kvp.Value.fileName, kvp.Value.mimeType);
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

        #if USE_TEST_API
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
                           && headerValue.Length > 14) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 20) + " [token truncated]";   
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
            
            UnityWebRequest webRequest = UnityWebRequest.Put(constructedURL, request.data);
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
                           && headerValue.Length > 14) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 20) + " [token truncated]";   
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                Debug.Log("EXECUTING PUT REQUEST"
                          + "\nEndpoint: " + constructedURL
                          + "\nHeaders: " + requestHeaders
                          + "\nData:\n" + request.data
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
                           && headerValue.Length > 14) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 14) + "...[TOKEN TRUNCATED FOR LOGGING]";   
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                string formFields = "";
                foreach(KeyValuePair<string, string> kvp in request.fieldValues)
                {
                    formFields += "\n" + kvp.Key + "=" + kvp.Value;
                }
                foreach(KeyValuePair<string, PostRequest.BinaryData> kvp in request.fieldData)
                {
                    formFields += "\n" + kvp.Key + "= [BINARY DATA] " + kvp.Value.fileName + "\n";
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
                           && headerValue.Length > 14) // Contains more than "Bearer "
                        {
                            requestHeaders += "\n" + headerKey + ": "
                                + headerValue.Substring(0, 14) + "...[TOKEN TRUNCATED FOR LOGGING]";   
                        }
                        else
                        {
                            requestHeaders += "\n" + headerKey + ": " + headerValue;
                        }
                    }
                }

                // string formFields = "";
                // foreach(KeyValuePair<string, string> kvp in request.fieldValues)
                // {
                //     formFields += "\n" + kvp.Key + "=" + kvp.Value;
                // }
                // foreach(KeyValuePair<string, Request.BinaryData> kvp in request.fieldData)
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
        public int gameID { get; private set; }
        private string apiKey = "";

        public void SetAccessContext(int gameID, string apiKey)
        {
            this.gameID = gameID;
            this.apiKey = apiKey;
        }

        // ---------[ AUTHENTICATION ]---------
        public void RequestSecurityCode(string emailAddress,
                                        ObjectCallback<APIMessage> onSuccess,
                                        ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "oauth/emailrequest";
            request.fieldValues.Add("api_key", apiKey);
            request.fieldValues.Add("email", emailAddress);

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
            request.fieldValues.Add("api_key", apiKey);
            request.fieldValues.Add("security_code", securityCode);

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
            string endpoint = "games/" + gameID;
            
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
            string endpoint = "games/" + gameID;
            onError(GenerateNotImplementedError(endpoint + ":PUT"));
        }

        // ---------[ MOD ENDPOINTS ]---------
        // Get All Mods
        public void GetAllMods(GetAllModsFilter filter,
                               ObjectCallback<ModInfo[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods";

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
        public void GetMod(int modID,
                           ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID;
            
            StartCoroutine(ExecuteQuery<API.ModObject>(endpoint, 
                                                       apiKey, 
                                                       Filter.None, 
                                                       result => OnSuccessWrapper(onSuccess, result), 
                                                       onError));
        }
        // Add Mod
        public void AddMod(string oAuthToken,
                           ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "games/" + gameID + "/mods";
            request.oAuthToken = oAuthToken;
            // request.fieldValues[]

            onError(GenerateNotImplementedError(request.endpoint + ":POST"));
        }
        // Edit Mod
        public void EditMod(int modID,
                            ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID;
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Delete Mod
        public void DeleteMod(int modID,
                              ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID;
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }

        // ---------[ MODFILE ENDPOINTS ]---------
        // Get All Modfiles
        public void GetAllModfiles(int modID, GetAllModfilesFilter filter,
                                   ObjectCallback<Modfile[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/files";

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
        public void GetModfile(int modID, int modfileID,
                               ObjectCallback<Modfile> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/files/" + modfileID;

            StartCoroutine(ExecuteQuery<API.ModfileObject>(endpoint, 
                                                           apiKey, 
                                                           Filter.None, 
                                                           result => OnSuccessWrapper(onSuccess, result), 
                                                           onError));
        }
        // Add Modfile
        public void AddModfile(int modID,
                               ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/files";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Edit Mod
        public void EditMod(int modID, int modfileID,
                            ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/files/" + modfileID;
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }

        // ---------[ MEDIA ENDPOINTS ]---------
        // Add GameInfo Media
        public void AddGameMedia(ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/media";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Add Mod Media
        public void AddModMedia(int modID,
                                ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/media";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Delete Mod Media
        public void DeleteModMedia(int modID,
                                   ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/media";
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }

        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        public void SubscribeToMod(string oAuthToken,
                                   int modID,
                                   ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            PostRequest request = new PostRequest();
            request.endpoint = "games/" + gameID + "/mods/" + modID + "/subscribe";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecutePostRequest<API.MessageObject>(request,
                                                                 result => OnSuccessWrapper(onSuccess, result),
                                                                 onError));
        }
        public void UnsubscribeFromMod(string oAuthToken,
                                       int modID,
                                       ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            DeleteRequest request = new DeleteRequest();
            request.endpoint = "games/" + gameID + "/mods/" + modID + "/subscribe";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecuteDeleteRequest<API.MessageObject>(request,
                                                                   result => OnSuccessWrapper(onSuccess, result),
                                                                   onError));
        }

        // ---------[ EVENT ENDPOINTS ]---------
        // Get Mod Events
        public void GetModEvents(int modID, GetModEventFilter filter, 
                                 ObjectCallback<ModEvent[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/events";

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
            string endpoint = "games/" + gameID + "/mods/events";

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
        public void GetAllModTags(int modID, GetAllModTagsFilter filter,
                                  ObjectCallback<ModTag[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/tags";

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
        public void AddModTag(int modID,
                                ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/tags";

            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Delete Mod Tag
        public void DeleteModTag(int modID,
                                 ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/tags";

            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }

        // ---------[ RATING ENDPOINTS ]---------
        // Add Mod Rating
        public void AddModRating(int modID, int ratingValue,
                                 ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/ratings";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }

        // ---------[ METADATA ENDPOINTS ]---------
        // Get All Mod KVP Metadata
        public void GetAllModKVPMetadata(int modID,
                                         ObjectCallback<MetadataKVP[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/metadatakvp";

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
        public void AddModKVPMetadata(int modID,
                                      ObjectCallback<MetadataKVP> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/metadatakvp";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Delete Mod KVP Metadata
        public void DeleteModKVPMetadata(int modID,
                                         ObjectCallback<MetadataKVP> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/metadatakvp";
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }

        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
        public void GetAllModDependencies(int modID, GetAllModDependenciesFilter filter,
                                          ObjectCallback<ModDependency[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/dependencies";

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
        public void AddModDependency(int modID,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/dependencies";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Delete Mod Dependencies
        public void DeleteModDependencies(int modID,
                                          ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/dependencies";
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }


        // ---------[ TEAM ENDPOINTS ]---------
        // Get All Mod Team Members
        public void GetAllModTeamMembers(int modID, GetAllModTeamMembersFilter filter,
                                         ObjectCallback<TeamMember[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/team";

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
            string endpoint = "games/" + gameID + "/team";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Add Mod Team Member
        public void AddModTeamMember(int modID,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/team";
            onError(GenerateNotImplementedError(endpoint + ":POST"));
        }
        // Update GameInfo Team Member
        public void UpdateGameTeamMember(int teamMemberID,
                                         ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/team/" + teamMemberID;
            onError(GenerateNotImplementedError(endpoint + ":PUT"));
        }
        // Update Mod Team Member
        public void UpdateModTeamMember(int modID, int teamMemberID,
                                        ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/team/" + teamMemberID;
            onError(GenerateNotImplementedError(endpoint + ":PUT"));
        }
        // Delete GameInfo Team Member
        public void DeleteGameTeamMember(int teamMemberID,
                                         ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/team/" + teamMemberID;
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }
        // Delete Mod Team Member
        public void DeleteModTeamMember(int modID, int teamMemberID,
                                        ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/team/" + teamMemberID;
            onError(GenerateNotImplementedError(endpoint + ":DELETE"));
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
        public void GetAllModComments(int modID, GetAllModCommentsFilter filter,
                                      ObjectCallback<UserComment[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/comments";

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
        public void GetModComment(int modID, int commentID,
                                  ObjectCallback<UserComment> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/comments/" + commentID;

            StartCoroutine(ExecuteQuery<API.CommentObject>(endpoint, 
                                                           apiKey, 
                                                           Filter.None, 
                                                           result => OnSuccessWrapper(onSuccess, result),
                                                           onError));
        }
        // Delete Mod Comment
        public void DeleteModComment(int modID, int commentID,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/comments/" + commentID;
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
            request.fieldValues.Add("resource_type", resourceType.ToString().ToLower());
            request.fieldValues.Add("resource_id", resourceID.ToString());

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
