using System;

namespace ModIO.API
{
    [Serializable]
    public struct AccessTokenObject
    {
        // - Fields -
        public string access_token; // OAuthToken that is assigned to the user for your game
    }
}