using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

namespace ModIO
{
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
            ClientRequest<GameProfile> gameRequest = new ClientRequest<GameProfile>();

            yield return ModManager.RequestGameProfile(gameRequest);

            if(gameRequest.error == null)
            {
                this.gameProfile = gameRequest.response;
            }
            else
            {
                APIClient.LogError(gameRequest.error);
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

                var request = new ClientRequest<List<ModEvent>>();

                yield return ModManager.RequestAndApplyAllModEventsToCache(this.lastCacheUpdate,
                                                                           updateStartTimeStamp,
                                                                           request);

                if(request.error == null)
                {
                    this.lastCacheUpdate = updateStartTimeStamp;
                }

                // TOOD(@jackson): Add User Events

                yield return new WaitForSeconds(AUTOMATIC_UPDATE_INTERVAL);
            }
        }
    }
}
