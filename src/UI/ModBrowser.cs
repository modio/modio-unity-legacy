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

        // --- Temp ---
        private List<int> m_queuedUnsubscribes
        {
            get { return UserAccountManagement.activeUser.queuedUnsubscribes; }
            set { UserAccountManagement.activeUser.queuedUnsubscribes = value; }
        }

        private List<int> m_queuedSubscribes
        {
            get { return UserAccountManagement.activeUser.queuedSubscribes; }
            set { UserAccountManagement.activeUser.queuedSubscribes = value; }
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
            UserAccountManagement.PushQueuedSubscriptionChanges();
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

            if(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                this.StartCoroutine(FetchUserProfile());
            }
            else // if invalid token, check if externally authenticated
            {
                bool isAttemptingReauth = false;

                if(!string.IsNullOrEmpty(UserAccountManagement.externalAuthentication.ticket))
                {
                    isAttemptingReauth = true;

                    UserAccountManagement.ReauthenticateWithExternalAuthToken(
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

            if(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                yield return this.StartCoroutine(SynchronizeSubscriptionsWithServer());
            }
            else
            {
                yield return this.StartCoroutine(UpdateAllSubscribedModProfiles());
            }

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
                        if(string.IsNullOrEmpty(UserAccountManagement.activeUser.oAuthToken))
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

                            UserAccountManagement.MarkAuthTokenRejected();
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
            Debug.Assert(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken);

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

                        UserAccountManagement.MarkAuthTokenRejected();

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

                        UserAccountManagement.MarkAuthTokenRejected();

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

        /// <summary>Pushes the queued subscribe actions to the server and returns the associated profiles.</summary>
        private System.Collections.IEnumerator PushQueuedSubscribes(Action<List<ModProfile>> onCompleted)
        {
            // early out if not authenticated or no queued subs
            if(UserAccountManagement.activeUser.AuthenticationState != AuthenticationState.ValidToken
               || this.m_queuedSubscribes.Count == 0)
            {
                if(onCompleted != null) { onCompleted(new List<ModProfile>()); }
                yield break;
            }

            // init
            int responsesPending = this.m_queuedSubscribes.Count;
            List<ModProfile> subProfiles = new List<ModProfile>();
            List<int> unavailableMods = new List<int>();

            // push all subs
            foreach(int modId in this.m_queuedSubscribes)
            {
                APIClient.SubscribeToMod(modId,
                (p) =>
                {
                   --responsesPending;
                   subProfiles.Add(p);
                },
                (e) =>
                {
                    // Error for "Mod is already subscribed"
                    if(e.webRequest.responseCode == 400)
                    {
                        APIClient.GetMod(modId,
                        (p) =>
                        {
                            --responsesPending;
                            subProfiles.Add(p);
                        },
                        (gmp_e) =>
                        {
                            --responsesPending;

                            Debug.Log("[mod.io] Failed to collect mod profile for subscription (modId:"
                                      + modId.ToString() + ")."
                                      + "\nError Code: " + e.webRequest.responseCode.ToString()
                                      + "\n" + e.displayMessage);

                            this.m_queuedSubscribes.Remove(modId);
                        });
                    }
                    // Error for "Mod is unavailable"
                    else if(e.webRequest.responseCode == 404)
                    {
                        --responsesPending;

                        this.m_queuedSubscribes.Remove(modId);

                        Debug.Log("[mod.io] Mod has become unavailable since and cannot be"
                                  + " subscribed to. (modId:" + modId.ToString() + ")");

                        unavailableMods.Add(modId);
                    }
                    else
                    {
                        --responsesPending;
                        if(e.isAuthenticationInvalid)
                        {
                            if(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken)
                            {
                                UserAccountManagement.MarkAuthTokenRejected();
                                MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                           e.displayMessage);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[mod.io] Failed to push queued subscribe action for modId:"
                                             + modId.ToString() + "."
                                             + "\nError Code: " + e.webRequest.responseCode.ToString()
                                             + "\n" + e.displayMessage);
                        }
                    }
                });
            }

            // wait for responses
            while(responsesPending > 0) { yield return null; }

            // remove from queue
            foreach(ModProfile profile in subProfiles)
            {
                this.m_queuedSubscribes.Remove(profile.id);
            }

            // check unavailableMods
            if(unavailableMods.Count > 0)
            {
                // update local data
                List<int> subscriptions = UserAccountManagement.activeUser.subscribedModIds;
                foreach(int modId in unavailableMods)
                {
                    subscriptions.Remove(modId);
                }
                UserAccountManagement.activeUser.subscribedModIds = subscriptions;

                // inform and act
                OnSubscriptionsChanged(null, unavailableMods);

                // message
                MessageSystem.QueueMessage(MessageDisplayData.Type.Warning,
                                           unavailableMods.Count + " mod"
                                           + (unavailableMods.Count > 1 ? "s have" : " has")
                                           + " become unavailable and will be removed from your"
                                           + " subscriptions.");
            }

            // done!
            if(onCompleted != null)
            {
                onCompleted(subProfiles);
            }
        }

        /// <summary>Pushes the queued unsubscribe actions to the server and returns the successful unsubscribes.</summary>
        private System.Collections.IEnumerator PushQueuedUnsubscribes(Action<List<int>> onCompleted)
        {
            // early out if not authenticated or no queued actions
            if(UserAccountManagement.activeUser.AuthenticationState != AuthenticationState.ValidToken
               || this.m_queuedUnsubscribes.Count == 0)
            {
                if(onCompleted != null) { onCompleted(new List<int>(0)); }
                yield break;
            }

            // init
            int responsesPending = this.m_queuedUnsubscribes.Count;
            List<int> successfulUnsubs = new List<int>(this.m_queuedUnsubscribes.Count);

            // push all unsubs
            foreach(int modId in this.m_queuedUnsubscribes)
            {
                APIClient.UnsubscribeFromMod(modId,
                () =>
                {
                   --responsesPending;
                   successfulUnsubs.Add(modId);
                },
                (e) =>
                {
                    // Error for "Mod is already subscribed"
                    if(e.webRequest.responseCode == 400)
                    {
                        --responsesPending;
                        successfulUnsubs.Add(modId);
                    }
                    // Error for "Mod is unavailable"
                    else if(e.webRequest.responseCode == 404)
                    {
                        --responsesPending;
                        successfulUnsubs.Add(modId);
                    }
                    else
                    {
                        --responsesPending;
                        if(e.isAuthenticationInvalid)
                        {
                            if(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken)
                            {
                                UserAccountManagement.MarkAuthTokenRejected();

                                MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                           e.displayMessage);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[mod.io] Failed to push queued unsubscribe action for modId:"
                                             + modId.ToString() + "."
                                             + "\nError Code: " + e.webRequest.responseCode.ToString()
                                             + "\n" + e.displayMessage);
                        }
                    }
                });
            }

            // wait for responses
            while(responsesPending > 0) { yield return null; }

            // remove from queue
            foreach(int modId in successfulUnsubs)
            {
                this.m_queuedUnsubscribes.Remove(modId);
            }

            // done!
            if(onCompleted != null)
            {
                onCompleted(successfulUnsubs);
            }
        }

        /// <summary>Recursively fetches all of the remotely subscribed profiles.</summary>
        private System.Collections.IEnumerator FetchRemoteSubscriptions(Action<RequestPage<ModProfile>> onPageReceived,
                                                                        Action onCompleted,
                                                                        Action<WebRequestError> onFailed)
        {
            // early out if not authenticated
            if(UserAccountManagement.activeUser.AuthenticationState != AuthenticationState.ValidToken)
            {
                if(onCompleted != null) { onCompleted(); }
                yield break;
            }

            // set filter and initial pagination
            RequestFilter subscriptionFilter = new RequestFilter();
            subscriptionFilter.AddFieldFilter(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                new EqualToFilter<int>() { filterValue = PluginSettings.data.gameId, });

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            // get pages
            bool isDone = false;
            while(!isDone)
            {
                bool requestFinished = false;
                WebRequestError error = null;

                APIClient.GetUserSubscriptions(subscriptionFilter, pagination,
                (response) =>
                {
                    onPageReceived(response);

                    pagination.offset = response.resultOffset + response.size;
                    isDone = (pagination.offset >= response.resultTotal);

                    error = null;
                    requestFinished = true;
                },
                (e) =>
                {
                    error = e;
                    requestFinished = true;
                });

                while(!requestFinished) { yield return null; }

                if(error != null)
                {
                    if(!error.isRequestUnresolvable)
                    {
                        int reattemptDelay = CalculateReattemptDelay(error);
                        Debug.Log("[mod.io] Encountered error when attempting to fetch"
                                  + " user subscriptions. Reattempting in "
                                  + reattemptDelay.ToString() + " seconds.\n"
                                  + error.ToUnityDebugString());

                        yield return new WaitForSecondsRealtime(reattemptDelay);
                        continue;
                    }
                    else
                    {
                        if(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken)
                        {
                            UserAccountManagement.MarkAuthTokenRejected();

                            MessageSystem.QueueMessage(MessageDisplayData.Type.Error,
                                                       error.displayMessage);
                        }

                        Debug.Log("[mod.io] Unresolvable error encountered when attempting to"
                                  + " fetch user subscriptions.\n"
                                  + error.ToUnityDebugString());

                        isDone = true;

                        if(onFailed != null) { onFailed(error); }
                        yield break;
                    }
                }
            }

            // done!
            if(onCompleted != null) { onCompleted(); }
        }

        private System.Collections.IEnumerator SynchronizeSubscriptionsWithServer()
        {
            if(UserAccountManagement.activeUser.AuthenticationState != AuthenticationState.ValidToken)
            {
                yield break;
            }

            // push local actions
            yield return this.StartCoroutine(PushQueuedSubscribes(null));
            yield return this.StartCoroutine(PushQueuedUnsubscribes(null));

            // confirm all subscriptions with remote
            int updateStartTimeStamp = ServerTimeStamp.Now;
            List<int> unmatchedSubscriptions = new List<int>(UserAccountManagement.activeUser.subscribedModIds);
            bool completedSuccessfully = false;
            int updateCount = 0;

            yield return this.StartCoroutine(FetchRemoteSubscriptions(
            (requestPage) =>
            {
                // check which profiles are new
                List<int> newSubs = new List<int>();
                foreach(ModProfile profile in requestPage.items)
                {
                    if(!unmatchedSubscriptions.Contains(profile.id)
                       && !m_queuedUnsubscribes.Contains(profile.id)
                       && !m_queuedSubscribes.Contains(profile.id))
                    {
                        newSubs.Add(profile.id);
                    }

                    unmatchedSubscriptions.Remove(profile.id);
                }

                // add to cache
                if(ModProfileRequestManager.instance.isCachingPermitted)
                {
                    foreach(ModProfile profile in requestPage.items)
                    {
                        ModProfileRequestManager.instance.profileCache[profile.id] = profile;
                    }
                }
                CacheClient.SaveModProfiles(requestPage.items);

                // add new subs
                if(newSubs.Count > 0)
                {
                    var subscribedModIds = UserAccountManagement.activeUser.subscribedModIds;
                    subscribedModIds.AddRange(newSubs);
                    UserAccountManagement.activeUser.subscribedModIds = subscribedModIds;
                    OnSubscriptionsChanged(newSubs, null);
                }

                updateCount += newSubs.Count;
            },
            ( ) => completedSuccessfully = true,
            null));

            // handle removed ids
            if(completedSuccessfully)
            {
                if(unmatchedSubscriptions.Count > 0)
                {
                    var remoteUnsubs = new List<int>(unmatchedSubscriptions);
                    var subscribedModIds = UserAccountManagement.activeUser.subscribedModIds;
                    foreach(int modId in unmatchedSubscriptions)
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
                    UserAccountManagement.activeUser.subscribedModIds = subscribedModIds;
                    OnSubscriptionsChanged(null, remoteUnsubs);

                    updateCount += unmatchedSubscriptions.Count;
                }

            }

            // display message
            if(updateCount > 0)
            {
                string message = (updateCount.ToString() + " subscription"
                                  + (updateCount > 1 ? "s" : "")
                                  + " synchronized with the server");
                MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
            }
        }

        private System.Collections.IEnumerator UpdateAllSubscribedModProfiles()
        {
            int updateStartTimeStamp = ServerTimeStamp.Now;
            List<int> subscribedModIds = UserAccountManagement.activeUser.subscribedModIds;

            // early out
            if(subscribedModIds.Count == 0)
            {
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
            modFilter.AddFieldFilter(ModIO.API.GetAllModsFilterFields.id,
                                     new InArrayFilter<int>() { filterArray = subscribedModIds.ToArray(), });

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

                        UserAccountManagement.MarkAuthTokenRejected();

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

                        yield return new WaitForSecondsRealtime(reattemptDelay);
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

                    subscribedModIds = UserAccountManagement.activeUser.subscribedModIds;
                    foreach(int modId in removedModIds)
                    {
                        subscribedModIds.Remove(modId);
                    }
                    UserAccountManagement.activeUser.subscribedModIds = subscribedModIds;

                    OnSubscriptionsChanged(null, removedModIds);

                    int deletedModCount = removedModIds.Count;
                    string message = (deletedModCount.ToString()
                                      + (deletedModCount > 1
                                         ? " subscribed mods have become unavailable and have been removed from your subscriptions."
                                         : " subscribed mod is now unavailable and was removed from your subscriptions."));
                    MessageSystem.QueueMessage(MessageDisplayData.Type.Info, message);
                }

            }
        }

        private System.Collections.IEnumerator FetchUserRatings()
        {
            APIPaginationParameters pagination = new APIPaginationParameters();
            RequestFilter filter = new RequestFilter();
            filter.AddFieldFilter(API.GetUserRatingsFilterFields.gameId,
                                  new EqualToFilter<int>() { filterValue = m_gameProfile.id });

            bool isRequestDone = false;
            List<ModRating> retrievedRatings = new List<ModRating>();

            while(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken
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

                        UserAccountManagement.MarkAuthTokenRejected();

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
            var subscribedModIds = UserAccountManagement.activeUser.subscribedModIds;
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
                    ModManager.TryUninstallAllModVersions(idPair.modId);
                }
            }

            // assert subbed mod installs
            List<Modfile> modfilesToAssert = new List<Modfile>(subscribedModIds.Count);
            bool isRequestDone = false;

            ModProfileRequestManager.instance.RequestModProfiles(subscribedModIds,
            (modProfiles) =>
            {
                subscribedModIds = UserAccountManagement.activeUser.subscribedModIds;

                foreach(ModProfile profile in modProfiles)
                {
                    if(profile != null
                       && profile.currentBuild != null
                       && subscribedModIds.Contains(profile.id))
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

                        UserAccountManagement.MarkAuthTokenRejected();
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
                var subbedMods = UserAccountManagement.activeUser.subscribedModIds;
                if(subbedMods != null
                   && subbedMods.Count > 0)
                {
                    List<ModEvent> modEventResponse = null;

                    ModManager.FetchModEventsAfterId(this.m_lastModEventId,
                                                     UserAccountManagement.activeUser.subscribedModIds,
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

                            UserAccountManagement.MarkAuthTokenRejected();
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

                        UserAccountManagement.MarkAuthTokenRejected();
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
                int updateStartTimeStamp = ServerTimeStamp.Now;

                isRequestDone = false;
                requestError = null;

                if(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken)
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

                            UserAccountManagement.MarkAuthTokenRejected();
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
                    else if(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken)
                    {
                        if(userEventReponse.Count > 0)
                        {
                            this.m_lastUserEventId = userEventReponse[userEventReponse.Count-1].id;
                            ProcessUserUpdates(userEventReponse);
                        }

                        yield return this.StartCoroutine(this.PushQueuedSubscribes(null));
                        yield return this.StartCoroutine(this.PushQueuedUnsubscribes(null));

                        StartCoroutine(FetchUserRatings());
                    }
                }

                yield return new WaitForSecondsRealtime(USER_EVENT_POLLING_PERIOD);
            }
        }

        private void PushSubscriptionChanges()
        {
            if(this == null
               || !this.isActiveAndEnabled
               || UserAccountManagement.activeUser.AuthenticationState != AuthenticationState.ValidToken)
            {
                return;
            }

            // push subs/unsubs
            this.StartCoroutine(PushQueuedSubscribes(null));
            this.StartCoroutine(PushQueuedUnsubscribes(null));
        }

        protected void ProcessUserUpdates(List<UserEvent> userEvents)
        {
            List<int> subscribedModIds = UserAccountManagement.activeUser.subscribedModIds;
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
                UserAccountManagement.activeUser.subscribedModIds = subscribedModIds;

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
                    var subscribedModIds = UserAccountManagement.activeUser.subscribedModIds;

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

                            UserAccountManagement.MarkAuthTokenRejected();
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

                        List<Modfile> latestBuilds = new List<Modfile>(response.items.Length);
                        List<int> subscribedModIds = UserAccountManagement.activeUser.subscribedModIds;
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
            m_queuedSubscribes.AddRange(UserAccountManagement.activeUser.subscribedModIds);

            if(UserAccountManagement.activeUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                this.StartCoroutine(SynchronizeSubscriptionsWithServer());
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

            // - clear current user -
            UserAccountManagement.activeUser = new LocalUser();
            UserAccountManagement.SaveActiveUser();

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
                   && UserAccountManagement.activeUser.subscribedModIds.Contains(p.id))
                {
                    this.StartCoroutine(ModManager.AssertDownloadedAndInstalled_Coroutine(new Modfile[] { p.currentBuild }));
                }
            },
            (requestError) =>
            {
                if(requestError.isAuthenticationInvalid)
                {
                    UserAccountManagement.MarkAuthTokenRejected();

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

            ModManager.TryUninstallAllModVersions(modId);

            DisableMod(modId);

            UpdateSubscriptionReceivers(null, new int[] { modId });
        }

        public void OnSubscriptionsChanged(IList<int> addedSubscriptions,
                                           IList<int> removedSubscriptions)
        {
            var enabledMods = UserAccountManagement.GetEnabledMods();

            // handle new subscriptions
            if(addedSubscriptions != null
               && addedSubscriptions.Count > 0)
            {
                // enable mods
                foreach(int modId in addedSubscriptions)
                {
                    if(!enabledMods.Contains(modId))
                    {
                        enabledMods.Add(modId);
                    }
                }

                // start downloads
                ModProfileRequestManager.instance.RequestModProfiles(addedSubscriptions,
                (modProfiles) =>
                {
                    if(this != null && this.isActiveAndEnabled)
                    {
                        var subbedMods = UserAccountManagement.activeUser.subscribedModIds;

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

                        UserAccountManagement.MarkAuthTokenRejected();
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

                    ModManager.TryUninstallAllModVersions(modId);

                    // disable
                    enabledMods.Remove(modId);
                }
            }

            UserAccountManagement.SetEnabledMods(enabledMods);
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
            IList<int> mods = UserAccountManagement.GetEnabledMods();
            if(!mods.Contains(modId))
            {
                mods.Add(modId);
                UserAccountManagement.SetEnabledMods(mods);
            }

            IEnumerable<IModEnabledReceiver> updateReceivers = GetComponentsInChildren<IModEnabledReceiver>(true);
            foreach(var receiver in updateReceivers)
            {
                receiver.OnModEnabled(modId);
            }
        }

        public void DisableMod(int modId)
        {
            IList<int> mods = UserAccountManagement.GetEnabledMods();
            if(mods.Contains(modId))
            {
                mods.Remove(modId);
                UserAccountManagement.SetEnabledMods(mods);
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

            if(!string.IsNullOrEmpty(UserAccountManagement.activeUser.oAuthToken))
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
