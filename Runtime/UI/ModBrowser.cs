using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Directory = System.IO.Directory;

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
                    ModBrowser._instance = UIUtilities.FindComponentInAllScenes<ModBrowser>(true);

                    if(ModBrowser._instance == null)
                    {
                        GameObject go = new GameObject("Mod Browser");
                        ModBrowser._instance = go.AddComponent<ModBrowser>();
                    }
                }

                return ModBrowser._instance;
            }
        }

        // ---------[ CONST & STATIC ]---------
        /// <summary>Number of seconds between mod event polls.</summary>
        private const float MOD_EVENT_POLLING_PERIOD = 120f;

        /// <summary>Number of seconds between user event polls.</summary>
        private const float USER_EVENT_POLLING_PERIOD = 15f;

        // --- RUNTIME DATA ---
        private GameProfile m_gameProfile = new GameProfile();
        private Dictionary<int, ModRatingValue> m_userRatings = new Dictionary<int, ModRatingValue>();
        private int m_lastModEventId = -1;
        private int m_lastUserEventId = -1;

        // --- ACCESSORS ---
        public GameProfile gameProfile
        {
            get { return this.m_gameProfile; }
        }

        // ---------[ INITIALIZATION ]---------
        private void Awake()
        {
            if(ModBrowser._instance == null)
            {
                ModBrowser._instance = this;
            }
            #if DEBUG
            else if(ModBrowser._instance != this)
            {
                Debug.LogWarning("[mod.io] Second instance of a ModBrowser"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a ModBrowser"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif
        }

        private void OnEnable()
        {
            this.StartCoroutine(StartFetchRemoteData());

            ModManager.onModBinaryInstalled += this.OnModInstalled;
            DownloadClient.modfileDownloadFailed += this.OnModfileDownloadFailed;
        }

        private void OnDisable()
        {
            UserAccountManagement.PushSubscriptionChanges(null, null);
            ModManager.onModBinaryInstalled -= this.OnModInstalled;
            DownloadClient.modfileDownloadFailed += this.OnModfileDownloadFailed;
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
        }

        private void LoadLocalData()
        {
            // - GameData -
            // TODO(@jackson): Remove?
            m_gameProfile = CacheClient.LoadGameProfile();
            if(m_gameProfile == null)
            {
                m_gameProfile = new GameProfile();
                m_gameProfile.id = PluginSettings.data.gameId;
            }

            IEnumerable<IGameProfileUpdateReceiver> updateReceivers = GetComponentsInChildren<IGameProfileUpdateReceiver>(true);
            foreach(var receiver in updateReceivers)
            {
                receiver.OnGameProfileUpdated(m_gameProfile);
            }
        }

        private System.Collections.IEnumerator StartFetchRemoteData()
        {
            // Ensure Start() has finished
            yield return null;

            if(this == null || !this.isActiveAndEnabled)
            {
                yield break;
            }

            if(!UserDataStorage.isInitialized)
            {
                bool isDone = false;
                UserDataStorage.InitializeForUser(null, () => { isDone = true; });

                while(!isDone) { yield return null; }
            }

            if(UserDataStorage.isInitialized)
            {
                bool isDone = false;
                LocalUser.Load(() => isDone = true);

                while(!isDone) { yield return null; }
            }
            else
            {
                Debug.LogWarning("[mod.io] Failed to initialize user data."
                                 + " A temporary LocalUser instance will be created.");

                LocalUser.instance = new LocalUser();
            }

            if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                this.StartCoroutine(FetchUserProfile());
            }
            else // if invalid token, check if externally authenticated
            {
                bool isAttemptingReauth = false;

                if(!string.IsNullOrEmpty(LocalUser.ExternalAuthentication.ticket))
                {
                    isAttemptingReauth = true;

                    UserAccountManagement.ReauthenticateWithStoredExternalAuthData(
                    (u) =>
                    {
                        IEnumerable<IAuthenticatedUserUpdateReceiver> updateReceivers = GetComponentsInChildren<IAuthenticatedUserUpdateReceiver>(true);
                        foreach(var receiver in updateReceivers)
                        {
                            receiver.OnUserProfileUpdated(u);
                        }

                        isAttemptingReauth = false;
                    },
                    (e) =>
                    {
                        Debug.Log("[mod.io] Failed to reauthenticate using stored external authentication data.\n"
                                  + e.errorMessage);

                        isAttemptingReauth = false;
                    });
                }

                while(isAttemptingReauth) { yield return null; }
            }

            this.StartCoroutine(FetchGameProfile());

            yield return this.StartCoroutine(SynchronizeSubscriptionsAndUpdateModProfiles());

            this.StartCoroutine(VerifySubscriptionInstallations());
            this.StartCoroutine(PollForSubscribedModEventsCoroutine());
            this.StartCoroutine(PollForUserEventsCoroutine());
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
                    if(this == null) { return; }

                    m_gameProfile = g;

                    IEnumerable<IGameProfileUpdateReceiver> updateReceivers = GetComponentsInChildren<IGameProfileUpdateReceiver>(true);
                    foreach(var receiver in updateReceivers)
                    {
                        receiver.OnGameProfileUpdated(g);
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
                        if(string.IsNullOrEmpty(LocalUser.OAuthToken))
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

                            LocalUser.WasTokenRejected = true;
                            LocalUser.Save();
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

                        yield return new WaitForSecondsRealtime(reattemptDelay);
                        continue;
                    }
                }
            }
        }

        private System.Collections.IEnumerator FetchUserProfile()
        {
            Debug.Assert(LocalUser.AuthenticationState == AuthenticationState.ValidToken);

            bool succeeded = false;

            // get user profile
            while(!succeeded)
            {
                bool isRequestDone = false;
                WebRequestError requestError = null;

                // requests
                UserAccountManagement.UpdateUserProfile(
                (u) =>
                {
                    IEnumerable<IAuthenticatedUserUpdateReceiver> updateReceivers = GetComponentsInChildren<IAuthenticatedUserUpdateReceiver>(true);
                    foreach(var receiver in updateReceivers)
                    {
                        receiver.OnUserProfileUpdated(u);
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

                        LocalUser.WasTokenRejected = true;
                        LocalUser.Save();

                        yield break;
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        Debug.LogWarning("[mod.io] Fetching User Profile failed.\n"
                                         + requestError.ToUnityDebugString());

                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to collect user profile data from mod.io.\n"
                                                   + requestError.displayMessage);

                        LocalUser.WasTokenRejected = true;
                        LocalUser.Save();

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

                        yield return new WaitForSecondsRealtime(reattemptDelay);
                        continue;
                    }
                }
            }

            StartCoroutine(FetchUserRatings());
        }

        private System.Collections.IEnumerator SynchronizeSubscriptionsAndUpdateModProfiles()
        {
            // ensure changes only effect the profile this call started with
            int userId = UserProfile.NULL_ID;
            if(LocalUser.Profile != null)
            {
                userId = LocalUser.Profile.id;
            }

            Func<bool> hasUserChanged = () =>
            {
                return ((LocalUser.Profile == null && userId != UserProfile.NULL_ID)
                        || (LocalUser.Profile != null && userId != LocalUser.Profile.id));
            };

            // - push any changes -
            if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                // push local actions
                bool isPushDone = false;
                UserAccountManagement.PushSubscriptionChanges(() => isPushDone = true,
                                                              (e) => isPushDone = true);

                while(!isPushDone) { yield return null; }
            }

            if(hasUserChanged()) { yield break; }

            // - configure request detail -
            Action<APIPaginationParameters> requestDelegate = null;
            bool request_isDone = false;
            RequestPage<ModProfile> request_page = null;
            WebRequestError request_error = null;

            if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                RequestFilter userSubFilter = new RequestFilter();
                userSubFilter.AddFieldFilter(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                          new EqualToFilter<int>(PluginSettings.data.gameId));

                requestDelegate
                    = (p) => APIClient.GetUserSubscriptions(userSubFilter, p,
                                                            (r) => { request_isDone = true; request_page = r; },
                                                            (e) => { request_isDone = true; request_error = e; });
            }
            else
            {
                int[] modIdArray = LocalUser.SubscribedModIds.ToArray();

                // early out
                if(modIdArray.Length == 0)
                {
                    yield break;
                }

                RequestFilter modFilter = new RequestFilter();
                modFilter.AddFieldFilter(ModIO.API.GetAllModsFilterFields.id,
                                         new InArrayFilter<int>(modIdArray));

                requestDelegate = (p) => APIClient.GetAllMods(modFilter, p,
                                                              (r) => { request_isDone = true; request_page = r; },
                                                              (e) => { request_isDone = true; request_error = e; });
            }

            // - set up fetch loop data -
            List<ModProfile> subProfiles = new List<ModProfile>();
            List<int> localOnlySubscriptions = new List<int>(LocalUser.SubscribedModIds);
            List<int> queuedUnsubscribes = LocalUser.QueuedUnsubscribes;
            List<int> subsAdded = new List<int>();

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            bool allPagesReceived = false;
            while(!allPagesReceived && !hasUserChanged())
            {
                request_isDone = false;
                request_page = null;
                request_error = null;

                requestDelegate(pagination);

                while(!request_isDone) { yield return null; }

                if(request_error != null)
                {
                    int reattemptDelay = CalculateReattemptDelay(request_error);
                    if(request_error.isAuthenticationInvalid)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                   request_error.displayMessage);

                        LocalUser.WasTokenRejected = true;
                        LocalUser.Save();

                        yield break;
                    }
                    else if(request_error.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to retrieve subscription data from mod.io servers.\n"
                                                   + request_error.displayMessage);
                        yield break;
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to retrieve subscription data from mod.io servers.\n"
                                                   + request_error.displayMessage
                                                   + "\nRetrying in "
                                                   + reattemptDelay.ToString()
                                                   + " seconds");

                        yield return new WaitForSecondsRealtime(reattemptDelay);
                        continue;
                    }
                }
                else
                {
                    // update tracking lists
                    foreach(ModProfile profile in request_page.items)
                    {
                        if(!queuedUnsubscribes.Contains(profile.id))
                        {
                            subProfiles.Add(profile);

                            if(!localOnlySubscriptions.Remove(profile.id))
                            {
                                subsAdded.Add(profile.id);
                            }
                        }
                    }

                    // check pages
                    allPagesReceived = (request_page.items.Length < request_page.size);
                    if(!allPagesReceived)
                    {
                        pagination.offset += pagination.limit;
                    }
                }
            }

            if(hasUserChanged() || !allPagesReceived) { yield break; }

            // handle removed ids
            List<int> queuedSubscribes = LocalUser.QueuedSubscribes;
            List<int> subsRemoved = new List<int>();

            foreach(int modId in localOnlySubscriptions)
            {
                // check if added during fetch
                if(!queuedSubscribes.Contains(modId))
                {
                    subsRemoved.Add(modId);
                    LocalUser.SubscribedModIds.Remove(modId);
                }
            }

            // append added sub modids
            LocalUser.SubscribedModIds.AddRange(subsAdded);
            LocalUser.Save();

            // cache profiles
            ModProfileRequestManager.instance.CacheModProfiles(subProfiles);

            // handle changes
            OnSubscriptionsChanged(subsAdded, subsRemoved);
        }

        private System.Collections.IEnumerator FetchUserRatings()
        {
            APIPaginationParameters pagination = new APIPaginationParameters();
            RequestFilter filter = new RequestFilter();
            filter.AddFieldFilter(API.GetUserRatingsFilterFields.gameId,
                                  new EqualToFilter<int>() { filterValue = m_gameProfile.id });

            bool isRequestDone = false;
            List<ModRating> retrievedRatings = new List<ModRating>();

            while(LocalUser.AuthenticationState == AuthenticationState.ValidToken
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

                        LocalUser.WasTokenRejected = true;
                        LocalUser.Save();

                        yield break;
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        yield break;
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(reattemptDelay);
                        continue;
                    }
                }
                else
                {
                    retrievedRatings.AddRange(response.items);

                    isRequestDone = (response.size + response.resultOffset >= response.resultTotal);
                }
            }

            m_userRatings = new Dictionary<int, ModRatingValue>();
            foreach(ModRating rating in retrievedRatings)
            {
                m_userRatings.Add(rating.modId, rating.ratingValue);
            }
        }

        private System.Collections.IEnumerator VerifySubscriptionInstallations()
        {
            var subscribedModIds = LocalUser.SubscribedModIds;
            IList<ModfileIdPair> installedModVersions = ModManager.GetInstalledModVersions(false);
            Dictionary<int, List<int>> groupedIds = new Dictionary<int, List<int>>();

            // create map
            foreach(int modId in subscribedModIds)
            {
                groupedIds.Add(modId, new List<int>());
            }

            // sort installs
            foreach(ModfileIdPair idPair in installedModVersions)
            {
                if(subscribedModIds.Contains(idPair.modId))
                {
                    groupedIds[idPair.modId].Add(idPair.modfileId);
                }
                else
                {
                    bool isUninstallDone = false;

                    ModManager.UninstallMod(idPair.modId, (s) => isUninstallDone = true);

                    while(!isUninstallDone) { yield return null; }
                }
            }

            // assert subbed mod installs
            List<Modfile> modfilesToAssert = new List<Modfile>(subscribedModIds.Count);
            bool isRequestDone = false;

            ModProfileRequestManager.instance.RequestModProfiles(subscribedModIds,
            (modProfiles) =>
            {
                foreach(ModProfile profile in modProfiles)
                {
                    if(profile != null
                       && profile.currentBuild != null
                       && LocalUser.SubscribedModIds.Contains(profile.id))
                    {
                        if(profile.currentBuild.modId != profile.id)
                        {
                            Debug.LogWarning("[mod.io] Profile \'" + profile.name + "("
                                             + profile.id.ToString() + ") has a bad modfile."
                                             + "\nThe modfile.modId is mismatched ("
                                             + profile.currentBuild.modId.ToString() + ").");
                        }
                        else
                        {
                            modfilesToAssert.Add(profile.currentBuild);
                        }
                    }
                }

                isRequestDone = true;
            },
            (e) =>
            {
                modfilesToAssert = null;
                isRequestDone = true;
            });

            while(!isRequestDone) { yield return null; }

            if(modfilesToAssert != null)
            {
                yield return this.StartCoroutine(ModManager.AssertDownloadedAndInstalled_Coroutine(modfilesToAssert));
            }
        }

        private System.Collections.IEnumerator FetchAllModProfiles(int[] modIds,
                                                                   Action<List<ModProfile>> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            // early out for no profiles
            if(modIds == null || modIds.Length == 0)
            {
                onSuccess(new List<ModProfile>(0));
                yield break;
            }

            List<ModProfile> modProfiles = new List<ModProfile>();

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };
            RequestFilter filter = new RequestFilter();
            filter.AddFieldFilter(API.GetAllModsFilterFields.id,
                new InArrayFilter<int>() { filterArray = modIds, });

            bool isDone = false;

            while(!isDone)
            {
                RequestPage<ModProfile> page = null;
                WebRequestError error = null;

                APIClient.GetAllMods(filter, pagination,
                                     (r) => page = r,
                                     (e) => error = e);

                while(page == null && error == null) { yield return null;}

                if(error != null)
                {
                    if(onError != null)
                    {
                        onError(error);
                    }

                    modProfiles = null;
                    isDone = true;
                }
                else
                {
                    modProfiles.AddRange(page.items);

                    if(page.resultTotal <= (page.resultOffset + page.size))
                    {
                        isDone = true;
                    }
                    else
                    {
                        pagination.offset = page.resultOffset + page.size;
                    }
                }
            }

            if(isDone && modProfiles != null)
            {
                onSuccess(modProfiles);
            }
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
        private System.Collections.IEnumerator PollForSubscribedModEventsCoroutine()
        {
            bool isRequestDone = false;
            WebRequestError requestError = null;
            bool cancelUpdates = false;

            // fetch initial id
            RequestFilter idFetchFilter = new RequestFilter()
            {
                sortFieldName = API.GetAllModEventsFilterFields.id,
                isSortAscending = false,
            };

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                offset = 0,
                limit = 1,
            };

            while(!isRequestDone)
            {
                APIClient.GetAllModEvents(idFetchFilter, pagination,
                (r) =>
                {
                    if(r.items.Length > 0)
                    {
                        this.m_lastModEventId = r.items[0].id;
                    }

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

                        LocalUser.WasTokenRejected = true;
                        LocalUser.Save();
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        cancelUpdates = true;
                    }
                    else
                    {
                        isRequestDone = false;

                        yield return new WaitForSecondsRealtime(reattemptDelay);
                    }
                }
            }

            // initial delay
            yield return new WaitForSecondsRealtime(MOD_EVENT_POLLING_PERIOD);

            // start loop
            while(this != null
                  && this.isActiveAndEnabled
                  && !cancelUpdates)
            {
                isRequestDone = false;
                requestError = null;

                // --- MOD EVENTS ---
                var subbedMods = LocalUser.SubscribedModIds;
                if(subbedMods != null
                   && subbedMods.Count > 0)
                {
                    List<ModEvent> modEventResponse = null;

                    ModManager.FetchModEventsAfterId(this.m_lastModEventId,
                                                     LocalUser.SubscribedModIds,
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

                            LocalUser.WasTokenRejected = true;
                            LocalUser.Save();
                        }
                        else if(requestError.isRequestUnresolvable
                                || reattemptDelay < 0)
                        {
                            cancelUpdates = true;
                        }
                        else
                        {
                            yield return new WaitForSecondsRealtime(reattemptDelay);
                            continue;
                        }
                    }
                    else
                    {
                        if(modEventResponse.Count > 0)
                        {
                            this.m_lastModEventId = modEventResponse[modEventResponse.Count-1].id;
                            yield return StartCoroutine(ProcessModUpdates(modEventResponse));
                        }
                    }
                }

                yield return new WaitForSecondsRealtime(MOD_EVENT_POLLING_PERIOD);
            }
        }

        private System.Collections.IEnumerator PollForUserEventsCoroutine()
        {
            bool isRequestDone = false;
            WebRequestError requestError = null;
            bool cancelUpdates = false;

            // fetch initial id
            RequestFilter idFetchFilter = new RequestFilter()
            {
                sortFieldName = API.GetUserEventsFilterFields.id,
                isSortAscending = false,
            };

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                offset = 0,
                limit = 1,
            };

            while(!isRequestDone)
            {
                APIClient.GetAllModEvents(idFetchFilter, pagination,
                (r) =>
                {
                    if(r.items.Length > 0)
                    {
                        this.m_lastUserEventId = r.items[0].id;
                    }

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

                        LocalUser.WasTokenRejected = true;
                        LocalUser.Save();
                    }
                    else if(requestError.isRequestUnresolvable
                            || reattemptDelay < 0)
                    {
                        cancelUpdates = true;
                    }
                    else
                    {
                        isRequestDone = false;

                        yield return new WaitForSecondsRealtime(reattemptDelay);
                    }
                }
            }

            yield return new WaitForSecondsRealtime(USER_EVENT_POLLING_PERIOD);

            while(this != null
                  && this.isActiveAndEnabled
                  && !cancelUpdates)
            {

                isRequestDone = false;
                requestError = null;

                if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
                {
                    // fetch user events
                    List<UserEvent> userEventReponse = null;

                    ModManager.FetchUserEventsAfterId(this.m_lastUserEventId,
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

                            LocalUser.WasTokenRejected = true;
                            LocalUser.Save();
                        }
                        else if(requestError.isRequestUnresolvable
                                || reattemptDelay < 0)
                        {
                            Debug.LogWarning("[mod.io] Polling for user updates failed."
                                             + requestError.ToUnityDebugString());

                            cancelUpdates = true;
                        }
                        else
                        {
                            MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                       "Failed to synchronize subscriptions with mod.io servers.\n"
                                                       + requestError.displayMessage
                                                       + "\nRetrying in "
                                                       + reattemptDelay.ToString()
                                                       + " seconds");

                            yield return new WaitForSecondsRealtime(reattemptDelay);
                            continue;
                        }
                    }
                    // This may have changed during the request execution
                    else if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
                    {
                        if(userEventReponse.Count > 0)
                        {
                            this.m_lastUserEventId = userEventReponse[userEventReponse.Count-1].id;
                            ProcessUserUpdates(userEventReponse);
                        }

                        bool isPushDone = false;

                        UserAccountManagement.PushSubscriptionChanges(() => isPushDone = true,
                                                                      (e) => isPushDone = true);

                        while(!isPushDone) { yield return null; }

                        StartCoroutine(FetchUserRatings());
                    }
                }

                yield return new WaitForSecondsRealtime(USER_EVENT_POLLING_PERIOD);
            }
        }

        protected void ProcessUserUpdates(List<UserEvent> userEvents)
        {
            List<int> subscribedModIds = LocalUser.SubscribedModIds;
            List<int> queuedSubscribes = LocalUser.QueuedSubscribes;
            List<int> queuedUnsubscribes = LocalUser.QueuedUnsubscribes;

            List<int> newSubs = new List<int>();
            List<int> newUnsubs = new List<int>();

            foreach(UserEvent ue in userEvents)
            {
                switch(ue.eventType)
                {
                    case UserEventType.ModSubscribed:
                    {
                        queuedSubscribes.Remove(ue.modId);

                        if(!subscribedModIds.Contains(ue.modId)
                           && !queuedUnsubscribes.Contains(ue.modId))
                        {
                            subscribedModIds.Add(ue.modId);
                            newSubs.Add(ue.modId);
                        }
                    }
                    break;

                    case UserEventType.ModUnsubscribed:
                    {
                        queuedUnsubscribes.Remove(ue.modId);

                        if(subscribedModIds.Contains(ue.modId)
                           && !queuedSubscribes.Contains(ue.modId))
                        {
                            subscribedModIds.Remove(ue.modId);
                            newUnsubs.Add(ue.modId);
                        }
                    }
                    break;
                }
            }

            LocalUser.Save();

            if(newSubs.Count > 0 || newUnsubs.Count > 0)
            {
                OnSubscriptionsChanged(newSubs, newUnsubs);

                if(newSubs.Count > 0)
                {
                    string message = (newSubs.Count.ToString() + " subscription"
                                      + (newSubs.Count > 1 ? "s" : "")
                                      + " retrieved from the server");
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
                }
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

                // remove subs for deleted mods
                if(deletedMods.Count > 0)
                {
                    var subscribedModIds = LocalUser.SubscribedModIds;

                    foreach(int modId in deletedMods)
                    {
                        subscribedModIds.Remove(modId);
                    }

                    OnSubscriptionsChanged(null, deletedMods);

                    int deletedModCount = deletedMods.Count;
                    string message;
                    if(deletedModCount == 1)
                    {
                        message = "One of your subscribed mods is now unavailable and was removed from your subscriptions.";
                    }
                    else
                    {
                        message = deletedModCount.ToString() + " subscribed mods have become unavailable and have been removed from your subscriptions.";
                    }
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
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
                    modFilter.AddFieldFilter(API.GetAllModsFilterFields.id, new InArrayFilter<int>()
                    {
                        filterArray = editedMods.ToArray()
                    });

                    APIClient.GetAllMods(modFilter, pagination,
                    (r) =>
                    {
                        ModProfileRequestManager.instance.CacheModProfiles(r.items);
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
                    modFilter.AddFieldFilter(API.GetAllModsFilterFields.id, new InArrayFilter<int>()
                    {
                        filterArray = modfileChanged.ToArray()
                    });

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

                            LocalUser.WasTokenRejected = true;
                            LocalUser.Save();
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
                        ModProfileRequestManager.instance.CacheModProfiles(response.items);

                        List<Modfile> latestBuilds = new List<Modfile>(response.items.Length);
                        List<int> subscribedModIds = LocalUser.SubscribedModIds;
                        foreach(ModProfile profile in response.items)
                        {
                            if(profile != null
                               && profile.currentBuild != null
                               && subscribedModIds.Contains(profile.id))
                            {
                                latestBuilds.Add(profile.currentBuild);
                            }
                        }

                        yield return this.StartCoroutine(ModManager.AssertDownloadedAndInstalled_Coroutine(latestBuilds));
                    }
                }
            }
        }

        // ---------[ USER CONTROL ]---------
        public void OnUserLogin()
        {
            if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                this.StartCoroutine(SynchronizeSubscriptionsAndUpdateModProfiles());
            }
        }

        public void LogUserOut()
        {
            // push queued subs/unsubs
            UserAccountManagement.PushSubscriptionChanges(null,
                                                          (e) => WebRequestError.LogAsWarning(e[0]));

            // - clear current user -
            LocalUser oldUser = LocalUser.instance;

            LocalUser.instance = new LocalUser()
            {
                subscribedModIds = oldUser.subscribedModIds,
                enabledModIds = oldUser.enabledModIds,
            };
            LocalUser.isLoaded = true;

            LocalUser.Save();

            // - notify receivers -
            IEnumerable<IAuthenticatedUserUpdateReceiver> updateReceivers = GetComponentsInChildren<IAuthenticatedUserUpdateReceiver>(true);
            foreach(var receiver in updateReceivers)
            {
                receiver.OnUserLoggedOut();
            }

            // - notify -
            MessageSystem.QueueMessage(MessageDisplayData.Type.Success,
                                       "Successfully logged out");
        }

        // ---------[ ENABLE/SUBSCRIBE MODS ]---------
        public void SubscribeToMod(int modId)
        {
            UserAccountManagement.SubscribeToMod(modId);
            OnSubscribedToMod(modId);
        }

        public void UnsubscribeFromMod(int modId)
        {
            UserAccountManagement.UnsubscribeFromMod(modId);
            OnUnsubscribedFromMod(modId);
        }

        public void OnSubscribedToMod(int modId)
        {
            EnableMod(modId);
            UpdateSubscriptionReceivers(new int[] { modId }, null);

            ModProfileRequestManager.instance.RequestModProfile(modId,
            (p) =>
            {
                if(this != null && this.isActiveAndEnabled
                   && p != null && p.currentBuild != null
                   && LocalUser.SubscribedModIds.Contains(p.id))
                {
                    this.StartCoroutine(ModManager.AssertDownloadedAndInstalled_Coroutine(new Modfile[] { p.currentBuild }));
                }
            },
            (requestError) =>
            {
                if(requestError.isAuthenticationInvalid)
                {
                    LocalUser.WasTokenRejected = true;
                    LocalUser.Save();

                    MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                               requestError.displayMessage);
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

            ModManager.UninstallMod(modId, null);

            DisableMod(modId);

            UpdateSubscriptionReceivers(null, new int[] { modId });
        }

        public void OnSubscriptionsChanged(IList<int> addedSubscriptions,
                                           IList<int> removedSubscriptions)
        {
            // handle new subscriptions
            if(addedSubscriptions != null
               && addedSubscriptions.Count > 0)
            {
                // enable mods
                foreach(int modId in addedSubscriptions)
                {
                    if(!LocalUser.EnabledModIds.Contains(modId))
                    {
                        LocalUser.EnabledModIds.Add(modId);
                    }
                }

                // start downloads
                ModProfileRequestManager.instance.RequestModProfiles(addedSubscriptions,
                (modProfiles) =>
                {
                    if(this != null && this.isActiveAndEnabled)
                    {
                        var subbedMods = LocalUser.SubscribedModIds;

                        List<Modfile> modfiles = new List<Modfile>(modProfiles.Length);

                        foreach(ModProfile p in modProfiles)
                        {
                            if(p != null
                               && p.currentBuild != null
                               && subbedMods.Contains(p.id))
                            {
                                modfiles.Add(p.currentBuild);
                            }
                        }

                        this.StartCoroutine(ModManager.AssertDownloadedAndInstalled_Coroutine(modfiles));
                    }
                },
                (requestError) =>
                {
                    if(requestError.isAuthenticationInvalid)
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                   requestError.displayMessage);

                        LocalUser.WasTokenRejected = true;
                        LocalUser.Save();
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   "Failed to start mod downloads. They will be retried shortly.\n"
                                                   + requestError.displayMessage);
                    }
                });
            }

            if(removedSubscriptions != null
               && removedSubscriptions.Count > 0)
            {
                foreach(int modId in removedSubscriptions)
                {
                    // remove from disk
                    CacheClient.DeleteAllModfileAndBinaryData(modId);

                    ModManager.UninstallMod(modId, null);

                    // disable
                    LocalUser.EnabledModIds.Remove(modId);
                }
            }

            LocalUser.Save();

            UpdateSubscriptionReceivers(addedSubscriptions, removedSubscriptions);
        }

        private void UpdateSubscriptionReceivers(IList<int> addedSubscriptions,
                                                 IList<int> removedSubscriptions)
        {
            if(addedSubscriptions == null)  { addedSubscriptions = new int[0]; }
            if(removedSubscriptions == null){ removedSubscriptions = new int[0]; }

            IEnumerable<IModSubscriptionsUpdateReceiver> updateReceivers = GetComponentsInChildren<IModSubscriptionsUpdateReceiver>(true);
            foreach(var receiver in updateReceivers)
            {
                receiver.OnModSubscriptionsUpdated(addedSubscriptions,
                                                   removedSubscriptions);
            }
        }

        public void EnableMod(int modId)
        {
            if(!LocalUser.EnabledModIds.Contains(modId))
            {
                LocalUser.EnabledModIds.Add(modId);
                LocalUser.Save();
            }

            IEnumerable<IModEnabledReceiver> updateReceivers = GetComponentsInChildren<IModEnabledReceiver>(true);
            foreach(var receiver in updateReceivers)
            {
                receiver.OnModEnabled(modId);
            }
        }

        public void DisableMod(int modId)
        {
            if(LocalUser.EnabledModIds.Contains(modId))
            {
                LocalUser.EnabledModIds.Remove(modId);
                LocalUser.Save();
            }

            IEnumerable<IModDisabledReceiver> updateReceivers = GetComponentsInChildren<IModDisabledReceiver>(true);
            foreach(var receiver in updateReceivers)
            {
                receiver.OnModDisabled(modId);
            }
        }

        public void AttemptRateMod(int modId, ModRatingValue ratingValue)
        {
            if(ratingValue == ModRatingValue.None)
            {
                Debug.Log("[mod.io] Clearing a rating is currently unsupported.");
                return;
            }

            if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                ModRatingValue oldRating = this.GetModRating(modId);

                // notify receivers
                IEnumerable<IModRatingAddedReceiver> ratingReceivers = GetComponentsInChildren<IModRatingAddedReceiver>(true);
                foreach(var receiver in ratingReceivers)
                {
                    receiver.OnModRatingAdded(modId, ratingValue);
                }

                // send request
                var ratingParameters = new API.AddModRatingParameters()
                {
                    ratingValue = ratingValue,
                };

                APIClient.AddModRating(modId, ratingParameters, (m) =>
                {
                    if(this != null)
                    {
                        this.m_userRatings[modId] = ratingValue;
                    }
                },
                (e) =>
                {
                    if(this == null) { return; }

                    // NOTE(@jackson): This is workaround is due to the response of a repeat rating
                    // request returning an error.
                    if(e.webRequest.responseCode == 400)
                    {
                        this.m_userRatings[modId] = ratingValue;
                    }
                    else
                    {
                        MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                                   e.displayMessage);

                        foreach(var receiver in ratingReceivers)
                        {
                            if(receiver != null)
                            {
                                receiver.OnModRatingAdded(modId, oldRating);
                            }
                        }
                    }
                });
            }
            else
            {
                ViewManager.instance.ShowLoginDialog();
            }
        }

        public ModRatingValue GetModRating(int modId)
        {
            ModRatingValue ratingValue;
            if(!this.m_userRatings.TryGetValue(modId, out ratingValue))
            {
                ratingValue = ModRatingValue.None;
            }
            return ratingValue;
        }

        // ---------[ EVENTS ]---------
        private void OnModInstalled(ModfileIdPair idPair)
        {
            if(this == null) { return; }

            ModProfileRequestManager.instance.RequestModProfile(idPair.modId,
            (p) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Info,
                                           p.name + " was successfully downloaded and installed.");
            },
            null);
        }

        private void OnModfileDownloadFailed(ModfileIdPair idPair, WebRequestError error)
        {
            if(this == null) { return; }

            ModProfileRequestManager.instance.RequestModProfile(idPair.modId,
            (p) =>
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                           p.name + " failed to download.\n"
                                           + error.displayMessage);
            },
            null);
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("No longer used.")]
        public const string MANIFEST_FILENAME = "browser_manifest.data";

        [Obsolete("Use PluginSettings.data.logAllRequests instead")]
        public bool debugAllAPIRequests
        {
            get { return PluginSettings.data.logAllRequests; }
        }
        [Obsolete][HideInInspector]
        public ExplorerView explorerView;
        [Obsolete][HideInInspector]
        public InspectorView inspectorView;
        [Obsolete][HideInInspector]
        public SubscriptionsView subscriptionsView;
        [Obsolete][HideInInspector]
        public LoginDialog loginDialog;
        [Obsolete][HideInInspector]
        public UserView loggedUserView;

        [Obsolete("Use AuthenticatedUserViewController.m_guestData instead.")]
        private UserDisplayData m_guestData;
        [Obsolete]
        private UserProfile m_userProfile;

        [Obsolete("Use ViewManager.ActivateExplorerView() instead.")]
        public void ShowExplorerView()
        {
            ViewManager.instance.ActivateExplorerView();
        }

        [Obsolete("Use ModProfileRequestManager.FetchModProfilePage() instead.")]
        public void RequestExplorerPage(int pageIndex,
                                        Action<RequestPage<ModProfile>> onSuccess,
                                        Action<WebRequestError> onError)
        {
            if(this.explorerView == null)
            {
                if(onError != null) { onError(null); }
            }
            else
            {
                ModProfileRequestManager.instance.FetchModProfilePage(this.explorerView.GenerateRequestFilter(),
                                                                      pageIndex * this.explorerView.itemsPerPage,
                                                                      this.explorerView.itemsPerPage,
                                                                      onSuccess, onError);
            }
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

        [Obsolete("Use ExplorerView.Refresh() instead.")]
        public void UpdateExplorerFilters()
        {
            explorerView.Refresh();
        }

        [Obsolete("Use ExplorerView.ChangePage() instead.")]
        public void ChangeExplorerPage(int direction)
        {
            explorerView.ChangePage(direction);
        }

        [Obsolete("Use ViewManager.ActivateSubscriptionsView() instead.")]
        public void ShowSubscriptionsView()
        {
            ViewManager.instance.ActivateSubscriptionsView();
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

        [Obsolete("Use SubscriptionsView.Refresh() instead.")]
        public void RequestSubscribedModProfiles(Action<List<ModProfile>> onSuccess,
                                                 Action<WebRequestError> onError)
        {
            subscriptionsView.Refresh();
        }

        [Obsolete("Use SubscriptionsView.UpdateFilter() instead.")]
        public void UpdateSubscriptionFilters()
        {
            subscriptionsView.Refresh();
        }

        [Obsolete("Use ViewManager.InspectMod() instead.")]
        public void InspectMod(int modId)
        {
            ViewManager.instance.InspectMod(modId);
        }

        [Obsolete("Use ViewManager.InspectMod() instead.")]
        public void InspectDiscoverItem(ModView view)
        {
            InspectMod(view.data.profile.modId);
        }

        [Obsolete("Use ViewManager.InspectMod() instead.")]
        public void InspectSubscriptionItem(ModView view)
        {
            InspectMod(view.data.profile.modId);
        }

        [Obsolete("Use InspectorView.gameObject.SetActive(false) instead.")]
        public void CloseInspector()
        {
            inspectorView.gameObject.SetActive(false);
        }

        [Obsolete("Use LoginDialog.gameObject.SetActive(true) instead.")]
        public void OpenLoginDialog()
        {
            loginDialog.gameObject.SetActive(true);
        }

        [Obsolete("Use LoginDialog.gameObject.SetActive(false) instead.")]
        public void CloseLoginDialog()
        {
            loginDialog.gameObject.SetActive(false);
        }
    }
}
