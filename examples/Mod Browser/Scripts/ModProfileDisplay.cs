using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModProfileDisplay : ModDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ModDisplayComponent> onClick;

        [Header("Settings")]
        [Tooltip("If the profile has no description, the description display element(s) can be filled with the summary instead.")]
        public bool             replaceMissingDescriptionWithSummary;

        [Header("UI Components")]
        public Text         nameDisplay;
        public Text         dateAddedDisplay;
        public Text         dateUpdatedDisplay;
        public Text         dateLiveDisplay;
        public Text         summaryDisplay;
        public Text         descriptionHTMLDisplay;
        public Text         descriptionTextDisplay;

        public UserDisplayComponent         creatorDisplay;
        public ModLogoDisplay               logoDisplay;
        public ModMediaCollectionContainer  mediaContainer;
        public ModfileDisplayComponent      buildDisplay;
        public ModBinaryRequestDisplay      downloadDisplay;

        // TODO(@jackson)
        public LayoutGroup      tagContainer;
        public GameObject       tagBadgePrefab;

        [Header("Display Data")]
        [SerializeField] private ModDisplayData m_data = new ModDisplayData();
        private List<TextLoadingOverlay> m_loadingOverlays = new List<TextLoadingOverlay>();

        private delegate string GetDisplayString(ModDisplayData data);
        private Dictionary<Text, GetDisplayString> m_displayMapping = null;

        // --- ACCESSORS ---
        public override ModDisplayData data
        {
            get { return m_data; }
            set
            {
                m_data = value;
                PresentData(value);
            }
        }

        private void PresentData(ModDisplayData displayData)
        {
            // - text displays -
            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(false);
            }
            foreach(var kvp in m_displayMapping)
            {
                kvp.Key.text = kvp.Value(displayData);
            }

            // - nested displays -
            if(creatorDisplay != null)
            {
                creatorDisplay.data = displayData.submittedBy;
            }
            if(logoDisplay != null)
            {
                // logoDisplay.DisplayLogo(profile.id, profile.logoLocator);
                // TODO(@jackson)
                Debug.LogWarning("NOT IMPLEMENTED");
            }
            if(mediaContainer != null)
            {
                // TODO(@jackson)
                Debug.LogWarning("NOT IMPLEMENTED");
                // mediaContainer.DisplayProfileMedia(profile);
            }
            if(buildDisplay != null)
            {
                buildDisplay.data = displayData.currentBuild;
            }
            if(downloadDisplay != null)
            {
                // ModBinaryRequest download = null;
                // foreach(ModBinaryRequest request in ModManager.downloadsInProgress)
                // {
                //     if(request.modId == profile.id)
                //     {
                //         download = request;
                //         break;
                //     }
                // }

                // downloadDisplay.DisplayRequest(download);

                // TODO(@jackson)
                Debug.LogWarning("NOT IMPLEMENTED");
            }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            BuildDisplayMap();
            CollectLoadingOverlays();
            InitializeNestedDisplays();
        }

        private void BuildDisplayMap()
        {
            Debug.LogWarning("NEEDS UPDATE");

            // - text displays -
            m_displayMapping = new Dictionary<Text, GetDisplayString>();

            if(nameDisplay != null)
            {
                m_displayMapping.Add(nameDisplay, (p) => p.name);
            }
            if(dateAddedDisplay != null)
            {
                m_displayMapping.Add(dateAddedDisplay, (p) => ServerTimeStamp.ToLocalDateTime(p.dateAdded).ToString());
            }
            if(dateUpdatedDisplay != null)
            {
                m_displayMapping.Add(dateUpdatedDisplay, (p) => ServerTimeStamp.ToLocalDateTime(p.dateUpdated).ToString());
            }
            if(dateLiveDisplay != null)
            {
                m_displayMapping.Add(dateLiveDisplay, (p) => ServerTimeStamp.ToLocalDateTime(p.dateLive).ToString());
            }
            if(summaryDisplay != null)
            {
                m_displayMapping.Add(summaryDisplay, (p) => p.summary);
            }
            if(descriptionHTMLDisplay != null)
            {
                m_displayMapping.Add(descriptionHTMLDisplay, (p) =>
                {
                    string description = p.description_HTML;

                    if(replaceMissingDescriptionWithSummary
                       && String.IsNullOrEmpty(description))
                    {
                        description = p.summary;
                    }

                    return description;
                });
            }
            if(descriptionTextDisplay != null)
            {
                m_displayMapping.Add(descriptionTextDisplay, (p) =>
                {
                    string description = p.description_text;

                    if(replaceMissingDescriptionWithSummary
                       && String.IsNullOrEmpty(description))
                    {
                        description = p.summary;
                    }

                    return description;
                });
            }
        }

        private void CollectLoadingOverlays()
        {
            TextLoadingOverlay[] childLoadingOverlays = this.gameObject.GetComponentsInChildren<TextLoadingOverlay>(true);
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

        private void InitializeNestedDisplays()
        {
            if(creatorDisplay != null)
            {
                creatorDisplay.Initialize();
            }
            if(logoDisplay != null)
            {
                logoDisplay.Initialize();
            }
            if(mediaContainer != null)
            {
                mediaContainer.Initialize();
            }
            if(buildDisplay != null)
            {
                buildDisplay.Initialize();
            }
            if(downloadDisplay != null)
            {
                downloadDisplay.Initialize();
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayProfile(ModProfile profile)
        {
            Debug.Assert(profile != null);

            UserDisplayData userData = new UserDisplayData();
            if(profile.submittedBy != null)
            {
                userData.userId          = profile.submittedBy.id;
                userData.nameId          = profile.submittedBy.nameId;
                userData.username        = profile.submittedBy.username;
                userData.lastOnline      = profile.submittedBy.lastOnline;
                userData.timezone        = profile.submittedBy.timezone;
                userData.language        = profile.submittedBy.language;
                userData.profileURL      = profile.submittedBy.profileURL;
                userData.avatarTexture   = null;
            }
            else
            {
                userData.userId = -1;
            }

            ModfileDisplayData modfileData = new ModfileDisplayData();
            if(profile.activeBuild != null)
            {
                modfileData.modfileId       = profile.activeBuild.id;
                modfileData.modId           = profile.activeBuild.modId;
                modfileData.dateAdded       = profile.activeBuild.dateAdded;
                modfileData.fileName        = profile.activeBuild.fileName;
                modfileData.fileSize        = profile.activeBuild.fileSize;
                modfileData.MD5             = profile.activeBuild.fileHash.md5;
                modfileData.version         = profile.activeBuild.version;
                modfileData.changelog       = profile.activeBuild.changelog;
                modfileData.metadataBlob    = profile.activeBuild.metadataBlob;
                modfileData.virusScanDate   = profile.activeBuild.dateScanned;
                modfileData.virusScanStatus = profile.activeBuild.virusScanStatus;
                modfileData.virusScanResult = profile.activeBuild.virusScanResult;
                modfileData.virusScanHash   = profile.activeBuild.virusScanHash;
            }
            else
            {
                modfileData.modfileId       = -1;
                modfileData.modId           = profile.id;
            }

            ModMediaDisplayData mediaData = new ModMediaDisplayData()
            {
                modId   = profile.id,
                logo    = null,
            };

            ModTagDisplayData[] tagData;
            if(profile.tags != null
               && profile.tags.Length > 0)
            {
                // TODO(@jackson): Add Categories
                tagData = new ModTagDisplayData[profile.tags.Length];

                for(int i = 0; i < tagData.Length; ++i)
                {
                    tagData[i].tagName = profile.tags[i].name;
                    tagData[i].categoryName = string.Empty;
                }
            }
            else
            {
                tagData = new ModTagDisplayData[0];
            }

            ModDisplayData modData = new ModDisplayData()
            {
                modId = profile.id,
                gameId = profile.gameId,
                status = profile.status,
                visibility = profile.visibility,
                dateAdded = profile.dateAdded,
                dateUpdated = profile.dateUpdated,
                dateLive = profile.dateLive,
                contentWarnings = profile.contentWarnings,
                homepageURL = profile.homepageURL,
                name = profile.name,
                nameId = profile.nameId,
                summary = profile.summary,
                description_HTML = profile.description_HTML,
                description_text = profile.description_text,
                metadataBlob = profile.metadataBlob,
                profileURL = profile.profileURL,
                metadataKVPs = profile.metadataKVPs,

                submittedBy = userData,
                currentBuild = modfileData,
                media = mediaData,
                tags = tagData,
            };
            m_data = modData;

            PresentData(modData);
        }

        public override void DisplayLoading()
        {
            // - text displays -
            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(true);
            }
            foreach(Text textComponent in m_displayMapping.Keys)
            {
                textComponent.text = string.Empty;
            }

            // - nested displays -
            if(creatorDisplay != null)
            {
                creatorDisplay.DisplayLoading();
            }
            if(logoDisplay != null)
            {
                logoDisplay.DisplayLoading();
            }
            if(mediaContainer != null)
            {
                mediaContainer.DisplayLoading();
            }
            if(buildDisplay != null)
            {
                buildDisplay.DisplayLoading();
            }
            if(downloadDisplay != null)
            {
                downloadDisplay.DisplayRequest(null);
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
            BuildDisplayMap();
            CollectLoadingOverlays();
            PresentData(m_data);
        }
        #endif
    }
}
