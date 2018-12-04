using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ModIO;

// TODO(@jackson): Clean up after removing IModBrowserView
// TODO(@jackson): Queue missed requests? (Unsub fail)
// TODO(@jackson): Correct subscription loading
// TODO(@jackson): Add user events
// TODO(@jackson): Error handling on log in
// TODO(@jackson): Update view function names (see FilterView)
public class ModBrowser : MonoBehaviour
{
    // ---------[ NESTED CLASSES ]---------
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

    // ---------[ CONST & STATIC ]---------
    public static string manifestFilePath { get { return CacheClient.GetCacheDirectory() + "browser_manifest.data"; } }
    public static readonly UserProfile GUEST_PROFILE = new UserProfile()
    {
        id = 0,
        username = "Guest",
    };
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
                                                        diff += (IsModEnabled(a) ? -1 : 0);
                                                        diff += (IsModEnabled(b) ? 1 : 0);

                                                        if(diff == 0)
                                                        {
                                                            diff = String.Compare(a.name, b.name);
                                                        }

                                                        return diff;
                                                    } ),
    };

    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public int gameId = 0;
    public string gameAPIKey = string.Empty;
    public bool isAutomaticUpdateEnabled = false;

    [Header("UI Components")]
    public ExplorerView explorerView;
    public SubscriptionsView subscriptionsView;
    public InspectorView inspectorView;
    public ModBrowserUserDisplay userDisplay;
    public LoginDialog loginDialog;
    public MessageDialog messageDialog;
    public Button prevPageButton;
    public Button nextPageButton;

    [Header("Display Data")]
    public InspectorViewData inspectorData = new InspectorViewData();
    public List<int> subscribedModIds = new List<int>();
    public UserProfile userProfile = null;
    public List<string> filterTags = new List<string>();

    [Header("Runtime Data")]
    public int lastCacheUpdate = -1;
    public RequestFilter explorerViewFilter = new RequestFilter();
    private SubscriptionViewFilter subscriptionViewFilter = new SubscriptionViewFilter();
    public List<ModBinaryRequest> modDownloads = new List<ModBinaryRequest>();
    public GameProfile gameProfile = null;


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


    // ---------[ INITIALIZATION ]---------
    private void Start()
    {
        #pragma warning disable 0162
        if(this.gameId <= 0)
        {
            if(GlobalSettings.GAME_ID <= 0)
            {
                Debug.LogError("[mod.io] Game ID is missing. Save it to GlobalSettings or this MonoBehaviour before starting the app",
                               this);
                return;
            }

            this.gameId = GlobalSettings.GAME_ID;
        }
        if(String.IsNullOrEmpty(this.gameAPIKey))
        {
            if(String.IsNullOrEmpty(GlobalSettings.GAME_APIKEY))
            {
                Debug.LogError("[mod.io] Game API Key is missing. Save it to GlobalSettings or this MonoBehaviour before starting the app",
                               this);
                return;
            }

            this.gameAPIKey = GlobalSettings.GAME_APIKEY;
        }
        #pragma warning restore 0162

        LoadLocalData();

        InitializeInspectorView();
        InitializeSubscriptionsView();
        InitializeExplorerView();
        InitializeDialogs();

        if(userDisplay != null)
        {
            userDisplay.button.onClick.AddListener(OpenLoginDialog);
            userDisplay.profile = ModBrowser.GUEST_PROFILE;
            userDisplay.UpdateUIComponents();
        }

        StartFetchRemoteData();
    }

    private void LoadLocalData()
    {
        // --- APIClient ---
        APIClient.gameId = this.gameId;
        APIClient.gameAPIKey = this.gameAPIKey;
        APIClient.userAuthorizationToken = CacheClient.LoadAuthenticatedUserToken();

        // --- Manifest ---
        ManifestData manifest = CacheClient.ReadJsonObjectFile<ManifestData>(ModBrowser.manifestFilePath);
        if(manifest != null)
        {
            this.lastCacheUpdate = manifest.lastCacheUpdate;
        }

        // --- UserData ---
        this.userProfile = CacheClient.LoadAuthenticatedUserProfile();
        if(this.userProfile == null)
        {
            this.userProfile = ModBrowser.GUEST_PROFILE;
        }

        this.subscribedModIds = CacheClient.LoadAuthenticatedUserSubscriptions();
        if(this.subscribedModIds == null)
        {
            this.subscribedModIds = new List<int>();
        }

        // --- GameData ---
        this.gameProfile = CacheClient.LoadGameProfile();
        if(this.gameProfile == null)
        {
            this.gameProfile = new GameProfile();
            this.gameProfile.id = this.gameId;
        }
    }

    private void InitializeInspectorView()
    {
        inspectorView.Initialize();
        inspectorView.subscribeRequested += SubscribeToMod;
        inspectorView.unsubscribeRequested += UnsubscribeFromMod;
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
        subscriptionsView.subscribeRequested += (i) => SubscribeToMod(i.profile);
        subscriptionsView.unsubscribeRequested += (i) => UnsubscribeFromMod(i.profile);
        subscriptionsView.toggleModEnabledRequested += (i) => ToggleModEnabled(i.profile);

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
    }

    private void InitializeExplorerView()
    {
        explorerView.Initialize();

        explorerView.inspectRequested += InspectDiscoverItem;
        explorerView.subscribeRequested += (i) => SubscribeToMod(i.profile);
        explorerView.unsubscribeRequested += (i) => UnsubscribeFromMod(i.profile);
        explorerView.toggleModEnabledRequested += (i) => ToggleModEnabled(i.profile);

        explorerView.subscribedModIds = this.subscribedModIds;

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

        // tags
        if(explorerView.tagFilterView != null)
        {
            explorerView.tagFilterView.Initialize();
            explorerView.tagFilterView.selectedTags = this.filterTags;
            explorerView.tagFilterView.gameObject.SetActive(false);
            explorerView.tagFilterView.onSelectedTagsChanged += () =>
            {
                this.filterTags = new List<string>(explorerView.tagFilterView.selectedTags);
            };

            if(explorerView.tagFilterBar != null)
            {
                explorerView.tagFilterView.onSelectedTagsChanged += () =>
                {
                    if(explorerView.tagFilterBar.selectedTags != this.filterTags)
                    {
                        explorerView.tagFilterBar.selectedTags = this.filterTags;
                    }
                    explorerView.tagFilterBar.UpdateDisplay();
                };
            }

            explorerView.tagFilterView.onSelectedTagsChanged += () => UpdateExplorerFilters();
        }

        if(explorerView.tagFilterBar != null)
        {
            explorerView.tagFilterBar.Initialize();
            explorerView.tagFilterBar.selectedTags = this.filterTags;
            explorerView.tagFilterBar.gameObject.SetActive(true);
            explorerView.tagFilterBar.onSelectedTagsChanged += () =>
            {
                if(this.filterTags != explorerView.tagFilterBar.selectedTags)
                {
                    this.filterTags = explorerView.tagFilterBar.selectedTags;
                }
            };
            if(explorerView.tagFilterView != null)
            {
                explorerView.tagFilterBar.onSelectedTagsChanged += () =>
                {
                    if(explorerView.tagFilterView.selectedTags != this.filterTags)
                    {
                        explorerView.tagFilterView.selectedTags = this.filterTags;
                    }
                    explorerView.tagFilterView.UpdateDisplay();
                };
            }

            explorerView.tagFilterBar.onSelectedTagsChanged += () => UpdateExplorerFilters();
        }

        // - setup filter -
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
    }

    private void InitializeDialogs()
    {
        messageDialog.gameObject.SetActive(false);

        loginDialog.gameObject.SetActive(false);
        loginDialog.onSecurityCodeSent += (m) =>
        {
            CloseLoginDialog();
            OpenMessageDialog_OneButton("Security Code Requested",
                                        m.message,
                                        "Back",
                                        () => { CloseMessageDialog(); OpenLoginDialog(); });
        };
        loginDialog.onUserOAuthTokenReceived += (t) =>
        {
            CloseLoginDialog();

            OpenMessageDialog_TwoButton("Login Successful",
                                        "Do you want to merge the local guest account mod subscriptions"
                                        + " with your mod subscriptions on the server?",
                                        "Merge Subscriptions", () => { CloseMessageDialog(); LogUserIn(t, false); },
                                        "Replace Subscriptions", () => { CloseMessageDialog(); LogUserIn(t, true); });
        };
        loginDialog.onAPIRequestError += (e) =>
        {
            CloseLoginDialog();

            OpenMessageDialog_OneButton("Authorization Failed",
                                        e.message,
                                        "Back",
                                        () => { CloseMessageDialog(); OpenLoginDialog(); });
        };
    }

    private void StartFetchRemoteData()
    {
        // --- GameProfile ---
        ModManager.GetGameProfile(
        (g) =>
        {
            gameProfile = g;

            if(explorerView.tagFilterView)
            {
                explorerView.tagFilterView.DisplayCategories(g.tagCategories);
            }
            if(explorerView.tagFilterBar)
            {
                explorerView.tagFilterBar.categories = g.tagCategories;
                explorerView.tagFilterBar.UpdateDisplay();
            }
        },
        WebRequestError.LogAsWarning);

        // --- UserData ---
        if(!String.IsNullOrEmpty(APIClient.userAuthorizationToken))
        {
            // callbacks
            Action<UserProfile> onGetUserProfile = (u) =>
            {
                this.userProfile = u;

                if(this.userDisplay != null)
                {
                    this.userDisplay.profile = u;
                    this.userDisplay.UpdateUIComponents();

                    this.userDisplay.button.onClick.RemoveListener(OpenLoginDialog);
                    this.userDisplay.button.onClick.AddListener(LogUserOut);
                }
            };

            // TODO(@jackson): DO BETTER - (CACHE?! DOWNLOADS?)
            Action<RequestPage<ModProfile>> onGetSubscriptions = (r) =>
            {
                this.subscribedModIds = new List<int>(r.items.Length);
                foreach(var modProfile in r.items)
                {
                    this.subscribedModIds.Add(modProfile.id);
                }
                UpdateViewSubscriptions();
            };

            // requests
            ModManager.GetAuthenticatedUserProfile(onGetUserProfile,
                                                   null);

            RequestFilter filter = new RequestFilter();
            filter.fieldFilters.Add(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                    new EqualToFilter<int>(){ filterValue = this.gameId });

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


        if(this.userDisplay != null)
        {
            this.userDisplay.button.onClick.RemoveListener(OpenLoginDialog);
            this.userDisplay.button.onClick.AddListener(LogUserOut);
        }

        StartCoroutine(UserLoginCoroutine(oAuthToken, clearExistingSubscriptions));
    }

    private IEnumerator UserLoginCoroutine(string oAuthToken, bool clearExistingSubscriptions)
    {
        bool isRequestDone = false;
        WebRequestError requestError = null;

        // NOTE(@jackson): Could be much improved by not deleting matching mod subscription files
        if(clearExistingSubscriptions)
        {
            foreach(int modId in subscribedModIds)
            {
                // remove from disk
                CacheClient.DeleteAllModfileAndBinaryData(modId);
            }
            CacheClient.ClearAuthenticatedUserSubscriptions();

            subscribedModIds = new List<int>(0);
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
        if(this.userDisplay != null)
        {
            userDisplay.profile = this.userProfile;
            userDisplay.UpdateUIComponents();
        }


        // - get server subscriptions -
        List<int> subscriptionsToPush = new List<int>(subscribedModIds);
        bool allPagesReceived = false;

        RequestFilter subscriptionFilter = new RequestFilter();
        subscriptionFilter.fieldFilters.Add(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                            new EqualToFilter<int>() { filterValue = this.gameId });

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
                if(!subscribedModIds.Contains(profile.id))
                {
                    subscribedModIds.Add(profile.id);

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

        CacheClient.SaveAuthenticatedUserSubscriptions(subscribedModIds);


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
        // - clear current user -
        APIClient.userAuthorizationToken = null;
        CacheClient.DeleteAuthenticatedUser();

        foreach(int modId in this.subscribedModIds)
        {
            CacheClient.DeleteAllModfileAndBinaryData(modId);
        }

        // - set up guest account -
        CacheClient.SaveAuthenticatedUserSubscriptions(this.subscribedModIds);

        this.userProfile = ModBrowser.GUEST_PROFILE;
        this.subscribedModIds = new List<int>(0);

        if(this.userDisplay != null)
        {
            this.userDisplay.profile = ModBrowser.GUEST_PROFILE;
            this.userDisplay.UpdateUIComponents();

            this.userDisplay.button.onClick.RemoveListener(LogUserOut);
            this.userDisplay.button.onClick.AddListener(OpenLoginDialog);
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
    }

    // ---------[ UI CONTROL ]---------
    // ---[ VIEW MANAGEMENT ]---
    public void ShowInspectorView()
    {
        inspectorView.gameObject.SetActive(true);
        explorerView.gameObject.SetActive(false);
        subscriptionsView.gameObject.SetActive(false);
    }
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

    public void ChangeInspectorPage(int direction)
    {
        int firstExplorerIndex = (explorerView.CurrentPageNumber-1) * explorerView.ItemCount;
        int newModIndex = inspectorData.currentModIndex + direction;
        int offsetIndex = newModIndex - firstExplorerIndex;

        // Debug.Assert(newModIndex >= 0);
        // Debug.Assert(newModIndex <= inspectorData.lastModIndex);

        // profile
        if(offsetIndex < 0)
        {
            ChangeExplorerPage(-1);

            offsetIndex += explorerView.ItemCount;
            inspectorView.profile = explorerView.targetPage.items[offsetIndex];
        }
        else if(offsetIndex >= explorerView.ItemCount)
        {
            ChangeExplorerPage(1);

            offsetIndex -= explorerView.ItemCount;
            inspectorView.profile = explorerView.targetPage.items[offsetIndex];
        }
        else
        {
            inspectorView.profile = explorerView.currentPage.items[offsetIndex];
        }

        inspectorView.UpdateProfileDisplay();

        // statistics
        inspectorView.statistics = null;
        ModManager.GetModStatistics(inspectorView.profile.id,
                                    (s) => { inspectorView.statistics = s; inspectorView.UpdateStatisticsDisplay(); },
                                    null);

        // subscription
        inspectorView.isModSubscribed = this.subscribedModIds.Contains(inspectorView.profile.id);
        inspectorView.UpdateIsSubscribedDisplay();

        // inspectorView stuff
        inspectorData.currentModIndex = newModIndex;

        if(inspectorView.scrollView != null) { inspectorView.scrollView.verticalNormalizedPosition = 1f; }

        UpdateInspectorViewPageButtonInteractibility();
    }

    public void SetInspectorViewProfile(ModProfile profile)
    {
        // profile
        inspectorView.profile = profile;
        inspectorView.UpdateProfileDisplay();

        // statistics
        inspectorView.statistics = null;
        ModManager.GetModStatistics(inspectorView.profile.id,
                                    (s) => { inspectorView.statistics = s; inspectorView.UpdateStatisticsDisplay(); },
                                    null);

        // subscription
        inspectorView.isModSubscribed = this.subscribedModIds.Contains(inspectorView.profile.id);
        inspectorView.UpdateIsSubscribedDisplay();

        // inspectorView stuff
        inspectorData.currentModIndex = -1;

        if(inspectorView.scrollView != null) { inspectorView.scrollView.verticalNormalizedPosition = 1f; }
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

    public void InspectDiscoverItem(ModBrowserItem item)
    {
        // TODO(@jackson): Load explorer page
        inspectorData.currentModIndex = item.index + explorerView.currentPage.resultOffset;

        if(inspectorView.backToDiscoverButton != null)
        {
            inspectorView.backToDiscoverButton.gameObject.SetActive(true);
        }
        if(inspectorView.backToSubscriptionsButton != null)
        {
            inspectorView.backToSubscriptionsButton.gameObject.SetActive(false);
        }

        SetInspectorViewProfile(item.profile);
        ShowInspectorView();
    }

    public void InspectSubscriptionItem(ModBrowserItem item)
    {
        // TODO(@jackson): Load explorer page
        inspectorData.currentModIndex = item.index + subscriptionsView.currentPage.resultOffset;

        if(inspectorView.backToSubscriptionsButton != null)
        {
            inspectorView.backToSubscriptionsButton.gameObject.SetActive(true);
        }
        if(inspectorView.backToDiscoverButton != null)
        {
            inspectorView.backToDiscoverButton.gameObject.SetActive(false);
        }

        SetInspectorViewProfile(item.profile);
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

    public void OpenMessageDialog_OneButton(string header, string content,
                                            string buttonText, Action buttonCallback)
    {
        messageDialog.button01.GetComponentInChildren<Text>().text = buttonText;

        messageDialog.button01.onClick.RemoveAllListeners();
        messageDialog.button01.onClick.AddListener(() => buttonCallback());

        messageDialog.button02.gameObject.SetActive(false);

        OpenMessageDialog(header, content);
    }

    public void OpenMessageDialog_TwoButton(string header, string content,
                                            string button01Text, Action button01Callback,
                                            string button02Text, Action button02Callback)
    {
        messageDialog.button01.GetComponentInChildren<Text>().text = button01Text;

        messageDialog.button01.onClick.RemoveAllListeners();
        messageDialog.button01.onClick.AddListener(() => button01Callback());

        messageDialog.button02.GetComponentInChildren<Text>().text = button02Text;

        messageDialog.button02.onClick.RemoveAllListeners();
        messageDialog.button02.onClick.AddListener(() => button02Callback());

        messageDialog.button02.gameObject.SetActive(true);

        OpenMessageDialog(header, content);
    }

    private void OpenMessageDialog(string header, string content)
    {
        messageDialog.header.text = header;
        messageDialog.content.text = content;

        messageDialog.gameObject.SetActive(true);
    }

    private void CloseMessageDialog()
    {
        messageDialog.gameObject.SetActive(false);
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

    public void ClearExplorerFilters()
    {
        if(explorerView.nameSearchField != null)
        {
            explorerView.nameSearchField.text = string.Empty;
        }

        this.filterTags.Clear();

        if(explorerView.tagFilterBar.selectedTags != this.filterTags)
        {
            explorerView.tagFilterBar.selectedTags = this.filterTags;
        }
        explorerView.tagFilterBar.UpdateDisplay();

        if(explorerView.tagFilterView.selectedTags != this.filterTags)
        {
            explorerView.tagFilterView.selectedTags = this.filterTags;
        }
        explorerView.tagFilterView.UpdateDisplay();

        UpdateExplorerFilters();
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
        string[] filterTagNames = this.filterTags.ToArray();

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

    public void SubscribeToMod(ModProfile profile)
    {
        if(this.userProfile.id == ModBrowser.GUEST_PROFILE.id)
        {
            OnSubscribedToMod(profile);
        }
        else
        {
            APIClient.SubscribeToMod(profile.id,
                                     OnSubscribedToMod,
                                     WebRequestError.LogAsWarning);
        }
    }

    public void OnSubscribedToMod(ModProfile profile)
    {
        Debug.Assert(profile != null);

        // update collection
        subscribedModIds.Add(profile.id);
        CacheClient.SaveAuthenticatedUserSubscriptions(subscribedModIds);

        // begin download
        ModBinaryRequest request = ModManager.RequestCurrentRelease(profile);

        // TODO(@jackson): Dirty hack
        ModBrowserItem[] mbiArray = Resources.FindObjectsOfTypeAll<ModBrowserItem>();
        foreach(ModBrowserItem mbi in mbiArray)
        {
            if(mbi.profile != null
               && mbi.profile.id == profile.id
               && mbi.profileDisplay != null
               && mbi.profileDisplay.downloadDisplay != null)
            {
                mbi.profileDisplay.downloadDisplay.gameObject.SetActive(true);
                mbi.profileDisplay.downloadDisplay.DisplayRequest(request);
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

    public void UnsubscribeFromMod(ModProfile profile)
    {
        if(this.userProfile.id == ModBrowser.GUEST_PROFILE.id)
        {
            OnUnsubscribedFromMod(profile);
        }
        else
        {
            APIClient.UnsubscribeFromMod(profile.id,
                                         () => OnUnsubscribedFromMod(profile),
                                         WebRequestError.LogAsWarning);
        }
    }

    public void OnUnsubscribedFromMod(ModProfile modProfile)
    {
        Debug.Assert(modProfile != null);

        // update collection
        subscribedModIds.Remove(modProfile.id);
        CacheClient.SaveAuthenticatedUserSubscriptions(subscribedModIds);

        // remove from disk
        CacheClient.DeleteAllModfileAndBinaryData(modProfile.id);

        UpdateViewSubscriptions();
    }

    private void UpdateViewSubscriptions()
    {
        // - explorerView -
        explorerView.subscribedModIds = this.subscribedModIds;
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
            inspectorView.isModSubscribed = this.subscribedModIds.Contains(inspectorView.profile.id);
            inspectorView.UpdateIsSubscribedDisplay();
        }
    }

    private static List<int> enabledMods = new List<int>();
    public static bool IsModEnabled(ModProfile profile)
    {
        Debug.LogError("[mod.io] This function handle is a placeholder "
                       + "for the enable/disable functionality that the "
                       + "game code may need to execute.");

        return enabledMods.Contains(profile.id);
    }

    public static void ToggleModEnabled(ModProfile profile)
    {
        Debug.LogError("[mod.io] This function handle is a placeholder "
                       + "for the enable/disable functionality that the "
                       + "game code may need to execute.");

        if(enabledMods.Contains(profile.id))
        {
            enabledMods.Remove(profile.id);
        }
        else
        {
            enabledMods.Add(profile.id);
        }
    }


    // ---------[ EVENT HANDLING ]---------
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

    // ---------[ UTILITY ]---------
    public static string ValueToDisplayString(int value)
    {
        if(value < 1000) // 0 - 999
        {
            return value.ToString();
        }
        else if(value < 100000) // 1.0K - 99.9K
        {
            // remove tens
            float truncatedValue = (value / 100) / 10f;
            return(truncatedValue.ToString() + "K");
        }
        else if(value < 10000000) // 100K - 999K
        {
            // remove hundreds
            int truncatedValue = (value / 1000);
            return(truncatedValue.ToString() + "K");
        }
        else if(value < 1000000000) // 1.0M - 99.9M
        {
            // remove tens of thousands
            float truncatedValue = (value / 100000) / 10f;
            return(truncatedValue.ToString() + "M");
        }
        else // 100M+
        {
            // remove hundreds of thousands
            int truncatedValue = (value / 1000000);
            return(truncatedValue.ToString() + "M");
        }
    }

    // TODO(@jackson): Add smallest unit
    public static string ByteCountToDisplayString(Int64 value)
    {
        string[] sizeSuffixes = new string[]{"B", "KB", "MB", "GB"};
        int sizeIndex = 0;
        Int64 adjustedSize = value;
        while(adjustedSize > 0x0400
              && (sizeIndex+1) < sizeSuffixes.Length)
        {
            adjustedSize /= 0x0400;
            ++sizeIndex;
        }

        if(sizeIndex > 0
           && adjustedSize < 100)
        {
            decimal displayValue = (decimal)value / (decimal)(0x0400^sizeIndex);
            return displayValue.ToString("0.0") + sizeSuffixes[sizeIndex];
        }
        else
        {
            return adjustedSize + sizeSuffixes[sizeIndex];
        }
    }

    public static Sprite CreateSpriteFromTexture(Texture2D texture)
    {
        return Sprite.Create(texture,
                             new Rect(0.0f, 0.0f, texture.width, texture.height),
                             Vector2.zero);
    }

    public static void OpenYouTubeVideoURL(string youTubeVideoId)
    {
        if(!String.IsNullOrEmpty(youTubeVideoId))
        {
            Application.OpenURL(@"https://youtu.be/" + youTubeVideoId);
        }
    }
}
