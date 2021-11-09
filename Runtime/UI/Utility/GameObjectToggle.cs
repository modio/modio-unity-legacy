using UnityEngine;

namespace ModIO.UI
{
    public class GameObjectToggle : StateToggleDisplay
    {
        // ---------[ FIELDS ]---------
        [Header("UI Components")]
        public GameObject onDisplay;
        public GameObject offDisplay;

        [Header("Display Data")]
        [SerializeField]
        private bool m_isOn = true;

        // --- ACCESSSORS ---
        public override bool isOn
        {
            get {
                return m_isOn;
            }
            set {
                if(m_isOn != value)
                {
                    m_isOn = value;
                    UpdateDisplay();
                }
            }
        }

        private void UpdateDisplay()
        {
            if(onDisplay != null)
            {
                onDisplay.SetActive(m_isOn);
            }
            if(offDisplay != null)
            {
                offDisplay.SetActive(!m_isOn);
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            UpdateDisplay();
        }

// ---------[ EVENTS ]---------
#if UNITY_EDITOR
        private void OnValidate()
        {
            if(Application.isPlaying && this != null)
            {
                UpdateDisplay();
            }
        }
#endif
    }
}
