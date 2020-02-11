using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO.UI
{
    /// <summary>Manages caching of the textures required by the UI.</summary>
    public class ImageRequestManager : MonoBehaviour, IModSubscriptionsUpdateReceiver
    {
        // ---------[ SINGLETON ]---------
        private static ImageRequestManager _instance = null;
        public static ImageRequestManager instance
        {
            get
            {
                if(ImageRequestManager._instance == null)
                {
                    ImageRequestManager._instance = UIUtilities.FindComponentInAllScenes<ImageRequestManager>(true);

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
        protected class Callbacks
        {
            public List<Action<Texture2D>> succeeded;
            public List<Action<WebRequestError>> failed;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Should requests made by the ImageRequestManager be logged.</summary>
        public bool logDownloads = false;

        /// <summary>Should the cache be cleared on disable.</summary>
        public bool clearCacheOnDisable = true;

        /// <summary>If enabled, stores retrieved images for subscribed mods.</summary>
        public bool storeIfSubscribed = true;

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
        /// <summary>Requests the image for a given locator.</summary>
        public virtual void RequestModLogo(int modId, LogoImageLocator locator,
                                           LogoSize size,
                                           Action<Texture2D> onLogoReceived,
                                           Action<Texture2D> onFallbackFound,
                                           Action<WebRequestError> onError)
        {
            Debug.Assert(locator != null);
            Debug.Assert(onLogoReceived != null);

            // set loading function
            Func<Texture2D> loadFromDisk = () => CacheClient.LoadModLogo(modId, locator.GetFileName(), size);

            // set saving function
            Action<Texture2D> saveToDisk = null;
            if(this.storeIfSubscribed)
            {
                saveToDisk = (t) =>
                {
                    if(ModManager.GetSubscribedModIds().Contains(modId))
                    {
                        CacheClient.SaveModLogo(modId, locator.GetFileName(), size, t);
                    }
                };
            }

            // do the work
            this.RequestImage_Internal(locator, size, loadFromDisk, saveToDisk,
                                       onLogoReceived, onFallbackFound, onError);
        }

        /// <summary>Requests the image for a given locator.</summary>
        public virtual void RequestModGalleryImage(int modId, GalleryImageLocator locator,
                                                   ModGalleryImageSize size,
                                                   Action<Texture2D> onImageReceived,
                                                   Action<Texture2D> onFallbackFound,
                                                   Action<WebRequestError> onError)
        {
            Debug.Assert(locator != null);
            Debug.Assert(onImageReceived != null);

            // set loading function
            Func<Texture2D> loadFromDisk = () => CacheClient.LoadModGalleryImage(modId, locator.GetFileName(), size);

            // set saving function
            Action<Texture2D> saveToDisk = null;
            if(this.storeIfSubscribed)
            {
                saveToDisk = (t) =>
                {
                    if(ModManager.GetSubscribedModIds().Contains(modId))
                    {
                        CacheClient.SaveModGalleryImage(modId, locator.GetFileName(), size, t);
                    }
                };
            }

            // do the work
            this.RequestImage_Internal(locator, size, loadFromDisk, saveToDisk,
                                       onImageReceived, onFallbackFound, onError);
        }

        /// <summary>Requests the user avatar for the given locator.</summary>
        public virtual void RequestUserAvatar(int userId, AvatarImageLocator locator,
                                              UserAvatarSize size,
                                              Action<Texture2D> onAvatarReceived,
                                              Action<Texture2D> onFallbackFound,
                                              Action<WebRequestError> onError)
        {
            Debug.Assert(locator != null);
            Debug.Assert(onAvatarReceived != null);

            // set loading function
            Func<Texture2D> loadFromDisk = () => CacheClient.LoadUserAvatar(userId, size);

            // do the work
            this.RequestImage_Internal(locator, size, loadFromDisk, null,
                                       onAvatarReceived, onFallbackFound, onError);
        }

        /// <summary>Requests the thumbnail for a given YouTube video.</summary>
        public virtual void RequestYouTubeThumbnail(int modId, string youTubeId,
                                                    Action<Texture2D> onThumbnailReceived,
                                                    Action<WebRequestError> onError)
        {
            Debug.Assert(onThumbnailReceived != null);

            // early out
            if(string.IsNullOrEmpty(youTubeId))
            {
                onThumbnailReceived(null);
                return;
            }

            // set loading function
            Func<Texture2D> loadFromDisk = () => CacheClient.LoadModYouTubeThumbnail(modId, youTubeId);

            // set saving function
            Action<Texture2D> saveToDisk = null;
            if(this.storeIfSubscribed)
            {
                saveToDisk = (t) =>
                {
                    if(ModManager.GetSubscribedModIds().Contains(modId))
                    {
                        CacheClient.SaveModYouTubeThumbnail(modId, youTubeId, t);
                    }
                };
            }

            // do the work
            this.RequestImage_Internal(Utility.GenerateYouTubeThumbnailURL(youTubeId),
                                       loadFromDisk, saveToDisk,
                                       onThumbnailReceived, onError);
        }

        /// <summary>Requests an image at a given URL.</summary>
        // NOTE(@jackson): This function *does not* check for data stored with CacheClient.
        public virtual void RequestImage(string url,
                                         Action<Texture2D> onSuccess,
                                         Action<WebRequestError> onError)
        {
            Debug.Assert(onSuccess != null);

            // do the work
            this.RequestImage_Internal(url, null, null, onSuccess, onError);
        }

        /// <summary>Handles computations for the image request.</summary>
        protected virtual void RequestImage_Internal(string url,
                                                     Func<Texture2D> loadFromDisk,
                                                     Action<Texture2D> saveToDisk,
                                                     Action<Texture2D> onSuccess,
                                                     Action<WebRequestError> onError)
        {
            Debug.Assert(onSuccess != null);

            // early out
            if(string.IsNullOrEmpty(url))
            {
                onSuccess(null);
                return;
            }

            // init vars
            Callbacks callbacks = null;

            // check cache
            Texture2D texture = null;
            if(this.cache.TryGetValue(url, out texture))
            {
                onSuccess(texture);
                return;
            }

            // check currently downloading
            if(this.m_callbackMap.TryGetValue(url, out callbacks))
            {
                // add callbacks
                callbacks.succeeded.Add(onSuccess);
                if(onError != null)
                {
                    callbacks.failed.Add(onError);
                }

                return;
            }

            // check disk
            if(loadFromDisk != null)
            {
                texture = loadFromDisk();
                if(texture != null)
                {
                    this.cache.Add(url, texture);
                    onSuccess(texture);
                    return;
                }
            }

            // create new callbacks entry
            callbacks = new Callbacks()
            {
                succeeded = new List<Action<Texture2D>>(),
                failed = new List<Action<WebRequestError>>(),
            };
            this.m_callbackMap.Add(url, callbacks);

            // add functions
            if(saveToDisk != null)
            {
                callbacks.succeeded.Add(saveToDisk);
            }
            callbacks.succeeded.Add(onSuccess);

            // start download
            this.DownloadImage(url);
        }

        /// <summary>Handles computations for the image request.</summary>
        protected virtual void RequestImage_Internal<E>(IMultiSizeImageLocator<E> locator, E size,
                                                        Func<Texture2D> loadFromDisk,
                                                        Action<Texture2D> saveToDisk,
                                                        Action<Texture2D> onSuccess,
                                                        Action<Texture2D> onFallback,
                                                        Action<WebRequestError> onError)
        {
            Debug.Assert(locator != null);
            Debug.Assert(onSuccess != null);

            // init vars
            string url = locator.GetSizeURL(size);
            Callbacks callbacks = null;

            // check for null URL
            if(string.IsNullOrEmpty(url))
            {
                #if UNITY_EDITOR
                if(this.logDownloads)
                {
                    Debug.Log("[mod.io] Attempted to fetch image with a Null or Empty"
                              + " url in the locator.");
                }
                #endif

                onSuccess(null);
                return;
            }

            // check cache
            Texture2D texture = null;
            if(this.cache.TryGetValue(url, out texture))
            {
                onSuccess(texture);
                return;
            }

            // check currently downloading
            if(this.m_callbackMap.TryGetValue(url, out callbacks))
            {
                // add callbacks
                callbacks.succeeded.Add(onSuccess);
                if(onError != null)
                {
                    callbacks.failed.Add(onError);
                }

                // check for fallback
                if(onFallback != null)
                {
                    Texture2D fallback = FindFallbackTexture(locator);
                    if(fallback != null)
                    {
                        onFallback(fallback);
                    }
                }

                return;
            }

            // check disk
            if(loadFromDisk != null)
            {
                texture = loadFromDisk();
                if(texture != null)
                {
                    this.cache.Add(url, texture);
                    onSuccess(texture);
                    return;
                }
            }

            // create new callbacks entry
            callbacks = new Callbacks()
            {
                succeeded = new List<Action<Texture2D>>(),
                failed = new List<Action<WebRequestError>>(),
            };
            this.m_callbackMap.Add(url, callbacks);

            // add functions
            if(saveToDisk != null)
            {
                callbacks.succeeded.Add(saveToDisk);
            }
            callbacks.succeeded.Add(onSuccess);

            // check for fallback
            if(onFallback != null)
            {
                Texture2D fallback = FindFallbackTexture(locator);
                if(fallback != null)
                {
                    onFallback(fallback);
                }
            }

            // start download
            this.DownloadImage(url);
        }

        /// <summary>Finds a fallback texture for the given locator.</summary>
        protected virtual Texture2D FindFallbackTexture<E>(IMultiSizeImageLocator<E> locator)
        {
            Debug.Assert(locator != null);

            Texture2D fallbackTexture = null;
            foreach(var pair in locator.GetAllURLs())
            {
                Texture2D cachedTexture = null;
                E originalSize = locator.GetOriginalSize();
                if(!pair.size.Equals(originalSize)
                   && this.cache.TryGetValue(pair.url, out cachedTexture))
                {
                    fallbackTexture = cachedTexture;
                }
            }

            return fallbackTexture;
        }

        /// <summary>Creates and sends an image download request for the given url.</summary>
        protected UnityWebRequestAsyncOperation DownloadImage(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            // create new download
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            // start download and attach callbacks
            var operation = webRequest.SendWebRequest();
            operation.completed += (o) =>
            {
                OnDownloadCompleted(operation.webRequest, url);
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
                Debug.Log("IMAGE DOWNLOAD STARTED"
                          + "\nURL: " + webRequest.url
                          + "\nTimeStamp: [" + timeStamp.ToString() + "] "
                          + ServerTimeStamp.ToLocalDateTime(timeStamp).ToString()
                          + "\nHeaders: " + requestHeaders);
            }
            #endif

            return operation;
        }

        /// <summary>Handles the completion of an image download.</summary>
        protected virtual void OnDownloadCompleted(UnityWebRequest webRequest, string imageURL)
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
                    string logString = ("IMAGE DOWNLOAD SUCCEEDED"
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
            Callbacks callbacks;
            bool isURLMapped = this.m_callbackMap.TryGetValue(imageURL, out callbacks);
            if(callbacks == null)
            {
                Debug.LogWarning("[mod.io] ImageRequestManager completed a download but the callbacks"
                                 + " entry for the download was null."
                                 + "\nImageURL = " + imageURL
                                 + "\nWebRequest.URL = " + webRequest.url
                                 + "\nm_callbackMap.TryGetValue() = " + isURLMapped.ToString()
                                 );
                return;
            }

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
                    this.cache[imageURL] = texture;
                }

                foreach(var successCallback in callbacks.succeeded)
                {
                    successCallback(texture);
                }
            }

            // remove from "in progress"
            this.m_callbackMap.Remove(imageURL);
        }

        // ---------[ EVENTS ]---------
        /// <summary>Stores any cached images when the mod subscriptions are updated.</summary>
        public void OnModSubscriptionsUpdated(IList<int> addedSubscriptions,
                                              IList<int> removedSubscriptions)
        {
            if(this.storeIfSubscribed
               && addedSubscriptions.Count > 0)
            {
                ModProfileRequestManager.instance.RequestModProfiles(addedSubscriptions,
                (modProfiles) =>
                {
                    if(this == null || !this.isActiveAndEnabled || modProfiles == null) { return; }

                    IList<int> subbedIds = ModManager.GetSubscribedModIds();

                    foreach(ModProfile profile in modProfiles)
                    {
                        if(profile != null && subbedIds.Contains(profile.id))
                        {
                            StoreModImages(profile);
                        }
                    }
                },
                null);
            }
        }

        // ---------[ UTILITY ]---------
        /// <summary>Finds any images relating to the profile and stores them using the CacheClient.</summary>
        protected virtual void StoreModImages(ModProfile profile)
        {
            Texture2D cachedTexture = null;

            // check for logo
            if(profile.logoLocator != null)
            {
                foreach(var sizeURLPair in profile.logoLocator.GetAllURLs())
                {
                    if(this.cache.TryGetValue(sizeURLPair.url, out cachedTexture))
                    {
                        CacheClient.SaveModLogo(profile.id, profile.logoLocator.GetFileName(),
                                                sizeURLPair.size, cachedTexture);
                    }
                }
            }

            // check for gallery images
            if(profile.media != null && profile.media.galleryImageLocators != null)
            {
                foreach(var locator in profile.media.galleryImageLocators)
                {
                    foreach(var sizeURLPair in locator.GetAllURLs())
                    {
                        if(this.cache.TryGetValue(sizeURLPair.url, out cachedTexture))
                        {
                            CacheClient.SaveModGalleryImage(profile.id, locator.GetFileName(),
                                                            sizeURLPair.size, cachedTexture);
                        }
                    }
                }
            }

            // check for YouTube thumbs
            if(profile.media != null && profile.media.youTubeURLs != null)
            {
                foreach(string videoURL in profile.media.youTubeURLs)
                {
                    string youTubeId = Utility.ExtractYouTubeIdFromURL(videoURL);
                    string thumbURL = Utility.GenerateYouTubeThumbnailURL(youTubeId);

                    if(this.cache.TryGetValue(thumbURL, out cachedTexture))
                    {
                        CacheClient.SaveModYouTubeThumbnail(profile.id, youTubeId, cachedTexture);
                    }
                }
            }
        }

        /// <summary>Get images from the cache that match the given URLS.</summary>
        protected Texture2D[] PullImagesFromCache(IList<string> urlList)
        {
            Debug.Assert(urlList != null);

            Texture2D[] result = new Texture2D[urlList.Count];

            for(int i = 0; i < result.Length; ++i)
            {
                Texture2D t = null;
                this.cache.TryGetValue(urlList[i], out t);

                result[i] = t;
            }

            return result;
        }

        // ---------[ OBSOLETE ]---------
        #pragma warning disable 0618
        [Obsolete("No longer supported.")]
        /// <summary>Requests the image for a given ImageDisplayData.</summary>
        public virtual void RequestImageForData(ImageDisplayData data, bool original,
                                                Action<Texture2D> onSuccess,
                                                Action<WebRequestError> onError)
        {
            string url = data.GetImageURL(original);

            // asserts
            Debug.Assert(onSuccess != null);
            Debug.Assert(!string.IsNullOrEmpty(url));

            // create delegates
            Func<Texture2D> loadFromDisk = null;
            Action<Texture2D> saveToDisk = null;
            switch(data.descriptor)
            {
                case ImageDescriptor.ModLogo:
                {
                    LogoSize size = (original ? LogoSize.Original : ImageDisplayData.logoThumbnailSize);

                    loadFromDisk = () => CacheClient.LoadModLogo(data.ownerId, data.imageId, size);

                    if(this.storeIfSubscribed)
                    {
                        saveToDisk = (t) =>
                        {
                            if(ModManager.GetSubscribedModIds().Contains(data.ownerId))
                            {
                                CacheClient.SaveModLogo(data.ownerId, data.imageId, size, t);
                            }
                        };
                    }
                }
                break;
                case ImageDescriptor.ModGalleryImage:
                {
                    ModGalleryImageSize size = (original
                                                ? ModGalleryImageSize.Original
                                                : ImageDisplayData.galleryThumbnailSize);

                    loadFromDisk = () => CacheClient.LoadModGalleryImage(data.ownerId, data.imageId, size);

                    if(this.storeIfSubscribed)
                    {
                        saveToDisk = (t) =>
                        {
                            if(ModManager.GetSubscribedModIds().Contains(data.ownerId))
                            {
                                CacheClient.SaveModGalleryImage(data.ownerId, data.imageId, size, t);
                            }
                        };
                    }
                }
                break;
                case ImageDescriptor.YouTubeThumbnail:
                {
                    loadFromDisk = () => CacheClient.LoadModYouTubeThumbnail(data.ownerId, data.imageId);

                    if(this.storeIfSubscribed)
                    {
                        saveToDisk = (t) =>
                        {
                            if(ModManager.GetSubscribedModIds().Contains(data.ownerId))
                            {
                                CacheClient.SaveModYouTubeThumbnail(data.ownerId, data.imageId, t);
                            }
                        };
                    }
                }
                break;
                case ImageDescriptor.UserAvatar:
                {
                    UserAvatarSize size = (original
                                           ? UserAvatarSize.Original
                                           : ImageDisplayData.avatarThumbnailSize);

                    loadFromDisk = () => CacheClient.LoadUserAvatar(data.ownerId, size);
                }
                break;
            }

            // request image
            this.RequestImage_Internal(url,
                                       loadFromDisk,
                                       saveToDisk,
                                       onSuccess,
                                       onError);
        }
        #pragma warning restore 0618
    }
}
