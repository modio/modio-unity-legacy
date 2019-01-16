// #define MEEPLESTATION_AUTO_INSTALL

using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        // TODO(@jackson): Add "custom"
        public enum APIServer
        {
            TestServer,
            ProductionServer,
        }

        [Serializable]
        private class ManifestData
        {
            public int lastCacheUpdate = -1;
            public int lastUserUpdate = -1;
            public List<int> queuedUnsubscribes = new List<int>();
            public List<int> queuedSubscribes = new List<int>();
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

        [Serializable]
        public struct APIData
        {
            public string   apiURL;
            public int      gameId;
            public string   gameAPIKey;
        }

        // ---------[ CONST & STATIC ]---------
        private const float AUTOMATIC_UPDATE_INTERVAL = 15f;

        public static string manifestFilePath { get { return CacheClient.GetCacheDirectory() + "browser_manifest.data"; } }
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
        public APIServer connectTo = APIServer.TestServer;
        // TODO(@jackson): Custom inspector hide
        public APIData testServerData = new APIData()
        {
            // TODO(@jackson): Make read-only in inspector
            apiURL = APIClient.API_URL_TESTSERVER + APIClient.API_VERSION,
            gameId = 0,
            gameAPIKey = string.Empty,
        };
        public APIData productionServerData = new APIData()
        {
            // TODO(@jackson): Make read-only in inspector
            apiURL = APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION,
            gameId = 0,
            gameAPIKey = string.Empty,
        };
        public UserDisplayData guestData = new UserDisplayData()
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
        private int lastCacheUpdate = -1;
        private int lastUserUpdate = -1;
        private RequestFilter explorerViewFilter = new RequestFilter();
        private SubscriptionViewFilter subscriptionViewFilter = new SubscriptionViewFilter();
        private GameProfile gameProfile = null;
        private Coroutine m_updatesCoroutine = null;
        private List<int> m_queuedUnsubscribes = new List<int>();
        private List<int> m_queuedSubscribes = new List<int>();

        // ---------[ ACCESSORS ]---------
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

        public APIData apiData
        {
            get
            {
                if(connectTo == APIServer.TestServer)
                {
                    return testServerData;
                }
                else
                {
                    return productionServerData;
                }
            }
            set
            {
                if(connectTo == APIServer.TestServer)
                {
                    testServerData = value;
                }
                else
                {
                    productionServerData = value;
                }
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            _instance = this;
            m_updatesCoroutine = StartCoroutine(PollForUpdatesCoroutine());
        }

        private void OnDisable()
        {
            if(m_updatesCoroutine != null)
            {
                StopCoroutine(m_updatesCoroutine);
            }

            _instance = null;
        }

        private void Start()
        {
            #if MODIO_TESTING
            if(!(apiData.gameId == 0 && String.IsNullOrEmpty(apiData.gameAPIKey)))
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

            // TODO(@jackson): TEMP
            #if MEEPLESTATION_AUTO_INSTALL
            DownloadClient.modfileDownloadSucceeded += (p, d) =>
            {
                string unzipLocation = (CacheClient.GetCacheDirectory()
                                        + "_installedMods/"
                                        + p.modId.ToString() + "/");

                CacheClient.DeleteDirectory(unzipLocation);

                ModManager.UnzipModBinaryToLocation(p.modId, p.modfileId,
                                                    unzipLocation);
            };
            #endif

            StartFetchRemoteData();
        }

        private void LoadLocalData()
        {
            APIData d = this.apiData;

            #pragma warning disable 0162
            if(d.gameId <= 0)
            {
                if(GlobalSettings.GAME_ID <= 0)
                {
                    Debug.LogError("[mod.io] Game ID is missing. Save it to GlobalSettings or this MonoBehaviour before starting the app",
                                   this);
                    return;
                }

                d.gameId = GlobalSettings.GAME_ID;
            }
            if(String.IsNullOrEmpty(d.gameAPIKey))
            {
                if(String.IsNullOrEmpty(GlobalSettings.GAME_APIKEY))
                {
                    Debug.LogError("[mod.io] Game API Key is missing. Save it to GlobalSettings or this MonoBehaviour before starting the app",
                                   this);
                    return;
                }

                d.gameAPIKey = GlobalSettings.GAME_APIKEY;
            }
            #pragma warning restore 0162

            this.apiData = d;

            // --- User Data ---
            UserAuthenticationData userData = ModManager.GetUserData();

            // --- APIClient ---
            APIClient.apiURL = apiData.apiURL;
            APIClient.gameId = apiData.gameId;
            APIClient.gameAPIKey = apiData.gameAPIKey;
            APIClient.userAuthorizationToken = userData.token;

            // --- Manifest ---
            ManifestData manifest = CacheClient.ReadJsonObjectFile<ManifestData>(ModBrowser.manifestFilePath);
            if(manifest != null)
            {
                this.lastCacheUpdate = manifest.lastCacheUpdate;
                this.lastUserUpdate = manifest.lastUserUpdate;
                this.m_queuedSubscribes = manifest.queuedSubscribes;
                this.m_queuedUnsubscribes = manifest.queuedUnsubscribes;
            }

            // --- UserData ---
            if(userData.userId > 0)
            {
                this.userProfile = CacheClient.LoadUserProfile(userData.userId);
            }

            // --- GameData ---
            this.gameProfile = CacheClient.LoadGameProfile();
            if(this.gameProfile == null)
            {
                this.gameProfile = new GameProfile();
                this.gameProfile.id = apiData.gameId;
            }
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
                    loggedUserView.data = guestData;
                }
                else
                {
                    loggedUserView.DisplayUser(userProfile);
                }

                loggedUserView.onClick += OnUserDisplayClicked;
            }
        }

        private void StartFetchRemoteData()
        {
            // --- GameProfile ---
            ModManager.GetGameProfile(
            (g) =>
            {
                gameProfile = g;
                explorerView.tagCategories = g.tagCategories;
                subscriptionsView.tagCategories = g.tagCategories;
                inspectorView.tagCategories = g.tagCategories;
            },
            WebRequestError.LogAsWarning);

            // --- UserData ---
            if(!String.IsNullOrEmpty(APIClient.userAuthorizationToken))
            {
                int requestTimeStamp = ServerTimeStamp.Now;

                // callbacks
                Action<UserProfile> onGetUserProfile = (u) =>
                {
                    this.userProfile = u;

                    if(this.loggedUserView != null)
                    {
                        this.loggedUserView.DisplayUser(u);
                    }
                };

                // requests
                ModManager.GetAuthenticatedUserProfile(onGetUserProfile, null);

                RequestFilter filter = new RequestFilter();
                filter.fieldFilters.Add(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                        new EqualToFilter<int>(){ filterValue = apiData.gameId });

                APIClient.GetUserSubscriptions(filter, null,
                                               (r) =>
                                               {
                                                ApplyRetrievedSubscriptions(r);
                                                this.lastUserUpdate = requestTimeStamp;
                                                WriteManifest();
                                               },
                                               null);
            }
        }

        private void ApplyRetrievedSubscriptions(RequestPage<ModProfile> response)
        {
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            // - filter for added / removed -
            List<int> removedSubscriptions = new List<int>(subscribedModIds);
            List<ModProfile> addedSubscriptions = new List<ModProfile>();

            foreach(var modProfile in response.items)
            {
                if(!subscribedModIds.Contains(modProfile.id))
                {
                    addedSubscriptions.Add(modProfile);
                    subscribedModIds.Add(modProfile.id);
                }
                removedSubscriptions.Remove(modProfile.id);
            }

            // TODO(@jackson): Optimize?
            foreach(int modId in removedSubscriptions)
            {
                subscribedModIds.Remove(modId);
            }

            // - apply added / removed -
            if(addedSubscriptions.Count > 0 || removedSubscriptions.Count > 0)
            {
                ModManager.SetSubscribedModIds(subscribedModIds);

                foreach(int modId in removedSubscriptions)
                {
                    // remove from disk
                    CacheClient.DeleteAllModfileAndBinaryData(modId);
                }

                foreach(ModProfile profile in addedSubscriptions)
                {
                    AssertModBinaryIsDownloaded(profile.id, profile.activeBuild.id);
                }

                int subscriptionUpdateCount = (addedSubscriptions.Count + removedSubscriptions.Count);
                string message = messageStrings.subscriptionsRetrieved.Replace("$UPDATE_COUNT$", subscriptionUpdateCount.ToString());
                MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);

                UpdateViewSubscriptions();
            }
        }

        // ---------[ UPDATES ]---------
        private System.Collections.IEnumerator PollForUpdatesCoroutine()
        {
            while(true)
            {
                yield return new WaitForSeconds(AUTOMATIC_UPDATE_INTERVAL);

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
                    // TODO(@jackson): Localize
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                               "Error fetching mod updates.\n"
                                               + requestError.message);
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
                if(userProfile != null)
                {
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
                                                     WebRequestError.LogAsWarning);
                    }

                    // fetch user events
                    List<UserEvent> userEventReponse = null;
                    ModManager.FetchAllUserEvents(lastUserUpdate,
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
                        // TODO(@jackson): Localize
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Error synchronizing user data.\n"
                                                   + requestError.message);
                    }
                    else
                    {
                        ProcessUserUpdates(userEventReponse);
                        this.lastUserUpdate = updateStartTimeStamp;
                        WriteManifest();
                    }
                }
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
                           && !m_queuedSubscribes.Contains(ue.modId))
                        {
                            addedSubscriptions.Add(ue.modId);
                            subscribedModIds.Add(ue.modId);
                        }
                    }
                    break;

                    case UserEventType.ModUnsubscribed:
                    {
                        if(subscribedModIds.Contains(ue.modId)
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

                foreach(int modId in removedSubscriptions)
                {
                    // remove from disk
                    CacheClient.DeleteAllModfileAndBinaryData(modId);
                }

                if(addedSubscriptions.Count > 0)
                {
                    Action<List<ModProfile>> assertBinariesAreDownloaded = (addedProfiles) =>
                    {
                        foreach(ModProfile profile in addedProfiles)
                        {
                            AssertModBinaryIsDownloaded(profile.id, profile.activeBuild.id);
                        }
                    };

                    // TODO(@jackson): Handle Error
                    ModManager.GetModProfiles(addedSubscriptions,
                                              assertBinariesAreDownloaded,
                                              WebRequestError.LogAsWarning);
                }

                int subscriptionUpdateCount = (addedSubscriptions.Count + removedSubscriptions.Count);
                string message = messageStrings.subscriptionsRetrieved.Replace("$UPDATE_COUNT$", subscriptionUpdateCount.ToString());
                MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);

                UpdateViewSubscriptions();
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
                lastCacheUpdate = this.lastCacheUpdate,
                lastUserUpdate = this.lastUserUpdate,
                queuedUnsubscribes = this.m_queuedUnsubscribes,
                queuedSubscribes = this.m_queuedSubscribes,
            };

            CacheClient.WriteJsonObjectFile(ModBrowser.manifestFilePath, manifest);
        }

        // TODO(@jackson): Incomplete
        protected void OnUpdateError(WebRequestError error)
        {
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
            bool isRequestDone = false;
            WebRequestError requestError = null;

            // - set APIClient var -
            APIClient.userAuthorizationToken = oAuthToken;

            // - get the user profile -
            UserProfile requestProfile = null;
            APIClient.GetAuthenticatedUser((p) => { isRequestDone = true; requestProfile = p; },
                                           (e) => { isRequestDone = true; requestError = e; });

            while(!isRequestDone) { yield return null; }

            if(requestError != null)
            {
                APIClient.userAuthorizationToken = string.Empty;
                throw new System.NotImplementedException();
                // return;
            }

            // - save user data -
            ModManager.SetUserData(requestProfile.id, oAuthToken);

            CacheClient.SaveUserProfile(requestProfile);
            this.userProfile = requestProfile;
            if(this.loggedUserView != null)
            {
                loggedUserView.DisplayUser(this.userProfile);
            }

            // - get server subscriptions -
            IList<int> subscriptionsToPush = ModManager.GetSubscribedModIds();
            bool allPagesReceived = false;

            RequestFilter subscriptionFilter = new RequestFilter();
            subscriptionFilter.fieldFilters.Add(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                                new EqualToFilter<int>() { filterValue = apiData.gameId });

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            RequestPage<ModProfile> requestPage = null;
            while(!allPagesReceived)
            {
                isRequestDone = false;
                requestError = null;
                requestPage = null;

                APIClient.GetUserSubscriptions(subscriptionFilter, pagination,
                                               (r) => { isRequestDone = true; requestPage = r; },
                                               (e) => { isRequestDone = true; requestError = e; });

                while(!isRequestDone)
                {
                    yield return null;
                }

                if(requestError != null)
                {
                    throw new System.NotImplementedException();
                    // return?
                }

                CacheClient.SaveModProfiles(requestPage.items);

                List<int> remoteSubscriptions = new List<int>();
                foreach(ModProfile profile in requestPage.items)
                {
                    if(!subscriptionsToPush.Contains(profile.id))
                    {
                        remoteSubscriptions.Add(profile.id);
                        EnableMod(profile.id);
                        AssertModBinaryIsDownloaded(profile.id, profile.activeBuild.id);
                    }

                    subscriptionsToPush.Remove(profile.id);
                }

                List<int> subscribedModIds = new List<int>(ModManager.GetSubscribedModIds());
                subscribedModIds.AddRange(remoteSubscriptions);
                ModManager.SetSubscribedModIds(subscribedModIds);
                UpdateViewSubscriptions();

                allPagesReceived = (requestPage.items.Length < requestPage.size);

                if(!allPagesReceived)
                {
                    pagination.offset += pagination.limit;
                }
            }

            // - push missing subscriptions -
            foreach(int modId in subscriptionsToPush)
            {
                APIClient.SubscribeToMod(modId,
                                         (p) => Debug.Log("[mod.io] Mod subscription merged: " + p.id + "-" + p.name),
                                         (e) => Debug.Log("[mod.io] Mod subscription merge failed: " + modId + "\n"
                                                          + e.ToUnityDebugString()));
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
            CacheClient.DeleteAuthenticatedUser();

            // - set up guest account -
            this.userProfile = null;
            if(this.loggedUserView != null)
            {
                this.loggedUserView.data = guestData;
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
                m_queuedSubscribes.Add(modId);
                WriteManifest();
            }
        }

        public void OnSubscribedToMod(int modId)
        {
            EnableMod(modId);
            UpdateViewSubscriptions();

            // TODO(@jackson): Record missing binary
            ModManager.GetModProfile(modId,
                                     (p) =>
                                     {
                                        AssertModBinaryIsDownloaded(p.id, p.activeBuild.id);
                                     },
                                     WebRequestError.LogAsWarning);

        }

        private void AssertModBinaryIsDownloaded(int modId, int modfileId)
        {
            if(!ModManager.IsBinaryDownloaded(modId, modfileId))
            {
                FileDownloadInfo downloadInfo = DownloadClient.GetActiveModBinaryDownload(modId, modfileId);

                if(downloadInfo == null)
                {
                    string zipFilePath = CacheClient.GenerateModBinaryZipFilePath(modId, modfileId);
                    DownloadClient.StartModBinaryDownload(modId, modfileId, zipFilePath);

                    downloadInfo = DownloadClient.GetActiveModBinaryDownload(modId, modfileId);
                }

                if(!downloadInfo.isDone)
                {
                    // TODO(@jackson): Dirty hack (now less dirty???)
                    ModView[] sceneViews = Resources.FindObjectsOfTypeAll<ModView>();
                    foreach(ModView modView in sceneViews)
                    {
                        if(modView.data.profile.modId == modId)
                        {
                            modView.DisplayDownload(downloadInfo);
                        }
                    }
                }
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
                m_queuedUnsubscribes.Add(modId);
                WriteManifest();
            }
        }

        public void OnUnsubscribedFromMod(int modId)
        {
            // remove from disk
            CacheClient.DeleteAllModfileAndBinaryData(modId);
            DisableMod(modId);
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

                // TODO(@jackson): Fire event
            }

            // TODO(@jackson): ugh?
            ModView[] sceneModViews = Resources.FindObjectsOfTypeAll<ModView>();
            foreach(ModView view in sceneModViews)
            {
                if(view.data.profile.modId == modId)
                {
                    ModDisplayData data = view.data;
                    data.isModEnabled = true;
                    view.data = data;
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

                // TODO(@jackson): Fire event
            }

            // TODO(@jackson): ugh?
            ModView[] sceneModViews = Resources.FindObjectsOfTypeAll<ModView>();
            foreach(ModView view in sceneModViews)
            {
                if(view.data.profile.modId == modId)
                {
                    ModDisplayData data = view.data;
                    data.isModEnabled = false;
                    view.data = data;
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

        // // ---------[ UI FUNCTIONALITY ]---------
        // protected virtual void LoadModPage()
        // {
        //     int pageSize = Mathf.Min(thumbnailContainer.modThumbnails.Length, this.modProfileCache.Count);
        //     this.thumbnailContainer.modIds = new int[pageSize];

        //     int i;

        //     for(i = 0; i < pageSize; ++i)
        //     {
        //         int thumbnailIndex = i;
        //         this.thumbnailContainer.modIds[i] = this.modProfileCache[i].id;
        //         this.thumbnailContainer.modThumbnails[i].sprite = CreateSpriteFromTexture(loadingPlaceholder);
        //         this.thumbnailContainer.modThumbnails[i].gameObject.SetActive(true);

        //         ModManager.GetModLogo(this.modProfileCache[i], logoThumbnailVersion,
        //                               (t) => this.thumbnailContainer.modThumbnails[thumbnailIndex].sprite = CreateSpriteFromTexture(t),
        //                               null);
        //     }

        //     while(i < this.thumbnailContainer.modThumbnails.Length)
        //     {
        //         this.thumbnailContainer.modThumbnails[i].gameObject.SetActive(false);
        //         ++i;
        //     }
        // }

        // protected virtual void OnThumbClicked(int index)
        // {
        //     ModManager.GetModProfile(thumbnailContainer.modIds[index], OnGetInspectedProfile, null);
        // }

        // protected virtual void OnGetInspectedProfile(ModProfile profile)
        // {
        //     // - set up inspector ui -
        //     inspectedProfile = profile;

        //     inspectorView.title.text = profile.name;
        //     inspectorView.author.text = profile.submittedBy.username;
        //     inspectorView.logo.sprite = CreateSpriteFromTexture(loadingPlaceholder);

        //     List<int> userSubscriptions = CacheClient.LoadAuthenticatedUserSubscriptions();

        //     if(userSubscriptions != null
        //        && userSubscriptions.Contains(profile.id))
        //     {
        //         inspectorView.subscribeButtonText.text = "Unsubscribe";
        //     }
        //     else
        //     {
        //         inspectorView.subscribeButtonText.text = "Subscribe";
        //     }

        //     ModManager.GetModLogo(profile, logoInspectorVersion,
        //                           (t) => inspectorView.logo.sprite = CreateSpriteFromTexture(t),
        //                           null);

        //     inspectorView.installButton.gameObject.SetActive(false);
        //     inspectorView.downloadButtonText.text = "Verifying local data";
        //     inspectorView.downloadButton.gameObject.SetActive(true);
        //     inspectorView.downloadButton.interactable = false;

        //     // - check binary status -
        //     ModManager.GetDownloadedBinaryStatus(profile.activeBuild,
        //                                          (status) =>
        //                                          {
        //                                             if(status == ModBinaryStatus.CompleteAndVerified)
        //                                             {
        //                                                 inspectorView.downloadButton.gameObject.SetActive(false);
        //                                                 inspectorView.installButton.gameObject.SetActive(true);
        //                                             }
        //                                             else
        //                                             {
        //                                                 inspectorView.downloadButtonText.text = "Download";
        //                                                 inspectorView.downloadButton.interactable = true;
        //                                             }
        //                                          });

        //     // - finalize -
        //     isInspecting = true;
        //     thumbnailContainer.gameObject.SetActive(false);
        //     inspectorView.gameObject.SetActive(true);
        // }

        // protected virtual void OnBackClicked()
        // {
        //     if(isInspecting)
        //     {
        //         isInspecting = false;
        //         thumbnailContainer.gameObject.SetActive(true);
        //         inspectorView.gameObject.SetActive(false);
        //     }
        // }

        // protected virtual void OnSubscribeClicked()
        // {
        //     List<int> subscriptions = CacheClient.LoadAuthenticatedUserSubscriptions();

        //     if(subscriptions == null)
        //     {
        //         subscriptions = new List<int>(1);
        //     }

        //     int modId = inspectedProfile.id;
        //     int subscriptionIndex = subscriptions.IndexOf(modId);

        //     if(subscriptionIndex == -1)
        //     {
        //         subscriptions.Add(modId);
        //         inspectorView.subscribeButtonText.text = "Unsubscribe";

        //         if(userProfile != null)
        //         {
        //             APIClient.SubscribeToMod(inspectedProfile.id,
        //                                      null, null);
        //         }
        //     }
        //     else
        //     {
        //         subscriptions.RemoveAt(subscriptionIndex);
        //         inspectorView.subscribeButtonText.text = "Subscribe";

        //         if(userProfile != null)
        //         {
        //             APIClient.UnsubscribeFromMod(inspectedProfile.id,
        //                                          null, null);
        //         }
        //     }

        //     CacheClient.SaveAuthenticatedUserSubscriptions(subscriptions);

        //     OnDownloadClicked();
        // }

        // protected virtual void OnDownloadClicked()
        // {
        //     this.activeDownload = ModManager.GetActiveModBinary(inspectedProfile);

        //     if(this.activeDownload.isDone)
        //     {
        //         inspectorView.installButton.gameObject.SetActive(true);
        //         inspectorView.downloadButton.gameObject.SetActive(true);

        //         this.activeDownload = null;
        //     }
        //     else
        //     {
        //         inspectorView.downloadButtonText.text = "Initializing Download...";

        //         this.activeDownload.succeeded += (d) =>
        //         {
        //             inspectorView.installButton.gameObject.SetActive(true);
        //             inspectorView.downloadButton.gameObject.SetActive(true);

        //             this.activeDownload = null;
        //         };
        //         this.activeDownload.failed += (d) =>
        //         {
        //             inspectorView.installButton.gameObject.SetActive(true);
        //             inspectorView.downloadButton.gameObject.SetActive(true);

        //             this.activeDownload = null;
        //         };
        //     }
        // }

        // protected virtual void OnInstallClicked()
        // {
        //     ModProfile modProfile = this.inspectedProfile;
        //     string unzipLocation = null;

        //     if(String.IsNullOrEmpty(unzipLocation))
        //     {
        //         Debug.LogWarning("[mod.io] This is a placeholder for game specific code that handles the"
        //                          + " installing the mod");
        //     }
        //     else
        //     {
        //         ModManager.UnzipModBinaryToLocation(modProfile.activeBuild, unzipLocation);
        //         // Do install code
        //     }
        // }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(!Application.isPlaying
                   && this != null)
                {
                    testServerData.apiURL = APIClient.API_URL_TESTSERVER + APIClient.API_VERSION;
                    productionServerData.apiURL = APIClient.API_URL_PRODUCTIONSERVER + APIClient.API_VERSION;
                }
            };
        }
        #endif
    }
}
