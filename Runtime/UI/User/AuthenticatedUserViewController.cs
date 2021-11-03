using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the details of the currently authenticated user.</summary>
    [RequireComponent(typeof(UserView))]
    public class AuthenticatedUserViewController : MonoBehaviour, IAuthenticatedUserUpdateReceiver
    {
        // ---------[ NESTED DATA-TYPES ]---------
        [System.Serializable]
        private struct UserProfileData
        {
            public UserProfile profile;
            public Texture2D avatar;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Display data for an unauthenticated user.</summary>
        [SerializeField]
        private UserProfileData m_unauthenticatedUser = new UserProfileData() {
            profile = new UserProfile(),
            avatar = null,
        };

        // --- ACCESSORS ---
        /// <summary>The UserView this component controls.</summary>
        public UserView view
        {
            get {
                return this.gameObject.GetComponent<UserView>();
            }
        }

        // ---------[ INITIALIZATION ]---------
        protected virtual void Start()
        {
            // cache the guest avatar
            this.m_unauthenticatedUser.profile.avatarLocator = new AvatarImageLocator() {
                fileName = "_AVATAR_",
                original = ImageRequestManager.GUEST_AVATAR_URL,
                thumbnail_50x50 = ImageRequestManager.GUEST_AVATAR_URL,
                thumbnail_100x100 = ImageRequestManager.GUEST_AVATAR_URL,
            };
            ImageRequestManager.instance.guestAvatar = this.m_unauthenticatedUser.avatar;

            // set view profile
            this.view.profile = this.m_unauthenticatedUser.profile;

            ModManager.GetAuthenticatedUserProfile(
                (p) => {
                    if(this != null)
                    {
                        this.view.profile = p;
                    }
                },
                (e) => {
                    MessageSystem.QueueMessage(
                        MessageDisplayData.Type.Error,
                        "Unable to fetch your profile from the mod.io servers.\n"
                            + e.displayMessage);
                });
        }

        // ---------[ EVENTS ]---------
        /// <summary>Interface for notification of user log-in event.</summary>
        public void OnUserLoggedIn(UserProfile profile)
        {
            this.view.profile = profile;
        }

        /// <summary>Interface for notification of user log-out event.</summary>
        public void OnUserLoggedOut()
        {
            this.view.profile = this.m_unauthenticatedUser.profile;
        }

        /// <summary>Interface for notification of changes made to user profile.</summary>
        public void OnUserProfileUpdated(UserProfile profile)
        {
            this.view.profile = profile;
        }

        // ---------[ OBSOLETE ]---------
        [System.Obsolete]
        [SerializeField]
        [HideInInspector]
        private UserDisplayData m_guestData;
    }
}
