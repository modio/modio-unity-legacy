using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModProfileDisplay : ModProfileDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ModProfileDisplayComponent> onClick;

        [Header("Settings")]
        [Tooltip("If the profile has no description, the description display element(s) can be filled with the summary instead.")]
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
        // TODO(@jackson)
        // public MetadataKVP[] metadataKVPs;

        [Header("Display Data")]
        [SerializeField] private ModDisplayData m_data = new ModDisplayData();
        private List<TextLoadingOverlay> m_loadingOverlays = new List<TextLoadingOverlay>();

        private delegate string GetDisplayString(ModProfileDisplayData data);
        private Dictionary<Text, GetDisplayString> m_displayMapping = null;

        // --- ACCESSORS ---
        public override ModDisplayData data
        {
            get { return m_data; }
            set
            {
                m_data = value;
                PresentData();
            }
        }

        private void PresentData()
        {
            // - text displays -
            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(false);
            }
            foreach(var kvp in m_displayMapping)
            {
                kvp.Key.text = kvp.Value(m_data.profile);
            }

            // - nested displays -
            // if(creatorDisplay != null)
            // {
            //     creatorDisplay.data = m_data.profile.submittedBy;
            // }
            // if(logoDisplay != null)
            // {
            //     ImageDisplayData logoData = new ImageDisplayData()
            //     {
            //         modId = m_data.profile.modId,
            //         mediaType = ImageDisplayData.MediaType.ModLogo,
            //         imageId = string.Empty,
            //         texture = null,
            //     };

            //     if(m_data.profile.media != null
            //        && m_data.profile.media.Length > 0)
            //     {
            //         foreach(ImageDisplayData imageData in m_data.profile.media)
            //         {
            //             if(imageData.mediaType == ImageDisplayData.MediaType.ModLogo)
            //             {
            //                 logoData = imageData;
            //                 break;
            //             }
            //         }
            //     }

            //     logoDisplay.data = logoData;
            // }
            // if(mediaContainer != null)
            // {
            //     mediaContainer.data = m_data.profile.media;
            // }
            // if(buildDisplay != null)
            // {
            //     buildDisplay.data = m_data.profile.currentBuild;
            // }
            // if(tagDisplay != null)
            // {
            //     tagDisplay.data = m_data.profile.tags;
            // }
            // if(downloadDisplay != null)
            // {
            //     // ModBinaryRequest download = null;
            //     // foreach(ModBinaryRequest request in ModManager.downloadsInProgress)
            //     // {
            //     //     if(request.modId == profile.id)
            //     //     {
            //     //         download = request;
            //     //         break;
            //     //     }
            //     // }

            //     // downloadDisplay.DisplayRequest(download);

            //     // TODO(@jackson)
            //     Debug.LogWarning("NOT IMPLEMENTED");
            // }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            BuildDisplayMap();
            CollectLoadingOverlays();
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

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayProfile(ModProfile profile, IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(profile != null);

            List<Action> fetchDelegates = new List<Action>();

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

            ModProfileDisplayData profileData = new ModProfileDisplayData()
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
            };

            ModDisplayData modData = new ModDisplayData()
            {
                modId = profile.id,
                profile = profileData,
            };
            m_data = modData;

            PresentData();

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
            PresentData();
        }
        #endif
    }
}
