using UnityEngine;

namespace ModIO.UI
{
    public class UserView : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        public event System.Action<UserView> onClick;

        public event System.Action<UserProfile> onProfileChanged;

        [Header("UI Components")]
        public UserProfileDisplayComponent  profileDisplay;
        public ImageDisplay                 avatarDisplay;

        [Header("Display Data")]
        [SerializeField] private UserDisplayData m_data = new UserDisplayData();

        private UserProfile m_profile = null;

        public UserProfile profile
        { get { return this.m_profile; } }

        // --- ACCESSORS ---
        public UserDisplayData data
        {
            get
            {
                return GetData();
            }

            set
            {
                SetData(value);
            }
        }

        private UserDisplayData GetData()
        {
            if(profileDisplay != null)
            {
                m_data.profile = profileDisplay.data;
            }
            if(avatarDisplay != null)
            {
                m_data.avatar = avatarDisplay.data;
            }

            return m_data;
        }

        private void SetData(UserDisplayData value)
        {
            m_data = value;

            if(profileDisplay != null)
            {
                profileDisplay.data = m_data.profile;
            }
            if(avatarDisplay != null)
            {
                avatarDisplay.data = m_data.avatar;
            }
        }

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

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
            // TODO(@jackson): Looks like it's not neccessary?
            var userViewElements = this.gameObject.GetComponents<IUserViewElement>();
            foreach(IUserViewElement viewElement in userViewElements)
            {
                viewElement.SetUserView(this);
            }

            userViewElements = this.gameObject.GetComponentsInChildren<IUserViewElement>(true);
            foreach(IUserViewElement viewElement in userViewElements)
            {
                viewElement.SetUserView(this);
            }
        }

        public void Initialize()
        {
            if(profileDisplay != null)
            {
                profileDisplay.Initialize();
            }

            if(avatarDisplay != null)
            {
                avatarDisplay.Initialize();
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

            // TEMP
            if(userProfile == null)
            {
                userProfile = new UserProfile();
            }

            this.DisplayUser(userProfile);
        }

        public void DisplayUser(UserProfile profile)
        {
            Debug.Assert(profile != null);

            m_data = new UserDisplayData();

            if(profileDisplay != null)
            {
                profileDisplay.DisplayProfile(profile);
                m_data.profile = profileDisplay.data;
            }
            else
            {
                m_data.profile = UserProfileDisplayData.CreateFromProfile(profile);
            }

            if(avatarDisplay != null)
            {
                avatarDisplay.DisplayAvatar(profile.id, profile.avatarLocator);
                m_data.avatar = avatarDisplay.data;
            }
            else
            {
                if(profile.avatarLocator != null)
                {
                    m_data.avatar = ImageDisplayData.CreateForUserAvatar(profile.id,
                                                                         profile.avatarLocator);
                }
                else
                {
                    m_data.avatar = new ImageDisplayData();
                }
            }

            if(profile != this.m_profile)
            {
                this.m_profile = profile;

                if(this.onProfileChanged != null)
                {
                    this.onProfileChanged(profile);
                }
            }
        }

        // ---------[ EVENTS ]---------
        public void NotifyClicked()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    SetData(m_data);
                }
            };
        }
        #endif
    }
}
