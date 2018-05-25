using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

namespace ModIO
{
    public delegate void ModProfilesEventHandler(IEnumerable<ModProfile> modProfiles);
    public delegate void ModIdsEventHandler(IEnumerable<int> modIds);
    public delegate void ModfileStubsEventHandler(IEnumerable<ModfileStub> modfiles);

    public class ClientRequest<T>
    {
        public T response = default(T);
        public WebRequestError error = null;
    }

    // TODO(@jackson): ErrorWrapper to handle specific error codes?
    public class ModBrowser : MonoBehaviour
    {
        // ---------[ EVENTS ]---------
        public event ModProfilesEventHandler     modsAvailable;
        public event ModProfilesEventHandler     modsEdited;
        public event ModfileStubsEventHandler    modReleasesUpdated;
        public event ModIdsEventHandler          modsUnavailable;

        // ---------[ NESTED CLASSES ]---------
        [System.Serializable]
        private class ManifestData
        {
            public int lastCacheUpdate;
        }

        // ---------[ FIELDS ]---------
        // --- Key Data ---
        public int gameId = GlobalSettings.GAME_ID;
        public string gameAPIKey = GlobalSettings.GAME_APIKEY;
        public bool isAutomaticUpdateEnabled = false;

        // --- Caching ---
        public GameProfile gameProfile;
        public ModProfile[] modProfileCache;

        // ---- Non Serialized ---
        [HideInInspector]
        public UserProfile userProfile = null;
        [HideInInspector]
        public int lastCacheUpdate = -1;

        // --- File Paths ---
        public static string manifestFilePath { get { return CacheClient.GetCacheDirectory() + "browser_manifest.data"; } }

        // ---------[ COROUTINE HELPERS ]---------
        private static void OnRequestSuccess<T>(T response, ClientRequest<T> request, out bool isDone)
        {
            request.response = response;
            isDone = true;
        }
        private static void OnRequestError<T>(WebRequestError error, ClientRequest<T> request, out bool isDone)
        {
            request.error = error;
            isDone = true;
        }

        // ---------[ INITIALIZATION ]---------
        protected bool _isInitialized = false;

        protected virtual void Start()
        {
            APIClient.gameId = this.gameId;
            APIClient.gameAPIKey = this.gameAPIKey;

            string userToken = CacheClient.LoadAuthenticatedUserToken();

            if(!String.IsNullOrEmpty(userToken))
            {
                APIClient.userAuthorizationToken = userToken;

                ModManager.GetAuthenticatedUserProfile((userProfile) =>
                {
                    this.userProfile = userProfile;

                    StartCoroutine(InitializationCoroutine(OnInitialized));
                },
                (e) => { StartCoroutine(InitializationCoroutine(OnInitialized)); });
            }
            else
            {
                StartCoroutine(InitializationCoroutine(OnInitialized));
            }
        }

        protected virtual IEnumerator InitializationCoroutine(Action onInitializedCallback)
        {
            // --- Load Manifest ---
            ManifestData manifest = CacheClient.ReadJsonObjectFile<ManifestData>(ModBrowser.manifestFilePath);
            if(manifest != null)
            {
                this.lastCacheUpdate = manifest.lastCacheUpdate;
            }

            // --- Load Game Profile ---
            bool isDone = false;
            ClientRequest<GameProfile> gameRequest = new ClientRequest<GameProfile>();

            ModManager.GetGameProfile((r) => ModBrowser.OnRequestSuccess(r, gameRequest, out isDone),
                                      (e) => ModBrowser.OnRequestError(e, gameRequest, out isDone));

            while(!isDone) { yield return null; }

            if(gameRequest.error == null)
            {
                this.gameProfile = gameRequest.response;
            }
            else
            {
                WebRequestError.LogAsWarning(gameRequest.error);
                this.gameProfile = null;
            }

            // --- Load Mod Profiles ---
            this.modProfileCache = (new List<ModProfile>(CacheClient.AllModProfiles())).ToArray();

            // --- Post Initialization ---
            this._isInitialized = true;

            if(onInitializedCallback != null)
            {
                onInitializedCallback();
            }
        }

        protected virtual void OnInitialized()
        {
            // TODO(@jackson): Fill cache fields
        }

        protected virtual void OnEnable() {}

        protected virtual void OnDisable() {}

        // ---------[ UPDATES ]---------
        private const float AUTOMATIC_UPDATE_INTERVAL = 15f;
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
                int updateStartTimeStamp = ServerTimeStamp.Now;

                bool isDone = false;

                var eventRequest = new ClientRequest<List<ModEvent>>();
                ModManager.FetchAllModEvents(this.lastCacheUpdate, updateStartTimeStamp,
                                             (r) => ModBrowser.OnRequestSuccess(r, eventRequest, out isDone),
                                             (e) => ModBrowser.OnRequestError(e, eventRequest, out isDone));
                while(!isDone) { yield return null; }

                if(eventRequest.response != null)
                {
                    isDone = false;

                    // - Callbacks -
                    Action<List<ModProfile>> onAvailable = (profiles) =>
                    {
                        if(this.modsAvailable != null)
                        {
                            this.modsAvailable(profiles);
                        }
                    };
                    Action<List<ModProfile>> onEdited = (profiles) =>
                    {
                        if(this.modsEdited != null)
                        {
                            this.modsEdited(profiles);
                        }
                    };
                    Action<List<ModfileStub>> onReleasesUpdated = (modfiles) =>
                    {
                        if(this.modReleasesUpdated != null)
                        {
                            this.modReleasesUpdated(modfiles);
                        }
                    };
                    Action<List<int>> onUnavailable = (ids) =>
                    {
                        if(this.modsUnavailable != null)
                        {
                            this.modsUnavailable(ids);
                        }
                    };
                    Action onSuccess = () =>
                    {
                        this.lastCacheUpdate = updateStartTimeStamp;
                        isDone = true;
                    };
                    Action<WebRequestError> onError = (error) =>
                    {
                        WebRequestError.LogAsWarning(error);
                        isDone = true;
                    };

                    ModManager.ApplyModEventsToCache(eventRequest.response,
                                                     onAvailable, onEdited,
                                                     onUnavailable, onReleasesUpdated,
                                                     onSuccess,
                                                     onError);

                    while(!isDone) { yield return null; }
                }

                // TODO(@jackson): Add User Events

                yield return new WaitForSeconds(AUTOMATIC_UPDATE_INTERVAL);
            }
        }
    }
}
