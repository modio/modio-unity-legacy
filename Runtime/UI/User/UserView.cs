using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A view that provides information to children IUserViewElement components</summary>
    [DisallowMultipleComponent]
    public class UserView : MonoBehaviour
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Event for notifying listeners of a change to the mod profile.</summary>
        [System.Serializable]
        public class ProfileChangedEvent : UnityEngine.Events.UnityEvent<UserProfile> {}

        // ---------[ FIELDS ]---------
        public event System.Action<UserView> onClick;

        /// <summary>Currently displayed user profile.</summary>
        [SerializeField]
        private UserProfile m_profile = null;

        /// <summary>Event fired when the profile changes.</summary>
        public ProfileChangedEvent onProfileChanged = null;

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
                        this.onProfileChanged.Invoke(this.m_profile);
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

        // ---------[ EVENTS ]---------
        public void NotifyClicked()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }
    }
}
