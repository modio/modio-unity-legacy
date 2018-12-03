using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class UserAvatarDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public delegate void OnClickDelegate(UserAvatarDisplay display, int userId);
    public event OnClickDelegate onClick;

    [Header("Settings")]
    public UserAvatarSize avatarSize;

    [Header("UI Components")]
    public Image image;
    public GameObject loadingDisplay;

    [Header("Display Data")]
    [SerializeField] private int m_userId;
    [SerializeField] private string m_imageFileName;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(image != null);
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayAvatar(UserProfile profile)
    {
        DisplayAvatar(profile.id, profile.avatarLocator);
    }

    public void DisplayAvatar(int userId, AvatarImageLocator avatarLocator)
    {
        Debug.Assert(userId > 0, "[mod.io] UserId needs to be set to a valid user profile id.");
        Debug.Assert(avatarLocator != null);

        DisplayLoading();

        m_userId = userId;
        m_imageFileName = avatarLocator.fileName;

        ModManager.GetUserAvatar(userId, avatarLocator, avatarSize,
                                 (t) => LoadTexture(t, avatarLocator.fileName),
                                 WebRequestError.LogAsWarning);
    }

    public void DisplayTexture(int userId, Texture2D avatarTexture)
    {
        Debug.Assert(userId > 0, "[mod.io] UserId needs to be set to a valid user profile id.");
        Debug.Assert(avatarTexture != null);

        m_userId = userId;
        m_imageFileName = string.Empty;

        LoadTexture(avatarTexture, string.Empty);
    }

    public void DisplayLoading(int userId = -1)
    {
        m_userId = userId;

        if(loadingDisplay != null)
        {
            loadingDisplay.SetActive(true);
        }

        image.enabled = false;
    }

    private void LoadTexture(Texture2D texture, string fileName)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        if(fileName != m_imageFileName
           || this.image == null)
        {
            return;
        }

        if(loadingDisplay != null)
        {
            loadingDisplay.SetActive(false);
        }

        image.sprite = ModBrowser.CreateSpriteFromTexture(texture);
        image.enabled = true;
    }

    // ---------[ EVENT HANDLING ]---------
    public void NotifyClicked()
    {
        if(this.onClick != null)
        {
            this.onClick(this, m_userId);
        }
    }
}
