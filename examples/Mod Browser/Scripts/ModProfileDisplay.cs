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
        // TODO(@jackson)
        // public MetadataKVP[] metadataKVPs;

        public UserDisplayComponent             creatorDisplay;
        public ModLogoDisplay                   logoDisplay;
        public ModMediaContainer                mediaContainer;
        public ModfileDisplayComponent          buildDisplay;
        public ModTagCollectionDisplayComponent tagDisplay;
        public ModBinaryRequestDisplay          downloadDisplay;

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
                logoDisplay.data = displayData.logo;
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
            if(tagDisplay != null)
            {
                tagDisplay.data = displayData.tags;
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
                m_displayMapping.Add(dateAddedDisplay, (d) => ServerTimeStamp.ToLocalDateTime(d.dateAdded).ToString());
            }
            if(dateUpdatedDisplay != null)
            {
                m_displayMapping.Add(dateUpdatedDisplay, (d) => ServerTimeStamp.ToLocalDateTime(d.dateUpdated).ToString());
            }
            if(dateLiveDisplay != null)
            {
                m_displayMapping.Add(dateLiveDisplay, (d) => ServerTimeStamp.ToLocalDateTime(d.dateLive).ToString());
            }
            if(contentWarningsDisplay != null)
            {
                m_displayMapping.Add(contentWarningsDisplay, (d) => d.contentWarnings.ToString());
            }
            if(homepageURLDisplay != null)
            {
                m_displayMapping.Add(homepageURLDisplay, (d) => d.homepageURL);
            }
            if(nameDisplay != null)
            {
                m_displayMapping.Add(nameDisplay, (d) => d.name);
            }
            if(nameIdDisplay != null)
            {
                m_displayMapping.Add(nameIdDisplay, (d) => d.nameId);
            }
            if(summaryDisplay != null)
            {
                m_displayMapping.Add(summaryDisplay, (d) => d.summary);
            }
            if(descriptionAsHTMLDisplay != null)
            {
                m_displayMapping.Add(descriptionAsHTMLDisplay, (d) =>
                {
                    string description = d.descriptionAsHTML;

                    if(replaceMissingDescriptionWithSummary
                       && String.IsNullOrEmpty(description))
                    {
                        description = d.summary;
                    }

                    return description;
                });
            }
            if(descriptionAsTextDisplay != null)
            {
                m_displayMapping.Add(descriptionAsTextDisplay, (d) =>
                {
                    string description = d.descriptionAsText;

                    if(replaceMissingDescriptionWithSummary
                       && String.IsNullOrEmpty(description))
                    {
                        description = d.summary;
                    }

                    return description;
                });
            }
            if(metadataBlobDisplay != null)
            {
                m_displayMapping.Add(metadataBlobDisplay, (d) => d.metadataBlob);
            }
            if(profileURLDisplay != null)
            {
                m_displayMapping.Add(profileURLDisplay, (d) => d.profileURL);
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
            if(tagDisplay != null)
            {
                tagDisplay.Initialize();
            }
            if(downloadDisplay != null)
            {
                downloadDisplay.Initialize();
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayProfile(ModProfile profile, IEnumerable<ModTagCategory> tagCategories)
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


            ImageDisplayData logoData = new ImageDisplayData()
            {
                modId = profile.id,
                mediaType = ImageDisplayData.MediaType.ModLogo,
                fileName = null,
                texture = null,
            };
            if(profile.logoLocator != null)
            {
                logoData.fileName = profile.logoLocator.fileName;
            }

            // ModMediaDisplayData mediaData = new ModMediaDisplayData()
            // {
            //     modId   = profile.id,
            //     logo    = null,
            // };

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

            ModTagDisplayData[] tagData = ModTagDisplayData.GenerateArray(profile.tagNames,
                                                                          tagCategories);

            ModDisplayData modData = new ModDisplayData()
            {
                modId               = profile.id,
                gameId              = profile.gameId,
                status              = profile.status,
                visibility          = profile.visibility,
                dateAdded           = profile.dateAdded,
                dateUpdated         = profile.dateUpdated,
                dateLive            = profile.dateLive,
                contentWarnings     = profile.contentWarnings,
                homepageURL         = profile.homepageURL,
                name                = profile.name,
                nameId              = profile.nameId,
                summary             = profile.summary,
                descriptionAsHTML   = profile.description_HTML,
                descriptionAsText   = profile.description_text,
                metadataBlob        = profile.metadataBlob,
                profileURL          = profile.profileURL,
                metadataKVPs        = profile.metadataKVPs,

                submittedBy         = userData,
                currentBuild        = modfileData,
                // media               = mediaData,
                tags                = tagData,
            };
            m_data = modData;

            PresentData(modData);

            // TODO(@jackson)
            Debug.LogWarning("UNFINISHED");
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
