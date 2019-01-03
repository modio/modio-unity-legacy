using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// TODO(@jackson): Clean up after removing IModBrowserView
// TODO(@jackson): Queue missed requests? (Unsub fail)
// TODO(@jackson): Correct subscription loading
// TODO(@jackson): Add user events
// TODO(@jackson): Error handling on log in
// TODO(@jackson): Update view function names (see FilterView)
namespace ModIO.UI
{
    public class ModBrowser : MonoBehaviour
    {
        // ---------[ NESTED CLASSES ]---------
        // TODO(@jackson): Replace with inspector dropdown
        public enum APIServer
        {
            TestServer,
            ProductionServer,
        }

        [Serializable]
        private class ManifestData
        {
            public int lastCacheUpdate = -1;
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
        }

        [Serializable]
        public struct APIData
        {
            public int gameId;
            public string gameAPIKey;
        }

        // ---------[ CONST & STATIC ]---------
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
        public APIData testServerData = new APIData();
        public APIData productionServerData = new APIData();
        public bool isAutomaticUpdateEnabled = false;
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

        [Header("Display Data")]
        public InspectorViewData inspectorData = new InspectorViewData();

        [Header("Runtime Data")]
        private UserProfile userProfile = null;
        private int lastCacheUpdate = -1;
        private RequestFilter explorerViewFilter = new RequestFilter();
        private SubscriptionViewFilter subscriptionViewFilter = new SubscriptionViewFilter();
        private List<ModBinaryRequest> modDownloads = new List<ModBinaryRequest>();
        private GameProfile gameProfile = null;


        // ---------[ ACCESSORS ]---------
        public void RequestExplorerPage(int pageIndex,
                                        Action<RequestPage<ModProfile>> onSuccess,
                                        Action<WebRequestError> onError)
        {
            // PaginationParameters
            APIPaginationParameters pagination = new APIPaginationParameters();
            pagination.limit = explorerView.ItemCount;
            pagination.offset = pageIndex * explorerView.ItemCount;

            // Send Request
            APIClient.GetAllMods(explorerViewFilter, pagination,
                                 onSuccess, onError);
        }

        public void RequestSubscriptionsPage(int pageIndex,
                                             Action<RequestPage<ModProfile>> onSuccess,
                                             Action<WebRequestError> onError)
        {
            int offset = pageIndex * subscriptionsView.TEMP_pageSize;
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            RequestPage<ModProfile> modPage = new RequestPage<ModProfile>()
            {
                size = subscriptionsView.TEMP_pageSize,
                resultOffset = offset,
                resultTotal = subscribedModIds.Count,
                items = null,
            };

            if(subscribedModIds.Count > offset)
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

                    int remainingModCount = filteredList.Count - offset;
                    if(remainingModCount > 0)
                    {
                        int arraySize = (int)Mathf.Min(modPage.size, remainingModCount);
                        modPage.items = new ModProfile[arraySize];

                        filteredList.Sort(subscriptionViewFilter.sortDelegate);
                        filteredList.CopyTo(0, modPage.items, 0, arraySize);
                    }

                    onSuccess(modPage);
                };

                ModManager.GetModProfiles(subscribedModIds,
                                          onGetModProfiles, onError);
            }
            else
            {
                modPage.items = new ModProfile[0];

                onSuccess(modPage);
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

            // --- APIClient ---
            APIClient.gameId = apiData.gameId;
            APIClient.gameAPIKey = apiData.gameAPIKey;
            APIClient.userAuthorizationToken = CacheClient.LoadAuthenticatedUserToken();

            // --- Manifest ---
            ManifestData manifest = CacheClient.ReadJsonObjectFile<ManifestData>(ModBrowser.manifestFilePath);
            if(manifest != null)
            {
                this.lastCacheUpdate = manifest.lastCacheUpdate;
            }

            // --- UserData ---
            this.userProfile = CacheClient.LoadAuthenticatedUserProfile();

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
            // TODO(@jackson): Add Enable/Disable
            inspectorView.gameObject.SetActive(false);

            UpdateInspectorViewPageButtonInteractibility();
        }

