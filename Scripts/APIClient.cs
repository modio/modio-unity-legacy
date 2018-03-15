using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using ModIO.API;

namespace ModIO
{
    public class BinaryUpload
    {
        public string fileName = string.Empty;
        public byte[] data = null;
    }

    public class BinaryDataField
    {
        public string key = "";
        public string fileName = null;
        public string mimeType = null;
        public byte[] contents = null;

        public static BinaryDataField Create(string key, string fileName, string mimeType, byte[] contents)
        {
            Debug.Assert(!String.IsNullOrEmpty(key) && contents != null);

            BinaryDataField retVal = new BinaryDataField();
            retVal.key = key;
            retVal.fileName = fileName;
            retVal.mimeType = mimeType;
            retVal.contents = contents;
            return retVal;
        }
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

    public static class APIClient
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
        public static void LogError(ErrorInfo errorInfo)
        {
            string errorMessage = errorInfo.method + " REQUEST FAILED";
            errorMessage += "\nURL: " + errorInfo.url;
            errorMessage += "\nCode: " + errorInfo.httpStatusCode;
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
            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                #if DEBUG
                    ErrorInfo errorInfo = ErrorInfo.GenerateFromWebRequest(webRequest);

                    if(GlobalSettings.LOG_ALL_WEBREQUESTS
                       && errorCallback != APIClient.LogError)
                    {
                        APIClient.LogError(errorInfo);
                    }

                    if(errorCallback != null)
                    {
                        errorCallback(errorInfo);
                    }
                #else
                    if(errorCallback != null)
                    {
                        errorCallback(ErrorInfo.GenerateFromWebRequest(webRequest));
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
                        if(typeof(T) == typeof(MessageObject))
                        {
                            MessageObject response = new MessageObject();
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
            }
        }


        // ---------[ AUTHENTICATION ]---------
        public static void RequestSecurityCode(string emailAddress,
                                               Action<MessageObject> successCallback,
                                               Action<ErrorInfo> errorCallback)
        {
            Debug.Assert(!String.IsNullOrEmpty(GlobalSettings.GAME_APIKEY),
                         "Please save your game's API Key into GlobalSettings.cs before using this plugin");

            string endpointURL = API_URL + "oauth/emailrequest";
            StringValueField[] valueFields = new StringValueField[]
            {
                StringValueField.Create("api_key", GlobalSettings.GAME_APIKEY),
                StringValueField.Create("email", emailAddress),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL, "",
                                                                                          valueFields,
                                                                                          null);


            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        public static void RequestOAuthToken(string securityCode,
                                             Action<string> successCallback,
                                             Action<ErrorInfo> errorCallback)
        {
            Debug.Assert(!String.IsNullOrEmpty(GlobalSettings.GAME_APIKEY),
                         "Please save your game's API Key into GlobalSettings.cs before using this plugin");

            string endpointURL = API_URL + "oauth/emailexchange";
            StringValueField[] valueFields = new StringValueField[]
            {
                StringValueField.Create("api_key", GlobalSettings.GAME_APIKEY),
                StringValueField.Create("security_code", securityCode),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<AccessTokenObject>(endpointURL,
                                                                                              "",
                                                                                              valueFields,
                                                                                              null);
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }


        // ---------[ GAME ENDPOINTS ]---------
        // Get All Games
        public static void GetAllGames(GetAllGamesFilter filter,
                                       Action<ObjectArray<API.GameObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Get GameInfo
        public static void GetGame(Action<API.GameObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID;
            
            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, Filter.None);
            
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Edit GameInfo
        public static void EditGame(string oAuthToken,
                                    EditableGameInfo gameInfo,
                                    Action<API.GameObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameInfo.id;
            StringValueField[] valueFields = gameInfo.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePutRequest<MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MOD ENDPOINTS ]---------
        // Get All Mods
        public static void GetAllMods(GetAllModsFilter filter,
                                      Action<ObjectArray<ModObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get Mod
        public static void GetMod(int modId,
                                  Action<ModObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId;
            
            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, Filter.None);
            
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod
        public static void AddMod(string oAuthToken,
                                  AddModParameters parameters,
                                  Action<ModObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<ModObject>(endpointURL,
                                                                                  oAuthToken,
                                                                                  parameters.valueFields.ToArray(),
                                                                                  parameters.dataFields.ToArray());
            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Edit Mod
        public static void EditMod(string oAuthToken,
                                   EditableModInfo modInfo,
                                   Action<ModObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + modInfo.gameId + "/mods/" + modInfo.id;
            StringValueField[] valueFields = modInfo.GetEditValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePutRequest<MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod
        public static void DeleteMod(string oAuthToken,
                                     int modId,
                                     Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        // Get All Modfiles
        public static void GetAllModfiles(int modId, GetAllModfilesFilter filter,
                                          Action<ObjectArray<ModfileObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/files";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get Modfile
        public static void GetModfile(int modId, int modfileId,
                                      Action<ModfileObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, Filter.None);

            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Modfile
        public static void AddModfile(string oAuthToken,
                                      ModfileProfile profile,
                                      string buildFilename, byte[] buildZipData,
                                      bool setPrimary,
                                      Action<ModfileObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            Debug.Assert(profile.modId > 0,
                         "Cannot upload modfile with unassigned mod");
            Debug.Assert(System.IO.Path.GetExtension(buildFilename) == ".zip",
                         "Mod IO only accepts zipped archives as builds");

            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + profile.modId + "/files";
            
            // - String Values -
            List<StringValueField> valueFields = new List<StringValueField>(5);
            if(!String.IsNullOrEmpty(profile.version))
            {
                valueFields.Add(StringValueField.Create("version", profile.version));
            }
            if(!String.IsNullOrEmpty(profile.changelog))
            {
                valueFields.Add(StringValueField.Create("changelog", profile.changelog));
            }
            if(!String.IsNullOrEmpty(profile.metadataBlob))
            {
                valueFields.Add(StringValueField.Create("metadata_blob", profile.metadataBlob));
            }
            valueFields.Add(StringValueField.Create("active", setPrimary.ToString().ToLower()));
            valueFields.Add(StringValueField.Create("filehash", Utility.GetMD5ForData(buildZipData)));

            // - Data Values -
            BinaryDataField dataField = new BinaryDataField();
            dataField.key = "filedata";
            dataField.fileName = buildFilename;
            dataField.contents = buildZipData;

            BinaryDataField[] dataFields = new BinaryDataField[] { dataField };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<ModfileObject>(endpointURL,
                                                                                          oAuthToken,
                                                                                          valueFields.ToArray(),
                                                                                          dataFields);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Edit Modfile
        public static void EditModfile(string oAuthToken,
                                       ModfileProfile profile,
                                       bool setPrimary,
                                       Action<ModfileObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + profile.modId + "/files/" + profile.modfileId;
            
            // - String Values -
            List<StringValueField> valueFields = new List<StringValueField>(4);
            if(!String.IsNullOrEmpty(profile.version))
            {
                valueFields.Add(StringValueField.Create("version", profile.version));
            }
            if(!String.IsNullOrEmpty(profile.changelog))
            {
                valueFields.Add(StringValueField.Create("changelog", profile.changelog));
            }
            if(!String.IsNullOrEmpty(profile.metadataBlob))
            {
                valueFields.Add(StringValueField.Create("metadata_blob", profile.metadataBlob));
            }
            valueFields.Add(StringValueField.Create("active", setPrimary.ToString().ToLower()));

            UnityWebRequest webRequest = APIClient.GeneratePutRequest<MessageObject>(endpointURL,
                                                                                         oAuthToken,
                                                                                         valueFields.ToArray());
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        // Add GameInfo Media
        public static void AddGameMedia(string oAuthToken,
                                        UnsubmittedGameMedia gameMedia,
                                        Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/media";
            BinaryDataField[] dataFields = gameMedia.GetDataFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                null,
                                                                                dataFields);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod Media
        public static void AddModMedia(string oAuthToken, int modId,
                                       BinaryUpload logo, BinaryUpload imageGalleryZip,
                                       string[] youtubeLinks, string[] sketchfabLinks,
                                       Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/media";

            // - String Values -
            List<StringValueField> valueFields = new List<StringValueField>(youtubeLinks.Length + sketchfabLinks.Length);
            foreach(string youtube in youtubeLinks)
            {
                valueFields.Add(StringValueField.Create("youtube[]", youtube));
            }
            foreach(string sketchfab in sketchfabLinks)
            {
                valueFields.Add(StringValueField.Create("sketchfab[]", sketchfab));
            }

            // - Data Values -
            List<BinaryDataField> dataFields = new List<BinaryDataField>(2);
            if(logo != null)
            {
                BinaryDataField logoField = new BinaryDataField()
                {
                    key = "logo",
                    contents = logo.data,
                    fileName = logo.fileName
                };
                dataFields.Add(logoField);
            }
            if(imageGalleryZip != null)
            {
                BinaryDataField logoField = new BinaryDataField()
                {
                    key = "images",
                    contents = imageGalleryZip.data,
                    fileName = "images.zip"
                };
                dataFields.Add(logoField);
            }

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                          oAuthToken,
                                                                                          valueFields.ToArray(),
                                                                                          dataFields.ToArray());
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Media
        public static void DeleteModMedia(string oAuthToken,
                                          ModMediaChanges modMediaToDelete,
                                          Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modMediaToDelete.modId + "/media";
            
            // - String Values -
            List<StringValueField> valueFields = new List<StringValueField>(modMediaToDelete.images.Length
                                                                            + modMediaToDelete.youtube.Length
                                                                            + modMediaToDelete.sketchfab.Length);
            foreach(string image in modMediaToDelete.images)
            {
                valueFields.Add(StringValueField.Create("images[]", image));
            }
            foreach(string youtubeLink in modMediaToDelete.youtube)
            {
                valueFields.Add(StringValueField.Create("youtube[]", youtubeLink));
            }
            foreach(string sketchfabLink in modMediaToDelete.sketchfab)
            {
                valueFields.Add(StringValueField.Create("sketchfab[]", sketchfabLink));
            }

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                            oAuthToken,
                                                                                            valueFields.ToArray());
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        public static void SubscribeToMod(string oAuthToken,
                                          int modId,
                                          Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                null,
                                                                                null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        public static void UnsubscribeFromMod(string oAuthToken,
                                              int modId,
                                              Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ EVENT ENDPOINTS ]---------
        // Get Mod Events
        public static void GetModEvents(int modId, GetModEventFilter filter,
                                        Action<ObjectArray<EventObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get All Mod Events
        public static void GetAllModEvents(GetAllModEventsFilter filter,
                                           Action<ObjectArray<EventObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TAG ENDPOINTS ]---------
        // Get All Game Tag Options
        public static void GetAllGameTagOptions(Action<ObjectArray<GameTagOptionObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, Filter.None);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Add Game Tag Option
        public static void AddGameTagOption(string oAuthToken,
                                            UnsubmittedGameTagOption tagOption,
                                            Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/tags";
            StringValueField[] valueFields = tagOption.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Delete Game Tag Option
        public static void DeleteGameTagOption(string oAuthToken,
                                               GameTagOptionToDelete gameTagOptionToDelete,
                                               Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/tags";
            StringValueField[] valueFields = gameTagOptionToDelete.GetValueFields();

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // Get All Mod Tags
        public static void GetAllModTags(int modId, GetAllModTagsFilter filter,
                                         Action<ObjectArray<ModTagObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod Tags
        public static void AddModTags(string oAuthToken,
                                      int modId, string[] tagNames,
                                      Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/tags";
            StringValueField[] valueFields = new StringValueField[tagNames.Length];

            for(int i = 0; i < tagNames.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("tags[]", tagNames[i]);
            }

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                          oAuthToken,
                                                                                          valueFields,
                                                                                          null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Tags
        public static void DeleteModTags(string oAuthToken,
                                         int modId, string[] tagsToDelete,
                                         Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/tags";
            StringValueField[] valueFields = new StringValueField[tagsToDelete.Length];
            for(int i = 0; i < tagsToDelete.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("tags[]", tagsToDelete[i]);
            }

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ RATING ENDPOINTS ]---------
        // Add Mod Rating
        public static void AddModRating(string oAuthToken,
                                        int modId, int ratingValue,
                                        Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/ratings";
            StringValueField[] valueFields = new StringValueField[]
            {
                StringValueField.Create("rating", ratingValue),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ METADATA ENDPOINTS ]---------
        // Get All Mod KVP Metadata
        public static void GetAllModKVPMetadata(int modId,
                                                Action<ObjectArray<MetadataKVPObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, Filter.None);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod KVP Metadata
        public static void AddModKVPMetadata(string oAuthToken,
                                             int modId, UnsubmittedMetadataKVP[] metadataKVPs,
                                             Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/metadatakvp";
            StringValueField[] valueFields = new StringValueField[metadataKVPs.Length];
            for(int i = 0; i < metadataKVPs.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("metadata[]",
                                                             metadataKVPs[i].key + ":" + metadataKVPs[i].value);
            }

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod KVP Metadata
        public static void DeleteModKVPMetadata(string oAuthToken,
                                                int modId, UnsubmittedMetadataKVP[] metadataKVPsToRemove,
                                                Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/metadatakvp";
            StringValueField[] valueFields = new StringValueField[metadataKVPsToRemove.Length];
            for(int i = 0; i < metadataKVPsToRemove.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("metadata[]",
                                                             metadataKVPsToRemove[i].key + ":" + metadataKVPsToRemove[i].value);
            }

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
        public static void GetAllModDependencies(int modId, GetAllModDependenciesFilter filter,
                                                 Action<ObjectArray<ModDependencyObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod Dependencies
        public static void AddModDependencies(string oAuthToken,
                                              int modId, int[] requiredModIds,
                                              Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/dependencies";
            StringValueField[] valueFields = new StringValueField[requiredModIds.Length];
            for(int i = 0; i < requiredModIds.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("dependencies[]", requiredModIds[i]);
            }

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Dependencies
        public static void DeleteModDependencies(string oAuthToken,
                                                 int modId, int[] modIdsToRemove,
                                                 Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/dependencies";
            StringValueField[] valueFields = new StringValueField[modIdsToRemove.Length];
            for(int i = 0; i < modIdsToRemove.Length; ++i)
            {
                valueFields[i] = StringValueField.Create("dependencies[]", modIdsToRemove[i]);
            }

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TEAM ENDPOINTS ]---------
        // Get All Mod Team Members
        public static void GetAllModTeamMembers(int modId, GetAllModTeamMembersFilter filter,
                                                Action<ObjectArray<TeamMemberObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Add Mod Team Member
        public static void AddModTeamMember(string oAuthToken,
                                            int modId, UnsubmittedTeamMember teamMember,
                                            Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/team";
            StringValueField[] valueFields = teamMember.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Update Mod Team Member
        // NOTE(@jackson): Untested
        public static void UpdateModTeamMember(string oAuthToken,
                                               int modId, EditableTeamMember teamMember,
                                               Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/team/" + teamMember.id;
            StringValueField[] valueFields = teamMember.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePutRequest<MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Team Member
        public static void DeleteModTeamMember(string oAuthToken,
                                               int modId, int teamMemberId,
                                               Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
        // NOTE(@jackson): Untested
        public static void GetAllModComments(int modId, GetAllModCommentsFilter filter,
                                             Action<ObjectArray<CommentObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/comments";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get Mod Comment
        // NOTE(@jackson): Untested
        public static void GetModComment(int modId, int commentId,
                                         Action<CommentObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, Filter.None);

            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Delete Mod Comment
        // NOTE(@jackson): Untested
        public static void DeleteModComment(string oAuthToken,
                                            int modId, int commentId,
                                            Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + GlobalSettings.GAME_ID + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest<MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
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
                                            Action<UserObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "general/owner";
            StringValueField[] valueFields = new StringValueField[]
            {
                StringValueField.Create("resource_type", resourceType.ToString().ToLower()),
                StringValueField.Create("resource_id", resourceID),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<UserObject>(endpointURL,
                                                                             oAuthToken,
                                                                             valueFields,
                                                                             null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get All Users
        public static void GetAllUsers(GetAllUsersFilter filter,
                                       Action<ObjectArray<UserObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "users";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, filter);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User
        public static void GetUser(int userID,
                                   Action<UserObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "users/" + userID;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, Filter.None);

            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // Submit Report
        // NOTE(@jackson): Untested
        public static void SubmitReport(string oAuthToken,
                                        UnsubmittedReport report,
                                        Action<MessageObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "report";
            StringValueField[] valueFields = report.GetValueFields();

            UnityWebRequest webRequest = APIClient.GeneratePostRequest<MessageObject>(endpointURL,
                                                                                oAuthToken,
                                                                                valueFields,
                                                                                null);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        

        // ---------[ ME ENDPOINTS ]---------
        // Get Authenticated User
        public static void GetAuthenticatedUser(string oAuthToken,
                                                Action<UserObject> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<UserObject>(endpointURL,
                                                                                            oAuthToken,
                                                                                            Filter.None);
            

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User Subscriptions
        public static void GetUserSubscriptions(string oAuthToken,
                                                GetUserSubscriptionsFilter filter,
                                                Action<ObjectArray<ModObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/subscribed";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<ObjectArray<ModObject>>(endpointURL,
                                                                                             oAuthToken,
                                                                                             filter);
            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User Games
        public static void GetUserGames(string oAuthToken,
                                        Action<ObjectArray<API.GameObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/games";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<ObjectArray<API.GameObject>>(endpointURL,
                                                                                              oAuthToken,
                                                                                              Filter.None);
            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User Mods
        public static void GetUserMods(string oAuthToken,
                                       Action<ObjectArray<ModObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/mods";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<ObjectArray<ModObject>>(endpointURL,
                                                                                             oAuthToken,
                                                                                             Filter.None);
            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
        // Get User Files
        public static void GetUserModfiles(string oAuthToken,
                                           Action<ObjectArray<ModfileObject>> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/files";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest<ObjectArray<ModfileObject>>(endpointURL,
                                                                                                 oAuthToken,
                                                                                                 Filter.None);
            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
    }
}
