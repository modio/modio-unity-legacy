using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component for easily displaying a user avatar.</summary>
    public class UserAvatarDisplay : MonoBehaviour, IUserViewElement
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Event for notifying that the texture has changed.</summary>
        [System.Serializable]
        public class TextureChangedEvent : UnityEngine.Events.UnityEvent<Texture2D>
        {
        }

        // ---------[ FIELDS ]---------
        /// <summary>Image component used to display the avatar.</summary>
        public Image image = null;

        /// <summary>Preferred Avatar Size.</summary>
        public UserAvatarSize avatarSize = UserAvatarSize.Original;

        /// <summary>Event notifying that the display texture was updated.</summary>
        public TextureChangedEvent onTextureChanged = null;

        /// <summary>Parent UserView.</summary>
        private UserView m_view = null;

        /// <summary>Current userId for the displayed avatar.</summary>
        private int m_userId = UserProfile.NULL_ID;

        /// <summary>Locator for the displayed avatar.</summary>
        private AvatarImageLocator m_locator = null;

        // --- ACCESSORS ---
        /// <summary>Current userId for the displayed avatar.</summary>
        public int UserId
        {
            get {
                return this.m_userId;
            }
        }

        /// <summary>Locator for the displayed avatar.</summary>
        public AvatarImageLocator Locator
        {
            get {
                return this.m_locator;
            }
        }

        // ---------[ INITIALIZATION ]---------
        /// <summary>IUserViewElement interface.</summary>
        public void SetUserView(UserView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayProfile);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayProfile);
                this.DisplayProfile(this.m_view.profile);
            }
            else
            {
                this.DisplayProfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the avatar of a profile.</summary>
        public virtual void DisplayProfile(UserProfile profile)
        {
            int userId = UserProfile.NULL_ID;
            AvatarImageLocator locator = null;
            if(profile != null)
            {
                userId = profile.id;
                locator = profile.avatarLocator;
            }

            this.DisplayAvatar(userId, locator);
        }

        /// <summary>Displays a User Avatar using the locator.</summary>
        public virtual void DisplayAvatar(int userId, AvatarImageLocator locator)
        {
            this.m_userId = userId;

            if(this.m_locator != locator)
            {
                this.m_locator = locator;

                this.image.sprite = null;
                this.image.enabled = false;

                if(this.onTextureChanged != null)
                {
                    this.onTextureChanged.Invoke(null);
                }

                if(locator != null)
                {
                    System.Action<Texture2D> displayDelegate = (t) => ApplyTexture(locator, t);

                    ImageRequestManager.instance.RequestUserAvatar(userId, locator, this.avatarSize,
                                                                   displayDelegate,
                                                                   displayDelegate, // fallback
                                                                   null);
                }
            }
        }

        /// <summary>Internal function for applying the texture.</summary>
        protected virtual void ApplyTexture(AvatarImageLocator locator, Texture2D texture)
        {
            if(this != null && this.m_locator == locator && texture != null)
            {
                this.image.sprite = UIUtilities.CreateSpriteFromTexture(texture);
                this.image.enabled = true;

                if(this.onTextureChanged != null)
                {
                    this.onTextureChanged.Invoke(texture);
                }
            }
        }
    }
}
