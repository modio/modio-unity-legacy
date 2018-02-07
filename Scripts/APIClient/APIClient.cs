#define USING_TEST_SERVER

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


// TODO(@jackson): Assert apiKey, and oAuthToken length to avoid user error
namespace ModIO
{
    public class APIClient
    {
        // ---------[ CONSTANTS ]---------
        public const string API_VERSION = "v1";

        #if USING_TEST_SERVER
        public const string API_URL = "https://api.test.mod.io/" + API_VERSION + "/";
        #else
        public const string API_URL = "https://api.mod.io/" + API_VERSION + "/";
        #endif

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

        // ---------[ INITIALIZATION ]---------
        internal interface IRequestHandler
        {
            void BeginRequest<T_APIObj>(UnityWebRequest webRequest,
                                        Action<T_APIObj> successCallback,
                                        Action<ErrorInfo> errorCallback);
        }
        private IRequestHandler requestHandler;


        public void InitializeWithCoroutineRequestHandler(MonoBehaviour coroutineHost)
        {
            RequestHandler_Coroutine handler = new RequestHandler_Coroutine();
            handler.coroutineBehaviour = coroutineHost;
            requestHandler = handler;
        }

        public void InitializeWithUpdateRequestHandler(out Action updateRequestsFunctionHandle)
        {
            RequestHandler_OnUpdate handler = new RequestHandler_OnUpdate();
            updateRequestsFunctionHandle = handler.OnUpdate;
            requestHandler = handler;
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
            API.StringValueField[] valueFields = new API.StringValueField[]
            {
                API.StringValueField.Create("api_key", apiKey),
                API.StringValueField.Create("email", emailAddress),
            };

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                "",
                                                                                                valueFields,
                                                                                                null);

            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }

        // NOTE(@jackson): Untested
        public void RequestOAuthToken(string apiKey,
                                      string securityCode,
                                      Action<string> successCallback,
                                      Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "oauth/emailexchange";
            API.StringValueField[] valueFields = new API.StringValueField[]
            {
                API.StringValueField.Create("api_key", apiKey),
                API.StringValueField.Create("security_code", securityCode),
            };

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.AccessTokenObject>(endpointURL,
                                                                                                    "",
                                                                                                    valueFields,
                                                                                                    null);
            Action<API.AccessTokenObject> onSuccess = (result) =>
            {
                successCallback(result.access_token);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ GAME ENDPOINTS ]---------
        // Get All Games
        public void GetAllGames(string apiKey,
                                GetAllGamesFilter filter,
                                Action<GameInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.GameObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameObject, GameInfo>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }

        // Get GameInfo
        public void GetGame(string apiKey, int gameId,
                            Action<GameInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId;
            
            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);
            
            
            Action<API.GameObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameObject, GameInfo>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }

