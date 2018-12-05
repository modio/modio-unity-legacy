using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(Image))]
    public class UserAvatarDisplay : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public delegate void OnClickDelegate(UserAvatarDisplay display);
        public event OnClickDelegate onClick;

        [Header("Settings")]
        public UserAvatarSize avatarSize;

        [Header("UI Components")]
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField] private UserDisplayData m_data = new UserDisplayData();
        private string m_loadingFileName = string.Empty;

        // --- ACCESSORS ---
        public UserDisplayData data
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
        public void Initialize()
        {
            Debug.Assert(image != null);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        private void PresentData()
        {
            if(m_data.avatarTexture != null)
            {
                image.sprite = ModBrowser.CreateSpriteFromTexture(m_data.avatarTexture);
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

        // public void DisplayAvatar(UserProfile profile)
        // {
        //     DisplayAvatar(profile.id, profile.avatarLocator);

        //     m_userId = userId;
        //     m_imageFileName = string.Empty;
        // }

        // public void DisplayAvatar(int userId, AvatarImageLocator avatarLocator)
        // {
        //     Debug.Assert(avatarLocator != null);

        //     DisplayLoading();

        //     m_userId = userId;
        //     m_imageFileName = avatarLocator.fileName;

        //     ModManager.GetUserAvatar(userId, avatarLocator, avatarSize,
        //                              (t) => LoadTexture(t, avatarLocator.fileName),
        //                              WebRequestError.LogAsWarning);
        // }

        public void DisplayLoading()
        {
            image.sprite = null;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }
        }

        // private void LoadTexture(Texture2D texture, string fileName)
        // {
        //     #if UNITY_EDITOR
        //     if(!Application.isPlaying) { return; }
        //     #endif

        //     if(fileName != m_imageFileName
        //        || this.image == null)
        //     {
        //         return;
        //     }
        // }

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
                // TODO(@jackson): Dispose of texture?
                PresentData();
            }
        }
        #endif
    }
}
