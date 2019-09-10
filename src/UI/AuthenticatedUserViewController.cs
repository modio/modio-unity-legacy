using System.Collections;

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
        private UserProfileData m_unauthenticatedUser = new UserProfileData()
        {
            profile = new UserProfile(),
            avatar = null,
        };

        // --- ACCESSORS ---
        /// <summary>The UserView this component controls.</summary>
        public UserView view
        {
            get { return this.gameObject.GetComponent<UserView>(); }
        }

        // ---------[ INITIALIZATION ]---------
        protected virtual void Start()
        {
            // cache the guest avatar
            string avatarURL = this.GetInstanceID().ToString() + @":GUEST_AVATAR";
            this.m_unauthenticatedUser.profile.avatarLocator = new AvatarImageLocator()
            {
                fileName = "_AVATAR_",
                original = avatarURL,
                thumbnail_50x50 = avatarURL,
                thumbnail_100x100 = avatarURL,
            };
            ImageRequestManager.instance.cache[avatarURL] = this.m_unauthenticatedUser.avatar;

            // set view profile
            this.view.profile = this.m_unauthenticatedUser.profile;

            if(UserAuthenticationData.instance.userId != UserProfile.NULL_ID)
            {
                StartCoroutine(FetchUserProfile());
            }
        }

        private IEnumerator FetchUserProfile()
        {
            bool isUnresolvable = false;
            UserProfile profile = null;

            float nextRetrySeconds = 0;
            string errorMessage = null;

            while(!isUnresolvable
                  && profile == null
                  && this != null)
            {
                WebRequestError error = null;
                bool isDone = false;

                ModManager.GetAuthenticatedUserProfile((p) => { isDone = true; profile = p; },
                                                       (e) => { isDone = true; error = e; } );

                while(!isDone) { yield return null; }

                if(error != null)
                {
                    isUnresolvable = error.isRequestUnresolvable;

                    if(isUnresolvable)
                    {
                        errorMessage = ("Unable to get your profile from the mod.io servers.\n"
                                        + error.displayMessage);
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(nextRetrySeconds);
                        nextRetrySeconds += 5f;
                    }
                }
                else if(profile == null)
                {
                    errorMessage = ("Unable to get your profile from the mod.io servers.\n"
                                    + "An unknown error has occurred.");
                    isUnresolvable = true;
                }
            }

            if(isUnresolvable)
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Error, errorMessage);
            }
            else if(this != null)
            {
                this.view.profile = profile;
            }
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
        [System.Obsolete][SerializeField][HideInInspector]
        private UserDisplayData m_guestData;
    }
}
