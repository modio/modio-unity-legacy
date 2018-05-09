using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

namespace ModIO
{
    // ----[ EVENT DELEGATES ]---
    public delegate void ModProfilesEventHandler(IEnumerable<ModProfile> modProfiles);
    public delegate void ModIdsEventHandler(IEnumerable<int> modIds);
    public delegate void ModfileStubsEventHandler(IEnumerable<ModfileStub> modfiles);

    public class ClientRequest<T>
    {
        public T response = default(T);
        public WebRequestError error = null;
    }

    public class ModBrowser : MonoBehaviour
    {
        // ---------[ INNER CLASSES ]---------
        [System.Serializable]
        private class ManifestData
        {
            public int lastCacheUpdate;
        }

        // ---------[ FIELDS ]---------
        // --- Key Data ---
        public int gameId = GlobalSettings.GAME_ID;
        public string gameKey = GlobalSettings.GAME_APIKEY;
        public bool isAutomaticUpdateEnabled = false;

        // --- Caching ---
        public GameProfile gameProfile;
        public ModProfile[] modProfileCache;

        // ---- Non Serialized ---
        [HideInInspector]
        public AuthenticatedUser authUser = null;
        [HideInInspector]
        public int lastCacheUpdate = -1;

        // --- File Paths ---
        public static string manifestFilePath { get { return CacheManager.GetCacheDirectory() + "browser_manifest.data"; } }

        // ---------[ EVENTS ]---------
        public event ModProfilesEventHandler modsAvailable;
        public event ModProfilesEventHandler modsEdited;
        public event ModfileStubsEventHandler modReleasesUpdated;
        public event ModIdsEventHandler modsUnavailable;

        // ---------[ INITIALIZATION ]---------
        protected bool _isInitialized = false;

        protected virtual void Start()
        {
            API.Client.SetGameDetails(gameId, gameKey);
            
            StartCoroutine(InitializationCoroutine(OnInitialized));
        }

        protected virtual IEnumerator InitializationCoroutine(Action onInitializedCallback)
        {
            // --- Load Manifest ---
            ManifestData manifest = CacheManager.ReadJsonObjectFile<ManifestData>(ModBrowser.manifestFilePath);
            if(manifest != null)
            {
                this.lastCacheUpdate = manifest.lastCacheUpdate;
            }

            // --- Load User ---
            this.authUser = CacheManager.LoadAuthenticatedUser();

            if(this.authUser != null)
            {
                API.Client.SetUserAuthorizationToken(this.authUser.oAuthToken);
            }

            // --- Load Game Profile ---
            ClientRequest<GameProfile> gameRequest = new ClientRequest<GameProfile>();

            yield return LoadOrDownloadGameProfile(gameRequest);

            if(gameRequest.error == null)
            {
                this.gameProfile = gameRequest.response;
            }
            else
            {
                API.Client.LogError(gameRequest.error);
                this.gameProfile = null;
            }

            // --- Post Initialization ---
            this._isInitialized = true;

            if(onInitializedCallback != null)
            {
                onInitializedCallback();
            }
        }

        protected virtual void OnInitialized()
        {
            // TODO(@jackson): Process Updates
        }

        // ---------[ COROUTINE HELPERS ]---------
        private static void OnSuccess<T>(T response, ClientRequest<T> request, out bool isDone)
        {
            request.response = response;
            isDone = true;
        }
        private static void OnError<T>(WebRequestError error, ClientRequest<T> request, out bool isDone)
        {
            request.error = error;
            isDone = true;
        }

        // ---------[ UPDATES ]---------
        private const float AUTOMATIC_UPDATE_INTERVAL = 2f;
        private Coroutine updatedRoutine = null;

        protected virtual void Update()
        {
            if(this._isInitialized)
            {
                bool isUpdateRunning = (updatedRoutine != null);
                if(this.isAutomaticUpdateEnabled != isUpdateRunning)
                {
                    if(this.isAutomaticUpdateEnabled)
                    {
                        this.updatedRoutine = StartCoroutine(AutomaticUpdateCoroutine());
                    }
                    else
                    {
                        StopCoroutine(this.updatedRoutine);
                        this.updatedRoutine = null;
                    }
                }
            }
        }

