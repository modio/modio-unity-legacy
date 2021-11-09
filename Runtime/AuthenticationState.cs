namespace ModIO
{
    /// <summary>An enum for summarising the state of a user's authentication.</summary>
    public enum AuthenticationState
    {
        NoToken,
        ValidToken,
        RejectedToken,
    }
}