        // Edit GameInfo
        public void EditGame(string oAuthToken,
                             EditableGameInfo gameInfo,
                             Action<GameInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameInfo.id;
            API.StringValueField[] valueFields = gameInfo.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            
            Action<API.GameObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameObject, GameInfo>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ MOD ENDPOINTS ]---------
        // Get All Mods
        public void GetAllMods(string apiKey, int gameId,
                               GetAllModsFilter filter,
                               Action<ModInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get Mod
        public void GetMod(string apiKey, int gameId,
                           int modId,
                           Action<ModInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId;
            
            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);
            
            
            Action<API.ModObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod
        public void AddMod(string oAuthToken, int gameId,
                           AddableModInfo modInfo,
                           Action<ModInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods";
            API.StringValueField[] valueFields = modInfo.GetValueFields();
            API.BinaryDataField[] dataFields = modInfo.GetDataFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.ModObject>(endpointURL,
                                                                                            oAuthToken,
                                                                                            valueFields,
                                                                                            dataFields);
            
            Action<API.ModObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Edit Mod
        public void EditMod(string oAuthToken,
                            EditableModInfo modInfo,
                            Action<ModInfo> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + modInfo.gameId + "/mods/" + modInfo.id;
            API.StringValueField[] valueFields = modInfo.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            
            Action<API.ModObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod
        public void DeleteMod(string oAuthToken, int gameId,
                              int modId,
                              Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId;

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        // Get All Modfiles
        public void GetAllModfiles(string apiKey, int gameId,
                                   int modId, GetAllModfilesFilter filter,
                                   Action<Modfile[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/files";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModfileObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get Modfile
        public void GetModfile(string apiKey, int gameId,
                               int modId, int modfileId,
                               Action<Modfile> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            
            Action<API.ModfileObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Modfile
        public void AddModfile(string oAuthToken, int gameId,
                               int modId, UnsubmittedModfile modfile,
                               Action<Modfile> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/files";
            API.StringValueField[] valueFields = modfile.GetValueFields();
            API.BinaryDataField[] dataFields = modfile.GetDataFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.ModfileObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                dataFields);
            
            Action<API.ModfileObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Edit Modfile
        public void EditModfile(string oAuthToken, int gameId,
                                EditableModfile modfile,
                                Action<Modfile> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modfile.modId + "/files/" + modfile.id;
            API.StringValueField[] valueFields = modfile.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            
            Action<API.ModfileObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        // Add GameInfo Media
        public void AddGameMedia(string oAuthToken, int gameId,
                                 UnsubmittedGameMedia gameMedia,
                                 Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/media";
            API.BinaryDataField[] dataFields = gameMedia.GetDataFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                null,
                                                                                                dataFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod Media
        public void AddModMedia(string oAuthToken, int gameId,
                                int modId, UnsubmittedModMedia modMedia,
                                Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/media";
            API.StringValueField[] valueFields = modMedia.GetValueFields();
            API.BinaryDataField[] dataFields = modMedia.GetDataFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                dataFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Media
        public void DeleteModMedia(string oAuthToken, int gameId,
                                   int modId, ModMediaToDelete modMediaToDelete,
                                   Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/media";
            API.StringValueField[] valueFields = modMediaToDelete.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        public void SubscribeToMod(string oAuthToken, int gameId,
                                   int modId,
                                   Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                null,
                                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        public void UnsubscribeFromMod(string oAuthToken, int gameId,
                                       int modId,
                                       Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ EVENT ENDPOINTS ]---------
        // Get Mod Events
        public void GetModEvents(string apiKey, int gameId,
                                 int modId, GetModEventFilter filter,
                                 Action<ModEvent[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/events";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModEventObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModEventObject, ModEvent>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get All Mod Events
        public void GetAllModEvents(string apiKey, int gameId,
                                    GetAllModEventsFilter filter,
                                    Action<ModEvent[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/events";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModEventObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModEventObject, ModEvent>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ TAG ENDPOINTS ]---------
        // Get All Game Tag Options
        public void GetAllGameTagOptions(string apiKey, int gameId,
                                         Action<GameTagOption[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/tags";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            Action<API.ObjectArray<API.GameTagOptionObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameTagOptionObject, GameTagOption>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }

        // Add Game Tag Option
        public void AddGameTagOption(string oAuthToken, int gameId,
                                     UnsubmittedGameTagOption tagOption,
                                     Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/tags";
            API.StringValueField[] valueFields = tagOption.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }

        // Delete Game Tag Option
        public void DeleteGameTagOption(string oAuthToken, int gameId,
                                        GameTagOptionToDelete gameTagOptionToDelete,
                                        Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/tags";
            API.StringValueField[] valueFields = gameTagOptionToDelete.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }

        // Get All Mod Tags
        public void GetAllModTags(string apiKey, int gameId,
                                  int modId, GetAllModTagsFilter filter,
                                  Action<ModTag[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModTagObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModTagObject, ModTag>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod Tags
        public void AddModTags(string oAuthToken, int gameId,
                               int modId, string[] tagNames,
                               Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/tags";
            API.StringValueField[] valueFields = new API.StringValueField[tagNames.Length];

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Tags
        public void DeleteModTags(string oAuthToken, int gameId,
                                  int modId, string[] tagsToDelete,
                                  Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/tags";
            API.StringValueField[] valueFields = new API.StringValueField[tagsToDelete.Length];
            for(int i = 0; i < tagsToDelete.Length; ++i)
            {
                valueFields[i] = API.StringValueField.Create("tags[]", tagsToDelete[i]);
            }

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ RATING ENDPOINTS ]---------
        // Add Mod Rating
        public void AddModRating(string oAuthToken, int gameId,
                                 int modId, int ratingValue,
                                 Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/ratings";
            API.StringValueField[] valueFields = new API.StringValueField[]
            {
                API.StringValueField.Create("rating", ratingValue),
            };

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ METADATA ENDPOINTS ]---------
        // Get All Mod KVP Metadata
        public void GetAllModKVPMetadata(string apiKey, int gameId,
                                         int modId,
                                         Action<MetadataKVP[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            Action<API.ObjectArray<API.MetadataKVPObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MetadataKVPObject, MetadataKVP>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod KVP Metadata
        public void AddModKVPMetadata(string oAuthToken, int gameId,
                                      int modId, UnsubmittedMetadataKVP[] metadataKVPs,
                                      Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/metadatakvp";
            API.StringValueField[] valueFields = new API.StringValueField[metadataKVPs.Length];
            for(int i = 0; i < metadataKVPs.Length; ++i)
            {
                valueFields[i] = API.StringValueField.Create("metadata[]",
                                                             metadataKVPs[i].key + ":" + metadataKVPs[i].value);
            }

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod KVP Metadata
        public void DeleteModKVPMetadata(string oAuthToken, int gameId,
                                         int modId, UnsubmittedMetadataKVP[] metadataKVPsToRemove,
                                         Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/metadatakvp";
            API.StringValueField[] valueFields = new API.StringValueField[metadataKVPsToRemove.Length];
            for(int i = 0; i < metadataKVPsToRemove.Length; ++i)
            {
                valueFields[i] = API.StringValueField.Create("metadata[]",
                                                             metadataKVPsToRemove[i].key + ":" + metadataKVPsToRemove[i].value);
            }

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
        public void GetAllModDependencies(string apiKey, int gameId,
                                          int modId, GetAllModDependenciesFilter filter,
                                          Action<ModDependency[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.ModDependencyObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModDependencyObject, ModDependency>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod Dependencies
        public void AddModDependencies(string oAuthToken, int gameId,
                                       int modId, int[] requiredModIds,
                                       Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/dependencies";
            API.StringValueField[] valueFields = new API.StringValueField[requiredModIds.Length];
            for(int i = 0; i < requiredModIds.Length; ++i)
            {
                valueFields[i] = API.StringValueField.Create("dependencies[]", requiredModIds[i]);
            }

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Dependencies
        public void DeleteModDependencies(string oAuthToken, int gameId,
                                          int modId, int[] modIdsToRemove,
                                          Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/dependencies";
            API.StringValueField[] valueFields = new API.StringValueField[modIdsToRemove.Length];
            for(int i = 0; i < modIdsToRemove.Length; ++i)
            {
                valueFields[i] = API.StringValueField.Create("dependencies[]", modIdsToRemove[i]);
            }

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ TEAM ENDPOINTS ]---------
        // Get All Mod Team Members
        public void GetAllModTeamMembers(string apiKey, int gameId,
                                         int modId, GetAllModTeamMembersFilter filter,
                                         Action<TeamMember[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.TeamMemberObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.TeamMemberObject, TeamMember>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Add Mod Team Member
        public void AddModTeamMember(string oAuthToken, int gameId,
                                     int modId, UnsubmittedTeamMember teamMember,
                                     Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team";
            API.StringValueField[] valueFields = teamMember.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Update Mod Team Member
        // NOTE(@jackson): Untested
        public void UpdateModTeamMember(string oAuthToken, int gameId,
                                        int modId, EditableTeamMember teamMember,
                                        Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team/" + teamMember.id;
            API.StringValueField[] valueFields = teamMember.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Team Member
        public void DeleteModTeamMember(string oAuthToken, int gameId,
                                        int modId, int teamMemberId,
                                        Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
        // NOTE(@jackson): Untested
        public void GetAllModComments(string apiKey, int gameId,
                                      int modId, GetAllModCommentsFilter filter,
                                      Action<UserComment[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/comments";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.CommentObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.CommentObject, UserComment>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get Mod Comment
        // NOTE(@jackson): Untested
        public void GetModComment(string apiKey, int gameId,
                                  int modId, int commentId,
                                  Action<UserComment> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            
            Action<API.CommentObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.CommentObject, UserComment>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Delete Mod Comment
        // NOTE(@jackson): Untested
        public void DeleteModComment(string oAuthToken, int gameId,
                                     int modId, int commentId,
                                     Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
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
            API.StringValueField[] valueFields = new API.StringValueField[]
            {
                API.StringValueField.Create("resource_type", resourceType.ToString().ToLower()),
                API.StringValueField.Create("resource_id", resourceID),
            };

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.UserObject>(endpointURL,
                                                                                             oAuthToken,
                                                                                             valueFields,
                                                                                             null);
            
            Action<API.UserObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.UserObject, User>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get All Users
        public void GetAllUsers(string apiKey,
                                GetAllUsersFilter filter,
                                Action<User[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "users";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            Action<API.ObjectArray<API.UserObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.UserObject, User>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User
        public void GetUser(string apiKey,
                            int userID,
                            Action<User> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "users/" + userID;

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            
            Action<API.UserObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.UserObject, User>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // Submit Report
        // NOTE(@jackson): Untested
        public void SubmitReport(string oAuthToken,
                                 UnsubmittedReport report,
                                 Action<APIMessage> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "report";
            API.StringValueField[] valueFields = report.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            
            Action<API.MessageObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.MessageObject, APIMessage>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        

        // ---------[ ME ENDPOINTS ]---------
        // Get Authenticated User
        public void GetAuthenticatedUser(string oAuthToken,
                                         Action<User> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.UserObject>(endpointURL,
                                                                                            oAuthToken,
                                                                                            Filter.None);
            
            Action<API.UserObject> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.UserObject, User>(result, successCallback);
            };

            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User Subscriptions
        public void GetUserSubscriptions(string oAuthToken,
                                         GetUserSubscriptionsFilter filter,
                                         Action<ModInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/subscribed";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.ModObject[]>(endpointURL,
                                                                                             oAuthToken,
                                                                                             filter);
            Action<API.ObjectArray<API.ModObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User Games
        public void GetUserGames(string oAuthToken,
                                 Action<GameInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/games";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.GameObject[]>(endpointURL,
                                                                                              oAuthToken,
                                                                                              Filter.None);
            Action<API.ObjectArray<API.GameObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.GameObject, GameInfo>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User Mods
        public void GetUserMods(string oAuthToken,
                                Action<ModInfo[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/mods";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.ModObject[]>(endpointURL,
                                                                                             oAuthToken,
                                                                                             Filter.None);
            Action<API.ObjectArray<API.ModObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModObject, ModInfo>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
        // Get User Files
        public void GetUserModfiles(string oAuthToken,
                                    Action<Modfile[]> successCallback, Action<ErrorInfo> errorCallback)
        {
            string endpointURL = API_URL + "me/files";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.ModfileObject[]>(endpointURL,
                                                                                                 oAuthToken,
                                                                                                 Filter.None);
            Action<API.ObjectArray<API.ModfileObject>> onSuccess = (result) =>
            {
                OnSuccessWrapper<API.ModfileObject, Modfile>(result, successCallback);
            };
            requestHandler.BeginRequest(webRequest, onSuccess, errorCallback);
        }
    }
}
