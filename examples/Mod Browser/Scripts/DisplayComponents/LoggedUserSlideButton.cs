using UnityEngine;

namespace ModIO.UI
{
    [RequireComponent(typeof(UserView))]
    public class LoggedUserSlideButton : SlideStateButton
    {
        private UserView view { get { return this.gameObject.GetComponent<UserView>(); } }

        // ---------[ INITIALIZATION ]---------
        protected override void Start()
        {
            Debug.Assert(view != null);

            base.Start();

            this.onUntoggledClick.AddListener(OnUntoggledClick);
            this.onToggledClick.AddListener(OnToggledClick);
        }

        // ---------[ EVENTS ]---------
        private void OnUntoggledClick()
        {
            if(view.data.profile.userId > 0)
            {
                this.isToggled = true;
            }
            else
            {
                view.NotifyClicked();
            }
        }

        private void OnToggledClick()
        {
            view.NotifyClicked();
        }
    }
}
