namespace ModIO.UI
{
    public interface IAuthenticatedUserUpdateReceiver
    {
        void OnUserLoggedIn(UserProfile profile);
        void OnUserLoggedOut();
        void OnUserProfileUpdated(UserProfile profile);
    }
}
