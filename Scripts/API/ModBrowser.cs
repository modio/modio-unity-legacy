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

        // ---------[ INITIALIZATION ]---------
        protected bool _isInitialized = false;

        protected virtual void Start()
        {
            API.Client.SetGameDetails(gameId, gameKey);
            ModManager.AuthorizeClientUsingCachedUser();

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

            // --- Load Game Profile ---
            ClientRequest<GameProfile> gameRequest = new ClientRequest<GameProfile>();

            yield return ModManager.RequestGameProfile(gameRequest);

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
            // TODO(@jackson): Fill cache fields
        }

        protected virtual void OnEnable() {}

        protected virtual void OnDisable() {}

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
                int updateStartTimeStamp = ServerTimeStamp.Now;

                yield return ModManager.FetchAndProcessAllEvents(this.lastCacheUpdate,
                                                                 updateStartTimeStamp);

                this.lastCacheUpdate = updateStartTimeStamp;


                yield return new WaitForSeconds(AUTOMATIC_UPDATE_INTERVAL);
            }
        }
    }
}
