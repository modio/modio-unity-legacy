using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [Obsolete("Use ModProfileFieldDisplay components instead")]
    public class ModProfileDisplay : ModProfileDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ModProfileDisplayComponent> onClick;

        [Header("Settings")]
        [Tooltip(
            "If the profile has no description, the description display element(s) can be filled with the summary instead.")]
        public bool replaceMissingDescriptionWithSummary;

        [Header("UI Components")]
        public Text modIdDisplay;
        public Text gameIdDisplay;
        public Text nameDisplay;
        public Text nameIdDisplay;
        public Text statusDisplay;
        public Text visibilityDisplay;
        public Text contentWarningsDisplay;
        public Text dateAddedDisplay;
        public Text dateUpdatedDisplay;
        public Text dateLiveDisplay;
        public Text summaryDisplay;
        public Text descriptionAsHTMLDisplay;
        public Text descriptionAsTextDisplay;
        public Text homepageURLDisplay;
        public Text profileURLDisplay;
        public Text metadataBlobDisplay;

        [Header("Display Data")]
        [SerializeField]
        private ModProfileDisplayData m_data = new ModProfileDisplayData();
        private List<TextLoadingOverlay> m_loadingOverlays = new List<TextLoadingOverlay>();

        private delegate string GetDisplayString(ModProfileDisplayData data);
        private Dictionary<Text, GetDisplayString> m_displayMapping = null;

        // --- ACCESSORS ---
        public override ModProfileDisplayData data
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
            if(!Application.isPlaying && this.m_displayMapping == null)
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
                if(loadingOverlay != null && loadingOverlay.gameObject != null)
                {
                    loadingOverlay.gameObject.SetActive(false);
                }
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

            if(modIdDisplay != null)
            {
                m_displayMapping.Add(modIdDisplay, (d) => d.modId.ToString());
            }
            if(gameIdDisplay != null)
            {
                m_displayMapping.Add(gameIdDisplay, (d) => d.gameId.ToString());
            }
            if(statusDisplay != null)
            {
                m_displayMapping.Add(statusDisplay, (d) => d.status.ToString());
            }
            if(visibilityDisplay != null)
            {
                m_displayMapping.Add(visibilityDisplay, (d) => d.visibility.ToString());
            }
            if(dateAddedDisplay != null)
            {
                m_displayMapping.Add(dateAddedDisplay,
                                     (d) =>
                                         ServerTimeStamp.ToLocalDateTime(d.dateAdded).ToString());
            }
            if(dateUpdatedDisplay != null)
            {
                m_displayMapping.Add(dateUpdatedDisplay,
                                     (d) =>
                                         ServerTimeStamp.ToLocalDateTime(d.dateUpdated).ToString());
            }
            if(dateLiveDisplay != null)
            {
                m_displayMapping.Add(dateLiveDisplay,
                                     (d) => ServerTimeStamp.ToLocalDateTime(d.dateLive).ToString());
            }
            if(contentWarningsDisplay != null)
            {
                m_displayMapping.Add(contentWarningsDisplay, (d) => d.contentWarnings.ToString());
            }
            if(homepageURLDisplay != null)
            {
                m_displayMapping.Add(homepageURLDisplay,
                                     (d) => Utility.SafeTrimString(d.homepageURL));
            }
            if(nameDisplay != null)
            {
                m_displayMapping.Add(nameDisplay, (d) => Utility.SafeTrimString(d.name));
            }
            if(nameIdDisplay != null)
            {
                m_displayMapping.Add(nameIdDisplay, (d) => Utility.SafeTrimString(d.nameId));
            }
            if(summaryDisplay != null)
            {
                m_displayMapping.Add(summaryDisplay, (d) => Utility.SafeTrimString(d.summary));
            }
            if(descriptionAsHTMLDisplay != null)
            {
                m_displayMapping.Add(descriptionAsHTMLDisplay, (d) => {
                    string description = d.descriptionAsHTML;

                    if(replaceMissingDescriptionWithSummary && String.IsNullOrEmpty(description))
                    {
                        description = d.summary;
                    }

                    return Utility.SafeTrimString(description);
                });
            }
            if(descriptionAsTextDisplay != null)
            {
                m_displayMapping.Add(descriptionAsTextDisplay, (d) => {
                    string description = d.descriptionAsText;

                    if(replaceMissingDescriptionWithSummary && String.IsNullOrEmpty(description))
                    {
                        description = d.summary;
                    }

                    return Utility.SafeTrimString(description);
                });
            }
            if(metadataBlobDisplay != null)
            {
                m_displayMapping.Add(metadataBlobDisplay, (d) => d.metadataBlob);
            }
            if(profileURLDisplay != null)
            {
                m_displayMapping.Add(profileURLDisplay,
                                     (d) => Utility.SafeTrimString(d.profileURL.Trim()));
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
        public override void DisplayProfile(ModProfile profile)
        {
            Debug.Assert(profile != null);
            m_data = ModProfileDisplayData.CreateFromProfile(profile);
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
