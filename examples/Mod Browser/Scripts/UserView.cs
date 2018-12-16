using UnityEngine;

namespace ModIO.UI
{
    public class UserView : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public event System.Action<UserView> onClick;

        [Header("UI Components")]
        public UserProfileDisplayComponent  profileDisplay;
        public UserAvatarDisplayComponent   avatarDisplay;

        [Header("Display Data")]
        [SerializeField] private UserDisplayData m_data = new UserDisplayData();

        // --- ACCESSORS ---
        public UserDisplayData data
        {
            get
            {
                if(profileDisplay != null)
                {
                    m_data.profile = profileDisplay.data;

                    if(m_data.userId <= 0)
                    {
                        m_data.userId = profileDisplay.data.userId;
                    }
                }
                if(avatarDisplay != null)
                {
                    m_data.avatar = avatarDisplay.data;

                    if(m_data.userId <= 0)
                    {
                        m_data.userId = avatarDisplay.data.userId;
                    }
                }

                return m_data;
            }
            set
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
        }

        // ---------[ INITIALIZATION ]---------
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

        public void DisplayUser(UserProfile profile)
        {
            Debug.Assert(profile != null);

            m_data = new UserDisplayData()
            {
                userId = profile.id,
            };

            if(profileDisplay != null)
            {
                profileDisplay.DisplayProfile(profile);
            }

            if(avatarDisplay != null)
            {
                avatarDisplay.DisplayAvatar(profile.id, profile.avatarLocator);
            }
        }
    }
}
