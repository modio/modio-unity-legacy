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
        private class Callbacks
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
            Func<Texture2D> retrieveFromDisk = null;
            Action<Texture2D> storeToDisk = null;
            switch(data.descriptor)
            {
                case ImageDescriptor.ModLogo:
                {
                    LogoSize size = (original ? LogoSize.Original : ImageDisplayData.logoThumbnailSize);

                    retrieveFromDisk = () => CacheClient.LoadModLogo(data.ownerId, data.imageId, size);

                    if(this.storeIfSubscribed)
                    {
                        storeToDisk = (t) =>
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

                    retrieveFromDisk = () => CacheClient.LoadModGalleryImage(data.ownerId, data.imageId, size);

                    if(this.storeIfSubscribed)
                    {
                        storeToDisk = (t) =>
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
                    retrieveFromDisk = () => CacheClient.LoadModYouTubeThumbnail(data.ownerId, data.imageId);

                    if(this.storeIfSubscribed)
                    {
                        storeToDisk = (t) =>
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

                    retrieveFromDisk = () => CacheClient.LoadUserAvatar(data.ownerId, size);
                }
                break;
            }

            this.RequestImage_Internal(url, retrieveFromDisk, storeToDisk, onSuccess, onError);
        }

        /// <summary>Requests an image at a given URL.</summary>
        // NOTE(@jackson): This function *does not* check for data stored with CacheClient.
        public virtual void RequestImage(string url,
                                         Action<Texture2D> onSuccess,
                                         Action<WebRequestError> onError)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(onSuccess != null);

            this.RequestImage_Internal(url, null, null, onSuccess, onError);
        }

        /// <summary>Handles the computations for the image request.</summary>
        protected virtual void RequestImage_Internal(string url,
                                                     Func<Texture2D> retrieveFromDisk,
                                                     Action<Texture2D> storeToDisk,
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
                    this.cache.Add(url, texture);

                    onSuccess(texture);
                    return;
                }
            }

            // create new callbacks entry
            callbacks = new Callbacks();
            callbacks.succeeded = new List<Action<Texture2D>>();
            callbacks.succeeded.Add(onSuccess);
            callbacks.failed = new List<Action<WebRequestError>>();
            if(onError != null)
            {
                callbacks.failed.Add(onError);
            }

            // check if storing on disk
            if(storeToDisk != null)
            {
                callbacks.succeeded.Add(storeToDisk);
            }

            // add to map
            this.m_callbackMap.Add(url, callbacks);

            // download
            this.DownloadImage(url);
        }

        /// <summary>Creates and sends an image download request for the given url.</summary>
        protected UnityWebRequestAsyncOperation DownloadImage(string url)
        {
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
    }
}
