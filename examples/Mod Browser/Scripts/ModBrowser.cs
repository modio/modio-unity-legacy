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

    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public int gameId = 0;
    public string gameAPIKey = string.Empty;
    public bool isAutomaticUpdateEnabled = false;
    public ModBrowserViewMode viewMode = ModBrowserViewMode.Collection;

    [Header("UI Components")]
    public ExplorerView explorerView;
    public CollectionView collectionView;
    public InspectorView inspectorView;
    public ModTagFilterView tagFilterView;
    public InputField titleSearchField;
    public ModBrowserUserDisplay userDisplay;
    public LoginDialog loginDialog;
    public MessageDialog messageDialog;
    public Button prevPageButton;
    public Button nextPageButton;

    [Header("Display Data")]
    public InspectorViewData inspectorData = new InspectorViewData();
    public List<int> collectionModIds = new List<int>();
    public UserProfile userProfile = null;
    public int modCount;

    [Header("Runtime Data")]
    public int lastCacheUpdate = -1;
    public string titleSearch = string.Empty;
    public Func<ModProfile, bool> nameFilterDelegate = (p) => { return true; };
    public Func<ModProfile, bool> tagFilterDelegate = (p) => { return true; };
    public List<ModBinaryRequest> modDownloads = new List<ModBinaryRequest>();


    // ---------[ ACCESSORS ]---------
    private void LoadFilteredProfiles(ModProfile[] destinationArray, int offset, int count)
    {
        Debug.Assert(count <= destinationArray.Length);

        IEnumerator<ModProfile> profileEnumerator = CacheClient.IterateAllModProfilesFromOffset(offset)
                .Where(nameFilterDelegate)
                .Where(tagFilterDelegate)
                .GetEnumerator();

        int i = 0;
        for(; i < count && profileEnumerator.MoveNext(); ++i)
        {
            destinationArray[i] = profileEnumerator.Current;
        }
        for(; i < count; ++i)
        {
            destinationArray[i] = null;
        }
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

        // searchBar.Initialize();
        // searchBar.profileFiltersUpdated += OnProfileFiltersUpdated;

        // initialize login dialog
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


        this.modCount = CacheClient.CountModProfiles();
        // initialize views
        inspectorView.Initialize();
        inspectorView.subscribeButton.onClick.AddListener(() => OnSubscribeButtonClicked(inspectorView.profile));
        inspectorView.gameObject.SetActive(false);
        UpdateInspectorViewPageButtonInteractibility();

        collectionView.Initialize();
        collectionView.onUnsubscribeClicked += OnUnsubscribeButtonClicked;
        collectionView.profileCollection = CacheClient.IterateAllModProfiles().Where(p => collectionModIds.Contains(p.id));
        collectionView.gameObject.SetActive(false);

        tagFilterView.gameObject.SetActive(false);
        tagFilterView.onSelectedTagsChanged += UpdateFilters;
        ModManager.GetGameProfile((g) => { tagFilterView.tagCategories = g.tagCategories; tagFilterView.Initialize(); },
                                  WebRequestError.LogAsWarning);

        InitializeExplorerView();


        // final elements
        // TODO(@jackson): titleSearchField.onValueChanged.AddListener((t) => {});
        titleSearchField.onEndEdit.AddListener((t) =>
        {
            if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                UpdateFilters();
            }
        } );
    }

    private void InitializeExplorerView()
    {
        explorerView.Initialize();
        explorerView.inspectRequested += OnExplorerItemClicked;
        explorerView.gameObject.SetActive(true);

        explorerView.currentPage = new ModPage();
        explorerView.currentPage.profileCount = (int)Mathf.Min(this.modCount, explorerView.itemCount);
        explorerView.currentPage.profiles = new ModProfile[explorerView.itemCount];

        explorerView.targetPage = new ModPage();
        explorerView.targetPage.profileCount = 0;
        explorerView.targetPage.profiles = new ModProfile[explorerView.itemCount];
        explorerView.targetPage.index = -1;

        if(this.modCount == 0)
        {
            explorerView.currentPage.index = -1;
            explorerView.pageCount = 0;
        }
        else
        {
            explorerView.currentPage.index = 0;
            explorerView.pageCount = (int)Mathf.Ceil((float)modCount / (float)explorerView.itemCount);

            LoadFilteredProfiles(explorerView.currentPage.profiles,
                                 0,
                                 explorerView.currentPage.profileCount);
        }

        explorerView.UpdateCurrentPageDisplay();

        UpdateExplorerViewPageButtonInteractibility();
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
        // IModBrowserView view = GetViewForMode(this.viewMode);
        // if(view.gameObject.activeSelf) { return; }

        // collectionView.gameObject.SetActive(false);
        // explorerView.gameObject.SetActive(false);

        // view.Refresh();
        // view.gameObject.SetActive(true);

        switch(this.viewMode)
        {
            case ModBrowserViewMode.Collection:
            {
                collectionView.gameObject.SetActive(true);
                explorerView.gameObject.SetActive(false);
            }
            break;
            case ModBrowserViewMode.Explorer:
            {
                explorerView.gameObject.SetActive(true);
                collectionView.gameObject.SetActive(false);
            }
            break;
        }
    }

    public void OnExplorerItemClicked(ModBrowserItem item)
    {
        inspectorData.currentModIndex = item.index + (explorerView.currentPage.index * explorerView.itemCount);
        Debug.Log("NEW currentModIndex=" + inspectorData.currentModIndex);

        ChangeInspectorPage(0);
    }

    public void CloseInspector()
    {
        inspectorView.gameObject.SetActive(false);
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
            Text buttonText = inspectorView.subscribeButton.GetComponent<Text>();
            if(buttonText == null)
            {
                buttonText = inspectorView.subscribeButton.GetComponentInChildren<Text>();
            }

            if(userProfile.id != ModBrowser.GUEST_PROFILE.id)
            {
                inspectorView.subscribeButton.interactable = false;

                // TODO(@jackson): Protect from switch
                Action<ModProfile> onSubscribe = (p) =>
                {
                    buttonText.text = "View In Collection";
                    inspectorView.subscribeButton.interactable = true;
                    OnSubscribedToMod(p);
                };

                // TODO(@jackson): onError
                Action<WebRequestError> onError = (e) =>
                {
                    Debug.Log("Failed to Subscribe");
                    inspectorView.subscribeButton.interactable = true;
                };

                APIClient.SubscribeToMod(profile.id, onSubscribe, onError);
            }
            else
            {
                buttonText.text = "View In Collection";
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

    public void ChangeExplorerPage(int direction)
    {
        // TODO(@jackson): Queue on isTransitioning?
        if(explorerView.isTransitioning)
        {
            Debug.LogWarning("[mod.io] Cannot change during transition");
            return;
        }

        int targetPageIndex = explorerView.currentPage.index + direction;
        int targetPageProfileOffset = targetPageIndex * explorerView.itemCount;

        Debug.Assert(targetPageIndex >= 0);
        Debug.Assert(targetPageIndex < explorerView.pageCount);

        explorerView.targetPage.index = targetPageIndex;
        explorerView.targetPage.profileCount = (int)Mathf.Min(explorerView.itemCount,
                                                              this.modCount - targetPageProfileOffset);
        LoadFilteredProfiles(explorerView.targetPage.profiles,
                             targetPageIndex * explorerView.itemCount,
                             explorerView.itemCount);

        explorerView.UpdateTargetPageDisplay();

        PageTransitionDirection transitionDirection = (direction < 0
                                                       ? PageTransitionDirection.FromLeft
                                                       : PageTransitionDirection.FromRight);

        explorerView.InitiateTargetPageTransition(transitionDirection, () =>
        {
            UpdateExplorerViewPageButtonInteractibility();
        });

        UpdateExplorerViewPageButtonInteractibility();
    }

    public void UpdateExplorerViewPageButtonInteractibility()
    {
        if(prevPageButton != null)
        {
            prevPageButton.interactable = (!explorerView.isTransitioning
                                           && explorerView.currentPage.index > 0);
        }
        if(nextPageButton != null)
        {
            nextPageButton.interactable = (!explorerView.isTransitioning
                                           && explorerView.currentPage.index < explorerView.pageCount - 1);
        }
    }

    public void ChangeInspectorPage(int direction)
    {
        int firstExplorerIndex = explorerView.currentPage.index * explorerView.itemCount;
        int newModIndex = inspectorData.currentModIndex + direction;
        int offsetIndex = newModIndex - firstExplorerIndex;

        Debug.Assert(newModIndex >= 0);
        Debug.Assert(newModIndex <= inspectorData.lastModIndex);

        if(offsetIndex < 0)
        {
            ChangeExplorerPage(-1);

            offsetIndex += explorerView.itemCount;
            inspectorView.profile = explorerView.targetPage.profiles[offsetIndex];
        }
        else if(offsetIndex >= explorerView.itemCount)
        {
            ChangeExplorerPage(1);

            offsetIndex -= explorerView.itemCount;
            inspectorView.profile = explorerView.targetPage.profiles[offsetIndex];
        }
        else
        {
            inspectorView.profile = explorerView.currentPage.profiles[offsetIndex];
        }

        inspectorView.statistics = null;
        inspectorView.UpdateProfileUIComponents();

        Text buttonText = inspectorView.subscribeButton.GetComponent<Text>();
        if(buttonText == null)
        {
            buttonText = inspectorView.subscribeButton.GetComponentInChildren<Text>();
        }

        if(buttonText != null)
        {
            if(collectionModIds.Contains(inspectorView.profile.id))
            {
                buttonText.text = "View In Collection";
            }
            else
            {
                buttonText.text = "Add To Collection";
            }
        }

        ModManager.GetModStatistics(inspectorView.profile.id,
                                    (s) => { inspectorView.statistics = s; inspectorView.UpdateStatisticsUIComponents(); },
                                    null);

        inspectorData.currentModIndex = newModIndex;
        inspectorView.gameObject.SetActive(true);

        if(inspectorView.scrollView != null) { inspectorView.scrollView.verticalNormalizedPosition = 1f; }

        UpdateInspectorViewPageButtonInteractibility();
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

    public void UpdateFilters()
    {
        // title
        if(String.IsNullOrEmpty(titleSearchField.text))
        {
            nameFilterDelegate = (p) => true;
        }
        else
        {
            string searchString = titleSearchField.text.ToUpper();
            nameFilterDelegate = (profile) =>
            {
                return profile.name.ToUpper().Contains(searchString);
            };
        }

        // tags
        string[] filterTagNames = tagFilterView.selectedTags.ToArray();

        if(filterTagNames.Length == 0)
        {
            tagFilterDelegate = (p) => true;
        }
        else
        {
            tagFilterDelegate = (profile) =>
            {
                if(profile.tags == null) { return false; }

                foreach(string filterTag in filterTagNames)
                {
                    if(!profile.tagNames.Contains(filterTag)) { return false; }
                }

                return true;
            };
        }

        // TODO(@jackson): BAD ZERO?
        LoadFilteredProfiles(explorerView.currentPage.profiles,
                             0,
                             explorerView.itemCount);
        // TODO(@jackson): Update Mod Count

        explorerView.UpdateCurrentPageDisplay();
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

    public static Sprite CreateSpriteWithTexture(Texture2D texture)
    {
        return Sprite.Create(texture,
                             new Rect(0.0f, 0.0f, texture.width, texture.height),
                             Vector2.zero);
    }
}
