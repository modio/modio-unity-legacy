using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO.UI
{
    /// <summary>Manages caching of the textures required by the UI.</summary>
    public class ImageRequestManager : MonoBehaviour
    {
        // ---------[ SINGLETON ]---------
        private static ImageRequestManager _instance = null;
        public static ImageRequestManager instance
        {
            get
            {
                if(ImageRequestManager._instance == null)
                {
                    ImageRequestManager._instance = UIUtilities.FindComponentInScene<ImageRequestManager>(true);

                    if(ImageRequestManager._instance == null)
                    {
                        GameObject irmGO = new GameObject("Image Request Manager");
                        ImageRequestManager._instance = irmGO.AddComponent<ImageRequestManager>();
                    }
                }

                return ImageRequestManager._instance;
            }
        }

        // ---------[ NESTED DATA-TYPES ]---------
        private class Callbacks
        {
            public List<Action<Texture2D>> succeeded;
            public List<Action<WebRequestError>> failed;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Should the cache be cleared on disable</summary>
        public bool clearCacheOnDisable = true;

        /// <summary>Cached images.</summary>
        public Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>Callback map for currently downloading images.</summary>
        private Dictionary<string, Callbacks> m_callbackMap = new Dictionary<string, Callbacks>();

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            if(ImageRequestManager._instance == null)
            {
                ImageRequestManager._instance = this;
            }
            #if DEBUG
            else if(ImageRequestManager._instance != this)
            {
                Debug.LogWarning("[mod.io] Second instance of a ImageRequestManager"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a ImageRequestManager"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif
        }

        protected virtual void OnDisable()
        {
            if(this.clearCacheOnDisable)
            {
                this.cache.Clear();
            }
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Requests an image at a given URL.</summary>
        public virtual void RequestImage(string url,
                                         Action<Texture2D> onSuccess,
                                         Action<WebRequestError> onError)
        {
            // check cache
            Texture2D texture = null;
            if(this.cache.TryGetValue(url, out texture))
            {
                onSuccess(texture);
                return;
            }

            // check currently downloading
            Callbacks callbacks = null;
            if(this.m_callbackMap.TryGetValue(url, out callbacks))
            {
                callbacks.succeeded.Add(onSuccess);
                callbacks.failed.Add(onError);
                return;
            }

            // create new download
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            // create new callbacks entry
            callbacks = new Callbacks();
            callbacks.succeeded = new List<Action<Texture2D>>();
            callbacks.succeeded.Add(onSuccess);
            callbacks.failed = new List<Action<WebRequestError>>();
            if(onError != null)
            {
                callbacks.failed.Add(onError);
            }
            this.m_callbackMap.Add(url, callbacks);

            // start download and attach callbacks
            var operation = webRequest.SendWebRequest();
            operation.completed += (o) =>
            {
                OnDownloadCompleted(operation.webRequest);
            };
        }

        /// <summary>Handles the completion of an image download.</summary>
        protected virtual void OnDownloadCompleted(UnityWebRequest webRequest)
        {
            if(this == null) { return; }

            Debug.Assert(webRequest != null);

            Callbacks callbacks = this.m_callbackMap[webRequest.url];

            if(webRequest.isHttpError || webRequest.isNetworkError)
            {
                if(callbacks.failed.Count > 0)
                {
                    WebRequestError error = WebRequestError.GenerateFromWebRequest(webRequest);

                    foreach(var errorCallback in callbacks.failed)
                    {
                        errorCallback(error);
                    }
                }
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;

                if(this.isActiveAndEnabled || !this.clearCacheOnDisable)
                {
                    this.cache[webRequest.url] = texture;
                }

                foreach(var successCallback in callbacks.succeeded)
                {
                    successCallback(texture);
                }

                this.cache[webRequest.url] = texture;
            }

            this.m_callbackMap.Remove(webRequest.url);
        }
    }
}
