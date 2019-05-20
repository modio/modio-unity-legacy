using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>The component to control the options menu UI for the mod.io plugin.</summary>
    public class OptionsMenu : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        [Header("UI Elements")]
        /// <summary>Determines if the button behaviour changes if externally authenticated.</summary>
        [Tooltip("If enabled, opens the browser for showHideButton.onClick if using Steam/GOG authentication.")]
        public bool openBrowserIfExternalAuth = true;
        /// <summary>The button that can be clicked to show/hide the menu.</summary>
        public Button showHideButton = null;
        /// <summary>The UI Element containing the menu elements.</summary>
        public RectTransform dropdown = null;
        /// <summary>The menu option for viewing the player's profile on the mod.io website.</summary>
        public Button viewProfileButton = null;
        /// <summary>The menu option that lets the player log out.</summary>
        public Button logoutButton = null;
        /// <summary>The menu option that lets the player log in.</summary>
        public Button loginButton = null;

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            this.showHideButton.onClick.AddListener(ToggleMenu);
            this.logoutButton.onClick.AddListener(HideMenu);
            this.loginButton.onClick.AddListener(HideMenu);
        }

        private void OnEnable()
        {
            this.dropdown.gameObject.SetActive(false);
        }

        // ---------[ EVENTS ]---------
        /// <summary>Determines which functionality to use when the show/hide button is clicked.</summary>
        public void ShowMenuOrOpenProfile()
        {
            UserAuthenticationData userData = UserAuthenticationData.instance;
            bool loggedIn = !(userData.Equals(UserAuthenticationData.NONE));
            bool isSteamAccount = (loggedIn && !string.IsNullOrEmpty(userData.steamTicket));

            if(this.openBrowserIfExternalAuth && isSteamAccount)
            {
                OpenProfileInBrowser();
            }
            else
            {
                ShowMenu();
            }
        }

        /// <summary>Shows the menu.</summary>
        public void ShowMenu()
        {
            UserAuthenticationData userData = UserAuthenticationData.instance;
            bool loggedIn = !(userData.Equals(UserAuthenticationData.NONE));

            this.logoutButton.gameObject.SetActive(loggedIn);
            this.loginButton.gameObject.SetActive(!loggedIn);
            this.viewProfileButton.gameObject.SetActive(loggedIn);

            Text logoutButtonText = this.logoutButton.GetComponentInChildren<Text>();
            if(logoutButtonText != null && loggedIn)
            {
                logoutButtonText.text = "Log out of account: " + userData.userId;
            }

            this.dropdown.gameObject.SetActive(true);
        }

        /// <summary>Hides the menu.</summary>
        public void HideMenu()
        {
            this.dropdown.gameObject.SetActive(false);
        }

        /// <summary>Toggles the menu between show/hide.</summary>
        public void ToggleMenu()
        {
            bool isActive = this.dropdown.gameObject.activeSelf;
            if(!isActive)
            {
                ShowMenuOrOpenProfile();
            }
            else
            {
                HideMenu();
            }
        }

        // ---------[ MENU OPTIONS ]---------
        /// <summary>Opens the user's menu profile in a web browser.</summary>
        public void OpenProfileInBrowser()
        {
            UserAuthenticationData userData = UserAuthenticationData.instance;
            if(userData.userId != UserProfile.NULL_ID)
            {
                this.viewProfileButton.interactable = false;

                ModManager.GetUserProfile(userData.userId,
                (p) =>
                {
                    if(userData.userId != UserProfile.NULL_ID)
                    {
                        string profileURL = p.profileURL + @"/edit";
                        if(!string.IsNullOrEmpty(userData.steamTicket))
                        {
                            profileURL += "?ref=steam";
                        }

                        Application.OpenURL(profileURL);
                        this.viewProfileButton.interactable = true;
                    }
                },
                (e) =>
                {
                    this.viewProfileButton.interactable = true;
                });
            }
        }
    }
}
