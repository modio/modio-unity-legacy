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

        /// <summary>External authentication data for the session.</summary>
        public static ExternalAuthenticationData externalAuthentication;

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
            return new List<int>(UserAccountManagement.activeUser.enabledModIds);
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

            UserAccountManagement.activeUser.enabledModIds = modList;
            SaveActiveUser();
        }

        /// <summary>Returns the subscribed mods for the active user.</summary>
        public static List<int> GetSubscribedMods()
        {
            return new List<int>(UserAccountManagement.activeUser.subscribedModIds);
        }

        /// <summary>Sets the subscribed mods for the active user.</summary>
        public static void SetSubscribedMods(IEnumerable<int> modIds)
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

            UserAccountManagement.activeUser.subscribedModIds = modList;
            SaveActiveUser();
        }

        /// <summary>Add a mod to the subscribed list and modifies the queued actions accordingly.</summary>
        public static void SubscribeToMod(int modId)
        {
            LocalUser userData = UserAccountManagement.activeUser;
            if(userData.subscribedModIds == null)
            {
                userData.subscribedModIds = new List<int>();
            }

            // add sub to list
            if(!userData.subscribedModIds.Contains(modId))
            {
                userData.subscribedModIds.Add(modId);
            }

            // check queues
            bool unsubQueued = (userData.queuedUnsubscribes != null
                                && userData.queuedUnsubscribes.Contains(modId));

            bool subQueued = (userData.queuedSubscribes != null
                              && userData.queuedSubscribes.Contains(modId));

            // add to/remove from queues
            if(unsubQueued)
            {
                userData.queuedUnsubscribes.Remove(modId);
            }
            else if(!subQueued)
            {
                if(userData.queuedSubscribes == null)
                {
                    userData.queuedSubscribes = new List<int>();
                }

                userData.queuedSubscribes.Add(modId);
            }

            // save
            UserAccountManagement.activeUser = userData;
            UserAccountManagement.SaveActiveUser();
        }

        /// <summary>Removes a mod from the subscribed list and modifies the queued actions accordingly.</summary>
        public static void UnsubscribeFromMod(int modId)
        {
            LocalUser userData = UserAccountManagement.activeUser;
            if(userData.subscribedModIds == null)
            {
                userData.subscribedModIds = new List<int>();
            }

            // remove sub from list
            userData.subscribedModIds.Remove(modId);

            // check queues
            bool unsubQueued = (userData.queuedUnsubscribes != null
                                && userData.queuedUnsubscribes.Contains(modId));

            bool subQueued = (userData.queuedSubscribes != null
                              && userData.queuedSubscribes.Contains(modId));

            // add to/remove from queues
            if(subQueued)
            {
                userData.queuedSubscribes.Remove(modId);
            }
            else if(!unsubQueued)
            {
                if(userData.queuedUnsubscribes == null)
                {
                    userData.queuedUnsubscribes = new List<int>();
                }

                userData.queuedUnsubscribes.Add(modId);
            }

            // save
            UserAccountManagement.activeUser = userData;
            UserAccountManagement.SaveActiveUser();
        }

        // ---------[ AUTHENTICATION ]---------
        /// <summary>Pulls any changes to the User Profile from the mod.io servers.</summary>
        public static void UpdateUserProfile(Action<UserProfile> onSuccess,
                                             Action<WebRequestError> onError)
        {
            if(UserAccountManagement.activeUser.AuthenticationState != AuthenticationState.NoToken)
            {
                APIClient.GetAuthenticatedUser((p) =>
                {
                    UserAccountManagement.activeUser.profile = p;
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
            UserAccountManagement.activeUser.wasTokenRejected = true;
            SaveActiveUser();
        }


        /// <summary>Begins the authentication process using a mod.io Security Code.</summary>
        public static void AuthenticateWithSecurityCode(string securityCode,
                                                        Action<UserProfile> onSuccess,
                                                        Action<WebRequestError> onError)
        {
            APIClient.GetOAuthToken(securityCode, (t) =>
            {
                UserAccountManagement.activeUser.oAuthToken = t;
                UserAccountManagement.activeUser.wasTokenRejected = false;
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
            UserAccountManagement.externalAuthentication = new ExternalAuthenticationData()
            {
                ticket = encodedTicket,
                provider = ExternalAuthenticationProvider.Steam,
            };

            APIClient.RequestSteamAuthentication(encodedTicket, (t) =>
            {
                UserAccountManagement.activeUser.oAuthToken = t;
                UserAccountManagement.activeUser.wasTokenRejected = false;
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
            UserAccountManagement.externalAuthentication = new ExternalAuthenticationData()
            {
                ticket = encodedTicket,
                provider = ExternalAuthenticationProvider.Steam,
            };

            APIClient.RequestGOGAuthentication(encodedTicket, (t) =>
            {
                UserAccountManagement.activeUser.oAuthToken = t;
                UserAccountManagement.activeUser.wasTokenRejected = false;
                UserAccountManagement.SaveActiveUser();

                UserAccountManagement.UpdateUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to reauthenticate using the stored external auth ticket.</summary>
        public static void ReauthenticateWithExternalAuthToken(Action<UserProfile> onSuccess,
                                                               Action<WebRequestError> onError)
        {
            Debug.Assert(!string.IsNullOrEmpty(UserAccountManagement.externalAuthentication.ticket));
            Debug.Assert(UserAccountManagement.externalAuthentication.provider != ExternalAuthenticationProvider.None);

            Action<string, Action<string>, Action<WebRequestError>> authAction = null;

            switch(UserAccountManagement.externalAuthentication.provider)
            {
                case ExternalAuthenticationProvider.Steam:
                {
                    authAction = APIClient.RequestSteamAuthentication;
                }
                break;

                case ExternalAuthenticationProvider.GOG:
                {
                    authAction = APIClient.RequestGOGAuthentication;
                }
                break;

                default:
                {
                    throw new System.NotImplementedException();
                }
            }

            authAction.Invoke(UserAccountManagement.externalAuthentication.ticket, (t) =>
            {
                UserAccountManagement.activeUser.oAuthToken = t;
                UserAccountManagement.activeUser.wasTokenRejected = false;
                UserAccountManagement.SaveActiveUser();

                if(onSuccess != null)
                {
                    UserAccountManagement.UpdateUserProfile(onSuccess, onError);
                }
            },
            onError);
        }

        // ---------[ USER MANAGEMENT ]---------
        /// <summary>Loads the active user data from disk.</summary>
        public static void LoadActiveUser()
        {
            // read file
            LocalUser userData;
            if(!UserDataStorage.TryReadJSONFile(UserAccountManagement.USER_DATA_FILENAME, out userData))
            {
                userData = new LocalUser()
                {
                    enabledModIds = new List<int>(),
                    subscribedModIds = new List<int>(),
                };
            }
            else
            {
                if(userData.enabledModIds == null)
                {
                    userData.enabledModIds = new List<int>();
                }
                if(userData.subscribedModIds == null)
                {
                    userData.subscribedModIds = new List<int>();
                }
            }

            // set
            UserAccountManagement.activeUser = userData;
        }

        /// <summary>Writes the active user data to disk.</summary>
        public static void SaveActiveUser()
        {
            UserDataStorage.TryWriteJSONFile(UserAccountManagement.USER_DATA_FILENAME,
                                             UserAccountManagement.activeUser);
        }
    }
}
