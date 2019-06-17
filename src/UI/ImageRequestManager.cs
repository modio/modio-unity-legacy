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
        /// <summary>Should requests made by the ImageRequestManager be logged.</summary>
        public bool logDownloads = true;

        /// <summary>Should the cache be cleared on disable.</summary>
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
        /// <summary>Requests the image for a given ImageDisplayData.</summary>
        public virtual void RequestImageForData(ImageDisplayData data, bool original,
                                                Action<Texture2D> onSuccess,
                                                Action<WebRequestError> onError)
        {
            string url = data.GetImageURL(original);

            // asserts
            Debug.Assert(onSuccess != null);
            Debug.Assert(!string.IsNullOrEmpty(url));

            // create disk retrieval function
            Func<Texture2D> retrieveFromDisk = null;
            switch(data.descriptor)
            {
                case ImageDescriptor.ModLogo:
                {
                    LogoSize size = (original ? LogoSize.Original : ImageDisplayData.logoThumbnailSize);
                    retrieveFromDisk = () =>
                    {
                        return CacheClient.LoadModLogo(data.ownerId, data.imageId, size);
                    };
                }
                break;
                case ImageDescriptor.ModGalleryImage:
                {
                    ModGalleryImageSize size = (original
                                                ? ModGalleryImageSize.Original
                                                : ImageDisplayData.galleryThumbnailSize);
                    retrieveFromDisk = () =>
                    {
                        return CacheClient.LoadModGalleryImage(data.ownerId, data.imageId, size);
                    };
                }
                break;
                case ImageDescriptor.YouTubeThumbnail:
                {
                    retrieveFromDisk = () =>
                    {
                        return CacheClient.LoadModYouTubeThumbnail(data.ownerId, data.imageId);
                    };
                }
                break;
                case ImageDescriptor.UserAvatar:
                {
                    UserAvatarSize size = (original
                                           ? UserAvatarSize.Original
                                           : ImageDisplayData.avatarThumbnailSize);
                    retrieveFromDisk = () =>
                    {
                        return CacheClient.LoadUserAvatar(data.ownerId, size);
                    };
                }
                break;
            }

            this.RequestImage_Internal(url, retrieveFromDisk, onSuccess, onError);
        }

        /// <summary>Requests an image at a given URL.</summary>
        // NOTE(@jackson): This function *does not* check for data stored with CacheClient.
        public virtual void RequestImage(string url,
                                         Action<Texture2D> onSuccess,
                                         Action<WebRequestError> onError)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(onSuccess != null);

            this.RequestImage_Internal(url, null, onSuccess, onError);
        }

        protected virtual void RequestImage_Internal(string url,
                                                     Func<Texture2D> retrieveFromDisk,
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
                if(onError != null)
                {
                    callbacks.failed.Add(onError);
                }
                return;
            }

            // check disk
            if(retrieveFromDisk != null)
            {
                texture = retrieveFromDisk();
                if(texture != null)
                {
                    onSuccess(texture);
                    return;
                }
            }

            // download
            this.DownloadImage(url, onSuccess, onError);
        }

        protected UnityWebRequestAsyncOperation DownloadImage(string url,
                                                              Action<Texture2D> onSuccess,
                                                              Action<WebRequestError> onError)
        {
            // create new download
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            // create new callbacks entry
            Callbacks callbacks = new Callbacks();
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

            #if DEBUG
            if(PluginSettings.data.logAllRequests && logDownloads)
            {
                string requestHeaders = "";
                List<string> requestKeys = new List<string>(APIClient.UNITY_REQUEST_HEADER_KEYS);
                requestKeys.AddRange(APIClient.MODIO_REQUEST_HEADER_KEYS);

                foreach(string headerKey in requestKeys)
                {
                    string headerValue = webRequest.GetRequestHeader(headerKey);
                    if(headerValue != null)
                    {
                        requestHeaders += "\n" + headerKey + ": " + headerValue;
                    }
                }

                int timeStamp = ServerTimeStamp.Now;
                Debug.Log("IMAGE REQUEST SENT"
                          + "\nURL: " + webRequest.url
                          + "\nTimeStamp: [" + timeStamp.ToString() + "] "
                          + ServerTimeStamp.ToLocalDateTime(timeStamp).ToString()
                          + "\nHeaders: " + requestHeaders);
            }
            #endif

            return operation;
        }

        /// <summary>Handles the completion of an image download.</summary>
        protected virtual void OnDownloadCompleted(UnityWebRequest webRequest)
        {
            // early out if destroyed
            if(this == null) { return; }

            Debug.Assert(webRequest != null);

            // - logging -
            #if DEBUG
            if(PluginSettings.data.logAllRequests && logDownloads)
            {
                if(webRequest.isNetworkError || webRequest.isHttpError)
                {
                    WebRequestError.LogAsWarning(WebRequestError.GenerateFromWebRequest(webRequest));
                }
                else
                {
                    var headerString = new System.Text.StringBuilder();
                    var responseHeaders = webRequest.GetResponseHeaders();
                    if(responseHeaders != null
                       && responseHeaders.Count > 0)
                    {
                        headerString.Append("\n");
                        foreach(var kvp in responseHeaders)
                        {
                            headerString.AppendLine("- [" + kvp.Key + "] " + kvp.Value);
                        }
                    }
                    else
                    {
                        headerString.Append(" NONE");
                    }

                    var responseTimeStamp = ServerTimeStamp.Now;
                    string logString = ("IMAGE DOWNLOAD SUCCEEDED\n"
                                        + "\nURL: " + webRequest.url
                                        + "\nTime Stamp: " + responseTimeStamp + " ("
                                        + ServerTimeStamp.ToLocalDateTime(responseTimeStamp) + ")"
                                        + "\nResponse Headers: " + headerString.ToString()
                                        + "\nResponse Code: " + webRequest.responseCode
                                        + "\nResponse Error: " + webRequest.error
                                        + "\n");
                    Debug.Log(logString);
                }
            }
            #endif

            // handle callbacks
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

            // remove from "in progress"
            this.m_callbackMap.Remove(webRequest.url);
        }
    }
}
