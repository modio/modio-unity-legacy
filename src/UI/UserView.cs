using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A view that provides information to children IUserViewElement components</summary>
    [DisallowMultipleComponent]
    public class UserView : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        public event System.Action<UserView> onClick;

        /// <summary>Event fired when the profile changes.</summary>
        public event System.Action<UserProfile> onProfileChanged;

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Currently displayed user profile.</summary>
        private UserProfile m_profile = null;

        // --- Accessors ---
        /// <summary>Currently displayed user profile.</summary>
        public UserProfile profile
        {
            get { return this.m_profile; }
            set
            {
                if(this.m_profile != value)
                {
                    this.m_profile = value;

                    if(this.onProfileChanged != null)
                    {
                        this.onProfileChanged(this.m_profile);
                    }
                }
            }
        }


        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            #if DEBUG
            UserView nested = this.gameObject.GetComponentInChildren<UserView>(true);
            if(nested != null && nested != this)
            {
                Debug.LogError("[mod.io] Nesting UserViews is currently not supported due to the"
                               + " way IUserViewElement component parenting works."
                               + "\nThe nested UserViews must be removed to allow UserView functionality."
                               + "\nthis=" + this.gameObject.name
                               + "\nnested=" + nested.gameObject.name,
                               this);
                return;
            }
            #endif

            // assign user view elements to this
            var userViewElements = this.gameObject.GetComponentsInChildren<IUserViewElement>(true);
            foreach(IUserViewElement viewElement in userViewElements)
            {
                viewElement.SetUserView(this);
            }
        }

        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged -= DisplayModSubmittor;
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged += DisplayModSubmittor;
                this.DisplayModSubmittor(this.m_view.profile);
            }
            else
            {
                this.DisplayModSubmittor(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the submittor for a ModProfile.</summary>
        public void DisplayModSubmittor(ModProfile modProfile)
        {
            UserProfile userProfile = null;
            if(modProfile != null)
            {
                userProfile = modProfile.submittedBy;
            }

            this.profile = userProfile;
        }

        public void DisplayUser(UserProfile userProfile)
        {
            this.profile = userProfile;
        }

        // ---------[ EVENTS ]---------
        public void NotifyClicked()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }

        // ---------[ OBSOLETE ]---------
        [System.Obsolete("Use UserAvatarDisplay component instead.")][HideInInspector]
        public ImageDisplay avatarDisplay;

        [System.Obsolete("Use UserProfileFieldDisplay components instead.")][HideInInspector]
        public UserProfileDisplayComponent  profileDisplay;

        [System.Obsolete]
        public UserDisplayData data
        {
            get
            {
                if(this.m_profile == null)
                {
                    return new UserDisplayData();
                }
                else
                {
                    UserDisplayData data = new UserDisplayData();
                    data.profile = UserProfileDisplayData.CreateFromProfile(profile);
                    data.avatar = ImageDisplayData.CreateForUserAvatar(profile.id, profile.avatarLocator);
                    return data;
                }
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        [System.Obsolete("No longer necessary.")]
        public void Initialize() {}
    }
}
