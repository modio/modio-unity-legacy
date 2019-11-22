// #define ENABLE_STEAM_AUTH
// #define ENABLE_GOG_AUTH

using System;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>A collection of user management functions provided for convenience.</summary>
    public static class UserAccountManagement
    {
        // ---------[ CONSTANTS ]---------
        #if ENABLE_STEAM_AUTH
        public static readonly string PROFILE_URL_POSTFIX = "?ref=steam";
        #elif ENABLE_GOG_AUTH
        public static readonly string PROFILE_URL_POSTFIX = "?ref=gog";
        #else
        public static readonly string PROFILE_URL_POSTFIX = string.Empty;
        #endif

        // ---------[ AUTHENTICATION ]---------
        /// <summary>Requests a login code be sent to an email address.</summary>
        public static void RequestSecurityCode(string emailAddress,
                                               Action onSuccess,
                                               Action<WebRequestError> onError)
        {
            if(onSuccess == null)
            {
                APIClient.SendSecurityCode(emailAddress, null, onError);
            }
            else
            {
                APIClient.SendSecurityCode(emailAddress, (m) => onSuccess(), onError);
            }
        }

        /// <summary>Attempts to authenticate a user using an emailed security code.</summary>
        public static void AuthenticateWithSecurityCode(string securityCode,
                                                        Action<UserProfile> onSuccess,
                                                        Action<WebRequestError> onError)
        {
            APIClient.GetOAuthToken(securityCode, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                    externalAuthToken = null,
                    externalAuthId = null,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
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
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket,
                                                                          onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        /// <remarks>This version is designed to match the FacePunch.SteamWorks implementation by
        /// @garrynewman at https://github.com/Facepunch/Facepunch.Steamworks</remarks>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] authTicketData,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(authTicketData, (uint)authTicketData.Length);
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket,
                                                                          onSuccess, onError);
        }


        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        public static void AuthenticateWithSteamEncryptedAppTicket(string encodedTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            APIClient.RequestSteamAuthentication(encodedTicket, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                    externalAuthToken = encodedTicket,
                    externalAuthId = null,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);

        }

        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(byte[] data, uint dataSize,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(data, dataSize);
            UserAccountManagement.AuthenticateWithGOGEncryptedAppTicket(encodedTicket,
                                                                        onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(string encodedTicket,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            APIClient.RequestGOGAuthentication(encodedTicket, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                    externalAuthToken = encodedTicket,
                    externalAuthId = null,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Stores the oAuthToken and steamTicket and fetches the UserProfile.</summary>
        private static void FetchUserProfile(Action<UserProfile> onSuccess,
                                             Action<WebRequestError> onError)
        {
            APIClient.GetAuthenticatedUser((p) =>
            {
                UserAuthenticationData data = UserAuthenticationData.instance;
                data.userId = p.id;
                UserAuthenticationData.instance = data;

                if(onSuccess != null)
                {
                    onSuccess(p);
                }
            },
            onError);
        }

        /// <summary>Attempts to reauthenticate using the enabled service.</summary>
        public static void ReauthenticateWithExternalAuthToken(Action<UserProfile> onSuccess,
                                                               Action<WebRequestError> onError)
        {
            Debug.Assert(!string.IsNullOrEmpty(UserAuthenticationData.instance.externalAuthToken));

            Action<string, Action<string>, Action<WebRequestError>> authAction = null;

            #if ENABLE_STEAM_AUTH
                authAction = APIClient.RequestSteamAuthentication;
            #elif ENABLE_GOG_AUTH
                authAction = APIClient.RequestGOGAuthentication;
            #endif

            #if DEBUG
                if(authAction == null)
                {
                    Debug.LogError("[mod.io] Cannot reauthenticate without enabling an external"
                                   + " authentication service. Please refer to this file"
                                   + " (UserAccountManagement.cs) and uncomment the #define for the"
                                   + " desired service at the beginning of the file.");
                }
            #endif

            authAction.Invoke(UserAuthenticationData.instance.externalAuthToken, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        // ---------[ UTILITY ]---------
        /// <summary>A wrapper function for setting the UserAuthenticationData.wasTokenRejected to false.</summary>
        public static void MarkAuthTokenRejected()
        {
            UserAuthenticationData data = UserAuthenticationData.instance;
            data.wasTokenRejected = true;
            UserAuthenticationData.instance = data;
        }
    }
}
