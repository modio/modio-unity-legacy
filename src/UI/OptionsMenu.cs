using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>The component to control the options menu UI for the mod.io plugin.</summary>
    public class OptionsMenu : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        [Header("UI Elements")]
        /// <summary>The button that can be clicked to show/hide the menu.</summary>
        public Button showHideButton;
        /// <summary>The UI Element containing the menu elements.</summary>
        public RectTransform dropdown;
        /// <summary>The menu option that lets the player log out.</summary>
        public Button logoutButton;
        /// <summary>The menu option that lets the player log in.</summary>
        public Button loginButton;

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            this.logoutButton.onClick.AddListener(Hide);
            this.loginButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            this.dropdown.gameObject.SetActive(false);

            OnUserAuthenticationDataUpdated();
        }

        // ---------[ EVENTS ]---------
        /// <summary>Updates the menu to match the current UserAuthenticationData state.</summary>
        public void OnUserAuthenticationDataUpdated()
        {
            UserAuthenticationData userData = UserAuthenticationData.instance;

            // enable/disable log in/out buttons
            bool loggedIn = !(userData.Equals(UserAuthenticationData.NONE));
            this.logoutButton.gameObject.SetActive(loggedIn);
            this.loginButton.gameObject.SetActive(!loggedIn);

            // currently no options for externally authenticated accounts
            this.showHideButton.enabled = (!loggedIn || string.IsNullOrEmpty(userData.steamTicket));
        }

        /// <summary>Shows the menu.</summary>
        public void Show()
        {
            this.dropdown.gameObject.SetActive(true);
        }

        /// <summary>Hides the menu.</summary>
        public void Hide()
        {
            this.dropdown.gameObject.SetActive(false);
        }

        /// <summary>Toggles the menu between show/hide.</summary>
        public void Toggle()
        {
            bool isActive = this.dropdown.gameObject.activeSelf;
            this.dropdown.gameObject.SetActive(!isActive);
        }
    }
}
