using System;

namespace ModIO
{
    /// <summary>A collection of user management functions provided for convenience.</summary>
    public static class UserAccountManagement
    {
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
                    steamTicket = null,
                    gogTicket = null,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] pTicket, uint pcbTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(pTicket, pcbTicket);

            APIClient.RequestSteamAuthentication(encodedTicket, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    steamTicket = encodedTicket,
                    gogTicket = null,
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

            APIClient.RequestGOGAuthentication(encodedTicket, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    gogTicket = encodedTicket,
                    steamTicket = null,
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
    }
}
