#define USING_TEST_SERVER

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
        // ---------[ CONSTANTS ]---------
        public const string VERSION = "v1";

        #if USING_TEST_SERVER
        public const string API_URL = "https://api.test.mod.io/" + VERSION + "/";
        #else
        public const string API_URL = "https://api.mod.io/" + VERSION + "/";
        #endif

        // ---------[ DEFAULT SUCCESS/ERROR FUNCTIONS ]---------
        public static void IgnoreResponse(object result) {}
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

        // ---------[ REQUEST EXECUTION ]---------
        public IEnumerator ExecuteRequest<T_APIObj, T>(UnityWebRequest webRequest,
                                                       ObjectCallback<T> onSuccess,
                                                       ErrorCallback onError)
                                                       where T_APIObj : struct
                                                       where T : IAPIObjectWrapper<T_APIObj>, new()
        {
            yield return webRequest.SendWebRequest();

            Action<T_APIObj> onSuccessWrapper = (result) =>
            {
                T wrapperObject = new T();
                wrapperObject.WrapAPIObject(result);
                onSuccess(wrapperObject);
            };

            // TODO(@jackson): whaaaaaaat?
            Action<ErrorInfo> oe = e => onError(e);

            API.WebRequests.ProcessWebResponse(webRequest,
                                               onSuccessWrapper,
                                               oe);
        }


        public IEnumerator ExecuteArrayRequest<T_APIObj, T>(UnityWebRequest webRequest,
                                                            ObjectCallback<T[]> onSuccess,
                                                            ErrorCallback onError)
                                                            where T_APIObj : struct
                                                            where T : IAPIObjectWrapper<T_APIObj>, new()
        {
            yield return webRequest.SendWebRequest();

            Action<API.ObjectArray<T_APIObj>> onSuccessArrayWrapper = (result) =>
            {
                T[] wrapperObjectArray = new T[result.data.Length];
                for(int i = 0;
                    i < result.data.Length;
                    ++i)
                {
                    T newObject = new T();
                    newObject.WrapAPIObject(result.data[i]);

                    wrapperObjectArray[i] = newObject;
                }
                onSuccess(wrapperObjectArray);
            };

            Action<ErrorInfo> oe = e => onError(e);

            API.WebRequests.ProcessWebResponse(webRequest,
                                               onSuccessArrayWrapper,
                                               oe);
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
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }


        private IEnumerator ExecuteFetchToken(UnityWebRequest webRequest,
                                              ObjectCallback<string> onSuccess,
                                              ErrorCallback onError)
        {
            yield return webRequest.SendWebRequest();

            Action<ErrorInfo> oe = e => onError(e);
            API.WebRequests.ProcessWebResponse<API.AccessTokenObject>(webRequest,
                                                                      result => onSuccess(result.access_token),
                                                                      oe);
        }

        public void RequestOAuthToken(string securityCode,
                                      ObjectCallback<string> onSuccess,
                                      ErrorCallback onError)
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
            // - Requires custom coroutine for returning a string -
            StartCoroutine(ExecuteFetchToken(webRequest, onSuccess, onError));
        }


        // ---------[ GAME ENDPOINTS ]---------
        // Get All Games
        public void GetAllGames(GetAllGamesFilter filter,
                                ObjectCallback<GameInfo[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.GameObject, GameInfo>(webRequest, onSuccess, onError));
        }

        // Get GameInfo
        public void GetGame(ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId;
            
            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);
            
            StartCoroutine(ExecuteRequest<API.GameObject, GameInfo>(webRequest, onSuccess, onError));
        }

        // Edit GameInfo
        public void EditGame(string oAuthToken,
                             EditableGameInfo gameInfo,
                             ObjectCallback<GameInfo> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameInfo.id;
            API.StringValueField[] valueFields = gameInfo.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            StartCoroutine(ExecuteRequest<API.GameObject, GameInfo>(webRequest, onSuccess, onError));
        }


        // ---------[ MOD ENDPOINTS ]---------
        // Get All Mods
        public void GetAllMods(GetAllModsFilter filter,
                               ObjectCallback<ModInfo[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.ModObject, ModInfo>(webRequest, onSuccess, onError));
        }
        // Get Mod
        public void GetMod(int modId,
                           ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId;
            
            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);
            
            StartCoroutine(ExecuteRequest<API.ModObject, ModInfo>(webRequest, onSuccess, onError));
        }
        // Add Mod
        public void AddMod(string oAuthToken,
                           AddableModInfo modInfo,
                           ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods";
            API.StringValueField[] valueFields = modInfo.GetValueFields();
            API.BinaryDataField[] dataFields = modInfo.GetDataFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.ModObject>(endpointURL,
                                                                                            oAuthToken,
                                                                                            valueFields,
                                                                                            dataFields);
            StartCoroutine(ExecuteRequest<API.ModObject, ModInfo>(webRequest, onSuccess, onError));
        }
        // Edit Mod
        public void EditMod(string oAuthToken,
                            EditableModInfo modInfo,
                            ObjectCallback<ModInfo> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modInfo.id;
            API.StringValueField[] valueFields = modInfo.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            StartCoroutine(ExecuteRequest<API.ModObject, ModInfo>(webRequest, onSuccess, onError));
        }
        // Delete Mod
        public void DeleteMod(string oAuthToken,
                              int modId,
                              ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId;

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        // Get All Modfiles
        public void GetAllModfiles(int modId, GetAllModfilesFilter filter,
                                   ObjectCallback<Modfile[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/files";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.ModfileObject, Modfile>(webRequest, onSuccess, onError));
        }
        // Get Modfile
        public void GetModfile(int modId, int modfileId,
                               ObjectCallback<Modfile> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/files/" + modfileId;

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            StartCoroutine(ExecuteRequest<API.ModfileObject, Modfile>(webRequest, onSuccess, onError));
        }
        // Add Modfile
        public void AddModfile(string oAuthToken,
                               int modId, UnsubmittedModfile modfile,
                               ObjectCallback<Modfile> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/files";
            API.StringValueField[] valueFields = modfile.GetValueFields();
            API.BinaryDataField[] dataFields = modfile.GetDataFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.ModfileObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                dataFields);
            StartCoroutine(ExecuteRequest<API.ModfileObject, Modfile>(webRequest, onSuccess, onError));
        }
        // Edit Modfile
        public void EditModfile(string oAuthToken,
                                EditableModfile modfile,
                                ObjectCallback<Modfile> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modfile.modId + "/files/" + modfile.id;
            API.StringValueField[] valueFields = modfile.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            StartCoroutine(ExecuteRequest<API.ModfileObject, Modfile>(webRequest, onSuccess, onError));
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        // Add GameInfo Media
        public void AddGameMedia(string oAuthToken,
                                 UnsubmittedGameMedia gameMedia,
                                 ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/media";
            API.BinaryDataField[] dataFields = gameMedia.GetDataFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                null,
                                                                                                dataFields);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }
        // Add Mod Media
        public void AddModMedia(string oAuthToken,
                                int modId, UnsubmittedModMedia modMedia,
                                ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/media";
            API.StringValueField[] valueFields = modMedia.GetValueFields();
            API.BinaryDataField[] dataFields = modMedia.GetDataFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                dataFields);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }
        // Delete Mod Media
        public void DeleteModMedia(string oAuthToken,
                                   int modId, ModMediaToDelete modMediaToDelete,
                                   ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/media";
            API.StringValueField[] valueFields = modMediaToDelete.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        public void SubscribeToMod(string oAuthToken,
                                   int modId,
                                   ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                null,
                                                                                                null);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }
        public void UnsubscribeFromMod(string oAuthToken,
                                       int modId,
                                       ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/subscribe";

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }


        // ---------[ EVENT ENDPOINTS ]---------
        // Get Mod Events
        public void GetModEvents(int modId, GetModEventFilter filter,
                                 ObjectCallback<ModEvent[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/events";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.ModEventObject, ModEvent>(webRequest, onSuccess, onError));
        }
        // Get All Mod Events
        public void GetAllModEvents(GetAllModEventsFilter filter,
                                    ObjectCallback<ModEvent[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/events";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.ModEventObject, ModEvent>(webRequest, onSuccess, onError));
        }


        // ---------[ TAG ENDPOINTS ]---------
        // Get All Game Tag Options
        public void GetAllGameTagOptions(ObjectCallback<GameTagOption[]> onSuccess,
                                         ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/tags";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            StartCoroutine(ExecuteArrayRequest<API.GameTagOptionObject, GameTagOption>(webRequest, onSuccess, onError));
        }

        // Add Game Tag Option
        public void AddGameTagOption(string oAuthToken,
                                     UnsubmittedGameTagOption tagOption,
                                     ObjectCallback<APIMessage> onSuccess,
                                     ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/tags";
            API.StringValueField[] valueFields = tagOption.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }

        // Delete Game Tag Option
        public void DeleteGameTagOption(string oAuthToken,
                                        GameTagOptionToDelete gameTagOptionToDelete,
                                        ObjectCallback<APIMessage> onSuccess,
                                        ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/tags";
            API.StringValueField[] valueFields = gameTagOptionToDelete.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  valueFields);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }

        // Get All Mod Tags
        public void GetAllModTags(int modId, GetAllModTagsFilter filter,
                                  ObjectCallback<ModTag[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/tags";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.ModTagObject, ModTag>(webRequest, onSuccess, onError));
        }
        // Add Mod Tags
        public void AddModTags(string oAuthToken,
                               int modId, string[] tagNames,
                               ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/tags";
            API.StringValueField[] valueFields = new API.StringValueField[tagNames.Length];

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }
        // Delete Mod Tags
        public void DeleteModTags(string oAuthToken,
                                  int modId, string[] tagsToDelete,
                                  ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
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
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }


        // ---------[ RATING ENDPOINTS ]---------
        // Add Mod Rating
        public void AddModRating(string oAuthToken,
                                 int modId, int ratingValue,
                                 ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
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
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }


        // ---------[ METADATA ENDPOINTS ]---------
        // Get All Mod KVP Metadata
        public void GetAllModKVPMetadata(int modId,
                                         ObjectCallback<MetadataKVP[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/metadatakvp";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            StartCoroutine(ExecuteArrayRequest<API.MetadataKVPObject, MetadataKVP>(webRequest, onSuccess, onError));
        }
        // Add Mod KVP Metadata
        public void AddModKVPMetadata(string oAuthToken,
                                      int modId, UnsubmittedMetadataKVP[] metadataKVPs,
                                      ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
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
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }
        // Delete Mod KVP Metadata
        public void DeleteModKVPMetadata(string oAuthToken,
                                         int modId, UnsubmittedMetadataKVP[] metadataKVPsToRemove,
                                         ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
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
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        // Get All Mod Dependencies
        public void GetAllModDependencies(int modId, GetAllModDependenciesFilter filter,
                                          ObjectCallback<ModDependency[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/dependencies";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.ModDependencyObject, ModDependency>(webRequest, onSuccess, onError));
        }
        // Add Mod Dependencies
        public void AddModDependencies(string oAuthToken,
                                       int modId, int[] requiredModIds,
                                       ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
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
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }
        // Delete Mod Dependencies
        public void DeleteModDependencies(string oAuthToken,
                                          int modId, int[] modIdsToRemove,
                                          ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
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
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }


        // ---------[ TEAM ENDPOINTS ]---------
        // Get All Mod Team Members
        public void GetAllModTeamMembers(int modId, GetAllModTeamMembersFilter filter,
                                         ObjectCallback<TeamMember[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.TeamMemberObject, TeamMember>(webRequest, onSuccess, onError));
        }
        // Add Mod Team Member
        public void AddModTeamMember(string oAuthToken,
                                     int modId, UnsubmittedTeamMember teamMember,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team";
            API.StringValueField[] valueFields = teamMember.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }
        // Update Mod Team Member
        // NOTE(@jackson): Untested
        public void UpdateModTeamMember(string oAuthToken,
                                        int modId, EditableTeamMember teamMember,
                                        ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team/" + teamMember.id;
            API.StringValueField[] valueFields = teamMember.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePutRequest<API.MessageObject>(endpointURL,
                                                                                               oAuthToken,
                                                                                               valueFields);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }
        // Delete Mod Team Member
        public void DeleteModTeamMember(string oAuthToken,
                                        int modId, int teamMemberId,
                                        ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/team/" + teamMemberId;

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        // Get All Mod Comments
        // NOTE(@jackson): Untested
        public void GetAllModComments(int modId, GetAllModCommentsFilter filter,
                                      ObjectCallback<UserComment[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/comments";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.CommentObject, UserComment>(webRequest, onSuccess, onError));
        }
        // Get Mod Comment
        // NOTE(@jackson): Untested
        public void GetModComment(int modId, int commentId,
                                  ObjectCallback<UserComment> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            StartCoroutine(ExecuteRequest<API.CommentObject, UserComment>(webRequest, onSuccess, onError));
        }
        // Delete Mod Comment
        // NOTE(@jackson): Untested
        public void DeleteModComment(string oAuthToken,
                                     int modId, int commentId,
                                     ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "games/" + gameId + "/mods/" + modId + "/comments/" + commentId;

            UnityWebRequest webRequest = API.WebRequests.GenerateDeleteRequest<API.MessageObject>(endpointURL,
                                                                                                  oAuthToken,
                                                                                                  null);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
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
            StartCoroutine(ExecuteRequest<API.UserObject, User>(webRequest, onSuccess, onError));
        }
        // Get All Users
        public void GetAllUsers(GetAllUsersFilter filter,
                                ObjectCallback<User[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "users";

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, filter);

            StartCoroutine(ExecuteArrayRequest<API.UserObject, User>(webRequest, onSuccess, onError));
        }
        // Get User
        public void GetUser(int userID,
                            ObjectCallback<User> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "users/" + userID;

            UnityWebRequest webRequest = API.WebRequests.GenerateQuery(endpointURL, apiKey, Filter.None);

            StartCoroutine(ExecuteRequest<API.UserObject, User>(webRequest, onSuccess, onError));
        }


        // ---------[ REPORT ENDPOINTS ]---------
        // Submit Report
        // NOTE(@jackson): Untested
        public void SubmitReport(string oAuthToken,
                                 UnsubmittedReport report,
                                 ObjectCallback<APIMessage> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "report";
            API.StringValueField[] valueFields = report.GetValueFields();

            UnityWebRequest webRequest = API.WebRequests.GeneratePostRequest<API.MessageObject>(endpointURL,
                                                                                                oAuthToken,
                                                                                                valueFields,
                                                                                                null);
            StartCoroutine(ExecuteRequest<API.MessageObject, APIMessage>(webRequest, onSuccess, onError));
        }
        

        // ---------[ ME ENDPOINTS ]---------
        // Get Authenticated User
        public void GetAuthenticatedUser(string oAuthToken,
                                         ObjectCallback<User> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "me";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.UserObject>(endpointURL,
                                                                                            oAuthToken,
                                                                                            Filter.None);
            StartCoroutine(ExecuteRequest<API.UserObject, User>(webRequest, onSuccess, onError));
        }
        // Get User Subscriptions
        public void GetUserSubscriptions(string oAuthToken,
                                         GetUserSubscriptionsFilter filter,
                                         ObjectCallback<ModInfo[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "me/subscribed";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.ModObject[]>(endpointURL,
                                                                                             oAuthToken,
                                                                                             filter);
            StartCoroutine(ExecuteArrayRequest<API.ModObject, ModInfo>(webRequest, onSuccess, onError));
        }
        // Get User Games
        public void GetUserGames(string oAuthToken,
                                 ObjectCallback<GameInfo[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "me/games";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.GameObject[]>(endpointURL,
                                                                                              oAuthToken,
                                                                                              Filter.None);
            StartCoroutine(ExecuteArrayRequest<API.GameObject, GameInfo>(webRequest, onSuccess, onError));
        }
        // Get User Mods
        public void GetUserMods(string oAuthToken,
                                ObjectCallback<ModInfo[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "me/mods";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.ModObject[]>(endpointURL,
                                                                                             oAuthToken,
                                                                                             Filter.None);
            StartCoroutine(ExecuteArrayRequest<API.ModObject, ModInfo>(webRequest, onSuccess, onError));
        }
        // Get User Files
        public void GetUserModfiles(string oAuthToken,
                                    ObjectCallback<Modfile[]> onSuccess, ErrorCallback onError)
        {
            string endpointURL = API_URL + "me/files";

            UnityWebRequest webRequest = API.WebRequests.GenerateGetRequest<API.ModfileObject[]>(endpointURL,
                                                                                                 oAuthToken,
                                                                                                 Filter.None);
            StartCoroutine(ExecuteArrayRequest<API.ModfileObject, Modfile>(webRequest, onSuccess, onError));
        }
    }
}
