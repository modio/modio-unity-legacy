using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ModIO;

// TODO(@jackson): Queue missed requests? (Unsub fail)
// TODO(@jackson): Correct subscription loading
// TODO(@jackson): Add user events
// TODO(@jackson): Error handling on log in
public class ModBrowser : MonoBehaviour
{
    // ---------[ NESTED CLASSES ]---------
    [System.Serializable]
    private class ManifestData
    {
        public int lastCacheUpdate = -1;
    }

    // ---------[ CONST & STATIC ]---------
    public static string manifestFilePath { get { return CacheClient.GetCacheDirectory() + "browser_manifest.data"; } }
    public static readonly UserProfile GUEST_PROFILE = new UserProfile()
    {
        id = 0,
        username = "Guest",
    };

    // ---------[ FIELDS ]---------
    // --- Key Data ---
    [Header("Settings")]
    public int gameId = 0;
    public string gameAPIKey = string.Empty;
    public bool isAutomaticUpdateEnabled = false;
    public ModBrowserViewMode viewMode = ModBrowserViewMode.Collection;

    // --- UI Components ---
    [Header("UI Components")]
    public ExplorerView explorerView;
    public ExplorerView_Scrolling explorerViewScrolling;
    public CollectionView collectionView;
    public InspectorView inspector;
    public ModBrowserSearchBar searchBar;
    public ModBrowserUserDisplay userDisplay;
    public LoginDialog loginDialog;
    public MessageDialog messageDialog;

    // --- Runtime Data ---
    [Header("Runtime Data")]
    public UserProfile userProfile = null;
    public int lastCacheUpdate = -1;
    public string titleSearch = string.Empty;
    public List<int> collectionModIds = new List<int>();
    public ModProfileFilter modProfileFilter = new ModProfileFilter();
    public List<ModBinaryRequest> modDownloads = new List<ModBinaryRequest>();

    // ---------[ ACCESSORS ]---------
    public IModBrowserView GetViewForMode(ModBrowserViewMode mode)
    {
        switch(mode)
        {
            case ModBrowserViewMode.Collection:
            {
                return this.collectionView;
            }
            case ModBrowserViewMode.Explorer:
            {
                return this.explorerView;
            }
        }

        return null;
    }

    public IEnumerable<ModProfile> GetFilteredProfileCollectionForMode(ModBrowserViewMode mode)
    {
        IEnumerable<ModProfile> profileCollection = CacheClient.IterateAllModProfiles();

        switch(mode)
        {
            case ModBrowserViewMode.Collection:
            {
                profileCollection = profileCollection.Where(p => collectionModIds.Contains(p.id));
            }
            break;
        }

        if(!String.IsNullOrEmpty(modProfileFilter.title))
        {
            profileCollection = profileCollection.Where(p => p.name.ToUpper().Contains(modProfileFilter.title.ToUpper()));
        }
        if(modProfileFilter.tags != null
           && modProfileFilter.tags.Count() > 0)
        {
            Func<ModProfile, bool> profileContainsTags = (profile) =>
            {
                bool isMatch = true;
                List<string> filterTagNames = new List<string>(SimpleModTag.EnumerateNames(modProfileFilter.tags));

                foreach(string filterTag in filterTagNames)
                {
                    isMatch &= profile.tagNames.Contains(filterTag);
                }

                return isMatch;
            };

            profileCollection = profileCollection.Where(profileContainsTags);
        }

        return profileCollection;
    }

