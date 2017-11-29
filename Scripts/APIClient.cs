#define LOG_ALL_QUERIES

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public delegate void ErrorCallback(APIErrorInformation errorInfo);
    public delegate void ObjectCallback<T>(T requestedObject);
    public delegate void ObjectArrayCallback<T>(T[] objectArray);
    public delegate void DownloadCallback(byte[] data);

    public class APIRequest
    {
        public class BinaryData
        {
            public byte[] contents = null;
            public string fileName = null;
            public string mimeType = null;
        }

        public string endpoint = "";
        public string oAuthToken = "";
        public Filter filter = Filter.NONE;
        public Dictionary<string, string> fieldValues = new Dictionary<string, string>();
        public Dictionary<string, BinaryData> fieldData = new Dictionary<string, BinaryData>();

        public void AddFieldsToForm(WWWForm form)
        {
            foreach(KeyValuePair<string, string> kvp in fieldValues)
            {
                form.AddField(kvp.Key, kvp.Value);
            }
            foreach(KeyValuePair<string, APIRequest.BinaryData> kvp in fieldData)
            {
                form.AddBinaryData(kvp.Key, kvp.Value.contents, kvp.Value.fileName, kvp.Value.mimeType);
            }
        }
    }

    public class APIErrorInformation
    {
        public static APIErrorInformation GenerateNotImplemented(string url)
        {
            APIErrorInformation retVal = new APIErrorInformation();
            retVal.message = "This APIClient function has not yet been implemented";
            retVal.url = url;
            return retVal;
        }

        public int code = -1;
        public string message = "";
        public string url = "";
        public Dictionary<string, string> headers = new Dictionary<string, string>(0);

    }

    public class APIClient : MonoBehaviour
    {
        // ---------[ CONSTANTS ]---------
        public const string VERSION = "v1";
        public const string URL = "https://api.mod.io/" + VERSION + "/";
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

        // ---------[ FIELDS ]---------
        public int gameID = 0;
        public string apiKey = "";
        public string oAuthToken = "";

        public static void IgnoreSuccess(object result) {}
        public static void IgnoreError(APIErrorInformation errorInformation) {}
        public static void LogError(APIErrorInformation errorInformation)
        {
            string errorMessage = "API ERROR";
            errorMessage += "\nURL: " + errorInformation.url;
            errorMessage += "\nCode: " + errorInformation.code;
            errorMessage += "\nMessage:" + errorInformation.message;
            errorMessage += "\nHeaders:";
            foreach(KeyValuePair<string, string> header in errorInformation.headers)
            {
                errorMessage += "\n\t" + header.Key + ": " + header.Value;
            }
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
            #if LOG_ALL_QUERIES
            Debug.Log("EXECUTING QUERY"
                      + "\nQuery: " + URL + endpoint);
            #endif

            string queryURL = URL + endpoint
                + "?api_key=" + apiKey
                + "&" + queryFilter.GenerateQueryString();

            UnityWebRequest webRequest = UnityWebRequest.Get(queryURL);
            yield return webRequest.SendWebRequest();
            
            ProcessJSONResponse<T>(webRequest, onSuccess, onError);
        }

        public static IEnumerator ExecuteGetRequest<T>(APIRequest request,
                                                       ObjectCallback<T> onSuccess,
                                                       ErrorCallback onError)
        {
            Debug.Assert((request.fieldValues == null || request.fieldValues.Count == 0)
                         && (request.fieldData == null || request.fieldData.Count == 0),
                         "Get Requests cannot submit field data");

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
                        requestHeaders += "\n" + headerKey + ": " + headerValue;
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

        public static IEnumerator ExecutePostRequest<T>(APIRequest request,
                                                        ObjectCallback<T> onSuccess,
                                                        ErrorCallback onError)
        {
            string constructedURL = URL + request.endpoint + "?" + request.filter.GenerateQueryString();

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
                        requestHeaders += "\n" + headerKey + ": " + headerValue;
                    }
                }

                string formFields = "";
                foreach(KeyValuePair<string, string> kvp in request.fieldValues)
                {
                    formFields += "\n" + kvp.Key + "=" + kvp.Value;
                }
                foreach(KeyValuePair<string, APIRequest.BinaryData> kvp in request.fieldData)
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

        public static IEnumerator ExecuteDeleteRequest<T>(APIRequest request,
                                                          ObjectCallback<T> onSuccess,
                                                          ErrorCallback onError)
        {
            string constructedURL = URL + request.endpoint + "?" + request.filter.GenerateQueryString();

            WWWForm form = new WWWForm();
            request.AddFieldsToForm(form);

            UnityWebRequest webRequest = UnityWebRequest.Post(constructedURL, form);
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
                        requestHeaders += "\n" + headerKey + ": " + headerValue;
                    }
                }

                string formFields = "";
                foreach(KeyValuePair<string, string> kvp in request.fieldValues)
                {
                    formFields += "\n" + kvp.Key + "=" + kvp.Value;
                }
                foreach(KeyValuePair<string, APIRequest.BinaryData> kvp in request.fieldData)
                {
                    formFields += "\n" + kvp.Key + "= [BINARY DATA] " + kvp.Value.fileName + "\n";
                }

                Debug.Log("EXECUTING DELETE REQUEST"
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

        public static IEnumerator DownloadData(string url,
                                               DownloadCallback onSuccess,
                                               ErrorCallback onError)
        {
            #if LOG_ALL_QUERIES
            Debug.Log("REQUESTING FILE DOWNLOAD"
                      + "\nSourceURI: " + url);
            #endif

            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            

            // - Handle Errors -
            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                HandleRequestError(webRequest, onError);
            }
            else
            {
                #if LOG_ALL_QUERIES
                Debug.Log("DOWNLOAD SUCEEDED"
                          + "\nSourceURI: " + url);
                #endif

                byte[] downloadedData = webRequest.downloadHandler.data;
                onSuccess(downloadedData);
            }
        }

        private static void ProcessJSONResponse<T>(UnityWebRequest webRequest,
                                                   ObjectCallback<T> onSuccess,
                                                   ErrorCallback onError)
        {
            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                HandleRequestError(webRequest, onError);
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

        private static void HandleRequestError(UnityWebRequest webRequest,
                                               ErrorCallback onError)
        {
            Debug.Assert(webRequest.isNetworkError || webRequest.isHttpError);

            if(webRequest.isNetworkError)
            {
                APIErrorInformation errorInfo = new APIErrorInformation();
                errorInfo.code = (int)webRequest.responseCode;
                errorInfo.message = webRequest.error;
                errorInfo.url = webRequest.url;
                errorInfo.headers = new Dictionary<string, string>();

                #if LOG_ALL_QUERIES
                if(onError != APIClient.LogError)
                {
                    APIClient.LogError(errorInfo);
                }
                #endif

                onError(errorInfo);
            }
            else // if(webRequest.isHttpError)
            {
                APIError error = JsonUtility.FromJson<APIError>(webRequest.downloadHandler.text);
                APIErrorInformation errorInfo = new APIErrorInformation();
                errorInfo.code = error.code;
                errorInfo.message = error.message;
                errorInfo.url = webRequest.url;
                errorInfo.headers = webRequest.GetResponseHeaders();

                #if LOG_ALL_QUERIES
                if(onError != APIClient.LogError)
                {
                    APIClient.LogError(errorInfo);
                }
                #endif

                onError(errorInfo);
            }
        }


        // ---------[ AUTHENTICATION ]---------
        public void RequestSecurityCode(string emailAddress,
                                        ObjectCallback<APIMessage> onSuccess,
                                        ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "oauth/emailrequest";
            request.fieldValues.Add("api_key", apiKey);
            request.fieldValues.Add("email", emailAddress);

            StartCoroutine(ExecutePostRequest<APIMessage>(request,
                                                          onSuccess,
                                                          onError));
        }
        public void RequestOAuthToken(string securityCode,
                                      ObjectCallback<AuthenticationData> onSuccess,
                                      ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "oauth/emailexchange";
            request.fieldValues.Add("api_key", apiKey);
            request.fieldValues.Add("security_code", securityCode);

            StartCoroutine(ExecutePostRequest<AuthenticationData>(request,
                                                                  onSuccess,
                                                                  onError));
        }

        // ---------[ GAME ENDPOINTS ]---------
        // Get Game
        public void GetGame(ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID;
            
            StartCoroutine(ExecuteQuery<Game>(endpoint,
                                              apiKey,
                                              Filter.NONE,
                                              onSuccess,
                                              onError));
        }
        // Edit Game
        public void EditGame(ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID;
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }

        // ---------[ MOD ENDPOINTS ]---------
        // Get All Mods
        public void GetAllMods(GetAllModsFilter filter,
                               ObjectCallback<Mod[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods";

            StartCoroutine(ExecuteQuery<APIObjectArray<Mod>>(endpoint,
                                                             apiKey,
                                                             filter,
                                                             results => onSuccess(results.data),
                                                             onError));
        }
        // Get Mod
        public void GetMod(int modID,
                           ObjectCallback<Mod> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID;
            
            StartCoroutine(ExecuteQuery<Mod>(endpoint,
                                             apiKey,
                                             Filter.NONE,
                                             onSuccess,
                                             onError));
        }
        // Add Mod
        public void AddMod(ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Edit Mod
        public void EditMod(int modID,
                            ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID;
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Delete Mod
        public void DeleteMod(int modID,
                              ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID;
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":DELETE"));
        }

        // ---------[ MODFILE ENDPOINTS ]---------
        // Get All Modfiles
        public void GetAllModfiles(int modID, GetAllModfilesFilter filter,
                                   ObjectCallback<Modfile[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/files";

            StartCoroutine(ExecuteQuery<APIObjectArray<Modfile>>(endpoint,
                                                                 apiKey,
                                                                 filter,
                                                                 results => onSuccess(results.data),
                                                                 onError));
        }
        // Get Modfile
        public void GetModfile(int modID, int fileID,
                               ObjectCallback<Modfile[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/files/" + fileID;

            StartCoroutine(ExecuteQuery<APIObjectArray<Modfile>>(endpoint,
                                                                 apiKey,
                                                                 Filter.NONE,
                                                                 results => onSuccess(results.data),
                                                                 onError));
        }
        // Add Modfile
        public void AddModfile(int modID,
                               ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/files";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Edit Mod
        public void EditMod(int modID, int fileID,
                            ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/files/" + fileID;
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }

        // ---------[ MEDIA ENDPOINTS ]---------
        // Add Game Media
        public void AddGameMedia(ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/media";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Add Mod Media
        public void AddModMedia(int modID,
                                ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/media";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Delete Mod Media
        public void DeleteModMedia(int modID,
                                   ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/media";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":DELETE"));
        }

        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        public void SubscribeToMod(int modID,
                                   ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "games/" + gameID + "/mods/" + modID + "/subscribe";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecutePostRequest<APIMessage>(request,
                                                          onSuccess,
                                                          onError));
        }
        public void UnsubscribeFromMod(int modID,
                                       ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "games/" + gameID + "/mods/" + modID + "/subscribe";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecuteDeleteRequest<APIMessage>(request,
                                                            onSuccess,
                                                            onError));
        }

        // ---------[ ACTIVITY ENDPOINTS ]---------
        // Get Game Activity
        public void GetGameActivity(GetGameActivityFilter filter,
                                    ObjectCallback<GameActivity[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/activity";

            StartCoroutine(ExecuteQuery<APIObjectArray<GameActivity>>(endpoint,
                                                                      apiKey,
                                                                      filter,
                                                                      results => onSuccess(results.data),
                                                                      onError));
        }
        // Get All Mod Activity By Game
        public void GetAllModActivityByGame(GetAllModActivityByGameFilter filter,
                                          ObjectCallback<ModActivity[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "mods/activity";

            StartCoroutine(ExecuteQuery<APIObjectArray<ModActivity>>(endpoint,
                                                                     apiKey,
                                                                     filter,
                                                                     results => onSuccess(results.data),
                                                                     onError));
        }
        // Get Mod Activity
        public void GetModActivity(int modID, GetModActivityFilter filter,
                                   ObjectCallback<ModActivity[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/activity";

            StartCoroutine(ExecuteQuery<APIObjectArray<ModActivity>>(endpoint,
                                                                     apiKey,
                                                                     filter,
                                                                     results => onSuccess(results.data),
                                                                     onError));
        }

        // ---------[ TAG ENDPOINTS ]---------
        // Get All Mod Tags
        public void GetAllModTags(int modID, GetAllModTagsFilter filter,
                                  ObjectCallback<ModTag[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/tags";

            StartCoroutine(ExecuteQuery<APIObjectArray<ModTag>>(endpoint,
                                                                apiKey,
                                                                filter,
                                                                results => onSuccess(results.data),
                                                                onError));
        }
        // Add Mod Tag
        public void AddModTag(int modID,
                                ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/tags";

            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Delete Mod Tag
        public void DeleteModTag(int modID,
                                 ObjectCallback<Game> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/tags";

            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":DELETE"));
        }

        // ---------[ RATING ENDPOINTS ]---------
        // Add Mod Rating
        public void AddModRating(int modID, int ratingValue,
                                 ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/ratings";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }

        // ---------[ METADATA ENDPOINTS ]---------
        // Get All Mod KVP Metadata
        public void GetAllModKVPMetadata(int modID, GetAllModKVPMetadataFilter filter,
                                         ObjectCallback<MetadataKVP[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/metadatakvp";

            StartCoroutine(ExecuteQuery<APIObjectArray<MetadataKVP>>(endpoint,
                                                                     apiKey,
                                                                     Filter.NONE,
                                                                     results => onSuccess(results.data),
                                                                     onError));
        }
        // Add Mod KVP Metadata
        public void AddModKVPMetadata(int modID,
                                      ObjectCallback<MetadataKVP> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/metadatakvp";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Delete Mod KVP Metadata
        public void DeleteModKVPMetadata(int modID,
                                         ObjectCallback<MetadataKVP> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/metadatakvp";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":DELETE"));
        }

        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
        public void GetAllModDependencies(int modID, GetAllModDependenciesFilter filter,
                                          ObjectCallback<ModDependency[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/dependencies";

            StartCoroutine(ExecuteQuery<APIObjectArray<ModDependency>>(endpoint,
                                                                       apiKey,
                                                                       Filter.NONE,
                                                                       results => onSuccess(results.data),
                                                                       onError));
        }
        // Add Mod Dependency
        public void AddModDependency(int modID,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/dependencies";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Delete Mod Dependencies
        public void DeleteModDependencies(int modID,
                                          ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/dependencies";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":DELETE"));
        }


        // ---------[ TEAM ENDPOINTS ]---------
        // Get All Game Team Members
        public void GetAllGameTeamMembers(GetAllGameTeamMembersFilter filter,
                                          ObjectCallback<TeamMember[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/team";

            StartCoroutine(ExecuteQuery<APIObjectArray<TeamMember>>(endpoint,
                                                                    apiKey,
                                                                    filter,
                                                                    results => onSuccess(results.data),
                                                                    onError));
        }
        // Get All Mod Team Members
        public void GetAllModTeamMembers(int modID, GetAllModTeamMembersFilter filter,
                                         ObjectCallback<TeamMember[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/team";

            StartCoroutine(ExecuteQuery<APIObjectArray<TeamMember>>(endpoint,
                                                                    apiKey,
                                                                    filter,
                                                                    results => onSuccess(results.data),
                                                                    onError));
        }
        // Add Game Team Member
        public void AddGameTeamMember(ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/team";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Add Mod Team Member
        public void AddModTeamMember(int modID,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/team";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        // Update Game Team Member
        public void UpdateGameTeamMember(int teamMemberID,
                                         ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/team/" + teamMemberID;
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":PUT"));
        }
        // Update Mod Team Member
        public void UpdateModTeamMember(int modID, int teamMemberID,
                                        ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/team/" + teamMemberID;
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":PUT"));
        }
        // Delete Game Team Member
        public void DeleteGameTeamMember(int teamMemberID,
                                         ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/team/" + teamMemberID;
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":DELETE"));
        }
        // Delete Mod Team Member
        public void DeleteModTeamMember(int modID, int teamMemberID,
                                        ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/team/" + teamMemberID;
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":DELETE"));
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
        public void GetAllModComments(int modID, GetAllModCommentsFilter filter,
                                      ObjectCallback<Comment[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/comments";

            StartCoroutine(ExecuteQuery<APIObjectArray<Comment>>(endpoint,
                                                                 apiKey,
                                                                 filter,
                                                                 results => onSuccess(results.data),
                                                                 onError));
        }
        // Get Mod Comment
        public void GetModComment(int modID, int commentID,
                                  ObjectCallback<Comment> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/comments/" + commentID;

            StartCoroutine(ExecuteQuery<Comment>(endpoint,
                                                 apiKey,
                                                 Filter.NONE,
                                                 onSuccess,
                                                 onError));
        }
        // Delete Mod Comment
        public void DeleteModComment(int modID, int commentID,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "games/" + gameID + "/mods/" + modID + "/comments/" + commentID;
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":DELETE"));
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
        public void GetResourceOwner(ResourceType resourceType, int resourceID,
                                     ObjectCallback<User> onSuccess, ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "general/owner";
            request.oAuthToken = oAuthToken;
            request.fieldValues.Add("resource_type", resourceType.ToString().ToLower());
            request.fieldValues.Add("resource_id", resourceID.ToString());

            StartCoroutine(ExecuteGetRequest<User>(request,
                                                   onSuccess,
                                                   onError));
        }
        // Get All Users
        public void GetAllUsers(GetAllUsersFilter filter,
                                ObjectCallback<User[]> onSuccess, ErrorCallback onError)
        {
            string endpoint = "users";

            StartCoroutine(ExecuteQuery<APIObjectArray<User>>(endpoint,
                                                              apiKey,
                                                              filter,
                                                              results => onSuccess(results.data),
                                                              onError));
        }
        // Get User
        public void GetUser(int userID,
                            ObjectCallback<User> onSuccess, ErrorCallback onError)
        {
            string endpoint = "users/" + userID;

            StartCoroutine(ExecuteQuery<User>(endpoint,
                                              apiKey,
                                              Filter.NONE,
                                              onSuccess,
                                              onError));
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // Submit Report
        public void SubmitReport(ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpoint = "report";
            onError(APIErrorInformation.GenerateNotImplemented(endpoint + ":POST"));
        }
        

        // ---------[ ME ENDPOINTS ]---------
        // Get Authenticated User
        public void GetAuthenticatedUser(ObjectCallback<User> onSuccess, ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "me";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecuteGetRequest<User>(request,
                                                   onSuccess,
                                                   onError));
        }
        // Get User Subscriptions
        public void GetUserSubscriptions(GetUserSubscriptionsFilter filter,
                                         ObjectCallback<Mod[]> onSuccess, ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "me/subscribed";
            request.oAuthToken = oAuthToken;
            request.filter = filter;

            StartCoroutine(ExecuteGetRequest<APIObjectArray<Mod>>(request,
                                                                  results => onSuccess(results.data),
                                                                  onError));
        }
        // Get User Games
        public void GetUserGames(ObjectCallback<Game[]> onSuccess, ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "me/games";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecuteGetRequest<APIObjectArray<Game>>(request,
                                                                   results => onSuccess(results.data),
                                                                   onError));
        }
        // Get User Mods
        public void GetUserMods(ObjectCallback<Mod[]> onSuccess, ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "me/mods";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecuteGetRequest<APIObjectArray<Mod>>(request,
                                                                  results => onSuccess(results.data),
                                                                  onError));
        }
        // Get User Files
        public void GetUserModfiles(ObjectCallback<Modfile[]> onSuccess, ErrorCallback onError)
        {
            APIRequest request = new APIRequest();
            request.endpoint = "me/files";
            request.oAuthToken = oAuthToken;

            StartCoroutine(ExecuteGetRequest<APIObjectArray<Modfile>>(request,
                                                                      results => onSuccess(results.data),
                                                                      onError));
        }
    }
}
