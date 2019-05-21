using System;

namespace ModIO
{
    /// <summary>A collection of user management functions provided for convenience.</summary>
    public static class UserAccountManagement
    {
        /// <summary>Attempts to authenticate a user using an emailed security code.</summary>
        public static void AuthenticateWithSecurityCode(string securityCode,
                                                        Action<UserProfile> onSuccess,
                                                        Action<WebRequestError> onError)
        {
            APIClient.GetOAuthToken(securityCode,
                                    (t) => UserAccountManagement.OnGetOAuthToken(t, null, onSuccess, onError),
                                    onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] pTicket, uint pcbTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.ConvertSteamEncryptedAppTicket(pTicket, pcbTicket);

            APIClient.RequestSteamAuthentication(encodedTicket,
                                                 (t) => UserAccountManagement.OnGetOAuthToken(t, encodedTicket, onSuccess, onError),
                                                 onError);
        }

        /// <summary>Stores the oAuthToken and steamTicket and fetches the UserProfile.</summary>
        private static void OnGetOAuthToken(string oAuthToken,
                                            string steamTicket,
                                            Action<UserProfile> onSuccess,
                                            Action<WebRequestError> onError)
        {
            UserAuthenticationData data = UserAuthenticationData.instance;
            data.token = oAuthToken;
            data.steamTicket = steamTicket;
            UserAuthenticationData.instance = data;

            APIClient.GetAuthenticatedUser((p) =>
            {
                UserAccountManagement.OnGetUserProfile(p);
                if(onSuccess != null)
                {
                    onSuccess(p);
                }
            },
            onError);
        }

        /// <summary>Stores the user id.</summary>
        private static void OnGetUserProfile(UserProfile profile)
        {
            UserAuthenticationData data = UserAuthenticationData.instance;
            data.userId = profile.id;
            UserAuthenticationData.instance = data;
        }
    }
}