    // ---------[ INITIALIZATION ]---------
    private void Start()
    {
        // load APIClient vars
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

        APIClient.gameId = this.gameId;
        APIClient.gameAPIKey = this.gameAPIKey;
        APIClient.userAuthorizationToken = CacheClient.LoadAuthenticatedUserToken();;

        // assert ui is prepared
        inspector.InitializeLayout();
        inspector.subscribeButton.onClick.AddListener(() => OnSubscribeButtonClicked(inspector.profile));
        inspector.gameObject.SetActive(false);

        searchBar.Initialize();
        searchBar.profileFiltersUpdated += OnProfileFiltersUpdated;

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
            Action clearLocalCollection = () =>
            {
                foreach(int modId in collectionModIds)
                {
                    // remove from disk
                    CacheClient.DeleteAllModfileAndBinaryData(modId);
                }
                collectionModIds = new List<int>(0);
                CacheClient.ClearAuthenticatedUserSubscriptions();
                collectionView.Refresh();
            };

            CloseLoginDialog();

            OpenMessageDialog_TwoButton("Login Successful",
                                        "Do you want to merge the local guest account mod collection"
                                        + " with your mod collection on the servers?",
                                        "Merge Collections", () => { CloseMessageDialog(); LogUserIn(t); },
                                        "Replace Collection", () => { CloseMessageDialog(); clearLocalCollection(); LogUserIn(t); });
        };
        loginDialog.onAPIRequestError += (e) =>
        {
            CloseLoginDialog();

            OpenMessageDialog_OneButton("Authorization Failed",
                                        e.message,
                                        "Back",
                                        () => { CloseMessageDialog(); OpenLoginDialog(); });
        };

        messageDialog.gameObject.SetActive(false);

        if(userDisplay != null)
        {
            userDisplay.button.onClick.AddListener(OpenLoginDialog);
            userDisplay.profile = ModBrowser.GUEST_PROFILE;
            userDisplay.UpdateUIComponents();
        }

        // load manifest
        ManifestData manifest = CacheClient.ReadJsonObjectFile<ManifestData>(ModBrowser.manifestFilePath);
        if(manifest != null)
        {
            this.lastCacheUpdate = manifest.lastCacheUpdate;
        }

        // load user
        this.userProfile = CacheClient.LoadAuthenticatedUserProfile();
        if(this.userProfile == null)
        {
            this.userProfile = ModBrowser.GUEST_PROFILE;
        }

        this.collectionModIds = CacheClient.LoadAuthenticatedUserSubscriptions();
        if(this.collectionModIds == null)
        {
            this.collectionModIds = new List<int>();
        }

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

            // TODO(@jackson): DO BETTER
            Action<APIResponseArray<ModProfile>> onGetSubscriptions = (r) =>
            {
                this.collectionModIds = new List<int>(r.Count);
                foreach(var modProfile in r)
                {
                    this.collectionModIds.Add(modProfile.id);
                }
            };

            // requests
            ModManager.GetAuthenticatedUserProfile(onGetUserProfile,
                                                   null);

            RequestFilter filter = new RequestFilter();
            filter.fieldFilters.Add(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                    new EqualToFilter<int>(){ filterValue = this.gameId });

