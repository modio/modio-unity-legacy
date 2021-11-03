using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [System.Obsolete("Use UserProfileFieldDisplayComponents instead.")]
    public class UserProfileDisplay : UserProfileDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<UserProfileDisplayComponent> onClick;

        [Header("UI Components")]
        public Text userIdDisplay;
        public Text nameIdDisplay;
        public Text usernameDisplay;
        public Text lastOnlineDisplay;
        public Text timezoneDisplay;
        public Text languageDisplay;
        public Text profileURLDisplay;

        [Header("Display Data")]
        [SerializeField]
        private UserProfileDisplayData m_data = new UserProfileDisplayData();
        private List<TextLoadingOverlay> m_loadingOverlays = new List<TextLoadingOverlay>();

        private delegate string GetDisplayString(UserProfileDisplayData data);
        private Dictionary<Text, GetDisplayString> m_displayMapping = null;

        // --- ACCESSORS ---
        public override UserProfileDisplayData data
        {
            get {
                return m_data;
            }
            set {
                m_data = value;
                PresentData();
            }
        }

        private void PresentData()
        {
#if UNITY_EDITOR
            if(!Application.isPlaying && m_displayMapping == null)
            {
                return;
            }
#endif

            if(this.m_displayMapping == null)
            {
                this.Initialize();
            }

            foreach(var kvp in m_displayMapping) { kvp.Key.text = kvp.Value(m_data); }

            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(false);
            }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            if(this.m_displayMapping == null)
            {
                BuildDisplayMap();
                CollectLoadingOverlays();
            }
        }

        private void BuildDisplayMap()
        {
            m_displayMapping = new Dictionary<Text, GetDisplayString>();
            if(userIdDisplay != null)
            {
                m_displayMapping.Add(userIdDisplay, (d) => d.userId.ToString());
            }
            if(nameIdDisplay != null)
            {
                m_displayMapping.Add(nameIdDisplay, (d) => d.nameId);
            }
            if(usernameDisplay != null)
            {
                m_displayMapping.Add(usernameDisplay, (d) => d.username);
            }
            if(lastOnlineDisplay != null)
            {
                m_displayMapping.Add(lastOnlineDisplay,
                                     (d) =>
                                         ServerTimeStamp.ToLocalDateTime(d.lastOnline).ToString());
            }
            if(timezoneDisplay != null)
            {
                m_displayMapping.Add(timezoneDisplay, (d) => d.timezone);
            }
            if(languageDisplay != null)
            {
                m_displayMapping.Add(languageDisplay, (d) => d.language);
            }
            if(profileURLDisplay != null)
            {
                m_displayMapping.Add(profileURLDisplay, (d) => d.profileURL);
            }
        }

        private void CollectLoadingOverlays()
        {
            TextLoadingOverlay[] childLoadingOverlays =
                this.gameObject.GetComponentsInChildren<TextLoadingOverlay>(true);
            List<Text> textDisplays = new List<Text>(m_displayMapping.Keys);

            m_loadingOverlays = new List<TextLoadingOverlay>();
            foreach(TextLoadingOverlay loadingOverlay in childLoadingOverlays)
            {
                if(textDisplays.Contains(loadingOverlay.textDisplayComponent))
                {
                    m_loadingOverlays.Add(loadingOverlay);
                }
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayProfile(UserProfile profile)
        {
            Debug.Assert(profile != null);

            UserProfileDisplayData userData = UserProfileDisplayData.CreateFromProfile(profile);
            m_data = userData;
            PresentData();
        }

        public override void DisplayLoading()
        {
            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(true);
            }

            foreach(Text textComponent in m_displayMapping.Keys)
            {
                textComponent.text = string.Empty;
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyClicked()
        {
            if(this.onClick != null)
            {
                this.onClick(this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    BuildDisplayMap();
                    CollectLoadingOverlays();
                    PresentData();
                }
            };
        }
#endif
    }
}
