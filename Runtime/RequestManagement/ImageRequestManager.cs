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
            get {
                if(ImageRequestManager._instance == null)
                {
                    ImageRequestManager._instance =
                        UIUtilities.FindComponentInAllScenes<ImageRequestManager>(true);

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
            public Texture2D fallback = null;
            public List<Action<Texture2D>> succeeded = null;
            public List<Action<WebRequestError>> failed = null;
            public Action<Texture2D> onTextureDownloaded = null;
        }

        // ---------[ CONSTANTS ]---------
        /// <summary>The URL for the unauthenticated guest account avatar.</summary>
        public const string GUEST_AVATAR_URL = @":GUEST_AVATAR:";

        // ---------[ FIELDS ]---------
        /// <summary>Should the downloads made by this object be excluded from logging?</summary>
        [Tooltip("Should the downloads made by this object be excluded from logging?")]
        public bool excludeDownloadsFromLogs = true;

        /// <summary>Should the cache be cleared on disable.</summary>
        public bool clearCacheOnDisable = true;

        /// <summary>If enabled, stores retrieved images for subscribed mods.</summary>
        public bool storeIfSubscribed = true;

        /// <summary>Texture for the guest avatar.</summary>
        public Texture2D guestAvatar = null;

        // /// <summary>Cached images.</summary>
        // public Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>Callback map for images currently being fetched.</summary>
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

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Requests the image for a given locator.</summary>
        public virtual void RequestModLogo(int modId, LogoImageLocator locator, LogoSize size,
                                           Action<Texture2D> onLogoReceived,
                                           Action<Texture2D> onFallbackFound,
                                           Action<WebRequestError> onError)
        {
            // - early outs -
            if(onLogoReceived == null)
            {
                return;
            }
            if(locator == null)
            {
                if(onError != null)
                {
                    onError.Invoke(this.GenerateErrorForMissingLocator());
                }
                return;
            }

            string url = locator.GetSizeURL(size);
            string fileName = locator.GetFileName();

            // check url
            if(string.IsNullOrEmpty(url))
            {
                if(onError != null)
                {
                    onError.Invoke(this.GenerateErrorForMissingURL());
                }
                return;
            }

            // check cache and existing callbacks
            if(this.TryAddCallbacksToExisting(url, onLogoReceived, onFallbackFound, onError))
            {
                return;
            }

            // - Start new request -
            Callbacks callbacks = this.CreateCallbacksEntry(url, onLogoReceived, onError);

            // add save function to download callback
            if(this.storeIfSubscribed)
            {
                callbacks.onTextureDownloaded = (texture) =>
                {
                    if(LocalUser.SubscribedModIds.Contains(modId))
                    {
                        CacheClient.SaveModLogo(modId, fileName, size, texture, null);
                    }
                };
            }

            // start process by checking the cache
            CacheClient.LoadModLogo(modId, fileName, size, (texture) => {
                if(this == null)
                {
                    return;
                }

                if(texture != null)
                {
                    this.OnRequestSucceeded(url, texture);
                }
                else
                {
                    // do the download
                    this.DownloadImage(url);
                }
            });
        }

        /// <summary>Requests the image for a given locator.</summary>
        public virtual void RequestModGalleryImage(int modId, GalleryImageLocator locator,
                                                   ModGalleryImageSize size,
                                                   Action<Texture2D> onImageReceived,
                                                   Action<Texture2D> onFallbackFound,
                                                   Action<WebRequestError> onError)
        {
            // - early outs -
            if(onImageReceived == null)
            {
                return;
            }
            if(locator == null)
            {
                if(onError != null)
                {
                    onError.Invoke(this.GenerateErrorForMissingLocator());
                }
                return;
            }

            string url = locator.GetSizeURL(size);
            string fileName = locator.GetFileName();

            // check url
            if(string.IsNullOrEmpty(url))
            {
                if(onError != null)
                {
                    onError.Invoke(this.GenerateErrorForMissingURL());
                }
                return;
            }

            // check cache and existing callbacks
            if(this.TryAddCallbacksToExisting(url, onImageReceived, onFallbackFound, onError))
            {
                return;
            }

            // - Start new request -
            Callbacks callbacks = this.CreateCallbacksEntry(url, onImageReceived, onError);

            // add save function to download callback
            if(this.storeIfSubscribed)
            {
                callbacks.onTextureDownloaded = (texture) =>
                {
                    if(LocalUser.SubscribedModIds.Contains(modId))
                    {
                        CacheClient.SaveModGalleryImage(modId, fileName, size, texture, null);
                    }
                };
            }

            // start process by checking the cache
            CacheClient.LoadModGalleryImage(modId, fileName, size, (texture) => {
                if(this == null)
                {
                    return;
                }

                if(texture != null)
                {
                    this.OnRequestSucceeded(url, texture);
                }
                else
                {
                    // do the download
                    this.DownloadImage(url);
                }
            });
        }

        /// <summary>Requests the user avatar for the given locator.</summary>
        public virtual void RequestUserAvatar(int userId, AvatarImageLocator locator,
                                              UserAvatarSize size,
                                              Action<Texture2D> onAvatarReceived,
                                              Action<Texture2D> onFallbackFound,
                                              Action<WebRequestError> onError)
        {
            // - early outs -
            if(onAvatarReceived == null)
            {
                return;
            }
            if(locator == null)
            {
                if(onError != null)
                {
                    onError.Invoke(this.GenerateErrorForMissingLocator());
                }
                return;
            }

            string url = locator.GetSizeURL(size);

            // check url
            if(string.IsNullOrEmpty(url))
            {
                if(onError != null)
                {
                    onError.Invoke(this.GenerateErrorForMissingURL());
                }
                return;
            }

            if(url == ImageRequestManager.GUEST_AVATAR_URL)
            {
                if(onAvatarReceived != null)
                {
                    onAvatarReceived.Invoke(this.guestAvatar);
                }
                return;
            }

            // check cache and existing callbacks
            if(this.TryAddCallbacksToExisting(url, onAvatarReceived, onFallbackFound, onError))
            {
                return;
            }

            // - Start new request -
            Callbacks callbacks = this.CreateCallbacksEntry(url, onAvatarReceived, onError);

            // start process by checking the cache
            CacheClient.LoadUserAvatar(userId, size, (texture) => {
                if(this == null)
                {
                    return;
                }

                if(texture != null)
                {
                    this.OnRequestSucceeded(url, texture);
                }
                else
                {
                    // do the download
                    this.DownloadImage(url);
                }
            });
        }

        /// <summary>Requests the thumbnail for a given YouTube video.</summary>
        public virtual void RequestYouTubeThumbnail(int modId, string youTubeId,
                                                    Action<Texture2D> onThumbnailReceived,
                                                    Action<WebRequestError> onError)
        {
            // - early outs -
            if(onThumbnailReceived == null)
            {
                return;
            }

            string url = Utility.GenerateYouTubeThumbnailURL(youTubeId);

            // check url
            if(string.IsNullOrEmpty(url))
            {
                if(onError != null)
                {
                    onError.Invoke(this.GenerateErrorForMissingURL());
                }
                return;
            }

            // check cache and existing callbacks
            if(this.TryAddCallbacksToExisting(url, onThumbnailReceived, null, onError))
            {
                return;
            }

            // - Start new request -
            Callbacks callbacks = this.CreateCallbacksEntry(url, onThumbnailReceived, onError);

            // add save function to download callback
            if(this.storeIfSubscribed)
            {
                callbacks.onTextureDownloaded = (texture) =>
                {
                    if(LocalUser.SubscribedModIds.Contains(modId))
                    {
                        CacheClient.SaveModYouTubeThumbnail(modId, youTubeId, texture, null);
                    }
                };
            }

            // start process by checking the cache
            CacheClient.LoadModYouTubeThumbnail(modId, youTubeId, (texture) => {
                if(this == null)
                {
                    return;
                }

                if(texture != null)
                {
                    this.OnRequestSucceeded(url, texture);
                }
                else
                {
                    // do the download
                    this.DownloadImage(url);
                }
            });
        }

        /// <summary>Requests an image at a given URL.</summary>
        // NOTE(@jackson): This function *does not* check for data stored with CacheClient.
        public virtual void RequestImage(string url, Action<Texture2D> onSuccess,
                                         Action<WebRequestError> onError)
        {
            // - early outs -
            if(onSuccess == null)
            {
                return;
            }
            if(string.IsNullOrEmpty(url))
            {
                if(onError != null)
                {
                    onError.Invoke(this.GenerateErrorForMissingURL());
                }
                return;
            }

            // check cache and existing callbacks
            if(this.TryAddCallbacksToExisting(url, onSuccess, null, onError))
            {
                return;
            }

            // - Start new request -
            this.CreateCallbacksEntry(url, onSuccess, onError);

            // do the download
            this.DownloadImage(url);
        }

        /// <summary>Checks the cache and the callback map for a given url.</summary>
        protected virtual bool TryAddCallbacksToExisting(string url, Action<Texture2D> onSuccess,
                                                         Action<Texture2D> onFallbackFound,
                                                         Action<WebRequestError> onError)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(onSuccess != null);

            // check requests in progress
            Callbacks callbacks = null;
            if(this.m_callbackMap.TryGetValue(url, out callbacks))
            {
                callbacks.succeeded.Add(onSuccess);
                callbacks.failed.Add(onError);

                if(onFallbackFound != null && callbacks.fallback != null)
                {
                    onFallbackFound.Invoke(callbacks.fallback);
                }

                return true;
            }

            // not found
            return false;
        }

        /// <summary>Creates a new entry in the callbacks map for a given url.</summary>
        protected virtual Callbacks CreateCallbacksEntry(string url, Action<Texture2D> onSuccess,
                                                         Action<WebRequestError> onError)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(onSuccess != null);

            Callbacks callbacks = new Callbacks() {
                fallback = null,
                succeeded = new List<Action<Texture2D>>(),
                failed = new List<Action<WebRequestError>>(),
            };

            callbacks.succeeded.Add(onSuccess);

            if(onError != null)
            {
                callbacks.failed.Add(onError);
            }

            this.m_callbackMap[url] = callbacks;

            return callbacks;
        }

        /// <summary>Creates and sends an image download request for the given url.</summary>
        protected UnityWebRequestAsyncOperation DownloadImage(string url)
        {
            // String should have been checked by this point
            Debug.Assert(!string.IsNullOrEmpty(url));

            // create new download
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            // start download and attach callbacks
            var operation = webRequest.SendWebRequest();
            operation.completed += (o) =>
            { OnDownloadCompleted(operation.webRequest, url); };

#if DEBUG
            if(!this.excludeDownloadsFromLogs)
            {
                DebugUtilities.DebugDownload(operation, LocalUser.instance, null);
            }
#endif

            return operation;
        }

        /// <summary>Handles the completion of an image download.</summary>
        protected virtual void OnDownloadCompleted(UnityWebRequest webRequest, string imageURL)
        {
            // - early outs -
            if(this == null)
            {
                return;
            }
            if(webRequest == null)
            {
                this.OnRequestFailed(imageURL,
                                     WebRequestError.GenerateLocal("Error downloading image"));
                return;
            }

            // handle callbacks
            Callbacks callbacks;
            bool isURLMapped = this.m_callbackMap.TryGetValue(imageURL, out callbacks);
            if(callbacks == null)
            {
                Debug.LogWarning(
                    "[mod.io] ImageRequestManager completed a download but the callbacks"
                    + " entry for the download was null." + "\nImageURL = " + imageURL
                    + "\nWebRequest.URL = " + webRequest.url
                    + "\nm_callbackMap.TryGetValue() = " + isURLMapped.ToString());
                return;
            }

            if(webRequest.IsError() || !(webRequest.downloadHandler is DownloadHandlerTexture))
            {
                WebRequestError error = WebRequestError.GenerateFromWebRequest(webRequest);
                this.OnRequestFailed(imageURL, error);
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;

                if(callbacks.onTextureDownloaded != null)
                {
                    callbacks.onTextureDownloaded.Invoke(texture);
                }

                this.OnRequestSucceeded(imageURL, texture);
            }
        }

        /// <summary>Handles a failed image request.</summary>
        protected virtual void OnRequestFailed(string url, WebRequestError error)
        {
            if(this == null || string.IsNullOrEmpty(url))
            {
                return;
            }

            if(this.m_callbackMap.ContainsKey(url))
            {
                foreach(var errorCallback in this.m_callbackMap[url].failed)
                {
                    if(errorCallback != null)
                    {
                        errorCallback.Invoke(error);
                    }
                }

                // remove from "in progress"
                this.m_callbackMap.Remove(url);
            }
        }

        /// <summary>Handles a successful image request.</summary>
        protected virtual void OnRequestSucceeded(string url, Texture2D texture)
        {
            if(this == null)
            {
                return;
            }

            // if(this.isActiveAndEnabled || !this.clearCacheOnDisable)
            // {
            //     this.cache[url] = texture;
            // }

            if(this.m_callbackMap.ContainsKey(url))
            {
                foreach(var successCallback in this.m_callbackMap[url].succeeded)
                {
                    if(successCallback != null)
                    {
                        successCallback.Invoke(texture);
                    }
                }

                // remove from "in progress"
                this.m_callbackMap.Remove(url);
            }
        }

        // ---------[ EVENTS ]---------
        /// <summary>Stores any cached images when the mod subscriptions are updated.</summary>
        public void OnModSubscriptionsUpdated(IList<int> addedSubscriptions,
                                              IList<int> removedSubscriptions)
        {
            if(this.storeIfSubscribed && addedSubscriptions.Count > 0)
            {
                ModManager.GetModProfiles(addedSubscriptions, (ModProfile[] modProfiles) => {
                    if(this == null || !this.isActiveAndEnabled || modProfiles == null)
                    {
                        return;
                    }

                    IList<int> subbedIds = LocalUser.SubscribedModIds;

                    foreach(ModProfile profile in modProfiles)
                    {
                        if(profile != null && subbedIds.Contains(profile.id))
                        {
                            StoreModImages(profile);
                        }
                    }
                }, null);
            }
        }

        // ---------[ UTILITY ]---------
        /// <summary>Finds any images relating to the profile and stores them using the
        /// CacheClient.</summary>
        protected virtual void StoreModImages(ModProfile profile)
        {
            // Texture2D cachedTexture = null;

            // // check for logo
            // if(profile.logoLocator != null)
            // {
            //     foreach(var sizeURLPair in profile.logoLocator.GetAllURLs())
            //     {
            //         if(this.cache.TryGetValue(sizeURLPair.url, out cachedTexture))
            //         {
            //             CacheClient.SaveModLogo(profile.id, profile.logoLocator.GetFileName(),
            //                                     sizeURLPair.size, cachedTexture, null);
            //         }
            //     }
            // }

            // // check for gallery images
            // if(profile.media != null && profile.media.galleryImageLocators != null)
            // {
            //     foreach(var locator in profile.media.galleryImageLocators)
            //     {
            //         foreach(var sizeURLPair in locator.GetAllURLs())
            //         {
            //             if(this.cache.TryGetValue(sizeURLPair.url, out cachedTexture))
            //             {
            //                 CacheClient.SaveModGalleryImage(profile.id, locator.GetFileName(),
            //                                                 sizeURLPair.size, cachedTexture,
            //                                                 null);
            //             }
            //         }
            //     }
            // }

            // // check for YouTube thumbs
            // if(profile.media != null && profile.media.youTubeURLs != null)
            // {
            //     foreach(string videoURL in profile.media.youTubeURLs)
            //     {
            //         string youTubeId = Utility.ExtractYouTubeIdFromURL(videoURL);
            //         string thumbURL = Utility.GenerateYouTubeThumbnailURL(youTubeId);

            //         if(this.cache.TryGetValue(thumbURL, out cachedTexture))
            //         {
            //             CacheClient.SaveModYouTubeThumbnail(profile.id, youTubeId, cachedTexture,
            //                                                 null);
            //         }
            //     }
            // }
        }

        /// <summary>Get images from the cache that match the given URLS.</summary>
        protected Texture2D[] PullImagesFromCache(IList<string> urlList)
        {
            Debug.Assert(urlList != null);

            Texture2D[] result = new Texture2D[urlList.Count];

            // for(int i = 0; i < result.Length; ++i)
            // {
            //     Texture2D t = null;
            //     this.cache.TryGetValue(urlList[i], out t);

            //     result[i] = t;
            // }

            return result;
        }

        /// <summary>Creates a WebRequestError for a missing locator.</summary>
        protected WebRequestError GenerateErrorForMissingLocator()
        {
            var error = WebRequestError.GenerateLocal("Locator supplied was null.");
            error.displayMessage = "There was an error downloading this image. Try again later.";
            return error;
        }

        /// <summary>Creates a WebRequestError for a missing URL.</summary>
        protected WebRequestError GenerateErrorForMissingURL()
        {
            var error = WebRequestError.GenerateLocal("No valid URL exists in the locator.");
            error.displayMessage = "There was an error downloading this image. Try again later.";
            return error;
        }