            APIClient.GetUserSubscriptions(filter, null, onGetSubscriptions, null);
        }

        // intialize views
        // collectionView.onItemClicked += OnExplorerItemClicked;
        collectionView.InitializeLayout();
        collectionView.profileCollection = CacheClient.IterateAllModProfiles()
                                                    .Where(p => collectionModIds.Contains(p.id));
        collectionView.gameObject.SetActive(false);
        collectionView.onUnsubscribeClicked += OnUnsubscribeButtonClicked;

        explorerView.onItemClicked += OnExplorerItemClicked;
        explorerView.profileCollection = CacheClient.IterateAllModProfiles();
        explorerView.InitializeLayout();
        explorerView.gameObject.SetActive(false);

        IModBrowserView view = GetViewForMode(this.viewMode);
        view.Refresh();
        view.gameObject.SetActive(true);
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

        //     inspector.installButtonText.text = "Downloading [" + downloaded.ToString("00.00") + "%]";
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
            Action<List<ModfileStub>> onReleasesUpdated = (modfiles) =>
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

                #if DEBUG
                if(Application.isPlaying)
                #endif
                {
                    IModBrowserView view = GetViewForMode(this.viewMode);
                    view.profileCollection = GetFilteredProfileCollectionForMode(this.viewMode);
                    view.Refresh();
                }
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
    public void LogUserIn(string oAuthToken)
    {
        Debug.Assert(!String.IsNullOrEmpty(oAuthToken),
                     "[mod.io] ModBrowser.LogUserIn requires a valid oAuthToken");

        if(this.userDisplay != null)
        {
            this.userDisplay.button.onClick.RemoveListener(OpenLoginDialog);
            this.userDisplay.button.onClick.AddListener(LogUserOut);
        }

        StartCoroutine(UserLoginCoroutine(oAuthToken));
    }

    private IEnumerator UserLoginCoroutine(string oAuthToken)
    {
        APIClient.userAuthorizationToken = oAuthToken;
        CacheClient.SaveAuthenticatedUserToken(oAuthToken);

        bool isRequestDone = false;
        WebRequestError requestError = null;

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

        // - get the subscriptions -
        Debug.Log("GETTING USER SUBSCRIPTIONS");

        List<int> subscribedModIds = new List<int>();
        bool allPagesReceived = false;

        RequestFilter subscriptionFilter = new RequestFilter();
        subscriptionFilter.fieldFilters.Add(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                            new EqualToFilter<int>() { filterValue = this.gameId });

        APIPaginationParameters pagination = new APIPaginationParameters()
        {
            limit = APIPaginationParameters.LIMIT_MAX,
            offset = 0,
        };

        APIResponseArray<ModProfile> responseArray = null;
        while(!allPagesReceived)
        {
            isRequestDone = false;
            requestError = null;
            responseArray = null;

            APIClient.GetUserSubscriptions(subscriptionFilter, pagination,
                                           (r) => { isRequestDone = true; responseArray = r; },
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

            foreach(ModProfile profile in responseArray)
            {
                subscribedModIds.Add(profile.id);
            }

            allPagesReceived = (responseArray.Count < responseArray.Limit);

            if(!allPagesReceived)
            {
                pagination.offset += pagination.limit;
            }
        }

        foreach(int modId in collectionModIds)
        {
            if(!subscribedModIds.Contains(modId))
            {
                APIClient.SubscribeToMod(modId,
                                         (p) => Debug.Log("[mod.io] Mod subscription merged: " + p.id + "-" + p.name),
                                         (e) => Debug.Log("[mod.io] Mod subscription merge failed: " + modId + "\n"
                                                          + e.ToUnityDebugString()));

                subscribedModIds.Add(modId);
            }
        }

        collectionModIds = subscribedModIds;
        CacheClient.SaveAuthenticatedUserSubscriptions(collectionModIds);

        isRequestDone = false;
        requestError = null;
        List<ModProfile> subscriptionProfiles = null;

        ModManager.GetModProfiles(collectionModIds,
                                  (r) => { isRequestDone = true; subscriptionProfiles = r; },
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

        collectionView.Refresh();

        foreach(ModProfile profile in subscriptionProfiles)
        {
            // begin download
            ModBinaryRequest request = ModManager.RequestCurrentRelease(profile);

            if(!request.isDone)
            {
                request.succeeded += (r) =>
                {
                    Debug.Log(profile.name + " Downloaded!");
                    modDownloads.Remove(request);
                };

                request.failed += (r) =>
                {
                    Debug.Log(profile.name + " Download Failed!");
                    modDownloads.Remove(request);
                };

                modDownloads.Add(request);
            }
        }
    }

    public void LogUserOut()
    {
        // - clear current user -
        APIClient.userAuthorizationToken = null;
        CacheClient.DeleteAuthenticatedUser();

        foreach(int modId in this.collectionModIds)
        {
            CacheClient.DeleteAllModfileAndBinaryData(modId);
        }

        // - set up guest account -
        CacheClient.SaveAuthenticatedUserSubscriptions(this.collectionModIds);

        this.userProfile = ModBrowser.GUEST_PROFILE;
        this.collectionModIds = new List<int>(0);
        this.collectionView.Refresh();

        if(this.userDisplay != null)
        {
            this.userDisplay.profile = ModBrowser.GUEST_PROFILE;
            this.userDisplay.UpdateUIComponents();

            this.userDisplay.button.onClick.RemoveListener(LogUserOut);
            this.userDisplay.button.onClick.AddListener(OpenLoginDialog);
        }
    }

    // ---------[ UI CONTROL ]---------
    public void SetExplorerViewLayoutGrid()
    {
        SetExplorerViewLayout(ModBrowserLayoutMode.Grid);
    }
    public void SetExplorerViewLayoutTable()
    {
        SetExplorerViewLayout(ModBrowserLayoutMode.Table);
    }
    public void SetExplorerViewLayout(ModBrowserLayoutMode layout)
    {
        // if(explorerView.layoutMode == layout) { return; }

        // // collectionView.layoutMode = layout;
        // // collectionView.InitializeLayout();
        // explorerView.layoutMode = layout;
        // explorerView.InitializeLayout();
        // explorerView.Refresh();
    }

    public void SetViewModeCollection()
    {
        this.viewMode = ModBrowserViewMode.Collection;
        this.UpdateViewMode();
    }
    public void SetViewModeBrowse()
    {
        this.viewMode = ModBrowserViewMode.Explorer;
        this.UpdateViewMode();
    }

    public void UpdateViewMode()
    {
        IModBrowserView view = GetViewForMode(this.viewMode);
        if(view.gameObject.activeSelf) { return; }

        collectionView.gameObject.SetActive(false);
        explorerView.gameObject.SetActive(false);

        view.Refresh();
        view.gameObject.SetActive(true);
    }

    public void OnExplorerItemClicked(ModBrowserItem item)
    {
        inspector.profile = item.profile;
        inspector.statistics = null;
        inspector.UpdateProfileUIComponents();

        if(collectionModIds.Contains(item.profile.id))
        {
            inspector.subscribeButtonText.text = "View In Collection";
        }
        else
        {
            inspector.subscribeButtonText.text = "Add To Collection";
        }

        ModManager.GetModStatistics(item.profile.id,
                                    (s) => { inspector.statistics = s; inspector.UpdateStatisticsUIComponents(); },
                                    null);

        inspector.gameObject.SetActive(true);
    }

    public void CloseInspector()
    {
        inspector.gameObject.SetActive(false);
    }

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

    // TODO(@jackson): Change parameter type
    public void OnProfileFiltersUpdated(string textFilter,
                                        IEnumerable<SimpleModTag> tagFilters)
    {
        this.modProfileFilter = new ModProfileFilter();
        this.modProfileFilter.title = textFilter;
        this.modProfileFilter.tags = tagFilters;

        IModBrowserView view = GetViewForMode(this.viewMode);
        view.profileCollection = GetFilteredProfileCollectionForMode(this.viewMode);
        view.Refresh();
    }

    public void OnSubscribeButtonClicked(ModProfile profile)
    {
        Debug.Assert(profile != null);

        if(collectionModIds.Contains(profile.id))
        {
            // "View In Collection"
            CloseInspector();
            SetViewModeCollection();
        }
        else
        {
            if(userProfile.id != ModBrowser.GUEST_PROFILE.id)
            {
                inspector.subscribeButton.interactable = false;

                // TODO(@jackson): Protect from switch
                Action<ModProfile> onSubscribe = (p) =>
                {
                    inspector.subscribeButtonText.text = "View In Collection";
                    inspector.subscribeButton.interactable = true;
                    OnSubscribedToMod(p);
                };

                // TODO(@jackson): onError
                Action<WebRequestError> onError = (e) =>
                {
                    Debug.Log("Failed to Subscribe");
                    inspector.subscribeButton.interactable = true;
                };

                APIClient.SubscribeToMod(profile.id, onSubscribe, onError);
            }
            else
            {
                inspector.subscribeButtonText.text = "View In Collection";
                OnSubscribedToMod(profile);
            }
        }
    }

    public void OnSubscribedToMod(ModProfile profile)
    {
        Debug.Assert(profile != null);

        // update collection
        collectionModIds.Add(profile.id);
        CacheClient.SaveAuthenticatedUserSubscriptions(collectionModIds);

        // begin download
        ModBinaryRequest request = ModManager.RequestCurrentRelease(profile);

        if(!request.isDone)
        {
            modDownloads.Add(request);

            request.succeeded += (r) =>
            {
                Debug.Log(profile.name + " Downloaded!");
                modDownloads.Remove(request);
            };
        }
    }

    public void OnUnsubscribeButtonClicked(ModProfile modProfile)
    {
        Debug.Assert(modProfile != null);

        Debug.Log("UserProfile.id = " + userProfile.id
                  + "\nModBrowser.GUEST_PROFILE.id = " + GUEST_PROFILE.id);

        if(userProfile.id != ModBrowser.GUEST_PROFILE.id)
        {
            Debug.Log("Unsubbing Account");
            collectionView.unsubscribeButton.interactable = false;

            Action onUnsubscribe = () =>
            {
                OnUnsubscribedFromMod(modProfile);
            };

            // TODO(@jackson): onError
            Action<WebRequestError> onError = (e) =>
            {
                Debug.Log("Failed to Unsubscribe");
                collectionView.unsubscribeButton.interactable = true;
            };

            APIClient.UnsubscribeFromMod(modProfile.id, onUnsubscribe, onError);
        }
        else
        {
            Debug.Log("Unsubbing Guest");
            OnUnsubscribedFromMod(modProfile);
        }
    }

    public void OnUnsubscribedFromMod(ModProfile modProfile)
    {
        Debug.Assert(modProfile != null);

        // update collection
        collectionModIds.Remove(modProfile.id);
        CacheClient.SaveAuthenticatedUserSubscriptions(collectionModIds);

        // remove from disk
        CacheClient.DeleteAllModfileAndBinaryData(modProfile.id);

        collectionView.Refresh();
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

    //     inspector.title.text = profile.name;
    //     inspector.author.text = profile.submittedBy.username;
    //     inspector.logo.sprite = CreateSpriteFromTexture(loadingPlaceholder);

    //     List<int> userSubscriptions = CacheClient.LoadAuthenticatedUserSubscriptions();

    //     if(userSubscriptions != null
    //        && userSubscriptions.Contains(profile.id))
    //     {
    //         inspector.subscribeButtonText.text = "Unsubscribe";
    //     }
    //     else
    //     {
    //         inspector.subscribeButtonText.text = "Subscribe";
    //     }

    //     ModManager.GetModLogo(profile, logoInspectorVersion,
    //                           (t) => inspector.logo.sprite = CreateSpriteFromTexture(t),
    //                           null);

    //     inspector.installButton.gameObject.SetActive(false);
    //     inspector.downloadButtonText.text = "Verifying local data";
    //     inspector.downloadButton.gameObject.SetActive(true);
    //     inspector.downloadButton.interactable = false;

    //     // - check binary status -
    //     ModManager.GetDownloadedBinaryStatus(profile.activeBuild,
    //                                          (status) =>
    //                                          {
    //                                             if(status == ModBinaryStatus.CompleteAndVerified)
    //                                             {
    //                                                 inspector.downloadButton.gameObject.SetActive(false);
    //                                                 inspector.installButton.gameObject.SetActive(true);
    //                                             }
    //                                             else
    //                                             {
    //                                                 inspector.downloadButtonText.text = "Download";
    //                                                 inspector.downloadButton.interactable = true;
    //                                             }
    //                                          });

    //     // - finalize -
    //     isInspecting = true;
    //     thumbnailContainer.gameObject.SetActive(false);
    //     inspector.gameObject.SetActive(true);
    // }

    // protected virtual void OnBackClicked()
    // {
    //     if(isInspecting)
    //     {
    //         isInspecting = false;
    //         thumbnailContainer.gameObject.SetActive(true);
    //         inspector.gameObject.SetActive(false);
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
    //         inspector.subscribeButtonText.text = "Unsubscribe";

    //         if(userProfile != null)
    //         {
    //             APIClient.SubscribeToMod(inspectedProfile.id,
    //                                      null, null);
    //         }
    //     }
    //     else
    //     {
    //         subscriptions.RemoveAt(subscriptionIndex);
    //         inspector.subscribeButtonText.text = "Subscribe";

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
    //         inspector.installButton.gameObject.SetActive(true);
    //         inspector.downloadButton.gameObject.SetActive(true);

    //         this.activeDownload = null;
    //     }
    //     else
    //     {
    //         inspector.downloadButtonText.text = "Initializing Download...";

    //         this.activeDownload.succeeded += (d) =>
    //         {
    //             inspector.installButton.gameObject.SetActive(true);
    //             inspector.downloadButton.gameObject.SetActive(true);

    //             this.activeDownload = null;
    //         };
    //         this.activeDownload.failed += (d) =>
    //         {
    //             inspector.installButton.gameObject.SetActive(true);
    //             inspector.downloadButton.gameObject.SetActive(true);

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
    public static string ConvertValueIntoShortText(int value)
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

    public static string ConvertByteCountIntoDisplayText(Int64 value)
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

    public static Sprite CreateSpriteWithTexture(Texture2D texture)
    {
        return Sprite.Create(texture,
                             new Rect(0.0f, 0.0f, texture.width, texture.height),
                             Vector2.zero);
    }
}
