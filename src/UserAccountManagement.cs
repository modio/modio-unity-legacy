using System;
using System.Collections.Generic;
using System.Linq;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Main functional wrapper for the LocalUser structure.</summary>
    public static class UserAccountManagement
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>File that this class uses to store user data.</summary>
        public static readonly string USER_DATA_FILENAME = "user.data";

        // ---------[ FIELDS ]---------
        /// <summary>Data instance.</summary>
        public static LocalUser activeUser;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Loads the default local user.</summary>
        static UserAccountManagement()
        {
            UserAccountManagement.LoadActiveUser();
        }

        // ---------[ MOD COLLECTION MANAGEMENT ]---------
        /// <summary>Returns the enabled mods for the active user.</summary>
        public static List<int> GetEnabledMods()
        {
            return new List<int>(LocalUser.EnabledModIds);
        }

        /// <summary>Sets the enabled mods for the active user.</summary>
        public static void SetEnabledMods(IEnumerable<int> modIds)
        {
            List<int> modList = null;
            if(modIds == null)
            {
                modList = new List<int>();
            }
            else
            {
                modList = new List<int>(modIds);
            }

            LocalUser.EnabledModIds = modList;
            SaveActiveUser();
        }

        /// <summary>Add a mod to the subscribed list and modifies the queued actions accordingly.</summary>
        public static void SubscribeToMod(int modId)
        {
            UserAccountManagement.AssertActiveUserListsNotNull();

            LocalUser userData = UserAccountManagement.activeUser;

            // add sub to list
            if(!userData.subscribedModIds.Contains(modId))
            {
                userData.subscribedModIds.Add(modId);
            }

            // check queues
            bool unsubQueued = userData.queuedUnsubscribes.Contains(modId);
            bool subQueued = userData.queuedSubscribes.Contains(modId);

            // add to/remove from queues
            if(unsubQueued)
            {
                userData.queuedUnsubscribes.Remove(modId);
            }
            else if(!subQueued)
            {
                userData.queuedSubscribes.Add(modId);
            }

            // save
            UserAccountManagement.activeUser = userData;
            UserAccountManagement.SaveActiveUser();
        }

        /// <summary>Removes a mod from the subscribed list and modifies the queued actions accordingly.</summary>
        public static void UnsubscribeFromMod(int modId)
        {
            UserAccountManagement.AssertActiveUserListsNotNull();

            LocalUser userData = UserAccountManagement.activeUser;

            // remove sub from list
            userData.subscribedModIds.Remove(modId);

            // check queues
            bool unsubQueued = userData.queuedUnsubscribes.Contains(modId);
            bool subQueued = userData.queuedSubscribes.Contains(modId);

            // add to/remove from queues
            if(subQueued)
            {
                userData.queuedSubscribes.Remove(modId);
            }
            else if(!unsubQueued)
            {
                userData.queuedUnsubscribes.Add(modId);
            }

            // save
            UserAccountManagement.activeUser = userData;
            UserAccountManagement.SaveActiveUser();
        }

        /// <summary>Pushes queued subscribe actions to the server.</summary>
        public static void PushSubscriptionChanges(Action onCompletedNoErrors,
                                                   Action<List<WebRequestError>> onCompletedWithErrors)
        {
            UserAccountManagement.AssertActiveUserListsNotNull();

            int responsesPending = (LocalUser.QueuedSubscribes.Count
                                    + LocalUser.QueuedUnsubscribes.Count);

            // early outs
            if(LocalUser.AuthenticationState == AuthenticationState.NoToken
               || responsesPending == 0)
            {
                if(onCompletedNoErrors != null)
                {
                    onCompletedNoErrors();
                }

                return;
            }

            // set up vars
            string userToken = LocalUser.OAuthToken;
            List<WebRequestError> errors = new List<WebRequestError>();

            List<int> subscribesPushed
                = new List<int>(LocalUser.QueuedSubscribes.Count);
            List<int> unsubscribesPushed
                = new List<int>(LocalUser.QueuedUnsubscribes.Count);

            // callback
            Action onRequestCompleted = () =>
            {
                if(responsesPending <= 0)
                {
                    if(userToken == LocalUser.OAuthToken)
                    {
                        foreach(int modId in subscribesPushed)
                        {
                            LocalUser.QueuedSubscribes.Remove(modId);
                        }
                        foreach(int modId in unsubscribesPushed)
                        {
                            LocalUser.QueuedUnsubscribes.Remove(modId);
                        }

                        UserAccountManagement.SaveActiveUser();
                    }

                    if(errors.Count == 0
                       && onCompletedNoErrors != null)
                    {
                        onCompletedNoErrors();
                    }
                    else if(errors.Count > 0
                            && onCompletedWithErrors != null)
                    {
                        onCompletedWithErrors(errors);
                    }
                }
            };

            // - push -
            foreach(int modId in LocalUser.QueuedSubscribes)
            {
                APIClient.SubscribeToMod(modId,
                (p) =>
                {
                    subscribesPushed.Add(modId);

                    --responsesPending;
                    onRequestCompleted();
                },
                (e) =>
                {
                    // Error for "Mod is already subscribed"
                    if(e.webRequest.responseCode == 400)
                    {
                        subscribesPushed.Add(modId);
                    }
                    // Error for "Mod is unavailable"
                    else if(e.webRequest.responseCode == 404)
                    {
                        subscribesPushed.Add(modId);
                    }
                    // Error for real
                    else
                    {
                        errors.Add(e);
                    }

                    --responsesPending;
                    onRequestCompleted();
                });
            }
            foreach(int modId in LocalUser.QueuedUnsubscribes)
            {
                APIClient.UnsubscribeFromMod(modId,
                () =>
                {
                    --responsesPending;
                    unsubscribesPushed.Remove(modId);

                    onRequestCompleted();
                },
                (e) =>
                {

                    // Error for "Mod is already subscribed"
                    if(e.webRequest.responseCode == 400)
                    {
                        unsubscribesPushed.Remove(modId);
                    }
                    // Error for "Mod is unavailable"
                    else if(e.webRequest.responseCode == 404)
                    {
                        unsubscribesPushed.Remove(modId);
                    }
                    // Error for real
                    else
                    {
                        errors.Add(e);
                    }

                    --responsesPending;
                    onRequestCompleted();
                });
            }
        }

        /// <summary>Pulls the subscriptions from the server and stores the changes.</summary>
        public static void PullSubscriptionChanges(Action<List<ModProfile>> onSuccess,
                                                   Action<WebRequestError> onError)
        {
            // early out
            if(LocalUser.AuthenticationState == AuthenticationState.NoToken)
            {
                if(onSuccess != null)
                {
                    onSuccess(new List<ModProfile>(0));
                }
                return;
            }

            UserAccountManagement.AssertActiveUserListsNotNull();

            // holding vars
            string userToken = LocalUser.OAuthToken;
            List<ModProfile> remoteOnlySubscriptions = new List<ModProfile>();

            // set filter and initial pagination
            RequestFilter subscriptionFilter = new RequestFilter();
            subscriptionFilter.AddFieldFilter(ModIO.API.GetUserSubscriptionsFilterFields.gameId,
                                              new EqualToFilter<int>(PluginSettings.data.gameId));

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            // define actions
            Action getNextPage = null;
            Action<RequestPage<ModProfile>> onPageReceived = null;
            Action onAllPagesReceived = null;

            getNextPage = () =>
            {
                APIClient.GetUserSubscriptions(subscriptionFilter, pagination,
                (response) =>
                {
                    onPageReceived(response);

                    // check if all pages received
                    if(response != null
                       && response.items != null
                       && response.items.Length > 0
                       && response.resultTotal > response.size + response.resultOffset)
                    {
                        pagination.offset = response.resultOffset + response.size;

                        getNextPage();
                    }
                    else
                    {
                        onAllPagesReceived();

                        if(onSuccess != null)
                        {
                            onSuccess(remoteOnlySubscriptions);
                        }
                    }
                },
                (e) =>
                {
                    if(onError != null)
                    {
                        onError(e);
                    }
                });
            };


            onPageReceived = (r) =>
            {
                foreach(ModProfile profile in r.items)
                {
                    if(profile != null)
                    {
                        remoteOnlySubscriptions.Add(profile);
                    }
                }
            };

            onAllPagesReceived = () =>
            {
                if(userToken != LocalUser.OAuthToken)
                {
                    return;
                }

                List<int> localOnlySubs
                = new List<int>(LocalUser.SubscribedModIds);

                // NOTE(@jackson): Unsub actions *should not* be found in activeUser.subscribedModIds
                foreach(int modId in LocalUser.QueuedUnsubscribes)
                {
                    #if DEBUG
                    if(localOnlySubs.Contains(modId))
                    {
                        Debug.LogWarning("[mod.io] A locally subscribed mod was found in the"
                                         + " queuedUnsubscribes. This should not occur - please"
                                         + " ensure that any mod ids added to"
                                         + " activeUser.queuedUnsubscribes are removed from"
                                         + " activeUser.subscribedModIds or use"
                                         + " UserAccountManagement.UnsubscribeFromMod() to handle"
                                         + " this automatically.");
                    }
                    #endif

                    localOnlySubs.Remove(modId);
                }

                List<int> newSubs = new List<int>();

                // build new subs list
                for(int i = 0; i < remoteOnlySubscriptions.Count; ++i)
                {
                    ModProfile profile = remoteOnlySubscriptions[i];

                    // remove if in queued subs
                    LocalUser.QueuedSubscribes.Remove(profile.id);

                    // if in unsub queue
                    if(LocalUser.QueuedUnsubscribes.Contains(profile.id))
                    {
                        remoteOnlySubscriptions.RemoveAt(i);
                        --i;
                    }
                    // if locally subbed
                    else if(localOnlySubs.Remove(profile.id))
                    {
                        remoteOnlySubscriptions.RemoveAt(i);
                        --i;
                    }
                    // if not locally subbed && if not in unsub queue
                    else
                    {
                        newSubs.Add(profile.id);
                    }
                }

                // -- update locally --
                // remove new unsubs
                foreach(int modId in localOnlySubs)
                {
                    // if not in sub queue
                    if(!LocalUser.QueuedSubscribes.Contains(modId))
                    {
                        LocalUser.SubscribedModIds.Remove(modId);
                    }
                }

                LocalUser.SubscribedModIds.AddRange(newSubs);

                // save
                UserAccountManagement.SaveActiveUser();
            };

            // get pages
            getNextPage();
        }

        // ---------[ AUTHENTICATION ]---------
        /// <summary>Pulls any changes to the User Profile from the mod.io servers.</summary>
        public static void UpdateUserProfile(Action<UserProfile> onSuccess,
                                             Action<WebRequestError> onError)
        {
            if(LocalUser.AuthenticationState != AuthenticationState.NoToken)
            {
                APIClient.GetAuthenticatedUser((p) =>
                {
                    LocalUser.Profile = p;
                    UserAccountManagement.SaveActiveUser();

                    if(onSuccess != null)
                    {
                        onSuccess(p);
                    }
                },
                onError);
            }
            else if(onSuccess != null)
            {
                onSuccess(null);
            }
        }

        /// <summary>A wrapper function for setting the UserAuthenticationData.wasTokenRejected to false.</summary>
        public static void MarkAuthTokenRejected()
        {
            LocalUser.WasTokenRejected = true;
            SaveActiveUser();
        }


        /// <summary>Begins the authentication process using a mod.io Security Code.</summary>
        public static void AuthenticateWithSecurityCode(string securityCode,
                                                        Action<UserProfile> onSuccess,
                                                        Action<WebRequestError> onError)
        {
            APIClient.GetOAuthToken(securityCode, (t) =>
            {
                LocalUser.OAuthToken = t;
                LocalUser.WasTokenRejected = false;
                UserAccountManagement.SaveActiveUser();

                UserAccountManagement.UpdateUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        /// <remarks>This version is designed to match the Steamworks.NET implementation by
        /// @rlabrecque at https://github.com/rlabrecque/Steamworks.NET</remarks>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] pTicket, uint pcbTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(pTicket, pcbTicket);
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket, onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        /// <remarks>This version is designed to match the FacePunch.SteamWorks implementation by
        /// @garrynewman at https://github.com/Facepunch/Facepunch.Steamworks</remarks>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] authTicketData,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(authTicketData, (uint)authTicketData.Length);
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket, onSuccess, onError);
        }


        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        public static void AuthenticateWithSteamEncryptedAppTicket(string encodedTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            LocalUser.ExternalAuthentication = new ExternalAuthenticationData()
            {
                ticket = encodedTicket,
                provider = ExternalAuthenticationProvider.Steam,
            };

            APIClient.RequestSteamAuthentication(encodedTicket, (t) =>
            {
                LocalUser.OAuthToken = t;
                LocalUser.WasTokenRejected = false;
                UserAccountManagement.SaveActiveUser();

                UserAccountManagement.UpdateUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(byte[] data, uint dataSize,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(data, dataSize);
            UserAccountManagement.AuthenticateWithGOGEncryptedAppTicket(encodedTicket, onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(string encodedTicket,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            LocalUser.ExternalAuthentication = new ExternalAuthenticationData()
            {
                ticket = encodedTicket,
                provider = ExternalAuthenticationProvider.Steam,
            };

            APIClient.RequestGOGAuthentication(encodedTicket, (t) =>
            {
                LocalUser.OAuthToken = t;
                LocalUser.WasTokenRejected = false;
                UserAccountManagement.SaveActiveUser();

                UserAccountManagement.UpdateUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to authenticate a user using an itch.io JWT Token.</summary>
        public static void AuthenticateWithItchIOToken(string jwtToken,
                                                       Action<UserProfile> onSuccess,
                                                       Action<WebRequestError> onError)
        {
            LocalUser.ExternalAuthentication = new ExternalAuthenticationData()
            {
                ticket = jwtToken,
                provider = ExternalAuthenticationProvider.ItchIO,
            };

            APIClient.RequestItchIOAuthentication(jwtToken, (t) =>
            {
                LocalUser.OAuthToken = t;
                LocalUser.WasTokenRejected = false;
                UserAccountManagement.SaveActiveUser();

                UserAccountManagement.UpdateUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to authenticate a user using Oculus Rift user data.</summary>
        public static void AuthenticateWithOculusRiftUserData(string oculusUserNonce,
                                                              int oculusUserId,
                                                              string oculusUserAccessToken,
                                                              Action<UserProfile> onSuccess,
                                                              Action<WebRequestError> onError)
        {
            LocalUser.ExternalAuthentication = new ExternalAuthenticationData()
            {
                provider = ExternalAuthenticationProvider.OculusRift,
                ticket = oculusUserAccessToken,
                additionalData = new Dictionary<string, string>()
                {
                    { ExternalAuthenticationData.OculusRiftKeys.NONCE, oculusUserNonce },
                    { ExternalAuthenticationData.OculusRiftKeys.USER_ID, oculusUserId.ToString() },
                },
            };

            APIClient.RequestOculusRiftAuthentication(oculusUserNonce, oculusUserId, oculusUserAccessToken,
            (t) =>
            {
                LocalUser.OAuthToken = t;
                LocalUser.WasTokenRejected = false;
                UserAccountManagement.SaveActiveUser();

                UserAccountManagement.UpdateUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to authenticate a user using Xbox Live credentials.</summary>
        public static void AuthenticateWithXboxLiveToken(string xboxLiveUserToken,
                                                         Action<UserProfile> onSuccess,
                                                         Action<WebRequestError> onError)
        {
            LocalUser.ExternalAuthentication = new ExternalAuthenticationData()
            {
                ticket = xboxLiveUserToken,
                provider = ExternalAuthenticationProvider.XboxLive,
            };

            APIClient.RequestXboxLiveAuthentication(xboxLiveUserToken, (t) =>
            {
                LocalUser.OAuthToken = t;
                LocalUser.WasTokenRejected = false;
                UserAccountManagement.SaveActiveUser();

                UserAccountManagement.UpdateUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to reauthenticate using the stored external auth ticket.</summary>
        public static void ReauthenticateWithStoredExternalAuthData(Action<UserProfile> onSuccess,
                                                                    Action<WebRequestError> onError)
        {
            ExternalAuthenticationData authData = LocalUser.ExternalAuthentication;

            Debug.Assert(!string.IsNullOrEmpty(authData.ticket));
            Debug.Assert(authData.provider != ExternalAuthenticationProvider.None);

            Action<string> onSuccessWrapper = (t) =>
            {
                LocalUser.OAuthToken = t;
                LocalUser.WasTokenRejected = false;
                UserAccountManagement.SaveActiveUser();

                if(onSuccess != null)
                {
                    UserAccountManagement.UpdateUserProfile(onSuccess, onError);
                }
            };

            switch(LocalUser.ExternalAuthentication.provider)
            {
                case ExternalAuthenticationProvider.Steam:
                {
                    APIClient.RequestSteamAuthentication(authData.ticket,
                                                         onSuccessWrapper,
                                                         onError);
                }
                break;

                case ExternalAuthenticationProvider.GOG:
                {
                    APIClient.RequestGOGAuthentication(authData.ticket,
                                                       onSuccessWrapper,
                                                       onError);
                }
                break;

                case ExternalAuthenticationProvider.ItchIO:
                {
                    APIClient.RequestItchIOAuthentication(authData.ticket,
                                                          onSuccessWrapper,
                                                          onError);
                }
                break;

                case ExternalAuthenticationProvider.OculusRift:
                {
                    string token = authData.ticket;
                    string nonce;
                    string userIdString;
                    int userId;

                    if(authData.additionalData == null)
                    {
                        var error = WebRequestError.GenerateLocal("Unable to authenticate using stored Oculus Rift user data."
                                                                  + " The user id and nonce are missing.");
                        WebRequestError.LogAsWarning(error);

                        if(onError != null)
                        {
                            onError(error);
                        }

                        return;
                    }
                    else if(!authData.additionalData.TryGetValue(ExternalAuthenticationData.OculusRiftKeys.NONCE, out nonce)
                            || string.IsNullOrEmpty(nonce))
                    {
                        var error = WebRequestError.GenerateLocal("Unable to authenticate using stored Oculus Rift user data."
                                                                  + " The nonce is missing.");
                        WebRequestError.LogAsWarning(error);

                        if(onError != null)
                        {
                            onError(error);
                        }

                        return;
                    }
                    else if(!authData.additionalData.TryGetValue(ExternalAuthenticationData.OculusRiftKeys.USER_ID, out userIdString)
                            || string.IsNullOrEmpty(userIdString))
                    {
                        var error = WebRequestError.GenerateLocal("Unable to authenticate using stored Oculus Rift user data."
                                                                  + " The user id is missing.");
                        WebRequestError.LogAsWarning(error);

                        if(onError != null)
                        {
                            onError(error);
                        }

                        return;
                    }
                    else if(!int.TryParse(userIdString, out userId))
                    {
                        var error = WebRequestError.GenerateLocal("Unable to authenticate using stored Oculus Rift user data."
                                                                  + " The user id is not parseable as an integer.");
                        WebRequestError.LogAsWarning(error);

                        if(onError != null)
                        {
                            onError(error);
                        }

                        return;
                    }
                    else
                    {
                        APIClient.RequestOculusRiftAuthentication(nonce, userId, token,
                                                                  onSuccessWrapper,
                                                                  onError);
                    }
                }
                break;

                case ExternalAuthenticationProvider.XboxLive:
                {
                    APIClient.RequestXboxLiveAuthentication(authData.ticket,
                                                            onSuccessWrapper,
                                                            onError);
                }
                break;

                default:
                {
                    throw new System.NotImplementedException();
                }
            }
        }

        // ---------[ USER MANAGEMENT ]---------
        /// <summary>Loads the active user data from disk.</summary>
        public static void LoadActiveUser()
        {
            UserDataStorage.TryReadJSONFile<LocalUser>(UserAccountManagement.USER_DATA_FILENAME, (success, fileData) =>
            {
                if(success)
                {
                    UserAccountManagement.activeUser = fileData;
                }
                else
                {
                    UserAccountManagement.activeUser = new LocalUser();
                }

                UserAccountManagement.AssertActiveUserListsNotNull();
            });
        }

        /// <summary>Writes the active user data to disk.</summary>
        public static void SaveActiveUser()
        {
            UserDataStorage.TryWriteJSONFile(UserAccountManagement.USER_DATA_FILENAME,
                                             UserAccountManagement.activeUser);
        }

        // ---------[ UTILITY ]---------
        /// <summary>Ensures that the user data list fields are non-null values.</summary>
        public static void AssertActiveUserListsNotNull()
        {
            LocalUser userData = UserAccountManagement.activeUser;
            if(userData.enabledModIds == null
               || userData.subscribedModIds == null
               || userData.queuedSubscribes == null
               || userData.queuedUnsubscribes == null)
            {
                if(userData.enabledModIds == null)
                {
                    userData.enabledModIds = new List<int>();
                }
                if(userData.subscribedModIds == null)
                {
                    userData.subscribedModIds = new List<int>();
                }
                if(userData.queuedSubscribes == null)
                {
                    userData.queuedSubscribes = new List<int>();
                }
                if(userData.queuedUnsubscribes == null)
                {
                    userData.queuedUnsubscribes = new List<int>();
                }

                UserAccountManagement.activeUser = userData;
            }
        }
    }
}
