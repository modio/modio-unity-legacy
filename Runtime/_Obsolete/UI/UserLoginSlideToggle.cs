using UnityEngine;

namespace ModIO.UI
{
    [System.Obsolete("No longer supported.")]
    [RequireComponent(typeof(UserView))]
    [RequireComponent(typeof(SlideToggle))]
    public class UserLoginSlideToggle : MonoBehaviour
    {
        private UserView view
        {
            get {
                return this.gameObject.GetComponent<UserView>();
            }
        }
        private SlideToggle slider
        {
            get {
                return this.gameObject.GetComponent<SlideToggle>();
            }
        }

        // ---------[ EVENTS ]---------
        public void OnUserClicked()
        {
            if(slider.isAnimating)
            {
                return;
            }

            if(view.profile.id != UserProfile.NULL_ID)
            {
                slider.isOn = true;
            }
            else
            {
                view.NotifyClicked();
            }
        }

        public void OnLogoutClicked()
        {
            if(slider.isAnimating)
            {
                return;
            }

            view.NotifyClicked();
            slider.isOn = false;
        }
    }
}