#pragma warning disable 0618

        // ---------[ OBSOLETE ]---------

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
                    LogoSize size =
                        (original ? LogoSize.Original : ImageDisplayData.logoThumbnailSize);

                    loadFromDisk = () => CacheClient.LoadModLogo(data.ownerId, data.imageId, size);

                    if(this.storeIfSubscribed)
                    {
                        saveToDisk = (t) =>
                        {
                            if(LocalUser.SubscribedModIds.Contains(data.ownerId))
                            {
                                CacheClient.SaveModLogo(data.ownerId, data.imageId, size, t);
                            }
                        };
                    }
                }
                break;
                case ImageDescriptor.ModGalleryImage:
                {
                    ModGalleryImageSize size = (original ? ModGalleryImageSize.Original
                                                         : ImageDisplayData.galleryThumbnailSize);

                    loadFromDisk = () =>
                        CacheClient.LoadModGalleryImage(data.ownerId, data.imageId, size);

                    if(this.storeIfSubscribed)
                    {
                        saveToDisk = (t) =>
                        {
                            if(LocalUser.SubscribedModIds.Contains(data.ownerId))
                            {
                                CacheClient.SaveModGalleryImage(data.ownerId, data.imageId, size,
                                                                t);
                            }
                        };
                    }
                }
                break;
                case ImageDescriptor.YouTubeThumbnail:
                {
                    loadFromDisk = () =>
                        CacheClient.LoadModYouTubeThumbnail(data.ownerId, data.imageId);

                    if(this.storeIfSubscribed)
                    {
                        saveToDisk = (t) =>
                        {
                            if(LocalUser.SubscribedModIds.Contains(data.ownerId))
                            {
                                CacheClient.SaveModYouTubeThumbnail(data.ownerId, data.imageId, t);
                            }
                        };
                    }
                }
                break;
                case ImageDescriptor.UserAvatar:
                {
                    UserAvatarSize size =
                        (original ? UserAvatarSize.Original : ImageDisplayData.avatarThumbnailSize);

                    loadFromDisk = () => CacheClient.LoadUserAvatar(data.ownerId, size);
                }
                break;
            }

            // request image
            this.RequestImage_Internal(url, loadFromDisk, saveToDisk, onSuccess, onError);
        }