        private void InitializeSubscriptionsView()
        {
            // TODO(@jackson): Update displays on subscribe/unsubscribe
            subscriptionsView.Initialize();

            // TODO(@jackson): Hook up events
            subscriptionsView.inspectRequested += InspectSubscriptionItem;
            subscriptionsView.subscribeRequested += (v) => SubscribeToMod(v.data.profile.modId);
            subscriptionsView.unsubscribeRequested += (v) => UnsubscribeFromMod(v.data.profile.modId);
            subscriptionsView.enableModRequested += (v) => EnableMod(v.data.profile.modId);
            subscriptionsView.disableModRequested += (v) => DisableMod(v.data.profile.modId);

            // - setup ui filter controls -
            // TODO(@jackson): nameSearchField.onValueChanged.AddListener((t) => {});
            if(subscriptionsView.nameSearchField != null)
            {
                subscriptionsView.nameSearchField.onEndEdit.AddListener((t) =>
                {
                    if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        UpdateSubscriptionFilters();
                    }
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
            RequestPage<ModProfile> modPage = new RequestPage<ModProfile>()
            {
                size = subscriptionsView.TEMP_pageSize,
                resultOffset = 0,
                resultTotal = 0,
                items = new ModProfile[0],
            };
            subscriptionsView.currentPage = modPage;

            RequestSubscriptionsPage(0,
                                     (page) =>
                                     {
                                        if(subscriptionsView.currentPage == modPage)
                                        {
                                            subscriptionsView.currentPage = page;
                                            subscriptionsView.UpdateCurrentPageDisplay();
                                        }
                                    },
                                    null);

            subscriptionsView.UpdateCurrentPageDisplay();
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
            // TODO(@jackson): nameSearchField.onValueChanged.AddListener((t) => {});
            if(explorerView.nameSearchField != null)
            {
                explorerView.nameSearchField.onEndEdit.AddListener((t) =>
                {
                    if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        UpdateExplorerFilters();
                    }
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

            RequestPage<ModProfile> modPage = new RequestPage<ModProfile>()
            {
                size = explorerView.ItemCount,
                items = new ModProfile[explorerView.ItemCount],
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
                LogUserIn(t, false);
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
                // callbacks
                Action<UserProfile> onGetUserProfile = (u) =>
                {
                    this.userProfile = u;

                    if(this.loggedUserView != null)
                    {
                        this.loggedUserView.DisplayUser(u);
                    }
                };

                // TODO(@jackson): DO BETTER - (CACHE?! DOWNLOADS?)
                Action<RequestPage<ModProfile>> onGetSubscriptions = (r) =>
                {
                    List<int> subscribedModIds = new List<int>(r.items.Length);
                    foreach(var modProfile in r.items)
                    {
                        subscribedModIds.Add(modProfile.id);
                    }
                    ModManager.SetSubscribedModIds(subscribedModIds);

                    UpdateViewSubscriptions();
                };

                // requests
                ModManager.GetAuthenticatedUserProfile(onGetUserProfile,
                                                       null);

                RequestFilter filter = new RequestFilter();
                filter.fieldFilters.Add(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                        new EqualToFilter<int>(){ filterValue = apiData.gameId });

                APIClient.GetUserSubscriptions(filter, null, onGetSubscriptions, null);
            }
        }

        // ---------[ UPDATES ]---------
        private const float AUTOMATIC_UPDATE_INTERVAL = 15f;
        private bool isUpdateRunning = false;

        private void Update()
        {
            if(this.isAutomaticUpdateEnabled)
            {
                if(!isUpdateRunning
                   && (ServerTimeStamp.Now - this.lastCacheUpdate) >= AUTOMATIC_UPDATE_INTERVAL)
                {
                    PollForServerUpdates();
                }
            }

            // if(activeDownload != null)
            // {
            //     float downloaded = 0f;
            //     if(activeDownload.webRequest != null)
            //     {
            //         downloaded = activeDownload.webRequest.downloadProgress * 100f;
            //     }

            //     inspectorView.installButtonText.text = "Downloading [" + downloaded.ToString("00.00") + "%]";
            // }
        }

        protected void PollForServerUpdates()
        {
            Debug.Assert(!isUpdateRunning);

            this.isUpdateRunning = true;

            int updateStartTimeStamp = ServerTimeStamp.Now;

            ModManager.FetchAllModEvents(this.lastCacheUpdate, updateStartTimeStamp,
                                         (me) => { this.ProcessUpdates(me, updateStartTimeStamp); },
                                         (e) => { WebRequestError.LogAsWarning(e); this.isUpdateRunning = false; });

            // TODO(@jackson): Add User Events
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
                    this.isUpdateRunning = false;

                    ManifestData manifest = new ManifestData()
                    {
                        lastCacheUpdate = this.lastCacheUpdate,
                    };

                    CacheClient.WriteJsonObjectFile(ModBrowser.manifestFilePath, manifest);

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
                    this.isUpdateRunning = false;
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
                this.isUpdateRunning = false;

                ManifestData manifest = new ManifestData()
                {
                    lastCacheUpdate = this.lastCacheUpdate,
                };

                CacheClient.WriteJsonObjectFile(ModBrowser.manifestFilePath, manifest);
            }
        }

        // ---------[ USER CONTROL ]---------
        public void LogUserIn(string oAuthToken, bool clearExistingSubscriptions)
        {
            Debug.Assert(!String.IsNullOrEmpty(oAuthToken),
                         "[mod.io] ModBrowser.LogUserIn requires a valid oAuthToken");

            StartCoroutine(UserLoginCoroutine(oAuthToken, clearExistingSubscriptions));
        }

        private IEnumerator UserLoginCoroutine(string oAuthToken, bool clearExistingSubscriptions)
        {
            bool isRequestDone = false;
            WebRequestError requestError = null;

            // NOTE(@jackson): Could be much improved by not deleting matching mod subscription files
            if(clearExistingSubscriptions)
            {
                foreach(int modId in ModManager.GetSubscribedModIds())
                {
                    // remove from disk
                    CacheClient.DeleteAllModfileAndBinaryData(modId);
                }
                ModManager.SetSubscribedModIds(null);

                subscriptionsView.currentPage = new RequestPage<ModProfile>()
                {
                    size = subscriptionsView.TEMP_pageSize,
                    items = new ModProfile[0],
                    resultOffset = 0,
                    resultTotal = 0,
                };
                subscriptionsView.UpdateCurrentPageDisplay();
            }

            // - set APIClient and CacheClient vars -
            APIClient.userAuthorizationToken = oAuthToken;
            CacheClient.SaveAuthenticatedUserToken(oAuthToken);

            // - get the user profile -
            UserProfile requestProfile = null;
            APIClient.GetAuthenticatedUser((p) => { isRequestDone = true; requestProfile = p; },
                                           (e) => { isRequestDone = true; requestError = e; });

            while(!isRequestDone) { yield return null; }

            if(requestError != null)
            {
                throw new System.NotImplementedException();
                // return;
            }

            CacheClient.SaveAuthenticatedUserProfile(requestProfile);
            this.userProfile = requestProfile;
            if(this.loggedUserView != null)
            {
                loggedUserView.DisplayUser(this.userProfile);
            }

            // - get server subscriptions -
            List<int> remoteSubscriptions = new List<int>();
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

                foreach(ModProfile profile in requestPage.items)
                {
                    if(!subscriptionsToPush.Contains(profile.id))
                    {
                        remoteSubscriptions.Add(profile.id);

                        // begin download
                        ModBinaryRequest binaryRequest = ModManager.RequestCurrentRelease(profile);

                        if(!binaryRequest.isDone)
                        {
                            binaryRequest.succeeded += (r) =>
                            {
                                Debug.Log(profile.name + " Downloaded!");
                                modDownloads.Remove(binaryRequest);
                            };

                            binaryRequest.failed += (r) =>
                            {
                                Debug.Log(profile.name + " Download Failed!");
                                modDownloads.Remove(binaryRequest);
                            };

                            modDownloads.Add(binaryRequest);
                        }
                    }

                    subscriptionsToPush.Remove(profile.id);
                }

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

            List<int> subscribedModIds = new List<int>(ModManager.GetSubscribedModIds());
            subscribedModIds.AddRange(remoteSubscriptions);
            ModManager.SetSubscribedModIds(subscribedModIds);
        }

        public void LogUserOut()
        {
            // - clear current user -
            APIClient.userAuthorizationToken = null;
            CacheClient.DeleteAuthenticatedUser();

            foreach(int modId in ModManager.GetSubscribedModIds())
            {
                CacheClient.DeleteAllModfileAndBinaryData(modId);
            }
            ModManager.SetSubscribedModIds(null);

            // - set up guest account -
            this.userProfile = null;
            if(this.loggedUserView != null)
            {
                this.loggedUserView.data = guestData;
            }

            // - clear subscription view -
            RequestPage<ModProfile> modPage = new RequestPage<ModProfile>()
            {
                size = subscriptionsView.TEMP_pageSize,
                resultOffset = 0,
                resultTotal = 0,
                items = new ModProfile[0],
            };
            subscriptionsView.currentPage = modPage;
            subscriptionsView.UpdateCurrentPageDisplay();

            // - notify -
            MessageSystem.QueueMessage(MessageDisplayData.Type.Success,
                                       messageStrings.userLoggedOut);
        }

        // ---------[ UI CONTROL ]---------
        // ---[ VIEW MANAGEMENT ]---
        public void ShowInspectorView()
        {
            inspectorView.gameObject.SetActive(true);
            explorerView.gameObject.SetActive(false);
            subscriptionsView.gameObject.SetActive(false);

            if(explorerViewButton != null)
            {
                explorerViewButton.isOn = false;
                explorerViewButton.interactable = true;
            }
            if(subscriptionsViewButton != null)
            {
                subscriptionsViewButton.isOn = false;
                subscriptionsViewButton.interactable = true;
            }
        }

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

        public void ChangeInspectorPage(int direction)
        {
            int firstExplorerIndex = (explorerView.CurrentPageNumber-1) * explorerView.ItemCount;
            int newModIndex = inspectorData.currentModIndex + direction;
            int offsetIndex = newModIndex - firstExplorerIndex;

            ModProfile profile;

            // profile
            if(offsetIndex < 0)
            {
                ChangeExplorerPage(-1);

                offsetIndex += explorerView.ItemCount;
                profile = explorerView.targetPage.items[offsetIndex];
            }
            else if(offsetIndex >= explorerView.ItemCount)
            {
                ChangeExplorerPage(1);

                offsetIndex -= explorerView.ItemCount;
                profile = explorerView.targetPage.items[offsetIndex];
            }
            else
            {
                profile = explorerView.currentPage.items[offsetIndex];
            }

            SetInspectorViewProfile(profile);
        }

        public void SetInspectorViewProfile(ModProfile profile)
        {
            // profile
            inspectorView.profile = profile;
            inspectorView.UpdateProfileDisplay();

            // statistics
            inspectorView.statistics = null;
            ModManager.GetModStatistics(inspectorView.profile.id,
                                        (s) =>
                                        {
                                            inspectorView.statistics = s;
                                            inspectorView.UpdateStatisticsDisplay();
                                        },
                                        WebRequestError.LogAsWarning);

            // subscription
            inspectorView.isModSubscribed = ModManager.GetSubscribedModIds().Contains(inspectorView.profile.id);
            inspectorView.UpdateIsSubscribedDisplay();

            // inspectorView stuff
            inspectorData.currentModIndex = -1;

            if(inspectorView.scrollView != null) { inspectorView.scrollView.verticalNormalizedPosition = 1f; }

            UpdateInspectorViewPageButtonInteractibility();
        }

        public void ChangeExplorerPage(int direction)
        {
            // TODO(@jackson): Queue on isTransitioning?
            if(explorerView.isTransitioning)
            {
                Debug.LogWarning("[mod.io] Cannot change during transition");
                return;
            }

            int targetPageIndex = explorerView.CurrentPageNumber - 1 + direction;
            int targetPageProfileOffset = targetPageIndex * explorerView.ItemCount;

            Debug.Assert(targetPageIndex >= 0);
            Debug.Assert(targetPageIndex < explorerView.CurrentPageCount);

            int pageItemCount = (int)Mathf.Min(explorerView.ItemCount,
                                               explorerView.currentPage.resultTotal - targetPageProfileOffset);

            RequestPage<ModProfile> targetPage = new RequestPage<ModProfile>()
            {
                size = explorerView.ItemCount,
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
            // TODO(@jackson): Load explorer page
            inspectorData.currentModIndex = (view.gameObject.GetComponent<ModBrowserItem>().index
                                             + explorerView.currentPage.resultOffset);

            if(inspectorView.backToDiscoverButton != null)
            {
                inspectorView.backToDiscoverButton.gameObject.SetActive(true);
            }
            if(inspectorView.backToSubscriptionsButton != null)
            {
                inspectorView.backToSubscriptionsButton.gameObject.SetActive(false);
            }

            inspectorView.DisplayLoading();

            ModManager.GetModProfile(view.data.profile.modId,
                                     SetInspectorViewProfile,
                                     WebRequestError.LogAsWarning);

            ShowInspectorView();
        }

        public void InspectSubscriptionItem(ModView view)
        {
            // TODO(@jackson): Load explorer page
            inspectorData.currentModIndex = (view.gameObject.GetComponent<ModBrowserItem>().index
                                             + subscriptionsView.currentPage.resultOffset);

            if(inspectorView.backToSubscriptionsButton != null)
            {
                inspectorView.backToSubscriptionsButton.gameObject.SetActive(true);
            }
            if(inspectorView.backToDiscoverButton != null)
            {
                inspectorView.backToDiscoverButton.gameObject.SetActive(false);
            }

            inspectorView.DisplayLoading();

            ModManager.GetModProfile(view.data.profile.modId,
                                     SetInspectorViewProfile,
                                     WebRequestError.LogAsWarning);

            ShowInspectorView();
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

            // TODO(@jackson): BAD ZERO?
            RequestPage<ModProfile> filteredPage = new RequestPage<ModProfile>()
            {
                size = explorerView.ItemCount,
                items = new ModProfile[explorerView.ItemCount],
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
            RequestSubscriptionsPage(0,
                                     (page) =>
                                     {
                                        subscriptionsView.currentPage = page;
                                        subscriptionsView.UpdateCurrentPageDisplay();
                                     },
                                     WebRequestError.LogAsWarning);
        }

        // TODO(@jackson): THIS IS TERRIBLE, I LIKELY ALREADY HAVE THIS DATA!
        public void SubscribeToMod(int modId)
        {
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            // early out
            if(subscribedModIds.Contains(modId)) { return; }

            // update collection
            subscribedModIds.Add(modId);
            ModManager.SetSubscribedModIds(subscribedModIds);

            // sub
            Action<WebRequestError> onSubscribeFailed = (e) =>
            {
                WebRequestError.LogAsWarning(e);

                IList<int> subMods = ModManager.GetSubscribedModIds();
                subMods.Remove(modId);
                ModManager.SetSubscribedModIds(subMods);
            };

            if(this.userProfile == null)
            {
                ModManager.GetModProfile(modId,
                                         OnSubscribedToMod,
                                         onSubscribeFailed);
            }
            else
            {
                APIClient.SubscribeToMod(modId,
                                         OnSubscribedToMod,
                                         onSubscribeFailed);
            }
        }

        public void OnSubscribedToMod(ModProfile profile)
        {
            Debug.Assert(profile != null);

            // begin download
            ModBinaryRequest request = ModManager.RequestCurrentRelease(profile);

            // TODO(@jackson): Dirty hack (now less dirty???)
            ModBinaryDownloadDisplay[] sceneDownloadDisplays
                = Resources.FindObjectsOfTypeAll<ModBinaryDownloadDisplay>();
            foreach(ModBinaryDownloadDisplay downloadDisplay in sceneDownloadDisplays)
            {
                if(downloadDisplay.modId == profile.id)
                {
                    downloadDisplay.gameObject.SetActive(true);
                    downloadDisplay.DisplayRequest(request);
                }
            }

            if(!request.isDone)
            {
                modDownloads.Add(request);

                request.succeeded += (r) =>
                {
                    modDownloads.Remove(request);
                };
            }

            UpdateViewSubscriptions();
        }

        public void UnsubscribeFromMod(int modId)
        {
            IList<int> subscribedModIds = ModManager.GetSubscribedModIds();

            // early out
            if(!subscribedModIds.Contains(modId)) { return; }

            // update collection
            subscribedModIds.Remove(modId);
            ModManager.SetSubscribedModIds(subscribedModIds);

            // unsub
            if(this.userProfile == null)
            {
                OnUnsubscribedFromMod(modId);
            }
            else
            {
                Action<WebRequestError> onSubscribeFailed = (e) =>
                {
                    WebRequestError.LogAsWarning(e);

                    IList<int> subMods = ModManager.GetSubscribedModIds();
                    subMods.Add(modId);
                    ModManager.SetSubscribedModIds(subMods);
                };

                APIClient.UnsubscribeFromMod(modId,
                                             () => OnUnsubscribedFromMod(modId),
                                             onSubscribeFailed);
            }
        }

        public void OnUnsubscribedFromMod(int modId)
        {
            // remove from disk
            CacheClient.DeleteAllModfileAndBinaryData(modId);

            UpdateViewSubscriptions();
        }

        private void UpdateViewSubscriptions()
        {
            // - explorerView -
            explorerView.UpdateSubscriptionsDisplay();

            // - subscriptionsView -
            RequestPage<ModProfile> modPage = subscriptionsView.currentPage;
            RequestSubscriptionsPage(modPage.resultOffset,
                                     (page) =>
                                     {
                                        if(subscriptionsView.currentPage == modPage)
                                        {
                                            subscriptionsView.currentPage = page;
                                            subscriptionsView.UpdateCurrentPageDisplay();
                                        }
                                    },
                                    null);

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
    }
}
