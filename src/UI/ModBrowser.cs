using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Directory = System.IO.Directory;

// TODO(@jackson): Use ModManager.USERID_GUEST
// TODO(@jackson): Error handling on log in
namespace ModIO.UI
{
    public class ModBrowser : MonoBehaviour
    {
        // ---------[ SINGLETON ]---------
        private static ModBrowser _instance = null;
        public static ModBrowser instance
        {
            get
            {
                if(ModBrowser._instance == null)
                {
                    // Instantiate
                }

                return ModBrowser._instance;
            }
        }

        // ---------[ NESTED CLASSES ]---------
        public enum ServerType
        {
            TestServer,
            ProductionServer,
            CustomServer,
        }

        [Serializable]
        private class ManifestData
        {
            public int lastCacheUpdate;
            public int lastSubscriptionSync;
            public List<int> queuedUnsubscribes;
            public List<int> queuedSubscribes;
        }

        // ---------[ CONST & STATIC ]---------
        /// <summary>File name used to store the browser manifest.</summary>
        public const string MANIFEST_FILENAME = "browser_manifest.data";

        /// <summary>Number of seconds between update polls.</summary>
        private const float AUTOMATIC_UPDATE_INTERVAL = 15f;

        // ---------[ FIELDS ]---------
        [Tooltip("Debug All API Requests")]
        public bool debugAllAPIRequests = false;

        [Tooltip("Size to use for the user avatar thumbnails")]
        public UserAvatarSize avatarThumbnailSize = UserAvatarSize.Thumbnail_50x50;
        [Tooltip("Size to use for the mod logo thumbnails")]
        public LogoSize logoThumbnailSize = LogoSize.Thumbnail_320x180;
        [Tooltip("Size to use for the mod gallery image thumbnails")]
        public ModGalleryImageSize galleryThumbnailSize = ModGalleryImageSize.Thumbnail_320x180;

        [SerializeField] private UserDisplayData m_guestData = new UserDisplayData()
        {
            profile = new UserProfileDisplayData()
            {
                userId = UserProfile.NULL_ID,
                username = "Guest",
            },
            avatar = new ImageDisplayData()
            {
                userId = UserProfile.NULL_ID,
                imageId = "guest_avatar",
                mediaType = ImageDisplayData.MediaType.UserAvatar,
                originalTexture = null,
                thumbnailTexture = null,
            },
        };

        [Header("UI Components")]
        public ExplorerView explorerView;
        public SubscriptionsView subscriptionsView;
        public InspectorView inspectorView;
        public UserView loggedUserView;
        public LoginDialog loginDialog;

        // --- RUNTIME DATA ---
        private GameProfile m_gameProfile = null;
        private UserProfile m_userProfile = null;
        private List<SimpleRating> m_userRatings = new List<SimpleRating>();
        private int lastSubscriptionSync = -1;
        private int lastCacheUpdate = -1;
        private Coroutine m_updatesCoroutine = null;
        private List<int> m_queuedUnsubscribes = new List<int>();
        private List<int> m_queuedSubscribes = new List<int>();
        private bool m_validOAuthToken = false;

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            if(ModBrowser._instance == null)
            {
                ModBrowser._instance = this;
            }
            #if DEBUG
            else
            {
                Debug.LogWarning("[mod.io] Second instance of a ModBrowser"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a ModBrowser"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif

            this.m_validOAuthToken = false;
            this.StartCoroutine(StartFetchRemoteData());
        }

        private void OnDisable()
        {
            if(m_updatesCoroutine != null)
            {
                StopCoroutine(m_updatesCoroutine);
            }

            if(m_userProfile != null
               && this.m_validOAuthToken)
            {
                PushSubscriptionChanges();
            }

            if(ModBrowser._instance == this)
            {
                ModBrowser._instance = null;
            }
        }

        private void Start()
        {
            #if DEBUG
            PluginSettings.Data settings = PluginSettings.data;

            if(settings.gameId == GameProfile.NULL_ID)
            {
                Debug.LogError("[mod.io] Game ID is missing from the Plugin Settings.\n"
                               + "This must be configured by selecting the mod.io > Edit Settings menu"
                               + " item, or by clicking \'Plugin Settings' on this Mod Browser object,"
                               + " before the mod.io Unity Plugin can be used.",
                               this);

                this.gameObject.SetActive(false);
                return;
            }
            if(String.IsNullOrEmpty(settings.gameAPIKey))
            {
                Debug.LogError("[mod.io] Game API Key is missing from the Plugin Settings.\n"
                               + "This must be configured by selecting the mod.io > Edit Settings menu"
                               + " item, or by clicking \'Plugin Settings' on this Mod Browser object,"
                               + " before the mod.io Unity Plugin can be used.",
                               this);

                this.gameObject.SetActive(false);
                return;
            }
            if(String.IsNullOrEmpty(settings.apiURL))
            {
                Debug.LogError("[mod.io] API URL is missing from the Plugin Settings.\n"
                               + "This must be configured by selecting the mod.io > Edit Settings menu"
                               + " item, or by clicking \'Plugin Settings' on this Mod Browser object,"
                               + " before the mod.io Unity Plugin can be used.",
                               this);

                this.gameObject.SetActive(false);
                return;
            }
            if(String.IsNullOrEmpty(settings.cacheDirectory))
            {
                Debug.LogError("[mod.io] Cache Directory is missing from the Plugin Settings.\n"
                               + "This must be configured by selecting the mod.io > Edit Settings menu"
                               + " item, or by clicking \'Plugin Settings' on this Mod Browser object,"
                               + " before the mod.io Unity Plugin can be used.",
                               this);

                this.gameObject.SetActive(false);
                return;
            }
            if(String.IsNullOrEmpty(settings.installationDirectory))
            {
                Debug.LogError("[mod.io] Mod Installation Directory is missing from the Plugin Settings.\n"
                               + "This must be configured by selecting the mod.io > Edit Settings menu"
                               + " item, or by clicking \'Plugin Settings' on this Mod Browser object,"
                               + " before the mod.io Unity Plugin can be used.",
                               this);

                this.gameObject.SetActive(false);
                return;
            }
            #endif

            LoadLocalData();

            InitializeDialogs();
            InitializeDisplays();
        }

        private void LoadLocalData()
        {
            // - UserData -
            if(UserAuthenticationData.instance.userId != UserProfile.NULL_ID)
            {
                m_userProfile = CacheClient.LoadUserProfile(UserAuthenticationData.instance.userId);
            }

            // - GameData -
            m_gameProfile = CacheClient.LoadGameProfile();
            if(m_gameProfile == null)
            {
                m_gameProfile = new GameProfile();
                m_gameProfile.id = PluginSettings.data.gameId;
            }

            // - Manifest -
            string manifestFilePath = IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                                              ModBrowser.MANIFEST_FILENAME);
            ManifestData manifest = IOUtilities.ReadJsonObjectFile<ManifestData>(manifestFilePath);
            if(manifest != null)
            {
                this.lastCacheUpdate = manifest.lastCacheUpdate;
                this.lastSubscriptionSync = manifest.lastSubscriptionSync;
                this.m_queuedSubscribes = manifest.queuedSubscribes;
                this.m_queuedUnsubscribes = manifest.queuedUnsubscribes;
            }
            else
            {
                this.lastCacheUpdate = 0;
                this.lastSubscriptionSync = 0;
                this.m_queuedSubscribes = new List<int>();
                this.m_queuedUnsubscribes = new List<int>();
                WriteManifest();
            }

            // - Log Requests -
            APIClient.logAllRequests = debugAllAPIRequests;
            DownloadClient.logAllRequests = debugAllAPIRequests;

            // - Image settings -
            ImageDisplayData.avatarThumbnailSize = this.avatarThumbnailSize;
            ImageDisplayData.logoThumbnailSize = this.logoThumbnailSize;
            ImageDisplayData.galleryThumbnailSize = this.galleryThumbnailSize;
        }

