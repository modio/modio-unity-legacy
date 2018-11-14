using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModBrowserUserDisplay : MonoBehaviour
{
    [Header("Settings")]
    public GameObject loadingPlaceholderPrefab;
    public Sprite guestAvatar;

    [Header("UI Components")]
    public RectTransform avatarContainer;
    public Text usernameText;
    public Button button;

    [Header("Runtime Data")]
    public UserProfile profile;

    public void UpdateUIComponents()
    {
        usernameText.text = profile.username;

        if(profile.id == ModBrowser.GUEST_PROFILE.id)
        {
            this.SetAvatar(guestAvatar);
        }
        else
        {
            GameObject placeholder_go = UnityEngine.Object.Instantiate(loadingPlaceholderPrefab, avatarContainer) as GameObject;

            RectTransform placeholder_rt = placeholder_go.GetComponent<RectTransform>();
            placeholder_rt.anchorMin = new Vector2(0f, 0f);
            placeholder_rt.anchorMax = new Vector2(1f, 1f);
            placeholder_rt.sizeDelta = new Vector2(0f, 0f);

            ModManager.GetUserAvatar(profile, UserAvatarSize.Thumbnail_50x50,
                                     (t) => this.SetAvatar(ModBrowser.CreateSpriteFromTexture(t)),
                                     (e) => Debug.LogWarning(e.ToUnityDebugString()));
        }
    }

    public void SetAvatar(Sprite avatarSprite)
    {
        foreach(Transform t in avatarContainer)
        {
            GameObject.Destroy(t.gameObject);
        }

        GameObject avatarImage_go = new GameObject("Avatar");
        RectTransform avatarImage_rt = avatarImage_go.AddComponent<RectTransform>();

        Image avatarImage = avatarImage_go.AddComponent<Image>();
        avatarImage.sprite = avatarSprite;

        avatarImage_rt.SetParent(avatarContainer, false);
        avatarImage_rt.anchorMin = new Vector2(0f, 0f);
        avatarImage_rt.anchorMax = new Vector2(1f, 1f);
        avatarImage_rt.sizeDelta = new Vector2(0f, 0f);
    }
}
