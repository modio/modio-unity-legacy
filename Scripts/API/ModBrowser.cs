using UnityEngine;

using System.Collections;
// using System.Collections.Generic;

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
        public AuthenticatedUser authUser = null;

        // --- Caching ---
        public GameProfile gameProfile;
        public ModProfile[] modProfileCache;

        [System.NonSerialized]
        public int lastCacheUpdate = -1;

        // --- File Paths ---
        public static string manifestFilePath { get { return CacheManager.GetCacheDirectory() + "browser_manifest.data"; } }

        // ---------[ INITIALIZATION ]---------
        protected virtual void Start()
        {
            API.Client.SetGameDetails(gameId, gameKey);
            
            StartCoroutine(this.InitializationCoroutine());
        }

        protected virtual IEnumerator InitializationCoroutine()
        {
            // --- Load Manifest ---
            ManifestData manifest = CacheManager.ReadJsonObjectFile<ManifestData>(ModBrowser.manifestFilePath);
            if(manifest != null)
            {
                this.lastCacheUpdate = manifest.lastCacheUpdate;
            }

            // --- Load User ---
            authUser = CacheManager.LoadAuthenticatedUser();

            if(authUser != null)
            {
                API.Client.SetUserAuthorizationToken(authUser.oAuthToken);
            }


            // --- Load Game Profile ---
            ClientRequest<GameProfile> gameRequest = new ClientRequest<GameProfile>();

            yield return LoadOrDownloadGameProfile(gameRequest);

            if(gameRequest.error == null)
            {
                gameProfile = gameRequest.response;
            }
            else
            {
                API.Client.LogError(gameRequest.error);
                gameProfile = null;
            }
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

        // ---------[ GAME ENDPOINTS ]---------
        public static IEnumerator LoadOrDownloadGameProfile(ClientRequest<GameProfile> request)
        {
            bool isDone = false;

            // - Attempt load from cache -
            CacheManager.LoadGameProfile((r) => ModBrowser.OnSuccess(r, request, out isDone));

            while(!isDone) { yield return null; }

            if(request.response == null)
            {
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