#pragma warning restore 0618

        /// <summary>Should requests made by the ImageRequestManager be logged.</summary>
        [Obsolete("Use ImageRequestManager.excludeDownloadsFromLogs instead")]
        public bool logDownloads
        {
            get {
                return !this.excludeDownloadsFromLogs;
            }
            set {
                this.excludeDownloadsFromLogs = !value;
            }
        }

        /// <summary>[Obsolete] Handles computations for the image request.</summary>
        [Obsolete("No longer supported.")] protected virtual void RequestImage_Internal(
            string url, Func<Texture2D> loadFromDisk, Action<Texture2D> saveToDisk,
            Action<Texture2D> onSuccess, Action<WebRequestError> onError)
        {
            // check cache and existing callbacks
            if(this.TryAddCallbacksToExisting(url, onSuccess, null, onError))
            {
                return;
            }

            // - Start new request -
            Callbacks callbacks = this.CreateCallbacksEntry(url, onSuccess, onError);

            // add save function to download callback
            callbacks.onTextureDownloaded = saveToDisk;

            // start process by checking the cache
            Texture2D texture = loadFromDisk();
            if(texture != null)
            {
                this.OnRequestSucceeded(url, texture);
            }
            else
            {
                // do the download
                this.DownloadImage(url);
            }
        }

        /// <summary>[Obsolete] Handles computations for the image request.</summary>
        [Obsolete("No longer supported.")]
        protected virtual void RequestImage_Internal<E>(IMultiSizeImageLocator<E> locator, E size,
                                                        Func<Texture2D> loadFromDisk,
                                                        Action<Texture2D> saveToDisk,
                                                        Action<Texture2D> onSuccess,
                                                        Action<Texture2D> onFallback,
                                                        Action<WebRequestError> onError)
        {
            Debug.Assert(locator != null);

            string url = locator.GetSizeURL(size);

            // check cache and existing callbacks
            if(this.TryAddCallbacksToExisting(url, onSuccess, onFallback, onError))
            {
                return;
            }

            // - Start new request -
            Callbacks callbacks = this.CreateCallbacksEntry(url, onSuccess, onError);

            // add save function to download callback
            callbacks.onTextureDownloaded = saveToDisk;

            // start process by checking the cache
            Texture2D texture = loadFromDisk();
            if(texture != null)
            {
                this.OnRequestSucceeded(url, texture);
            }
            else
            {
                // do the download
                this.DownloadImage(url);
            }
        }
    }
}
