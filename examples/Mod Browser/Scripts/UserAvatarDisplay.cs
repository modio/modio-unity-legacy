using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(Image))]
    public class UserAvatarDisplay : UserDataDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event System.Action<UserDataDisplayComponent> onClick;

        [Header("Settings")]
        public UserAvatarSize avatarSize;

        [Header("UI Components")]
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField] private UserDisplayData m_data = new UserDisplayData();

        // --- ACCESSORS ---
        public override UserDisplayData data
        {
            get { return data; }
            set
            {
                m_data = value;
                PresentData();
            }
        }
        public Image image
        {
            get { return this.gameObject.GetComponent<Image>(); }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            Debug.Assert(image != null);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        private void PresentData()
        {
            if(m_data.avatarTexture != null)
            {
                image.sprite = UIUtilities.CreateSpriteFromTexture(m_data.avatarTexture);
            }
            else
            {
                image.sprite = null;
            }

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
        }

        public override void DisplayProfile(UserProfile profile)
        {
            UserDisplayData userData = new UserDisplayData()
            {
                userId          = profile.id,
                nameId          = profile.nameId,
                username        = profile.username,
                lastOnline      = profile.lastOnline,
                timezone        = profile.timezone,
                language        = profile.language,
                profileURL      = profile.profileURL,
                avatarTexture   = null,
            };
            DisplayInternal(userData, profile.avatarLocator);
        }

        public void DisplayAvatar(int userId, AvatarImageLocator avatarLocator)
        {
            UserDisplayData userData = new UserDisplayData()
            {
                userId          = userId,
                nameId          = string.Empty,
                username        = string.Empty,
                lastOnline      = 0,
                timezone        = string.Empty,
                language        = string.Empty,
                profileURL      = string.Empty,
                avatarTexture   = null,
            };
            DisplayInternal(userData, avatarLocator);
        }

        private void DisplayInternal(UserDisplayData userData, AvatarImageLocator avatarLocator)
        {
            m_data = userData;

            if(avatarLocator == null)
            {
                PresentData();
            }
            else
            {
                DisplayLoading();

                ModManager.GetUserAvatar(userData.userId,
                                         avatarLocator,
                                         avatarSize,
                                         (t) =>
                                         {
                                            if(!Application.isPlaying) { return; }

                                            if(m_data.Equals(userData))
                                            {
                                                m_data.avatarTexture = t;
                                                PresentData();
                                            }
                                         },
                                         WebRequestError.LogAsWarning);
            }
        }

        public override void DisplayLoading()
        {
            image.sprite = null;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyClicked()
        {
            if(this.onClick != null)
            {
                this.onClick(this);
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if(image != null)
            {
                // NOTE(@jackson): Didn't notice any memory leakage with replacing textures.
                // "Should" be fine.
                PresentData();
            }
        }
        #endif
    }
}