        private void InitializeDialogs()
        {
            loginDialog.gameObject.SetActive(false);
            loginDialog.onSecurityCodeSent += (m) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Success,
                                           m.message);
            };
            loginDialog.onUserOAuthTokenReceived += (t) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Success,
                                           "Authorization Successful");
                CloseLoginDialog();
                LogUserIn(t);
            };
            loginDialog.onWebRequestError += (e) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                           e.displayMessage);
            };
            loginDialog.onInvalidSubmissionAttempted += (m) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                           m);
            };
            loginDialog.onEmailRefused += (m) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                           m);
            };
            loginDialog.onSecurityCodeRefused += (m) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                           m);
            };
        }

        private void InitializeDisplays()
        {
            if(loggedUserView != null)
            {
                loggedUserView.Initialize();

                if(m_userProfile == null)
                {
                    loggedUserView.data = m_guestData;
                }
                else
                {
                    loggedUserView.DisplayUser(m_userProfile);
                }

                loggedUserView.onClick += OnUserDisplayClicked;
            }
        }

        private System.Collections.IEnumerator StartFetchRemoteData()
        {
            // Ensure Start() has been finished
            yield return null;

            if(!this.isActiveAndEnabled)
            {
                yield break;
            }

            this.StartCoroutine(FetchGameProfile());

            if(UserAuthenticationData.instance.Equals(UserAuthenticationData.NONE))
            {
                yield return this.StartCoroutine(FetchAllSubscribedModProfiles());
            }
            else
            {
                yield return this.StartCoroutine(FetchUserProfile());

                // NOTE(@jackson): There is the potential that the UserProfile request fails
                if(m_validOAuthToken)
                {
                    yield return this.StartCoroutine(SynchronizeSubscriptionsWithServer());
                }
            }

            VerifySubscriptionInstallations();

            m_updatesCoroutine = this.StartCoroutine(PollForUpdatesCoroutine());
        }

        private System.Collections.IEnumerator FetchGameProfile()
        {
            bool succeeded = false;

            while(!succeeded)
            {
                bool isRequestDone = false;
                WebRequestError requestError = null;

                // --- GameProfile ---
                APIClient.GetGame(
                (g) =>
                {
                    m_gameProfile = g;
                    if(explorerView != null)
                    {
                        explorerView.OnGameProfileUpdated(g);
                    }
                    if(subscriptionsView != null)
                    {
                        subscriptionsView.OnGameProfileUpdated(g);
                    }
                    if(inspectorView != null)
                    {
                        inspectorView.OnGameProfileUpdated(g);
                    }

                    succeeded = true;
                    isRequestDone = true;
                },
                (e) =>
                {
                    requestError = e;
                    isRequestDone = true;
                });

                while(!isRequestDone) { yield return null; }

                if(requestError != null)
                {
                    int reattemptDelay = CalculateReattemptDelay(requestError);
                    if(requestError.isAuthenticationInvalid)
                    {
                        if(String.IsNullOrEmpty(UserAuthenticationData.instance.token))
                        {
                            Debug.LogWarning("[mod.io] Unable to retrieve the game profile from the mod.io"
                                             + " servers. Please check you Game Id and APIKey in the"
                                             + " PluginSettings. [Resources/modio_settings]");

                            MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                       "Failed to collect game data from mod.io.\n"
                                                       + requestError.displayMessage);
                        }
                        else
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                       requestError.displayMessage);

                            m_validOAuthToken = false;
                        }

                        yield break;
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        Debug.LogWarning("[mod.io] Fetching Game Profile failed.\n"
                                         + requestError.ToUnityDebugString());

                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to collect game data from mod.io.\n"
                                                   + requestError.displayMessage);
                        yield break;
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to collect game data from mod.io.\n"
                                                   + requestError.displayMessage
                                                   + "\nRetrying in "
                                                   + reattemptDelay.ToString()
                                                   + " seconds");

                        yield return new WaitForSeconds(reattemptDelay);
                        continue;
                    }
                }
            }
        }

        private System.Collections.IEnumerator FetchUserProfile()
        {
            Debug.Assert(!String.IsNullOrEmpty(UserAuthenticationData.instance.token));

            bool succeeded = false;

            // get user profile
            while(!succeeded)
            {
                bool isRequestDone = false;
                WebRequestError requestError = null;

                // requests
                APIClient.GetAuthenticatedUser(
                (u) =>
                {
                    CacheClient.SaveUserProfile(u);

                    m_userProfile = u;

                    if(this.loggedUserView != null)
                    {
                        this.loggedUserView.DisplayUser(u);
                    }

                    succeeded = true;
                    isRequestDone = true;
                },
                (e) =>
                {
                    requestError = e;
                    isRequestDone = true;
                });

                while(!isRequestDone) { yield return null; }

                if(requestError != null)
                {
                    int reattemptDelay = CalculateReattemptDelay(requestError);
                    if(requestError.isAuthenticationInvalid)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                   requestError.displayMessage);

                        m_validOAuthToken = false;
                        yield break;
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        Debug.LogWarning("[mod.io] Fetching User Profile failed."
                                         + requestError.ToUnityDebugString());

                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to collect user profile data from mod.io.\n"
                                                   + requestError.displayMessage);

                        m_validOAuthToken = false;
                        yield break;
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to collect user profile data from mod.io.\n"
                                                   + requestError.displayMessage
                                                   + "\nRetrying in "
                                                   + reattemptDelay.ToString()
                                                   + " seconds");

                        yield return new WaitForSeconds(reattemptDelay);
                        continue;
                    }
                }
            }

            this.m_validOAuthToken = succeeded;

            StartCoroutine(FetchUserRatings());
        }

        private System.Collections.IEnumerator SynchronizeSubscriptionsWithServer()
        {
            if(!m_validOAuthToken) { yield break; }

            int updateStartTimeStamp = ServerTimeStamp.Now;
            int updateCount = 0;

            // set up initial vars
            bool allPagesReceived = false;

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            RequestFilter subscriptionFilter = new RequestFilter();
            subscriptionFilter.fieldFilters.Add(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                                new EqualToFilter<int>() { filterValue = m_gameProfile.id });

            List<int> localSubscriptions = new List<int>(ModManager.GetSubscribedModIds());

            // loop until done or broken
            while(m_validOAuthToken
                  && !allPagesReceived)
            {
                bool isRequestDone = false;
                WebRequestError requestError = null;
                RequestPage<ModProfile> requestPage = null;

                APIClient.GetUserSubscriptions(subscriptionFilter, pagination,
                                               (r) => { isRequestDone = true; requestPage = r; },
                                               (e) => { isRequestDone = true; requestError = e; });

                while(!isRequestDone) { yield return null; }

                if(requestError != null)
                {
                    int reattemptDelay = CalculateReattemptDelay(requestError);
                    if(requestError.isAuthenticationInvalid)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                   requestError.displayMessage);

                        m_validOAuthToken = false;
                        yield break;
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to synchronize subscriptions with mod.io servers.\n"
                                                   + requestError.displayMessage);
                        yield break;
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to synchronize subscriptions with mod.io servers.\n"
                                                   + requestError.displayMessage
                                                   + "\nRetrying in "
                                                   + reattemptDelay.ToString()
                                                   + " seconds");

                        yield return new WaitForSeconds(reattemptDelay);
                        continue;
                    }
                }
                else
                {
                    // check which profiles are new
                    List<int> newSubs = new List<int>();
                    foreach(ModProfile profile in requestPage.items)
                    {
                        if(!localSubscriptions.Contains(profile.id)
                           && !m_queuedUnsubscribes.Contains(profile.id)
                           && !m_queuedSubscribes.Contains(profile.id))
                        {
                            newSubs.Add(profile.id);
                        }

                        localSubscriptions.Remove(profile.id);
                    }

                    // add new subs
                    CacheClient.SaveModProfiles(requestPage.items);
                    if(newSubs.Count > 0)
                    {
                        var subscribedModIds = ModManager.GetSubscribedModIds();
                        subscribedModIds.AddRange(newSubs);
                        ModManager.SetSubscribedModIds(subscribedModIds);
                        OnSubscriptionsChanged(newSubs, null);
                    }

                    updateCount += newSubs.Count;

                    // check pages
                    allPagesReceived = (requestPage.items.Length < requestPage.size);
                    if(!allPagesReceived)
                    {
                        pagination.offset += pagination.limit;
                    }
                }
            }

            // handle removed ids
            if(allPagesReceived)
            {
                if(localSubscriptions.Count > 0)
                {
                    var remoteUnsubs = new List<int>(localSubscriptions);
                    var subscribedModIds = ModManager.GetSubscribedModIds();
                    foreach(int modId in localSubscriptions)
                    {
                        if(m_queuedSubscribes.Contains(modId))
                        {
                            remoteUnsubs.Remove(modId);
                        }
                        else
                        {
                            subscribedModIds.Remove(modId);
                        }
                    }
                    ModManager.SetSubscribedModIds(subscribedModIds);
                    OnSubscriptionsChanged(null, remoteUnsubs);

                    updateCount += localSubscriptions.Count;
                }

                this.lastSubscriptionSync = updateStartTimeStamp;
                this.lastCacheUpdate = updateStartTimeStamp;
            }

            if(updateCount > 0)
            {
                string message = updateCount.ToString() + " subscription(s) synchronized with the server";
                MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
            }

            PushSubscriptionChanges();
        }

        private System.Collections.IEnumerator FetchAllSubscribedModProfiles()
        {
            int updateStartTimeStamp = ServerTimeStamp.Now;
            List<int> subscribedModIds = ModManager.GetSubscribedModIds();

            // early out
            if(subscribedModIds.Count == 0)
            {
                this.lastSubscriptionSync = updateStartTimeStamp;
                this.lastCacheUpdate = updateStartTimeStamp;
                yield break;
            }

            // set up initial vars
            bool allPagesReceived = false;

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            RequestFilter modFilter = new RequestFilter();
            modFilter.fieldFilters[ModIO.API.GetAllModsFilterFields.id]
            = new InArrayFilter<int>()
            {
                filterArray = subscribedModIds.ToArray()
            };

            // loop until done or broken
            while(!allPagesReceived)
            {
                bool isRequestDone = false;
                WebRequestError requestError = null;
                RequestPage<ModProfile> requestPage = null;

                APIClient.GetAllMods(modFilter, pagination,
                                     (r) => { isRequestDone = true; requestPage = r; },
                                     (e) => { isRequestDone = true; requestError = e; });

                while(!isRequestDone) { yield return null; }

                if(requestError != null)
                {
                    int reattemptDelay = CalculateReattemptDelay(requestError);
                    if(requestError.isAuthenticationInvalid)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                   requestError.displayMessage);

                        m_validOAuthToken = false;
                        yield break;
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to retrieve subscription data from mod.io servers.\n"
                                                   + requestError.displayMessage);
                        yield break;
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to retrieve subscription data from mod.io servers.\n"
                                                   + requestError.displayMessage
                                                   + "\nRetrying in "
                                                   + reattemptDelay.ToString()
                                                   + " seconds");

                        yield return new WaitForSeconds(reattemptDelay);
                        continue;
                    }
                }
                else
                {
                    // add new subs
                    CacheClient.SaveModProfiles(requestPage.items);

                    // remove from the list (for the purpose of determining missing/deleted mods)
                    foreach(ModProfile profile in requestPage.items)
                    {
                        while(subscribedModIds.Remove(profile.id)) {}
                    }

                    // check pages
                    allPagesReceived = (requestPage.items.Length < requestPage.size);
                    if(!allPagesReceived)
                    {
                        pagination.offset += pagination.limit;
                    }
                }
            }

            // handle removed ids
            if(allPagesReceived)
            {
                if(subscribedModIds.Count > 0)
                {
                    List<int> removedModIds = subscribedModIds;

                    subscribedModIds = ModManager.GetSubscribedModIds();
                    foreach(int modId in removedModIds)
                    {
                        subscribedModIds.Remove(modId);
                    }
                    ModManager.SetSubscribedModIds(subscribedModIds);

                    OnSubscriptionsChanged(null, removedModIds);

                    string message = (removedModIds.Count.ToString() + " subscribed mods have become"
                                      + " unavailable and have been removed from your subscriptions.");
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
                }

                this.lastSubscriptionSync = updateStartTimeStamp;
                this.lastCacheUpdate = updateStartTimeStamp;
            }
        }

        private System.Collections.IEnumerator FetchUserRatings()
        {
            if(!m_validOAuthToken) { yield break; }

            APIPaginationParameters pagination = new APIPaginationParameters();
            RequestFilter filter = new RequestFilter();
            filter.fieldFilters[API.GetUserRatingsFilterFields.gameId]
                = new EqualToFilter<int>() { filterValue = m_gameProfile.id };

            bool isRequestDone = false;
            List<ModRating> retrievedRatings = new List<ModRating>();

            while(m_validOAuthToken
                  && !isRequestDone)
            {
                RequestPage<ModRating> response = null;
                WebRequestError requestError = null;

                APIClient.GetUserRatings(filter, pagination,
                                         (r) =>
                                         {
                                            response = r;
                                            isRequestDone = true;
                                         },
                                         (e) =>
                                         {
                                            requestError = e;
                                            isRequestDone = true;
                                         });

                while(!isRequestDone) { yield return null; }

                if(requestError != null)
                {
                    int reattemptDelay = CalculateReattemptDelay(requestError);
                    if(requestError.isAuthenticationInvalid)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                   requestError.displayMessage);

                        m_validOAuthToken = false;
                        yield break;
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        yield break;
                    }
                    else
                    {
                        yield return new WaitForSeconds(reattemptDelay);
                        continue;
                    }
                }
                else
                {
                    retrievedRatings.AddRange(response.items);

                    isRequestDone = (response.size + response.resultOffset >= response.resultTotal);
                }
            }

            m_userRatings = new List<SimpleRating>(retrievedRatings.Count);
            foreach(ModRating rating in retrievedRatings)
            {
                m_userRatings.Add(new SimpleRating()
                {
                    modId = rating.modId,
                    isPositive = (rating.ratingValue == ModRating.POSITIVE_VALUE),
                });
            }
        }

        private void VerifySubscriptionInstallations()
        {
            var subscribedModIds = ModManager.GetSubscribedModIds();
            IList<ModfileIdPair> installedModVersions = ModManager.GetInstalledModVersions(false);
            Dictionary<int, List<int>> groupedIds = new Dictionary<int, List<int>>();

            // remove unsubbed mods
            foreach(ModfileIdPair idPair in installedModVersions)
            {
                if(!subscribedModIds.Contains(idPair.modId))
                {
                    ModManager.TryUninstallAllModVersions(idPair.modId);
                }
                else
                {
                    List<int> modfileIds = null;
                    if(!groupedIds.TryGetValue(idPair.modId, out modfileIds))
                    {
                        modfileIds = new List<int>();
                        groupedIds.Add(idPair.modId, modfileIds);
                    }

                    modfileIds.Add(idPair.modfileId);
                }
            }

            // assert subbed mod installs
            foreach(int modId in subscribedModIds)
            {
                ModProfile profile = CacheClient.LoadModProfile(modId);

                if(profile == null)
                {
                    ModManager.GetModProfile(modId, (p) => AssertInstalledLatest(p, groupedIds[modId]), null);
                }
                else
                {
                    AssertInstalledLatest(profile, groupedIds[modId]);
                }
            }
        }

        private void AssertInstalledLatest(ModProfile profile, List<int> installedIds)
        {
            bool isInstalled = false;
            List<int> wrongVersions = new List<int>();
            foreach(int modfileId in installedIds)
            {
                if(modfileId == profile.currentBuild.id)
                {
                    isInstalled = true;
                }
                else if(!wrongVersions.Contains(modfileId))
                {
                    wrongVersions.Add(modfileId);
                }
            }

            if(!isInstalled)
            {
                this.StartCoroutine(DownloadAndInstallModVersion(profile.id, profile.currentBuild.id));
            }
            // isInstalled &&
            else if(wrongVersions.Count > 0)
            {
                foreach(int modfileId in wrongVersions)
                {
                    ModManager.TryUninstallModVersion(profile.id, modfileId);
                }
            }
        }

        // ----------[ MANIFEST ]---------
        protected void WriteManifest()
        {
            ManifestData manifest = new ManifestData()
            {
                lastCacheUpdate = this.lastCacheUpdate,
                lastSubscriptionSync = this.lastSubscriptionSync,
                queuedUnsubscribes = this.m_queuedUnsubscribes,
                queuedSubscribes = this.m_queuedSubscribes,
            };

            string manifestFilePath = IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                                              ModBrowser.MANIFEST_FILENAME);
            IOUtilities.WriteJsonObjectFile(manifestFilePath, manifest);
        }

        // ---------[ REQUESTS ]---------
        private int CalculateReattemptDelay(WebRequestError requestError)
        {
            Debug.Assert(requestError != null);

            if(requestError.limitedUntilTimeStamp > 0)
            {
                return (requestError.limitedUntilTimeStamp - ServerTimeStamp.Now);
            }
            else if(!requestError.isRequestUnresolvable)
            {
                if(requestError.isServerUnreachable
                   && requestError.webRequest.responseCode > 0)
                {
                    return 60;
                }
                else
                {
                    return 15;
                }
            }
            else
            {
                return -1;
            }
        }

        // ---------[ UPDATES ]---------
        private System.Collections.IEnumerator PollForUpdatesCoroutine()
        {
            bool cancelUpdates = false;

            while(!cancelUpdates)
            {
                int updateStartTimeStamp = ServerTimeStamp.Now;

                bool isRequestDone = false;
                WebRequestError requestError = null;

                // --- USER EVENTS ---
                if(this.m_validOAuthToken)
                {
                    // fetch user events
                    List<UserEvent> userEventReponse = null;
                    ModManager.FetchAllUserEvents(lastSubscriptionSync,
                                                  updateStartTimeStamp,
                                                  (ue) =>
                                                  {
                                                    userEventReponse = ue;
                                                    isRequestDone = true;
                                                  },
                                                  (e) =>
                                                  {
                                                     requestError = e;
                                                     isRequestDone = true;
                                                  });

                    while(!isRequestDone) { yield return null; }

                    if(requestError != null)
                    {
                        int reattemptDelay = CalculateReattemptDelay(requestError);
                        if(requestError.isAuthenticationInvalid)
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                       requestError.displayMessage);

                            m_validOAuthToken = false;
                        }
                        else if(requestError.isRequestUnresolvable
                                || reattemptDelay < 0)
                        {
                            Debug.LogWarning("[mod.io] Polling for user updates failed."
                                             + requestError.ToUnityDebugString());
                        }
                        else
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                       "Failed to synchronize subscriptions with mod.io servers.\n"
                                                       + requestError.displayMessage
                                                       + "\nRetrying in "
                                                       + reattemptDelay.ToString()
                                                       + " seconds");

                            yield return new WaitForSeconds(reattemptDelay);
                            continue;
                        }
                    }
                    // This may have changed during the request execution
                    else if(this.m_userProfile != null
                            && m_validOAuthToken)
                    {
                        ProcessUserUpdates(userEventReponse);
                        this.lastSubscriptionSync = updateStartTimeStamp;
                        WriteManifest();

                        PushSubscriptionChanges();

                        StartCoroutine(FetchUserRatings());
                    }
                }

                isRequestDone = false;
                requestError = null;

                // --- MOD EVENTS ---
                var subbedMods = ModManager.GetSubscribedModIds();
                if(subbedMods != null
                   && subbedMods.Count > 0)
                {
                    List<ModEvent> modEventResponse = null;
                    ModManager.FetchModEvents(ModManager.GetSubscribedModIds(),
                                              this.lastCacheUpdate,
                                              updateStartTimeStamp,
                                              (me) =>
                                              {
                                                modEventResponse = me;
                                                isRequestDone = true;
                                              },
                                              (e) =>
                                              {
                                                requestError = e;
                                                isRequestDone = true;
                                              });

                    while(!isRequestDone) { yield return null; }

                    if(requestError != null)
                    {
                        int reattemptDelay = CalculateReattemptDelay(requestError);
                        if(requestError.isAuthenticationInvalid)
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                       requestError.displayMessage);

                            m_validOAuthToken = false;
                        }
                        else if(requestError.isRequestUnresolvable
                                || reattemptDelay < 0)
                        {
                            cancelUpdates = true;
                        }
                        else
                        {
                            yield return new WaitForSeconds(reattemptDelay);
                            continue;
                        }
                    }
                    else
                    {
                        this.lastCacheUpdate = updateStartTimeStamp;
                        WriteManifest();
                        yield return StartCoroutine(ProcessModUpdates(modEventResponse));
                    }
                }

                yield return new WaitForSeconds(AUTOMATIC_UPDATE_INTERVAL);
            }

            m_updatesCoroutine = null;
        }

        private void PushSubscriptionChanges()
        {
            if(!this.m_validOAuthToken) { return; }

            // NOTE(@jackson): This is workaround is due to the response of a repeat sub and unsub
            // request returning an error.
            Action<WebRequestError, int> onSubFail = (e, modId) =>
            {
                // Ignore error for "Mod is already subscribed"
                if(e.webRequest.responseCode != 400)
                {
                    if(e.isAuthenticationInvalid)
                    {
                        if(m_validOAuthToken)
                        {
                            m_validOAuthToken = false;
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                       e.displayMessage);
                        }
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   e.displayMessage);
                    }
                }

                if(e.isRequestUnresolvable
                   && !e.isAuthenticationInvalid)
                {
                    m_queuedSubscribes.Remove(modId);
                    WriteManifest();
                }
            };
            Action<WebRequestError, int> onUnsubFail = (e, modId) =>
            {
                // Ignore error for "Mod is not subscribed"
                if(e.webRequest.responseCode != 400)
                {
                    if(e.isAuthenticationInvalid)
                    {
                        if(m_validOAuthToken)
                        {
                            m_validOAuthToken = false;
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                       e.displayMessage);
                        }
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   e.displayMessage);
                    }
                }

                if(e.isRequestUnresolvable
                   && !e.isAuthenticationInvalid)
                {
                    m_queuedUnsubscribes.Remove(modId);
                    WriteManifest();
                }
            };

            // push subs/unsubs
            foreach(int modId in m_queuedSubscribes)
            {
                APIClient.SubscribeToMod(modId,
                                         (p) =>
                                         {
                                            m_queuedSubscribes.Remove(p.id);
                                            WriteManifest();
                                         },
                                         (e) => onSubFail(e, modId));
            }
            foreach(int modId in m_queuedUnsubscribes)
            {
                APIClient.UnsubscribeFromMod(modId,
                                             () =>
                                             {
                                                m_queuedUnsubscribes.Remove(modId);
                                                WriteManifest();
                                             },
                                             (e) => onUnsubFail(e, modId));
            }
        }

        protected void ProcessUserUpdates(List<UserEvent> userEvents)
        {
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();
            List<int> addedSubscriptions = new List<int>(userEvents.Count / 2);
            List<int> removedSubscriptions = new List<int>(userEvents.Count / 2);

            foreach(UserEvent ue in userEvents)
            {
                switch(ue.eventType)
                {
                    case UserEventType.ModSubscribed:
                    {
                        if(!subscribedModIds.Contains(ue.modId)
                           && !m_queuedSubscribes.Contains(ue.modId)
                           && !m_queuedUnsubscribes.Contains(ue.modId))
                        {
                            addedSubscriptions.Add(ue.modId);
                            subscribedModIds.Add(ue.modId);
                        }
                    }
                    break;

                    case UserEventType.ModUnsubscribed:
                    {
                        if(subscribedModIds.Contains(ue.modId)
                           && !m_queuedSubscribes.Contains(ue.modId)
                           && !m_queuedUnsubscribes.Contains(ue.modId))
                        {
                            removedSubscriptions.Add(ue.modId);
                            subscribedModIds.Remove(ue.modId);
                        }
                    }
                    break;
                }
            }

            if(addedSubscriptions.Count > 0 || removedSubscriptions.Count > 0)
            {
                ModManager.SetSubscribedModIds(subscribedModIds);

                OnSubscriptionsChanged(addedSubscriptions, removedSubscriptions);

                int subscriptionUpdateCount = (addedSubscriptions.Count + removedSubscriptions.Count);
                string message = subscriptionUpdateCount.ToString() + " subscription(s) synchronized with the server";
                MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
            }
        }

        protected System.Collections.IEnumerator ProcessModUpdates(List<ModEvent> modEvents)
        {
            if(modEvents != null
               && modEvents.Count > 0)
            {
                List<int> editedMods = new List<int>();
                List<int> modfileChanged = new List<int>();
                List<int> deletedMods = new List<int>();

                foreach(ModEvent modEvent in modEvents)
                {
                    switch(modEvent.eventType)
                    {
                        case ModEventType.ModEdited:
                        {
                            editedMods.Add(modEvent.modId);
                        }
                        break;
                        case ModEventType.ModfileChanged:
                        {
                            modfileChanged.Add(modEvent.modId);
                        }
                        break;
                        case ModEventType.ModDeleted:
                        {
                            deletedMods.Add(modEvent.modId);
                        }
                        break;
                    }
                }

                // remove subs for deletedmods
                if(deletedMods.Count > 0)
                {
                    var subscribedModIds = ModManager.GetSubscribedModIds();

                    foreach(int modId in deletedMods)
                    {
                        subscribedModIds.Remove(modId);
                    }

                    OnSubscriptionsChanged(null, deletedMods);

                    // TODO(@jackson): QueueMessage
                }

                // remove duplicates from editedMods
                if(editedMods.Count > 0
                   && modfileChanged.Count > 0)
                {
                    foreach(int modId in modfileChanged)
                    {
                        editedMods.Remove(modId);
                    }
                }

                // fetch and cache profile edits
                if(editedMods.Count > 0)
                {
                    var pagination = new APIPaginationParameters()
                    {
                        limit = APIPaginationParameters.LIMIT_MAX,
                        offset = 0,
                    };

                    RequestFilter modFilter = new RequestFilter();
                    modFilter.sortFieldName = API.GetAllModsFilterFields.id;
                    modFilter.fieldFilters[API.GetAllModsFilterFields.id]
                    = new InArrayFilter<int>()
                    {
                        filterArray = editedMods.ToArray()
                    };

                    APIClient.GetAllMods(modFilter, pagination,
                    (r) =>
                    {
                        CacheClient.SaveModProfiles(r.items);
                    },
                    WebRequestError.LogAsWarning);
                }

                // get new versions of subscribed mods
                if(modfileChanged.Count > 0)
                {
                    // setup request data
                    var pagination = new APIPaginationParameters()
                    {
                        limit = APIPaginationParameters.LIMIT_MAX,
                        offset = 0,
                    };

                    RequestFilter modFilter = new RequestFilter();
                    modFilter.sortFieldName = API.GetAllModsFilterFields.id;
                    modFilter.fieldFilters[API.GetAllModsFilterFields.id]
                    = new InArrayFilter<int>()
                    {
                        filterArray = modfileChanged.ToArray()
                    };

                    bool isRequestDone = false;
                    RequestPage<ModProfile> response = null;
                    WebRequestError requestError = null;

                    // send request
                    APIClient.GetAllMods(modFilter, pagination,
                    (r) =>
                    {
                        isRequestDone = true;
                        response = r;
                    },
                    (e) =>
                    {
                        isRequestDone = true;
                        requestError = e;
                    });

                    while(!isRequestDone) { yield return null; }

                    // catch error
                    if(requestError != null)
                    {
                        if(requestError.isAuthenticationInvalid)
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                       requestError.displayMessage);

                            m_validOAuthToken = false;
                        }
                        else
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                       "Failed to update installed mods.\n"
                                                       + requestError.displayMessage);
                        }
                        yield break;
                    }
                    else
                    {
                        // installs
                        CacheClient.SaveModProfiles(response.items);

                        foreach(ModProfile profile in response.items)
                        {
                            yield return StartCoroutine(DownloadAndInstallModVersion(profile.id, profile.currentBuild.id));
                        }
                    }
                }
            }
        }

        // ---------[ USER CONTROL ]---------
        public void LogUserIn(string oAuthToken)
        {
            Debug.Assert(!String.IsNullOrEmpty(oAuthToken),
                         "[mod.io] ModBrowser.LogUserIn requires a valid oAuthToken");

            StartCoroutine(UserLoginCoroutine(oAuthToken));
        }

        private IEnumerator UserLoginCoroutine(string oAuthToken)
        {
            UserAuthenticationData.instance = new UserAuthenticationData()
            {
                userId = UserProfile.NULL_ID,
                token = oAuthToken,
            };

            m_queuedSubscribes.AddRange(ModManager.GetSubscribedModIds());
            WriteManifest();

            yield return this.StartCoroutine(FetchUserProfile());

            if(m_userProfile != null)
            {
                // - save user data -
                UserAuthenticationData.instance = new UserAuthenticationData()
                {
                    userId = m_userProfile.id,
                    token = oAuthToken,
                };

                yield return this.StartCoroutine(SynchronizeSubscriptionsWithServer());
            }
        }

        public void LogUserOut()
        {
            // push queued subs/unsubs
            foreach(int modId in m_queuedSubscribes)
            {
                APIClient.SubscribeToMod(modId, null,
                                         WebRequestError.LogAsWarning);
            }
            foreach(int modId in m_queuedUnsubscribes)
            {
                APIClient.UnsubscribeFromMod(modId, null,
                                             WebRequestError.LogAsWarning);
            }
            m_queuedSubscribes.Clear();
            m_queuedUnsubscribes.Clear();
            WriteManifest();

            // - clear current user -
            this.m_validOAuthToken = false;
            UserAuthenticationData.Clear();

            // - set up guest account -
            m_userProfile = null;
            if(this.loggedUserView != null)
            {
                this.loggedUserView.data = m_guestData;
            }

            // - notify -
            MessageSystem.QueueMessage(MessageDisplayData.Type.Success,
                                       "Successfully logged out");
        }

        // ---------[ INSTALLATION ]---------
        /// <summary>Downloads (if necessary) the modfile and installs it, removing other installed versions.</summary>
        // NOTE(@jackson): Checks 'this.isActiveAndEnabled' before unzipping.
        private System.Collections.IEnumerator DownloadAndInstallModVersion(int modId, int modfileId)
        {
            bool isRequestDone = false;
            WebRequestError requestError = null;

            // try and get the modfile
            Modfile modfile = CacheClient.LoadModfile(modId, modfileId);
            string zipFilePath = CacheClient.GenerateModBinaryZipFilePath(modId, modfileId);
            bool isBinaryZipValid = (System.IO.File.Exists(zipFilePath)
                                     && modfile != null
                                     && modfile.fileSize == IOUtilities.GetFileSize(zipFilePath)
                                     && modfile.fileHash != null
                                     && modfile.fileHash.md5 == IOUtilities.CalculateFileMD5Hash(zipFilePath));

            if(modfile == null
               || modfile.fileHash == null
               || modfile.downloadLocator == null
               || modfile.downloadLocator.dateExpires <= ServerTimeStamp.Now)
            {
                modfile = null;
            }

            // get modfile
            while(modfile == null
                  && this.isActiveAndEnabled)
            {
                APIClient.GetModfile(modId, modfileId,
                                     (mf) =>
                                     {
                                        modfile = mf;
                                        isRequestDone = true;
                                     },
                                     (e) =>
                                     {
                                        requestError = e;
                                        isRequestDone = true;
                                     });

                while(!isRequestDone) { yield return null; }
                isRequestDone = false;

                if(requestError != null)
                {
                    ModProfile profile = CacheClient.LoadModProfile(modId);
                    string modNamePrefix = string.Empty;
                    if(profile != null)
                    {
                        modNamePrefix = profile.name;
                    }
                    else
                    {
                        modNamePrefix = "Mods have";
                    }

                    int reattemptDelay = CalculateReattemptDelay(requestError);
                    if(requestError.isAuthenticationInvalid)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                   requestError.displayMessage);

                        m_validOAuthToken = false;
                        yield break;
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   modNamePrefix
                                                   + " failed to download.\n"
                                                   + requestError.displayMessage);
                        yield break;
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   modNamePrefix
                                                   + " failed to download.\n"
                                                   + requestError.displayMessage
                                                   + "\nRetrying in "
                                                   + reattemptDelay.ToString()
                                                   + " seconds");

                        yield return new WaitForSeconds(reattemptDelay);
                        continue;
                    }
                }
                else if(modfile != null)
                {
                    CacheClient.SaveModfile(modfile);

                    // recheck binary
                    isBinaryZipValid = (System.IO.File.Exists(zipFilePath)
                                        && modfile.fileSize == IOUtilities.GetFileSize(zipFilePath)
                                        && modfile.fileHash.md5 == IOUtilities.CalculateFileMD5Hash(zipFilePath));
                }
            }

            // get binary
            while(!isBinaryZipValid
                  && modfile.downloadLocator.dateExpires > ServerTimeStamp.Now)
            {
                FileDownloadInfo downloadInfo = DownloadClient.GetActiveModBinaryDownload(modId, modfileId);
                if(downloadInfo != null)
                {
                    // NOTE(@jackson): Already downloading!
                    yield break;
                }

                downloadInfo = DownloadClient.StartModBinaryDownload(modId, modfileId, zipFilePath);

                if(this.explorerView != null)
                {
                    this.explorerView.OnModDownloadStarted(modId, downloadInfo);
                }

                if(this.subscriptionsView != null)
                {
                    this.subscriptionsView.OnModDownloadStarted(modId, downloadInfo);
                }

                if(this.inspectorView != null)
                {
                    this.inspectorView.OnModDownloadStarted(modId, downloadInfo);
                }

                while(!downloadInfo.isDone) { yield return null; }

                if(downloadInfo.error != null)
                {
                    ModProfile profile = CacheClient.LoadModProfile(modId);
                    string modNamePrefix = string.Empty;
                    if(profile != null)
                    {
                        modNamePrefix = profile.name;
                    }
                    else
                    {
                        modNamePrefix = "Mods have";
                    }

                    int reattemptDelay = CalculateReattemptDelay(downloadInfo.error);
                    if(downloadInfo.error.isAuthenticationInvalid)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                   downloadInfo.error.displayMessage);

                        m_validOAuthToken = false;
                        yield break;
                    }
                    else if(downloadInfo.error.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   modNamePrefix
                                                   + " failed to download.\n"
                                                   + downloadInfo.error.displayMessage);
                        yield break;
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   modNamePrefix
                                                   + " failed to download.\n"
                                                   + downloadInfo.error.displayMessage
                                                   + "\nRetrying in "
                                                   + reattemptDelay.ToString()
                                                   + " seconds");

                        yield return new WaitForSeconds(reattemptDelay);
                        continue;
                    }
                }
                else if(downloadInfo.wasAborted)
                {
                    // NOTE(@jackson): Done here
                    yield break;
                }
                else
                {
                    bool fileExists = System.IO.File.Exists(zipFilePath);
                    Int64 binarySize = (fileExists ? IOUtilities.GetFileSize(zipFilePath) : -1);
                    string binaryHash = (fileExists ? IOUtilities.CalculateFileMD5Hash(zipFilePath) : string.Empty);

                    isBinaryZipValid = (fileExists
                                        && modfile.fileSize == binarySize
                                        && modfile.fileHash.md5 == binaryHash);

                    if(!isBinaryZipValid)
                    {
                        string errorMessage = string.Empty;
                        ModProfile profile = CacheClient.LoadModProfile(modId);
                        if(profile != null)
                        {
                            errorMessage = profile.name;
                        }
                        else
                        {
                            errorMessage = "Mods have";
                        }

                        errorMessage += (" failed to download correctly."
                                         + "\nEnsure your internet connection is stable - the"
                                         + " download will be retried shortly.");

                        if(!fileExists)
                        {
                            errorMessage += "[ErrorCode: IBZV_FM]";
                        }
                        else if(modfile.fileSize != binarySize)
                        {
                            errorMessage += "[ErrorCode: IBZV_FS]";
                        }
                        else if(modfile.fileHash.md5 != binaryHash)
                        {
                            errorMessage += "[ErrorCode: IBZV_FH]";
                        }
                        else
                        {
                            errorMessage += "[ErrorCode: IBZV_UE]";
                        }

                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   errorMessage);

                        yield return new WaitForSeconds(5f);

                        yield return StartCoroutine(DownloadAndInstallModVersion(modId, modfileId));

                        yield break;
                    }
                }
            }

            // NOTE(@jackson): Do not uninstall/install unless the ModBrowser is active!
            if(isBinaryZipValid
               && this.isActiveAndEnabled)
            {
                bool isUpdate = (ModManager.IterateInstalledMods(new int[] { modId }).Count() > 0);
                bool didInstall = ModManager.TryInstallMod(modId, modfileId, true);

                string message_namePart = string.Empty;
                ModProfile profile = CacheClient.LoadModProfile(modId);
                if(profile != null)
                {
                    message_namePart = profile.name + " was ";
                }
                else
                {
                    message_namePart = "Mods were ";
                }

                if(didInstall && isUpdate)
                {
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Info,
                                               message_namePart + "updated to the latest version");
                }
                else if(didInstall)
                {
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Info,
                                               message_namePart + "download and installed");
                }
                else if(isUpdate)
                {
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Info,
                                               message_namePart + "queued for update on restart");
                }
                else
                {
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Info,
                                               message_namePart + "queued for installation on restart");
                }
            }
        }

        // ---------[ UI CONTROL ]---------
        // ---[ VIEW MANAGEMENT ]---
        public void ShowExplorerView()
        {
            explorerView.gameObject.SetActive(true);
            inspectorView.gameObject.SetActive(false);
            subscriptionsView.gameObject.SetActive(false);
        }
        public void ShowSubscriptionsView()
        {
            subscriptionsView.gameObject.SetActive(true);
            inspectorView.gameObject.SetActive(false);
            explorerView.gameObject.SetActive(false);
        }

        public void InspectMod(int modId)
        {
            if(inspectorView != null)
            {
                inspectorView.modId = modId;
                inspectorView.gameObject.SetActive(true);
            }
        }

        public void CloseInspector()
        {
            inspectorView.gameObject.SetActive(false);
        }

        public void InspectDiscoverItem(ModView view)
        {
            InspectMod(view.data.profile.modId);
        }

        public void InspectSubscriptionItem(ModView view)
        {
            InspectMod(view.data.profile.modId);
        }

        // ---[ DIALOGS ]---
        public void OpenLoginDialog()
        {
            loginDialog.gameObject.SetActive(true);
            loginDialog.Initialize();
        }

        public void CloseLoginDialog()
        {
            loginDialog.gameObject.SetActive(false);
        }

        // ---------[ ENABLE/SUBSCRIBE MODS ]---------
        public void SubscribeToMod(int modId)
        {
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            // early out
            if(subscribedModIds.Contains(modId)) { return; }

            // update collection
            subscribedModIds.Add(modId);
            ModManager.SetSubscribedModIds(subscribedModIds);
            OnSubscribedToMod(modId);

            // push sub
            if(m_userProfile != null)
            {
                if(!m_queuedSubscribes.Contains(modId))
                {
                    m_queuedSubscribes.Add(modId);
                }
                m_queuedUnsubscribes.Remove(modId);
                WriteManifest();
            }
        }

        public void UnsubscribeFromMod(int modId)
        {
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            // early out
            if(!subscribedModIds.Contains(modId)) { return; }

            // update collection
            subscribedModIds.Remove(modId);
            ModManager.SetSubscribedModIds(subscribedModIds);
            OnUnsubscribedFromMod(modId);

            // push unsub
            if(m_userProfile != null)
            {
                if(!m_queuedUnsubscribes.Contains(modId))
                {
                    m_queuedUnsubscribes.Add(modId);
                }
                m_queuedSubscribes.Remove(modId);
                WriteManifest();
            }
        }

        public void OnSubscribedToMod(int modId)
        {
            EnableMod(modId);
            UpdateViewSubscriptions();

            ModManager.GetModProfile(modId,
            (p) =>
            {
                if(this.isActiveAndEnabled)
                {
                    string installDir = ModManager.GetModInstallDirectory(p.id, p.currentBuild.id);
                    if(!Directory.Exists(installDir))
                    {
                        this.StartCoroutine(DownloadAndInstallModVersion(p.id, p.currentBuild.id));
                    }
                }
            },
            (requestError) =>
            {
                if(requestError.isAuthenticationInvalid)
                {
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                               requestError.displayMessage);

                    m_validOAuthToken = false;
                }
                else
                {
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                               "Failed to start mod download. It will be retried shortly.\n"
                                               + requestError.displayMessage);
                }
            });
        }

        public void OnUnsubscribedFromMod(int modId)
        {
            DownloadClient.CancelAnyModBinaryDownloads(modId);

            // remove from disk
            CacheClient.DeleteAllModfileAndBinaryData(modId);

            ModManager.TryUninstallAllModVersions(modId);

            DisableMod(modId);

            UpdateViewSubscriptions();
        }

        public void OnSubscriptionsChanged(IList<int> addedSubscriptions,
                                           IList<int> removedSubscriptions)
        {
            var enabledMods = ModManager.GetEnabledModIds();

            if(addedSubscriptions != null
               && addedSubscriptions.Count > 0)
            {
                foreach(int modId in addedSubscriptions)
                {
                    if(!enabledMods.Contains(modId))
                    {
                        enabledMods.Add(modId);
                    }

                    ModManager.GetModProfile(modId,
                    (p) =>
                    {
                        if(this.isActiveAndEnabled)
                        {
                            string installDir = ModManager.GetModInstallDirectory(p.id, p.currentBuild.id);
                            if(!Directory.Exists(installDir))
                            {
                                this.StartCoroutine(DownloadAndInstallModVersion(p.id, p.currentBuild.id));
                            }
                        }
                    },
                    (requestError) =>
                    {
                        if(requestError.isAuthenticationInvalid)
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                       requestError.displayMessage);

                            m_validOAuthToken = false;
                        }
                        else
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                       "Failed to start mod download. It will be retried shortly.\n"
                                                       + requestError.displayMessage);
                        }
                    });
                }
            }

            if(removedSubscriptions != null
               && removedSubscriptions.Count > 0)
            {
                foreach(int modId in removedSubscriptions)
                {
                    // remove from disk
                    CacheClient.DeleteAllModfileAndBinaryData(modId);

                    ModManager.TryUninstallAllModVersions(modId);

                    // disable
                    enabledMods.Remove(modId);
                }
            }

            ModManager.SetEnabledModIds(enabledMods);
            UpdateViewSubscriptions();
        }

        private void UpdateViewSubscriptions()
        {
            explorerView.OnSubscriptionsUpdated();
            subscriptionsView.OnSubscriptionsUpdated();
            inspectorView.OnSubscriptionsUpdated();
        }

        public void EnableMod(int modId)
        {
            IList<int> mods = ModManager.GetEnabledModIds();
            if(!mods.Contains(modId))
            {
                mods.Add(modId);
                ModManager.SetEnabledModIds(mods);
            }

            if(this.explorerView != null)
            {
                this.explorerView.OnModEnabled(modId);
            }

            if(this.subscriptionsView != null)
            {
                this.subscriptionsView.OnModEnabled(modId);
            }

            if(this.inspectorView != null)
            {
                this.inspectorView.OnModEnabled(modId);
            }
        }

        public void DisableMod(int modId)
        {
            IList<int> mods = ModManager.GetEnabledModIds();
            if(mods.Contains(modId))
            {
                mods.Remove(modId);
                ModManager.SetEnabledModIds(mods);
            }

            if(this.explorerView != null)
            {
                this.explorerView.OnModDisabled(modId);
            }

            if(this.subscriptionsView != null)
            {
                this.subscriptionsView.OnModDisabled(modId);
            }

            if(this.inspectorView != null)
            {
                this.inspectorView.OnModDisabled(modId);
            }
        }

        // ---------[ EVENT HANDLING ]---------
        private void OnUserDisplayClicked(UserView view)
        {
            if(m_userProfile == null)
            {
                OpenLoginDialog();
            }
            else
            {
                LogUserOut();
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            APIClient.logAllRequests = debugAllAPIRequests;
            DownloadClient.logAllRequests = debugAllAPIRequests;
        }
        #endif

        // ---------[ OBSOLETE ]---------
        [Obsolete("Use ExplorerView.FetchPage() instead.")]
        public void RequestExplorerPage(int pageIndex,
                                        Action<RequestPage<ModProfile>> onSuccess,
                                        Action<WebRequestError> onError)
        {
            explorerView.FetchPage(pageIndex, onSuccess, onError);
        }

        [Obsolete("Use ExplorerView.prevPageButton instead.")]
        public Button prevPageButton
        {
            get { return explorerView.prevPageButton; }
            set { explorerView.prevPageButton = value; }
        }
        [Obsolete("Use ExplorerView.nextPageButton instead.")]
        public Button nextPageButton
        {
            get { return explorerView.nextPageButton; }
            set { explorerView.nextPageButton = value; }
        }
        [Obsolete("Use ExporerView.isActiveIndicator instead.")]
        public StateToggleDisplay explorerViewIndicator
        {
            get { return this.explorerView.isActiveIndicator; }
        }

        [Obsolete("Use ExplorerView.UpdatePageButtonInteractibility() instead.")]
        public void UpdateExplorerViewPageButtonInteractibility()
        {
            this.explorerView.UpdatePageButtonInteractibility();
        }

        [Obsolete("Use ExplorerView.UpdateFilter() instead.")]
        public void UpdateExplorerFilters()
        {
            explorerView.UpdateFilter();
        }

        [Obsolete("Use ExplorerView.ChangePage() instead.")]
        public void ChangeExplorerPage(int direction)
        {
            explorerView.ChangePage(direction);
        }

        [Obsolete]
        public struct SubscriptionViewFilter
        {
            public Func<ModProfile, bool> titleFilterDelegate;
            public Comparison<ModProfile> sortDelegate;
        }
        [Obsolete("Use SubscriptionView.titleFilterDelegate and sortDelegate instead.")]
        public SubscriptionViewFilter subscriptionViewFilter;

        [Obsolete("Use SubscriptionView.isActiveIndicator instead")]
        public StateToggleDisplay subscriptionsViewIndicator
        {
            get { return this.subscriptionsView.isActiveIndicator; }
        }

        [Obsolete("Use SubscriptionsView.FetchProfiles() instead.")]
        public void RequestSubscribedModProfiles(Action<List<ModProfile>> onSuccess,
                                                 Action<WebRequestError> onError)
        {
            subscriptionsView.FetchProfiles(onSuccess, onError);
        }

        [Obsolete("User SubscriptionsView.UpdateFilter() instead.")]
        public void UpdateSubscriptionFilters()
        {
            subscriptionsView.UpdateFilter();
        }
    }
}
