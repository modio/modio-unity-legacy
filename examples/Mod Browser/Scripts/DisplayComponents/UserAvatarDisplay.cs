using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class UserAvatarDisplay : UserAvatarDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event System.Action<ImageDataDisplayComponent> onClick;

        [Header("Settings")]
        [SerializeField] private UserAvatarSize m_avatarSize;

        [Header("UI Components")]
        public Image image;
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField] private ImageDisplayData m_data = new ImageDisplayData();

        // --- ACCESSORS ---
        public override UserAvatarSize avatarSize
        {
            get { return m_avatarSize; }
        }
        public override ImageDisplayData data
        {
            get { return m_data; }
            set
            {
                m_data = value;
                PresentData();
            }
        }
        private void PresentData()
        {
            if(m_data.texture != null)
            {
                image.sprite = UIUtilities.CreateSpriteFromTexture(m_data.texture);
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

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            Debug.Assert(image != null);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayAvatar(int userId, AvatarImageLocator locator)
        {
            Debug.Assert(locator != null);

            ImageDisplayData avatarData = new ImageDisplayData()
            {
                userId = userId,
                mediaType = ImageDisplayData.MediaType.UserAvatar,
                imageId = locator.fileName,
                texture = null,
            };

            DisplayInternal(avatarData, locator);
        }

        // NOTE(@jackson): Called internally, this is only used when displayData.texture == null
        private void DisplayInternal(ImageDisplayData displayData, AvatarImageLocator locator)
        {
            Debug.Assert(displayData.texture == null);

            m_data = displayData;

            if(locator == null)
            {
                PresentData();
            }
            else
            {
                DisplayLoading();

                ModManager.GetUserAvatar(displayData.userId,
                                         locator,
                                         avatarSize,
                                         (t) =>
                                         {
                                            if(!Application.isPlaying) { return; }

                                            if(m_data.Equals(displayData))
                                            {
                                                m_data.texture = t;
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
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null
                   && this.image != null)
                {
                    // NOTE(@jackson): Didn't notice any memory leakage with replacing textures.
                    // "Should" be fine.
                    PresentData();
                }
            };
        }
        #endif
    }
}
