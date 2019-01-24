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
                return _instance;
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
            public SimpleVersion lastRunVersion;
            public int lastCacheUpdate;
            public int lastSubscriptionSync;
            public List<int> queuedUnsubscribes;
            public List<int> queuedSubscribes;
        }

        [Serializable]
        private class ExplorerSortOption
        {
            public string displayText;
            public string apiFieldName;
            public bool isSortAscending;

            public static ExplorerSortOption Create(string displayText, string apiFieldName, bool isSortAscending)
            {
                ExplorerSortOption newSOD = new ExplorerSortOption()
                {
                    displayText = displayText,
                    apiFieldName = apiFieldName,
                    isSortAscending = isSortAscending,
                };
                return newSOD;
            }
        }

        [Serializable]
        private class SubscriptionSortOption
        {
            public string displayText;
            public Comparison<ModProfile> sortDelegate;

            public static SubscriptionSortOption Create(string displayText, Comparison<ModProfile> sortDelegate)
            {
                SubscriptionSortOption newSOD = new SubscriptionSortOption()
                {
                    displayText = displayText,
                    sortDelegate = sortDelegate,
                };
                return newSOD;
            }
        }

        [Serializable]
        private class SubscriptionViewFilter
        {
            public Func<ModProfile, bool> titleFilterDelegate;
            public Comparison<ModProfile> sortDelegate;
        }

        [Serializable]
        public class InspectorViewData
        {
            public int currentModIndex;
            public int lastModIndex;
        }

        [Serializable]
        public struct MessageSystemStrings
        {
            public string userLoggedOut;
            public string subscriptionsRetrieved;
        }

        // ---------[ CONST & STATIC ]---------
        private const float AUTOMATIC_UPDATE_INTERVAL = 15f;
        public static readonly SimpleVersion VERSION = new SimpleVersion(0, 9);

        public static string manifestFilePath
        { get { return IOUtilities.CombinePath(CacheClient.settings.directory, "browser_manifest.data"); } }

        private readonly ExplorerSortOption[] explorerSortOptions = new ExplorerSortOption[]
        {
            ExplorerSortOption.Create("NEWEST",         ModIO.API.GetAllModsFilterFields.dateLive, false),
            ExplorerSortOption.Create("POPULARITY",     ModIO.API.GetAllModsFilterFields.popular, true),
            ExplorerSortOption.Create("RATING",         ModIO.API.GetAllModsFilterFields.rating, false),
            ExplorerSortOption.Create("SUBSCRIBERS",    ModIO.API.GetAllModsFilterFields.subscribers, false),
        };
        private readonly SubscriptionSortOption[] subscriptionSortOptions = new SubscriptionSortOption[]
        {
            SubscriptionSortOption.Create("A-Z",       (a,b) => { return String.Compare(a.name, b.name); }),
            SubscriptionSortOption.Create("LARGEST",   (a,b) => { return (int)(a.activeBuild.fileSize - a.activeBuild.fileSize); }),
            SubscriptionSortOption.Create("UPDATED",   (a,b) => { return b.dateUpdated - a.dateUpdated; }),
            SubscriptionSortOption.Create("ENABLED",   (a,b) =>
                                                        {
                                                            int diff = 0;
                                                            diff += (ModManager.GetEnabledModIds().Contains(a.id) ? -1 : 0);
                                                            diff += (ModManager.GetEnabledModIds().Contains(b.id) ? 1 : 0);

                                                            if(diff == 0)
                                                            {
                                                                diff = String.Compare(a.name, b.name);
                                                            }

                                                            return diff;
                                                        } ),
        };

        // ---------[ FIELDS ]---------
        [Header("Settings")]
        public ServerType connectTo = ServerType.TestServer;
        public ServerSettings testServerSettings = new ServerSettings()
        {
            apiURL = APIClient.API_URL_TESTSERVER + APIClient.API_VERSION,
            cacheDir = "$PERSISTENT_DATA_PATH$/modio_test",
            gameId = 0,
            gameAPIKey = string.Empty,
        };
        public ServerSettings productionServerSettings = new ServerSettings()
        {
            apiURL = APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION,
            cacheDir = "$PERSISTENT_DATA_PATH$/modio",
            gameId = 0,
            gameAPIKey = string.Empty,
        };
        public ServerSettings customServerSettings = new ServerSettings()
        {
            apiURL = string.Empty,
            cacheDir = "$PERSISTENT_DATA_PATH$/modio_custom",
            gameId = 0,
            gameAPIKey = string.Empty,
        };
        [Tooltip("Debug All API Requests")]
        public bool debugAllAPIRequests = false;

        public string modInstallDirectory = "$PERSISTENT_DATA_PATH$/modio/_installedMods";

        [SerializeField] private UserDisplayData m_guestData = new UserDisplayData()
        {
            profile = new UserProfileDisplayData()
            {
                userId = -1,
                username = "Guest",
            },
            avatar = new ImageDisplayData()
            {
                userId = -1,
                imageId = "guest_avatar",
                mediaType = ImageDisplayData.MediaType.UserAvatar,
                texture = null,
            },
        };

        public MessageSystemStrings messageStrings = new MessageSystemStrings()
        {
            userLoggedOut = "Successfully logged out",
            subscriptionsRetrieved = "$UPDATE_COUNT$ new subscription(s) synchronized with the server",
        };

        [Header("UI Components")]
        public ExplorerView explorerView;
        public Toggle explorerViewButton;
        public SubscriptionsView subscriptionsView;
        public Toggle subscriptionsViewButton;
        public InspectorView inspectorView;
        public UserView loggedUserView;
        public LoginDialog loginDialog;
        public Button prevPageButton;
        public Button nextPageButton;

        [Header("Runtime Data")]
        private InspectorViewData inspectorData = new InspectorViewData();
        private UserProfile userProfile = null;
        private int lastSubscriptionSync = -1;
        private int lastCacheUpdate = -1;
        private RequestFilter explorerViewFilter = new RequestFilter();
        private SubscriptionViewFilter subscriptionViewFilter = new SubscriptionViewFilter();
        private GameProfile gameProfile = null;
        private Coroutine m_updatesCoroutine = null;
        private List<int> m_queuedUnsubscribes = new List<int>();
        private List<int> m_queuedSubscribes = new List<int>();
        private bool m_onlineMode = true;
        private bool m_validOAuthToken = false;

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            _instance = this;

            this.m_validOAuthToken = false;
            this.m_onlineMode = true;

            this.StartCoroutine(StartFetchRemoteData());
        }

        private void OnDisable()
        {
            if(m_updatesCoroutine != null)
            {
                StopCoroutine(m_updatesCoroutine);
            }

            if(userProfile != null)
            {
                PushSubscriptionChanges();
            }

            _instance = null;
        }

        private void Start()
        {
            #if MODIO_TESTING
            if(testServerSettings.gameId > 0
               || productionServerSettings.gameId > 0
               || customServerSettings.gameId > 0)
            {
                Debug.LogError("OI DOOFUS! YOU SAVED AUTHENTICATION THE DETAILS TO THE PREFAB AGAIN!!!!!!");
                return;
            }
            #endif

            LoadLocalData();

            InitializeInspectorView();
            InitializeSubscriptionsView();
            InitializeExplorerView();
            InitializeDialogs();
            InitializeDisplays();
        }

        private void LoadLocalData()
        {
            // - Server Settings -
            ServerSettings settings;
            switch(connectTo)
            {
                case ServerType.TestServer:
                {
                    settings = testServerSettings;
                }
                break;
                case ServerType.ProductionServer:
                {
                    settings = productionServerSettings;
                }
                break;
                case ServerType.CustomServer:
                {
                    settings = customServerSettings;
                }
                break;
                default:
                {
                    settings = new ServerSettings();
                }
                break;
            }

            #if MODIO_TESTING
            settings.gameId = ModIO_Testing.GAME_ID;
            settings.gameAPIKey = ModIO_Testing.GAME_APIKEY;
            #endif

            if(settings.gameId <= 0)
            {
                Debug.LogError("[mod.io] Game ID is missing. Ensure that the appropriate server is"
                               + " selected in \'connectTo\', and the server settings have been stored.",
                               this);
                return;
            }
            if(String.IsNullOrEmpty(settings.gameAPIKey))
            {
                Debug.LogError("[mod.io] Game API Key is missing. Ensure that the appropriate server is"
                               + " selected in \'connectTo\', and the server settings have been stored.",
                               this);
                return;
            }

            // - CacheClient Data -
            string[] cacheDirParts = settings.cacheDir.Split('\\', '/');
            for(int i = 0; i < cacheDirParts.Length; ++i)
            {
                if(cacheDirParts[i].ToUpper().Equals("$PERSISTENT_DATA_PATH$"))
                {
                    cacheDirParts[i] = Application.persistentDataPath;
                }
            }

            string cacheDir = IOUtilities.CombinePath(cacheDirParts);
            var cacheSettings = CacheClient.settings;
            cacheSettings.directory = cacheDir;
            CacheClient.settings = cacheSettings;

            // - UserData -
            UserAuthenticationData userData = ModManager.GetUserData();
            if(userData.userId > 0)
            {
                this.userProfile = CacheClient.LoadUserProfile(userData.userId);
            }

            // - GameData -
            this.gameProfile = CacheClient.LoadGameProfile();
            if(this.gameProfile == null)
            {
                this.gameProfile = new GameProfile();
                this.gameProfile.id = settings.gameId;
            }

            // - Manifest -
            ManifestData manifest = IOUtilities.ReadJsonObjectFile<ManifestData>(ModBrowser.manifestFilePath);
            if(manifest != null)
            {
                this.lastCacheUpdate = manifest.lastCacheUpdate;
                this.lastSubscriptionSync = manifest.lastSubscriptionSync;
                this.m_queuedSubscribes = manifest.queuedSubscribes;
                this.m_queuedUnsubscribes = manifest.queuedUnsubscribes;

                if(manifest.lastRunVersion < ModBrowser.VERSION)
                {
                    ModBrowserPatcher.Run(manifest.lastRunVersion);
                    WriteManifest();
                }
            }
            else
            {
                this.lastCacheUpdate = 0;
                this.lastSubscriptionSync = 0;
                this.m_queuedSubscribes = new List<int>();
                this.m_queuedUnsubscribes = new List<int>();
                WriteManifest();
            }

            // - APIClient Data -
            APIClient.apiURL = settings.apiURL;
            APIClient.gameId = settings.gameId;
            APIClient.gameAPIKey = settings.gameAPIKey;
            APIClient.logAllRequests = debugAllAPIRequests;
            APIClient.userAuthorizationToken = userData.token;

            // - Installation Data -
            DownloadClient.logAllRequests = debugAllAPIRequests;
            string[] installDirParts = modInstallDirectory.Split('\\', '/');
            for(int i = 0; i < installDirParts.Length; ++i)
            {
                if(installDirParts[i].ToUpper().Equals("$PERSISTENT_DATA_PATH$"))
                {
                    installDirParts[i] = Application.persistentDataPath;
                }
            }
            var modioSettings = ModManager.settings;
            modioSettings.installDirectory = IOUtilities.CombinePath(installDirParts);
            ModManager.settings = modioSettings;
        }

        private void InitializeInspectorView()
        {
            inspectorView.Initialize();
            inspectorView.subscribeRequested += (p) => SubscribeToMod(p.id);
            inspectorView.unsubscribeRequested += (p) => UnsubscribeFromMod(p.id);
            inspectorView.gameObject.SetActive(false);

            UpdateInspectorViewPageButtonInteractibility();
        }

        private void InitializeSubscriptionsView()
        {
            subscriptionsView.Initialize();

            subscriptionsView.inspectRequested += InspectSubscriptionItem;
            subscriptionsView.subscribeRequested += (v) => SubscribeToMod(v.data.profile.modId);
            subscriptionsView.unsubscribeRequested += (v) => UnsubscribeFromMod(v.data.profile.modId);
            subscriptionsView.enableModRequested += (v) => EnableMod(v.data.profile.modId);
            subscriptionsView.disableModRequested += (v) => DisableMod(v.data.profile.modId);

            // - setup ui filter controls -
            if(subscriptionsView.nameSearchField != null)
            {
                subscriptionsView.nameSearchField.onEndEdit.AddListener((t) =>
                {
                    UpdateSubscriptionFilters();
                });
            }

            if(subscriptionsView.sortByDropdown != null)
            {
                subscriptionsView.sortByDropdown.options = new List<Dropdown.OptionData>(subscriptionSortOptions.Count());
                foreach(SubscriptionSortOption option in subscriptionSortOptions)
                {
                    subscriptionsView.sortByDropdown.options.Add(new Dropdown.OptionData() { text = option.displayText });
                }
                subscriptionsView.sortByDropdown.value = 0;
                subscriptionsView.sortByDropdown.captionText.text = subscriptionSortOptions[0].displayText;

                subscriptionsView.sortByDropdown.onValueChanged.AddListener((v) => UpdateSubscriptionFilters());
            }

            // - initialize filter -
            subscriptionViewFilter.sortDelegate = subscriptionSortOptions[0].sortDelegate;
            subscriptionViewFilter.titleFilterDelegate = (p) => true;

            // get page
            subscriptionsView.DisplayProfiles(null);

            RequestSubscribedModProfiles(subscriptionsView.DisplayProfiles,
                                         (e) => MessageSystem.QueueWebRequestError("Failed to retrieve subscribed mod profiles\n", e, null));

            subscriptionsView.gameObject.SetActive(false);


            if(subscriptionsViewButton != null)
            {
                subscriptionsViewButton.onValueChanged.AddListener((doShow) =>
                {
                    if(doShow) { ShowSubscriptionsView(); }
                });
                subscriptionsViewButton.isOn = false;
                subscriptionsViewButton.interactable = true;
            }
        }

        private void InitializeExplorerView()
        {
            explorerView.Initialize();

            explorerView.inspectRequested += InspectDiscoverItem;
            explorerView.subscribeRequested += (v) => SubscribeToMod(v.data.profile.modId);
            explorerView.unsubscribeRequested += (v) => UnsubscribeFromMod(v.data.profile.modId);
            explorerView.enableModRequested += (v) => EnableMod(v.data.profile.modId);
            explorerView.disableModRequested += (v) => DisableMod(v.data.profile.modId);

            // - setup ui filter controls -
            if(explorerView.nameSearchField != null)
            {
                explorerView.nameSearchField.onEndEdit.AddListener((t) =>
                {
                    UpdateExplorerFilters();
                } );
            }

            if(explorerView.sortByDropdown != null)
            {
                explorerView.sortByDropdown.options = new List<Dropdown.OptionData>(explorerSortOptions.Count());
                foreach(ExplorerSortOption option in explorerSortOptions)
                {
                    explorerView.sortByDropdown.options.Add(new Dropdown.OptionData() { text = option.displayText });
                }
                explorerView.sortByDropdown.value = 0;
                explorerView.sortByDropdown.captionText.text = explorerSortOptions[0].displayText;

                explorerView.sortByDropdown.onValueChanged.AddListener((v) => UpdateExplorerFilters());
            }

            // - setup filter -
            explorerView.onFilterTagsChanged += () => UpdateExplorerFilters();

            explorerViewFilter = new RequestFilter();

            ExplorerSortOption sortOption = explorerSortOptions[0];
            explorerViewFilter.sortFieldName = sortOption.apiFieldName;
            explorerViewFilter.isSortAscending = sortOption.isSortAscending;

            int pageSize = explorerView.itemsPerPage;
            RequestPage<ModProfile> modPage = new RequestPage<ModProfile>()
            {
                size = pageSize,
                items = new ModProfile[pageSize],
                resultOffset = 0,
                resultTotal = 0,
            };
            explorerView.currentPage = modPage;

            RequestExplorerPage(0,
                                (page) =>
                                {
                                    #if DEBUG
                                    if(!Application.isPlaying)
                                    {
                                        return;
                                    }
                                    #endif

                                    if(explorerView.currentPage == modPage)
                                    {
                                        explorerView.currentPage = page;
                                        explorerView.UpdateCurrentPageDisplay();
                                        UpdateExplorerViewPageButtonInteractibility();
                                    }
                                },
                                null);

            explorerView.targetPage = null;

            explorerView.UpdateCurrentPageDisplay();
            explorerView.gameObject.SetActive(true);

            UpdateExplorerViewPageButtonInteractibility();

            if(explorerViewButton != null)
            {
                explorerViewButton.onValueChanged.AddListener((doShow) =>
                {
                    if(doShow) { ShowExplorerView(); }
                });
                explorerViewButton.isOn = true;
                explorerViewButton.interactable = false;
            }
        }

        private void InitializeDialogs()
        {
            loginDialog.gameObject.SetActive(false);
            loginDialog.onSecurityCodeSent += (m) =>
            {
                OpenMessageDisplay_Success(m.message);
            };
            loginDialog.onUserOAuthTokenReceived += (t) =>
            {
                OpenMessageDisplay_Success("Authorization Successful");
                CloseLoginDialog();
                LogUserIn(t);
            };
            loginDialog.onAPIRequestError += (e) =>
            {
                OpenMessageDisplay_Error(e.message);
            };
            loginDialog.onInvalidSubmissionAttempted += (m) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Error, m);
            };
        }

        private void InitializeDisplays()
        {
            if(loggedUserView != null)
            {
                loggedUserView.Initialize();

                if(userProfile == null)
                {
                    loggedUserView.data = m_guestData;
                }
                else
                {
                    loggedUserView.DisplayUser(userProfile);
                }

                loggedUserView.onClick += OnUserDisplayClicked;
            }
        }

        private System.Collections.IEnumerator StartFetchRemoteData()
        {
            // Ensure Start() has been finished
            yield return null;

            this.StartCoroutine(FetchGameProfile());

            if(!String.IsNullOrEmpty(APIClient.userAuthorizationToken))
            {
                yield return this.StartCoroutine(FetchUserProfile());
                yield return this.StartCoroutine(FetchAllUserSubscriptionsAndUpdate());
            }
            else
            {
                yield return this.StartCoroutine(FetchAllSubscribedModProfiles());
            }

            UpdateInstalledModsUsingSubscriptions();

            m_updatesCoroutine = this.StartCoroutine(PollForUpdatesCoroutine());
        }

        private System.Collections.IEnumerator FetchGameProfile()
        {
            bool succeeded = false;
            bool cancelRequest = false;

            while(!cancelRequest
                  && !succeeded
                  && m_onlineMode)
            {
                bool isRequestDone = false;
                WebRequestError error = null;

                // --- GameProfile ---
                APIClient.GetGame(
                (g) =>
                {
                    this.gameProfile = g;
                    explorerView.tagCategories = g.tagCategories;
                    subscriptionsView.tagCategories = g.tagCategories;
                    inspectorView.tagCategories = g.tagCategories;

                    succeeded = true;
                    isRequestDone = true;
                },
                (e) =>
                {
                    error = e;
                    succeeded = false;
                    isRequestDone = true;
                });

                while(!isRequestDone) { yield return null; }

                if(error != null)
                {
                    int secondsUntilRetry;
                    string displayMessage;

                    ProcessRequestError(error, out cancelRequest,
                                        out secondsUntilRetry, out displayMessage);

                    if(secondsUntilRetry > 0)
                    {
                        yield return new WaitForSeconds(secondsUntilRetry + 1);
                        continue;
                    }

                    if(cancelRequest)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   displayMessage);
                    }
                }
            }
        }

        private System.Collections.IEnumerator FetchUserProfile()
        {
            Debug.Assert(!String.IsNullOrEmpty(APIClient.userAuthorizationToken));

            bool succeeded = false;
            bool cancelRequest = false;

            // get user profile
            while(!cancelRequest
                  && !succeeded
                  && m_onlineMode)
            {
                bool isRequestDone = false;
                WebRequestError error = null;

                // requests
                APIClient.GetAuthenticatedUser(
                (u) =>
                {
                    CacheClient.SaveUserProfile(u);

                    this.userProfile = u;

                    if(this.loggedUserView != null)
                    {
                        this.loggedUserView.DisplayUser(u);
                    }

                    error = null;
                    succeeded = true;
                    isRequestDone = true;
                },
                (e) =>
                {
                    error = e;
                    succeeded = false;
                    isRequestDone = true;
                });

                while(!isRequestDone) { yield return null; }

                if(error != null)
                {
                    int secondsUntilRetry;
                    string displayMessage;

                    ProcessRequestError(error, out cancelRequest,
                                        out secondsUntilRetry, out displayMessage);

                    if(secondsUntilRetry > 0)
                    {
                        yield return new WaitForSeconds(secondsUntilRetry + 1);
                        continue;
                    }

                    if(cancelRequest)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   displayMessage);
                    }
                }
            }

            this.m_validOAuthToken = succeeded;
        }

        private System.Collections.IEnumerator FetchAllUserSubscriptionsAndUpdate()
        {
            Debug.Assert(!String.IsNullOrEmpty(APIClient.userAuthorizationToken));

            int updateStartTimeStamp = ServerTimeStamp.Now;
            int updateCount = 0;

            // set up initial vars
            bool cancelRequest = false;
            bool allPagesReceived = false;

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            RequestFilter subscriptionFilter = new RequestFilter();
            subscriptionFilter.fieldFilters.Add(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                                new EqualToFilter<int>() { filterValue = gameProfile.id });

            List<int> localSubscriptions = new List<int>(ModManager.GetSubscribedModIds());

            // loop until done or broken
            while(m_validOAuthToken
                  && m_onlineMode
                  && !cancelRequest
                  && !allPagesReceived)
            {
                bool isRequestDone = false;
                WebRequestError error = null;
                RequestPage<ModProfile> requestPage = null;

                APIClient.GetUserSubscriptions(subscriptionFilter, pagination,
                                               (r) => { isRequestDone = true; requestPage = r; },
                                               (e) => { isRequestDone = true; error = e; });

                while(!isRequestDone) { yield return null; }

                if(error != null)
                {
                    // handle error
                    int secondsUntilRetry;
                    string displayMessage;

                    ProcessRequestError(error, out cancelRequest,
                                        out secondsUntilRetry, out displayMessage);

                    if(cancelRequest)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   displayMessage);
                    }
                    else if(secondsUntilRetry > 0)
                    {
                        yield return new WaitForSeconds(secondsUntilRetry + 1);
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
                    var subscribedModIds = ModManager.GetSubscribedModIds();
                    foreach(int modId in localSubscriptions)
                    {
                        if(m_queuedSubscribes.Contains(modId))
                        {
                            localSubscriptions.Remove(modId);
                        }
                        else
                        {
                            subscribedModIds.Remove(modId);
                        }
                    }
                    ModManager.SetSubscribedModIds(subscribedModIds);
                    OnSubscriptionsChanged(null, localSubscriptions);

                    updateCount += localSubscriptions.Count;
                }

                this.lastSubscriptionSync = updateStartTimeStamp;
                this.lastCacheUpdate = updateStartTimeStamp;
            }

            if(updateCount > 0)
            {
                string message = messageStrings.subscriptionsRetrieved.Replace("$UPDATE_COUNT$",
                                                                               updateCount.ToString());
                MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
            }
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
            bool cancelRequest = false;
            bool allPagesReceived = false;

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            RequestFilter subscriptionFilter = new RequestFilter();
            subscriptionFilter.fieldFilters.Add(ModIO.API.GetAllModsFilterFields.gameId,
                                                new EqualToFilter<int>() { filterValue = gameProfile.id });
            subscriptionFilter.fieldFilters.Add(ModIO.API.GetAllModsFilterFields.id,
                                                new InArrayFilter<int>()
                                                {
                                                    filterArray = subscribedModIds.ToArray()
                                                });

            // loop until done or broken
            while(m_onlineMode
                  && !cancelRequest
                  && !allPagesReceived)
            {
                bool isRequestDone = false;
                WebRequestError error = null;
                RequestPage<ModProfile> requestPage = null;

                APIClient.GetAllMods(subscriptionFilter, pagination,
                                     (r) => { isRequestDone = true; requestPage = r; },
                                     (e) => { isRequestDone = true; error = e; });

                while(!isRequestDone) { yield return null; }

                if(error != null)
                {
                    // handle error
                    int secondsUntilRetry;
                    string displayMessage;

                    ProcessRequestError(error, out cancelRequest,
                                        out secondsUntilRetry, out displayMessage);

                    if(cancelRequest)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   displayMessage);
                    }
                    else if(secondsUntilRetry > 0)
                    {
                        yield return new WaitForSeconds(secondsUntilRetry + 1);
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

        private void UpdateInstalledModsUsingSubscriptions()
        {
            var subscribedModIds = ModManager.GetSubscribedModIds();
            var installedModVersions = ModManager.GetInstalledModVersions(false);

            foreach(ModfileIdPair idPair in installedModVersions)
            {
                if(!subscribedModIds.Contains(idPair.modId))
                {
                    ModManager.TryUninstallAllModVersions(idPair.modId);
                }
            }

            foreach(int modId in subscribedModIds)
            {
                ModProfile profile = CacheClient.LoadModProfile(modId);

                if(profile == null)
                {
                    Debug.LogWarning("[mod.io] Subscribed mod profile not found in cache. (Id: " + modId + ")");
                    ModManager.GetModProfile(modId, null, null);
                }
                else
                {
                    bool isInstalled = false;
                    List<ModfileIdPair> wrongVersions = new List<ModfileIdPair>();
                    foreach(ModfileIdPair idPair in installedModVersions)
                    {
                        if(idPair.modId == profile.id)
                        {
                            if(idPair.modfileId == profile.activeBuild.id)
                            {
                                isInstalled = true;
                            }
                            else if(!wrongVersions.Contains(idPair))
                            {
                                wrongVersions.Add(idPair);
                            }
                        }
                    }

                    if(!isInstalled)
                    {
                        this.StartCoroutine(DownloadAndInstallModVersion(profile.id, profile.activeBuild.id));
                    }
                    // isInstalled &&
                    else if(wrongVersions.Count > 0)
                    {
                        foreach(ModfileIdPair idPair in wrongVersions)
                        {
                            ModManager.TryUninstallModVersion(idPair.modId, idPair.modfileId);
                        }
                    }
                }
            }
        }

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

            while(modfile == null
                  && m_onlineMode
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
                    bool cancel;
                    int reattemptDelay;
                    string message;

                    ProcessRequestError(requestError, out cancel,
                                        out reattemptDelay, out message);

                    if(reattemptDelay > 0)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Mods have failed to download.\n"
                                                   + message
                                                   + "\nRetrying in "
                                                   + reattemptDelay.ToString()
                                                   + " seconds");
                        yield return new WaitForSeconds(reattemptDelay + 1);
                    }
                    else if(cancel)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Mods have failed to download.\n"
                                                   + message);
                        yield break;
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

            if(modfile == null)
            {
                Debug.LogWarning("[mod.io] Failed to retrieve the Modfile and thus cannot download"
                                 + " the mod binary. (ModId: " + modId.ToString() + " - ModfileId: "
                                 + modfileId.ToString());
                yield break;
            }


            while(!isBinaryZipValid
                  && modfile.downloadLocator.dateExpires > ServerTimeStamp.Now
                  && m_onlineMode)
            {
                FileDownloadInfo downloadInfo = DownloadClient.GetActiveModBinaryDownload(modId, modfileId);
                if(downloadInfo == null)
                {
                    downloadInfo = DownloadClient.StartModBinaryDownload(modId, modfileId, zipFilePath);
                }

                foreach(ModView modView in IterateModViews())
                {
                    if(modView.data.profile.modId == modId)
                    {
                        modView.DisplayDownload(downloadInfo);
                    }
                }

                while(!downloadInfo.isDone) { yield return null; }

                if(downloadInfo.error != null)
                {
                    bool cancel;
                    int reattemptDelay;
                    string message;

                    ProcessRequestError(downloadInfo.error, out cancel,
                                        out reattemptDelay, out message);

                    if(reattemptDelay > 0)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Mods have failed to download.\n"
                                                   + message
                                                   + "\nRetrying in "
                                                   + reattemptDelay.ToString()
                                                   + " seconds");
                        yield return new WaitForSeconds(reattemptDelay + 1);
                    }
                    else if(cancel)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Mods have failed to download.\n"
                                                   + message);
                        yield break;
                    }
                }
                else
                {
                    isBinaryZipValid = (System.IO.File.Exists(zipFilePath)
                                        && modfile.fileSize == IOUtilities.GetFileSize(zipFilePath)
                                        && modfile.fileHash.md5 == IOUtilities.CalculateFileMD5Hash(zipFilePath));

                    if(!isBinaryZipValid)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Mods have failed to download correctly.");
                        yield break;
                    }
                }
            }

            // NOTE(@jackson): Do not uninstall/install unless the ModBrowser is active!
            if(isBinaryZipValid
               && this.isActiveAndEnabled)
            {
                if(!ModManager.TryInstallMod(modId, modfileId, true) )
                {
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                               "Mods have failed to install. Restarting may resolve this issue.");
                }
            }
        }

        // ---------[ REQUESTS ]---------
        private void ProcessRequestError(WebRequestError requestError,
                                         out bool cancelFurtherAttempts,
                                         out int reattemptDelaySeconds,
                                         out string displayMessage)
        {
            cancelFurtherAttempts = false;

            switch(requestError.responseCode)
            {
                // Bad authorization
                case 401:
                {
                    reattemptDelaySeconds = -1;
                    displayMessage = ("Your mod.io user authorization details have changed."
                                      + "\nLogging out and in again should correct this issue.");
                    cancelFurtherAttempts = true;

                    m_validOAuthToken = false;
                }
                break;

                // Not found
                case 404:
                // Gone
                case 410:
                {
                    reattemptDelaySeconds = -1;
                    displayMessage = requestError.message;

                    cancelFurtherAttempts = true;
                }
                break;

                // Over limit
                case 429:
                {
                    string sur_string;
                    if(!(requestError.responseHeaders.TryGetValue("X-Ratelimit-RetryAfter", out sur_string)
                         && Int32.TryParse(sur_string, out reattemptDelaySeconds)))
                    {
                        reattemptDelaySeconds = 60;

                        Debug.LogWarning("[mod.io] Too many APIRequests have been made, however"
                                         + " no valid X-Ratelimit-RetryAfter header was detected."
                                         + "\nPlease report this to mod.io staff.");
                    }

                    displayMessage = requestError.message;
                }
                break;

                // Internal server error
                case 500:
                {
                    reattemptDelaySeconds = -1;
                    displayMessage = ("There was an error with the mod.io servers. Staff have been"
                                      + " notified, and will attempt to fix the issue as soon as possible.");
                    cancelFurtherAttempts = true;
                }
                break;

                // Service Unavailable
                case 503:
                {
                    reattemptDelaySeconds = -1;
                    displayMessage = ("The mod.io servers are currently offline. Please try again later.");
                    cancelFurtherAttempts = true;

                    m_onlineMode = false;
                }
                break;

                default:
                {
                    // Cannot connect resolve destination host
                    if(requestError.responseCode <= 0)
                    {
                        reattemptDelaySeconds = 60;
                        displayMessage = ("Unable to connect to the mod.io servers.");
                    }
                    else
                    {
                        Debug.LogWarning("[mod.io] An unhandled error was returned when retrieving mod updates."
                                         + "\nPlease report this to mod.io staff with the following information:\n"
                                         + requestError.ToUnityDebugString());

                        reattemptDelaySeconds = 15;
                        displayMessage = ("Error synchronizing with the mod.io servers.\n"
                                          + requestError.message);
                    }
                }
                break;
            }
        }

        public void RequestExplorerPage(int pageIndex,
                                        Action<RequestPage<ModProfile>> onSuccess,
                                        Action<WebRequestError> onError)
        {
            // PaginationParameters
            APIPaginationParameters pagination = new APIPaginationParameters();
            int pageSize = explorerView.itemsPerPage;
            pagination.limit = pageSize;
            pagination.offset = pageIndex * pageSize;

            // Send Request
            APIClient.GetAllMods(explorerViewFilter, pagination,
                                 onSuccess, onError);
        }

        public void RequestSubscribedModProfiles(Action<List<ModProfile>> onSuccess,
                                                 Action<WebRequestError> onError)
        {
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            if(subscribedModIds.Count > 0)
            {
                Action<List<ModProfile>> onGetModProfiles = (list) =>
                {
                    List<ModProfile> filteredList = new List<ModProfile>(list.Count);
                    foreach(ModProfile profile in list)
                    {
                        if(subscriptionViewFilter.titleFilterDelegate(profile))
                        {
                            filteredList.Add(profile);
                        }
                    }

                    onSuccess(filteredList);
                };

                ModManager.GetModProfiles(subscribedModIds, onGetModProfiles, onError);
            }
            else
            {
                onSuccess(new List<ModProfile>(0));
            }
        }

        // ---------[ UPDATES ]---------
        private System.Collections.IEnumerator PollForUpdatesCoroutine()
        {
            bool cancelUpdates = false;

            while(m_onlineMode && !cancelUpdates)
            {
                int updateStartTimeStamp = ServerTimeStamp.Now;

                bool isRequestDone = false;
                WebRequestError requestError = null;

                // --- MOD EVENTS ---
                List<ModEvent> modEventResponse = null;
                ModManager.FetchAllModEvents(this.lastCacheUpdate,
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
                    int secondsUntilRetry;
                    string displayMessage;

                    ProcessRequestError(requestError, out cancelUpdates,
                                        out secondsUntilRetry, out displayMessage);


                    if(secondsUntilRetry > 0)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   displayMessage
                                                   + "\nRetrying in "
                                                   + secondsUntilRetry.ToString()
                                                   + " seconds");

                        yield return new WaitForSeconds(secondsUntilRetry + 1);
                        continue;
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   displayMessage);
                    }

                    if(cancelUpdates)
                    {
                        break;
                    }
                }
                else
                {
                    ProcessUpdates(modEventResponse, updateStartTimeStamp);
                    this.lastCacheUpdate = updateStartTimeStamp;
                    WriteManifest();
                }

                isRequestDone = false;
                requestError = null;

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
                        int secondsUntilRetry;
                        string displayMessage;

                        ProcessRequestError(requestError, out cancelUpdates,
                                            out secondsUntilRetry, out displayMessage);

                        if(secondsUntilRetry > 0)
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                       displayMessage
                                                       + "\nRetrying in "
                                                       + secondsUntilRetry.ToString()
                                                       + " seconds");

                            yield return new WaitForSeconds(secondsUntilRetry + 1);
                            continue;
                        }
                        else
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                       displayMessage);
                        }

                        if(cancelUpdates)
                        {
                            break;
                        }
                    }
                    else
                    {
                        ProcessUserUpdates(userEventReponse);
                        this.lastSubscriptionSync = updateStartTimeStamp;
                        WriteManifest();

                        PushSubscriptionChanges();
                    }
                }

                yield return new WaitForSeconds(AUTOMATIC_UPDATE_INTERVAL);
            }

            m_updatesCoroutine = null;
        }

        private void PushSubscriptionChanges()
        {
            Debug.Assert(this.userProfile != null);
            Debug.Assert(this.m_validOAuthToken);

            // NOTE(@jackson): This is workaround is due to the response of an unsub request
            // on an non-subbed mod being an error.
            Debug.Assert(APIClient.API_VERSION == "v1");
            Action<WebRequestError, int> onUnsubFail = (e, modId) =>
            {
                if(e.responseCode == 400)
                {
                    // Mod is already unsubscribed
                    m_queuedUnsubscribes.Remove(modId);
                    WriteManifest();
                }
                else
                {
                    WebRequestError.LogAsWarning(e);
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
                                         WebRequestError.LogAsWarning);
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
                string message = messageStrings.subscriptionsRetrieved.Replace("$UPDATE_COUNT$", subscriptionUpdateCount.ToString());
                MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
            }
        }

        protected void ProcessUpdates(List<ModEvent> modEvents, int updateStartTimeStamp)
        {
            if(modEvents != null)
            {
                // - Event Handler Notification -
                Action<List<ModProfile>> onAvailable = (profiles) =>
                {
                    // this.OnModsAvailable(profiles);
                };
                Action<List<ModProfile>> onEdited = (profiles) =>
                {
                    // this.OnModsEdited(profiles);
                };
                Action<List<Modfile>> onReleasesUpdated = (modfiles) =>
                {
                    // this.OnModReleasesUpdated(modfiles);
                };
                Action<List<int>> onUnavailable = (ids) =>
                {
                    // this.OnModsUnavailable(ids);
                };
                Action<List<int>> onDeleted = (ids) =>
                {
                    // this.OnModsDeleted(ids);
                };

                Action onSuccess = () =>
                {
                    this.lastCacheUpdate = updateStartTimeStamp;
                    // this.isUpdateRunning = false;

                    WriteManifest();

                    // #if DEBUG
                    // if(Application.isPlaying)
                    // #endif
                    // {
                    //     IModBrowserView view = GetViewForMode(this.viewMode);
                    //     view.profileCollection = GetFilteredProfileCollectionForMode(this.viewMode);
                    //     view.Refresh();
                    // }
                };

                Action<WebRequestError> onError = (error) =>
                {
                    WebRequestError.LogAsWarning(error);
                    // this.isUpdateRunning = false;
                };

                ModManager.ApplyModEventsToCache(modEvents,
                                                 onAvailable, onEdited,
                                                 onUnavailable, onDeleted,
                                                 onReleasesUpdated,
                                                 onSuccess,
                                                 onError);
            }
            else
            {
                this.lastCacheUpdate = updateStartTimeStamp;
                // this.isUpdateRunning = false;

                WriteManifest();
            }
        }

        protected void WriteManifest()
        {
            ManifestData manifest = new ManifestData()
            {
                lastRunVersion = ModBrowser.VERSION,
                lastCacheUpdate = this.lastCacheUpdate,
                lastSubscriptionSync = this.lastSubscriptionSync,
                queuedUnsubscribes = this.m_queuedUnsubscribes,
                queuedSubscribes = this.m_queuedSubscribes,
            };

            IOUtilities.WriteJsonObjectFile(ModBrowser.manifestFilePath, manifest);
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
            APIClient.userAuthorizationToken = oAuthToken;

            yield return this.StartCoroutine(FetchUserProfile());

            if(this.userProfile != null)
            {
                // - save user data -
                ModManager.SetUserData(this.userProfile.id, oAuthToken);
                yield return this.StartCoroutine(FetchAllUserSubscriptionsAndUpdate());
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
            APIClient.userAuthorizationToken = null;
            this.m_validOAuthToken = false;
            ModManager.SetUserData(UserAuthenticationData.NONE);

            // - set up guest account -
            this.userProfile = null;
            if(this.loggedUserView != null)
            {
                this.loggedUserView.data = m_guestData;
            }

            // - notify -
            MessageSystem.QueueMessage(MessageDisplayData.Type.Success,
                                       messageStrings.userLoggedOut);
        }

        // ---------[ UI CONTROL ]---------
        // ---[ VIEW MANAGEMENT ]---
        public void ShowExplorerView()
        {
            explorerView.gameObject.SetActive(true);
            inspectorView.gameObject.SetActive(false);
            subscriptionsView.gameObject.SetActive(false);

            if(explorerViewButton != null)
            {
                explorerViewButton.isOn = true;
                explorerViewButton.interactable = false;
            }
            if(subscriptionsViewButton != null)
            {
                subscriptionsViewButton.isOn = false;
                subscriptionsViewButton.interactable = true;
            }
        }
        public void ShowSubscriptionsView()
        {
            subscriptionsView.gameObject.SetActive(true);
            inspectorView.gameObject.SetActive(false);
            explorerView.gameObject.SetActive(false);

            if(explorerViewButton != null)
            {
                explorerViewButton.isOn = false;
                explorerViewButton.interactable = true;
            }
            if(subscriptionsViewButton != null)
            {
                subscriptionsViewButton.isOn = true;
                subscriptionsViewButton.interactable = false;
            }
        }

        public void InspectMod(int modId)
        {
            inspectorView.DisplayLoading();
            inspectorView.profile = null;
            inspectorView.statistics = null;

            inspectorView.gameObject.SetActive(true);

            // profile
            ModManager.GetModProfile(modId,
                                     (p) =>
                                     {
                                        inspectorView.profile = p;
                                        inspectorView.UpdateProfileDisplay();
                                     },
                                     WebRequestError.LogAsWarning);


            // statistics
            ModManager.GetModStatistics(modId,
                                        (s) =>
                                        {
                                            inspectorView.statistics = s;
                                            inspectorView.UpdateStatisticsDisplay();
                                        },
                                        WebRequestError.LogAsWarning);

            // subscription
            inspectorView.isModSubscribed = ModManager.GetSubscribedModIds().Contains(modId);
            inspectorView.UpdateIsSubscribedDisplay();

            if(inspectorView.scrollView != null) { inspectorView.scrollView.verticalNormalizedPosition = 1f; }

            UpdateInspectorViewPageButtonInteractibility();
        }

        public void CloseInspector()
        {
            inspectorView.gameObject.SetActive(false);
        }

        private IEnumerable<ModView> IterateModViews()
        {
            if(this.inspectorView != null
               && this.inspectorView.modView != null)
            {
                yield return this.inspectorView.modView;
            }

            if(this.explorerView != null)
            {
                foreach(var modView in this.explorerView.modViews)
                {
                    yield return modView;
                }
            }

            if(this.subscriptionsView != null)
            {
                foreach(var modView in this.subscriptionsView.modViews)
                {
                    yield return modView;
                }
            }
        }

        // public void ChangeInspectorPage(int direction)
        // {
        //     int pageSize = explorerView.itemsPerPage;
        //     int firstExplorerIndex = (explorerView.CurrentPageNumber-1) * pageSize;
        //     int newModIndex = inspectorData.currentModIndex + direction;
        //     int offsetIndex = newModIndex - firstExplorerIndex;

        //     ModProfile profile;

        //     // profile
        //     if(offsetIndex < 0)
        //     {
        //         ChangeExplorerPage(-1);

        //         offsetIndex += pageSize;
        //         profile = explorerView.targetPage.items[offsetIndex];
        //     }
        //     else if(offsetIndex >= pageSize)
        //     {
        //         ChangeExplorerPage(1);

        //         offsetIndex -= pageSize;
        //         profile = explorerView.targetPage.items[offsetIndex];
        //     }
        //     else
        //     {
        //         profile = explorerView.currentPage.items[offsetIndex];
        //     }

        //     SetInspectorViewProfile(profile);
        // }

        public void ChangeExplorerPage(int direction)
        {
            // TODO(@jackson): Queue on isTransitioning?
            if(explorerView.isTransitioning)
            {
                Debug.LogWarning("[mod.io] Cannot change during transition");
                return;
            }

            int pageSize = explorerView.itemsPerPage;
            int targetPageIndex = explorerView.CurrentPageNumber - 1 + direction;
            int targetPageProfileOffset = targetPageIndex * pageSize;

            Debug.Assert(targetPageIndex >= 0);
            Debug.Assert(targetPageIndex < explorerView.CurrentPageCount);

            int pageItemCount = (int)Mathf.Min(pageSize,
                                               explorerView.currentPage.resultTotal - targetPageProfileOffset);

            RequestPage<ModProfile> targetPage = new RequestPage<ModProfile>()
            {
                size = pageSize,
                items = new ModProfile[pageItemCount],
                resultOffset = targetPageProfileOffset,
                resultTotal = explorerView.currentPage.resultTotal,
            };
            explorerView.targetPage = targetPage;
            explorerView.UpdateTargetPageDisplay();

            RequestExplorerPage(targetPageIndex,
                                (page) =>
                                {
                                    if(explorerView.targetPage == targetPage)
                                    {
                                        explorerView.targetPage = page;
                                        explorerView.UpdateTargetPageDisplay();
                                    }
                                    if(explorerView.currentPage == targetPage)
                                    {
                                        explorerView.currentPage = page;
                                        explorerView.UpdateCurrentPageDisplay();
                                        UpdateExplorerViewPageButtonInteractibility();
                                    }
                                },
                                null);

            PageTransitionDirection transitionDirection = (direction < 0
                                                           ? PageTransitionDirection.FromLeft
                                                           : PageTransitionDirection.FromRight);

            explorerView.InitiateTargetPageTransition(transitionDirection, () =>
            {
                UpdateExplorerViewPageButtonInteractibility();
            });
            UpdateExplorerViewPageButtonInteractibility();
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

        public void OpenMessageDisplay_OneButton(string header, string content,
                                                string buttonText, Action buttonCallback)
        {
            // messageDialog.button01.GetComponentInChildren<Text>().text = buttonText;

            // messageDialog.button01.onClick.RemoveAllListeners();
            // messageDialog.button01.onClick.AddListener(() => buttonCallback());

            // messageDialog.button02.gameObject.SetActive(false);

            // OpenMessageDisplay(header, content);

            OpenMessageDisplay_Info(content);
        }

        public void OpenMessageDisplay_TwoButton(string header, string content,
                                                string button01Text, Action button01Callback,
                                                string button02Text, Action button02Callback)
        {
            // messageDialog.button01.GetComponentInChildren<Text>().text = button01Text;

            // messageDialog.button01.onClick.RemoveAllListeners();
            // messageDialog.button01.onClick.AddListener(() => button01Callback());

            // messageDialog.button02.GetComponentInChildren<Text>().text = button02Text;

            // messageDialog.button02.onClick.RemoveAllListeners();
            // messageDialog.button02.onClick.AddListener(() => button02Callback());

            // messageDialog.button02.gameObject.SetActive(true);

            // OpenMessageDisplay(header, content);

            OpenMessageDisplay_Info(content);
        }

        private void OpenMessageDisplay(string header, string content)
        {
            OpenMessageDisplay_Info(content);
        }

        private void OpenMessageDisplay_Error(string message)
        {
            MessageSystem.QueueMessage(MessageDisplayData.Type.Error, message);
        }

        private void OpenMessageDisplay_Success(string message)
        {
            MessageSystem.QueueMessage(MessageDisplayData.Type.Success, message);
        }

        private void OpenMessageDisplay_Info(string message)
        {
            MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
        }

        private void OpenMessageDisplay_Warning(string message)
        {
            MessageSystem.QueueMessage(MessageDisplayData.Type.Warning, message);
        }

        public void UpdateExplorerViewPageButtonInteractibility()
        {
            if(prevPageButton != null)
            {
                prevPageButton.interactable = (!explorerView.isTransitioning
                                               && explorerView.CurrentPageNumber > 1);
            }
            if(nextPageButton != null)
            {
                nextPageButton.interactable = (!explorerView.isTransitioning
                                               && explorerView.CurrentPageNumber < explorerView.CurrentPageCount);
            }
        }

        public void UpdateInspectorViewPageButtonInteractibility()
        {
            if(inspectorView.previousModButton != null)
            {
                inspectorView.previousModButton.interactable = (inspectorData.currentModIndex > 0);
            }
            if(inspectorView.nextModButton != null)
            {
                inspectorView.nextModButton.interactable = (inspectorData.currentModIndex < inspectorData.lastModIndex);
            }
        }

        // TODO(@jackson): Don't request page!!!!!!!
        public void UpdateExplorerFilters()
        {
            // sort
            if(explorerView.sortByDropdown == null)
            {
                explorerViewFilter.sortFieldName = ModIO.API.GetAllModsFilterFields.popular;
                explorerViewFilter.isSortAscending = false;
            }
            else
            {
                ExplorerSortOption optionData = explorerSortOptions[explorerView.sortByDropdown.value];
                explorerViewFilter.sortFieldName = optionData.apiFieldName;
                explorerViewFilter.isSortAscending = optionData.isSortAscending;
            }

            // title
            if(explorerView.nameSearchField == null
               || String.IsNullOrEmpty(explorerView.nameSearchField.text))
            {
                explorerViewFilter.fieldFilters.Remove(ModIO.API.GetAllModsFilterFields.name);
            }
            else
            {
                explorerViewFilter.fieldFilters[ModIO.API.GetAllModsFilterFields.name]
                    = new StringLikeFilter() { likeValue = "*"+explorerView.nameSearchField.text+"*" };
            }

            // tags
            string[] filterTagNames = explorerView.filterTags.ToArray();

            if(filterTagNames.Length == 0)
            {
                explorerViewFilter.fieldFilters.Remove(ModIO.API.GetAllModsFilterFields.tags);
            }
            else
            {
                explorerViewFilter.fieldFilters[ModIO.API.GetAllModsFilterFields.tags]
                    = new MatchesArrayFilter<string>() { filterArray = filterTagNames };
            }

            int pageSize = explorerView.itemsPerPage;
            // TODO(@jackson): BAD ZERO?
            RequestPage<ModProfile> filteredPage = new RequestPage<ModProfile>()
            {
                size = pageSize,
                items = new ModProfile[pageSize],
                resultOffset = 0,
                resultTotal = 0,
            };
            explorerView.currentPage = filteredPage;

            RequestExplorerPage(0,
                                (page) =>
                                {
                                    if(explorerView.currentPage == filteredPage)
                                    {
                                        explorerView.currentPage = page;
                                        explorerView.UpdateCurrentPageDisplay();
                                        UpdateExplorerViewPageButtonInteractibility();
                                    }
                                },
                                null);

            // TODO(@jackson): Update Mod Count
            explorerView.UpdateCurrentPageDisplay();
        }

        public void UpdateSubscriptionFilters()
        {
            // sort
            if(subscriptionsView.sortByDropdown == null)
            {
                subscriptionViewFilter.sortDelegate = subscriptionSortOptions[0].sortDelegate;
            }
            else
            {
                subscriptionViewFilter.sortDelegate = subscriptionSortOptions[subscriptionsView.sortByDropdown.value].sortDelegate;
            }

            // name
            if(subscriptionsView.nameSearchField == null
               || String.IsNullOrEmpty(subscriptionsView.nameSearchField.text))
            {
                subscriptionViewFilter.titleFilterDelegate = (p) => true;
            }
            else
            {
                subscriptionViewFilter.titleFilterDelegate = (p) =>
                {
                    return p.name.ToUpper().Contains(subscriptionsView.nameSearchField.text.ToUpper());
                };
            }

            // request page
            RequestSubscribedModProfiles(subscriptionsView.DisplayProfiles,
                                         (e) => MessageSystem.QueueWebRequestError("Failed to retrieve subscribed mod profiles\n", e, null));
        }

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
            if(this.userProfile != null)
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
            if(this.userProfile != null)
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
                    string installDir = ModManager.GetModInstallDirectory(p.id, p.activeBuild.id);
                    if(!Directory.Exists(installDir))
                    {
                        this.StartCoroutine(DownloadAndInstallModVersion(p.id, p.activeBuild.id));
                    }
                }
            },
            WebRequestError.LogAsWarning);
        }

        public void OnUnsubscribedFromMod(int modId)
        {
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
                            string installDir = ModManager.GetModInstallDirectory(p.id, p.activeBuild.id);
                            if(!Directory.Exists(installDir))
                            {
                                this.StartCoroutine(DownloadAndInstallModVersion(p.id, p.activeBuild.id));
                            }
                        }
                    },
                    WebRequestError.LogAsWarning);
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
            // - explorerView -
            explorerView.UpdateSubscriptionsDisplay();

            // - subscriptionsView -
            RequestSubscribedModProfiles(subscriptionsView.DisplayProfiles,
                                         (e) => MessageSystem.QueueWebRequestError("Failed to retrieve subscribed mod profiles\n", e, null));

            // - inspectorView -
            if(inspectorView.profile != null)
            {
                inspectorView.isModSubscribed = ModManager.GetSubscribedModIds().Contains(inspectorView.profile.id);
                inspectorView.UpdateIsSubscribedDisplay();
            }
        }

        public static void EnableMod(int modId)
        {
            IList<int> mods = ModManager.GetEnabledModIds();
            if(!mods.Contains(modId))
            {
                mods.Add(modId);
                ModManager.SetEnabledModIds(mods);
            }

            if(instance != null
               && instance.isActiveAndEnabled)
            {
                foreach(ModView view in ModBrowser.instance.IterateModViews())
                {
                    if(view.data.profile.modId == modId)
                    {
                        ModDisplayData data = view.data;
                        data.isModEnabled = true;
                        view.data = data;
                    }
                }
            }
        }

        public static void DisableMod(int modId)
        {
            IList<int> mods = ModManager.GetEnabledModIds();
            if(mods.Contains(modId))
            {
                mods.Remove(modId);
                ModManager.SetEnabledModIds(mods);
            }

            if(instance != null
               && instance.isActiveAndEnabled)
            {
                foreach(ModView view in ModBrowser.instance.IterateModViews())
                {
                    if(view.data.profile.modId == modId)
                    {
                        ModDisplayData data = view.data;
                        data.isModEnabled = false;
                        view.data = data;
                    }
                }
            }
        }

        // ---------[ EVENT HANDLING ]---------
        private void OnUserDisplayClicked(UserView view)
        {
            if(userProfile == null)
            {
                OpenLoginDialog();
            }
            else
            {
                LogUserOut();
            }
        }

        // private void OnModsAvailable(IEnumerable<ModProfile> addedProfiles)
        // {
        //     List<ModProfile> undisplayedProfiles = new List<ModProfile>(addedProfiles);
        //     List<int> cachedIds = modProfileCache.ConvertAll<int>(p => p.id);

        //     undisplayedProfiles.RemoveAll(p => cachedIds.Contains(p.id));

        //     this.modProfileCache.AddRange(undisplayedProfiles);

        //     LoadModPage();
        // }
        // private void OnModsEdited(IEnumerable<ModProfile> editedProfiles)
        // {
        //     List<ModProfile> editedProfileList = new List<ModProfile>(editedProfiles);
        //     List<int> editedIds = editedProfileList.ConvertAll<int>(p => p.id);

        //     this.modProfileCache.RemoveAll(p => editedIds.Contains(p.id));
        //     this.modProfileCache.AddRange(editedProfileList);

        //     LoadModPage();
        // }
        // private void OnModReleasesUpdated(IEnumerable<ModfileStub> modfiles)
        // {
        //     foreach(ModfileStub modfile in modfiles)
        //     {
        //         Debug.Log("Modfile Updated: " + modfile.version);
        //     }
        // }
        // private void OnModsUnavailable(IEnumerable<int> modIds)
        // {
        //     List<int> removedModIds = new List<int>(modIds);
        //     this.modProfileCache.RemoveAll(p => removedModIds.Contains(p.id));

        //     LoadModPage();
        // }
        // private void OnModsDeleted(IEnumerable<int> modIds)
        // {
        //     List<int> removedModIds = new List<int>(modIds);
        //     this.modProfileCache.RemoveAll(p => removedModIds.Contains(p.id));

        //     LoadModPage();
        // }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(!Application.isPlaying
                   && this != null)
                {
                    testServerSettings.apiURL = APIClient.API_URL_TESTSERVER + APIClient.API_VERSION;
                    productionServerSettings.apiURL = APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION;
                }
            };
        }
        #endif
    }
}