        protected IEnumerator AutomaticUpdateCoroutine()
        {
            while(true)
            {
                yield return FetchAndProcessAllEvents();

                yield return new WaitForSeconds(AUTOMATIC_UPDATE_INTERVAL);
            }
        }

        public virtual IEnumerator FetchAndProcessAllEvents()
        {
            int updateStartTimeStamp = ServerTimeStamp.Now;

            // - Get Mod Updates -
            yield return FetchAndProcessModEvents(this.lastCacheUpdate,
                                                  updateStartTimeStamp);

            this.lastCacheUpdate = updateStartTimeStamp;
        }

        // TODO(@jackson): Sort dateAdded desc, save lastCacheUpdate as most recent event (re:errors, etc)
        public virtual IEnumerator FetchAndProcessModEvents(int fromTimeStamp,
                                                            int untilTimeStamp)
        {
            // - Filter & Pagination -
            API.RequestFilter modEventFilter = new API.RequestFilter();
            modEventFilter.sortFieldName = API.GetAllModEventsFilterFields.dateAdded;
            modEventFilter.fieldFilters[API.GetAllModEventsFilterFields.dateAdded]
                = new API.RangeFilter<int>()
                {
                    min = fromTimeStamp,
                    isMinInclusive = false,
                    max = untilTimeStamp,
                    isMaxInclusive = true,
                };

            API.PaginationParameters modEventPagination = new API.PaginationParameters()
            {
                limit = API.PaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            // - Get All Events -
            bool isRequestCompleted = false;
            while(!isRequestCompleted)
            {
                var modEventRequest = new ClientRequest<API.ResponseArray<ModEvent>>();
                bool isDone = false;

                API.Client.GetAllModEvents(modEventFilter,
                                           modEventPagination,
                                           (r) => ModBrowser.OnSuccess(r, modEventRequest, out isDone),
                                           (e) => ModBrowser.OnError(e, modEventRequest, out isDone));

                while(!isDone) { yield return null; }

                if(modEventRequest.response != null)
                {
                    yield return ProcessModEvents(modEventRequest.response);

                    if(modEventRequest.response.Count < modEventRequest.response.Limit)
                    {
                        isRequestCompleted = true;
                    }
                    else
                    {
                        modEventPagination.offset += modEventPagination.limit;
                    }

                }
                else
                {
                    if(modEventRequest.error != null)
                    {
                        Debug.LogWarning(modEventRequest.error.ToUnityDebugString());
                    }

                    isRequestCompleted = true;
                }
            }
        }

        // OPTIMIZE(@jackson): Replace List with HashSet?
        public virtual IEnumerator ProcessModEvents(IEnumerable<ModEvent> modEvents)
        {
            List<int> addedIds = new List<int>();
            List<int> editedIds = new List<int>();
            List<int> modfileChangedIds = new List<int>();
            List<int> removedIds = new List<int>();

            // Sort by event type
            foreach(ModEvent modEvent in modEvents)
            {
                switch(modEvent.eventType)
                {
                    case ModEventType.ModAvailable:
                    {
                        addedIds.Add(modEvent.modId);
                    }
                    break;
                    case ModEventType.ModEdited:
                    {
                        editedIds.Add(modEvent.modId);
                    }
                    break;
                    case ModEventType.ModfileChanged:
                    {
                        modfileChangedIds.Add(modEvent.modId);
                    }
                    break;
                    case ModEventType.ModUnavailable:
                    {
                        removedIds.Add(modEvent.modId);
                    }
                    break;
                }
            }

            // --- Process Add/Edit/ModfileChanged ---
            List<int> modsToFetch = new List<int>(addedIds.Count + editedIds.Count + modfileChangedIds.Count);
            modsToFetch.AddRange(addedIds);
            modsToFetch.AddRange(editedIds);
            modsToFetch.AddRange(modfileChangedIds);

            // - Create Update Lists -
            List<ModProfile> updatedProfiles = new List<ModProfile>(modsToFetch.Count);
            List<ModProfile> addedProfiles = new List<ModProfile>(addedIds.Count);
            List<ModProfile> editedProfiles = new List<ModProfile>(editedIds.Count);
            List<ModfileStub> modfileChangedStubs = new List<ModfileStub>(modfileChangedIds.Count);

            if(modsToFetch.Count > 0)
            {
                // - Filter & Pagination -
                API.RequestFilter modsFilter = new API.RequestFilter();
                modsFilter.fieldFilters[API.GetAllModsFilterFields.id]
                = new API.InArrayFilter<int>()
                {
                    filterArray = modsToFetch.ToArray(),
                };

                API.PaginationParameters modsPagination = new API.PaginationParameters()
                {
                    limit = API.PaginationParameters.LIMIT_MAX,
                    offset = 0,
                };

                // - Get Mods -
                bool isRequestCompleted = false;
                while(!isRequestCompleted)
                {
                    var modRequest = new ClientRequest<API.ResponseArray<ModProfile>>();
                    bool isDone = false;

                    API.Client.GetAllMods(modsFilter,
                                          modsPagination,
                                          (r) => ModBrowser.OnSuccess(r, modRequest, out isDone),
                                          (e) => ModBrowser.OnError(e, modRequest, out isDone));

                    while(!isDone) { yield return null; }

                    if(modRequest.response != null)
                    {
                        foreach(ModProfile profile in modRequest.response)
                        {
                            int idIndex;
                            // NOTE(@jackson): If added, ignore everything else
                            if((idIndex = addedIds.IndexOf(profile.id)) >= 0)
                            {
                                addedIds.RemoveAt(idIndex);
                                addedProfiles.Add(profile);
                            }
                            else
                            {
                                if((idIndex = editedIds.IndexOf(profile.id)) >= 0)
                                {
                                    editedIds.RemoveAt(idIndex);
                                    editedProfiles.Add(profile);
                                }
                                if((idIndex = modfileChangedIds.IndexOf(profile.id)) >= 0)
                                {
                                    modfileChangedIds.RemoveAt(idIndex);
                                    modfileChangedStubs.Add(profile.currentRelease);
                                }
                            }

                            updatedProfiles.Add(profile);
                        }

                        if(modRequest.response.Count < modRequest.response.Limit)
                        {
                            isRequestCompleted = true;
                        }
                        else
                        {
                            modsPagination.offset += modsPagination.limit;
                        }
                    }
                    else
                    {
                        if(modRequest.error != null)
                        {
                            Debug.LogWarning(modRequest.error.ToUnityDebugString());
                        }

                        isRequestCompleted = true;
                    }
                }

                // - Save changed to cache -
                CacheManager.SaveModProfiles(updatedProfiles);
            }

            // --- Process Removed ---
            if(removedIds.Count > 0)
            {
                foreach(int modId in removedIds)
                {
                    CacheManager.UncacheMod(modId);
                }

                // TODO(@jackson): Remove from local array
                // TODO(@jackson): Compare with subscriptions
            }

            // --- Notifications ---
            if(this.modsAvailable != null
               && addedProfiles.Count > 0)
            {
                this.modsAvailable(addedProfiles);
            }

            if(this.modsEdited != null
               && editedProfiles.Count > 0)
            {
                this.modsEdited(editedProfiles);
            }

            if(this.modReleasesUpdated != null
               && modfileChangedStubs.Count > 0)
            {
                this.modReleasesUpdated(modfileChangedStubs);
            }

            if(this.modsUnavailable != null
               && removedIds.Count > 0)
            {
                this.modsUnavailable(removedIds);
            }
        }

        // ---------[ GAME ENDPOINTS ]---------
        public static IEnumerator LoadOrDownloadGameProfile(ClientRequest<GameProfile> request)
        {
            bool isDone = false;

            // - Attempt load from cache -
            CacheManager.LoadGameProfile((r) => ModBrowser.OnSuccess(r, request, out isDone));

            while(!isDone) { yield return null; }

            if(request.response == null)
            {
                isDone = false;

                API.Client.GetGame((r) => ModBrowser.OnSuccess(r, request, out isDone),
                                   (e) => ModBrowser.OnError(e, request, out isDone));

                while(!isDone) { yield return null; }

                if(request.error == null)
                {
                    CacheManager.SaveGameProfile(request.response);
                }
            }
        }
    }
}
