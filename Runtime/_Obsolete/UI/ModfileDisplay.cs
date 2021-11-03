using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [Obsolete("Use ModfileFieldDisplay components instead.")]
    public class ModfileDisplay : ModfileDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ModfileDisplayComponent> onClick;

        [Header("UI Components")]
        public Text modfileIdDisplay;
        public Text modIdDisplay;
        public Text dateAddedDisplay;
        public Text fileNameDisplay;
        public Text fileSizeDisplay;
        public Text MD5Display;
        public Text versionDisplay;
        public Text changelogDisplay;
        public Text metadataBlobDisplay;
        public Text virusScanDateDisplay;
        public Text virusScanStatusDisplay;
        public Text virusScanResultDisplay;
        public Text virusScanHashDisplay;

        [Header("Display Data")]
        [SerializeField]
        private ModfileDisplayData m_data = new ModfileDisplayData();
        private List<TextLoadingOverlay> m_loadingOverlays = null;

        private delegate string GetDisplayString(ModfileDisplayData data);
        private Dictionary<Text, GetDisplayString> m_displayMapping = null;

        // --- ACCESSORS --
        public override ModfileDisplayData data
        {
            get {
                return m_data;
            }
            set {
                m_data = value;
                PresentData(value);
            }
        }

        private void PresentData(ModfileDisplayData displayData)
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

            foreach(var kvp in m_displayMapping) { kvp.Key.text = kvp.Value(displayData); }

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
            string dateTimeFormat =
                System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

            m_displayMapping = new Dictionary<Text, GetDisplayString>();

            if(modfileIdDisplay != null)
            {
                m_displayMapping.Add(modfileIdDisplay, (d) => d.modfileId.ToString());
            }
            if(modIdDisplay != null)
            {
                m_displayMapping.Add(modIdDisplay, (d) => d.modId.ToString());
            }
            if(dateAddedDisplay != null)
            {
                m_displayMapping.Add(
                    dateAddedDisplay,
                    (d) => ServerTimeStamp.ToLocalDateTime(d.dateAdded).ToString(dateTimeFormat));
            }
            if(fileNameDisplay != null)
            {
                m_displayMapping.Add(fileNameDisplay, (d) => d.fileName);
            }
            if(fileSizeDisplay != null)
            {
                m_displayMapping.Add(fileSizeDisplay,
                                     (d) => ValueFormatting.ByteCount(d.fileSize, "0.0"));
            }
            if(MD5Display != null)
            {
                m_displayMapping.Add(MD5Display, (d) => d.MD5);
            }
            if(versionDisplay != null)
            {
                m_displayMapping.Add(versionDisplay, (d) => d.version);
            }
            if(changelogDisplay != null)
            {
                m_displayMapping.Add(changelogDisplay, (d) => d.changelog);
            }
            if(metadataBlobDisplay != null)
            {
                m_displayMapping.Add(metadataBlobDisplay, (d) => d.metadataBlob);
            }
            if(virusScanDateDisplay != null)
            {
                m_displayMapping.Add(
                    virusScanDateDisplay,
                    (d) =>
                        ServerTimeStamp.ToLocalDateTime(d.virusScanDate).ToString(dateTimeFormat));
            }
            if(virusScanStatusDisplay != null)
            {
                m_displayMapping.Add(virusScanStatusDisplay, (d) => d.virusScanStatus.ToString());
            }
            if(virusScanResultDisplay != null)
            {
                m_displayMapping.Add(virusScanResultDisplay, (d) => d.virusScanResult.ToString());
            }
            if(virusScanHashDisplay != null)
            {
                m_displayMapping.Add(virusScanHashDisplay, (d) => d.virusScanHash);
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
        public override void DisplayModfile(Modfile modfile)
        {
            Debug.Assert(modfile != null);

            ModfileDisplayData modfileData = new ModfileDisplayData() {
                modfileId = modfile.id,
                modId = modfile.modId,
                dateAdded = modfile.dateAdded,
                fileName = modfile.fileName,
                fileSize = modfile.fileSize,
                MD5 = (modfile.fileHash == null ? string.Empty : modfile.fileHash.md5),
                version = modfile.version,
                changelog = modfile.changelog,
                metadataBlob = modfile.metadataBlob,
                virusScanDate = modfile.dateScanned,
                virusScanStatus = modfile.virusScanStatus,
                virusScanResult = modfile.virusScanResult,
                virusScanHash = modfile.virusScanHash,
            };
            m_data = modfileData;

            PresentData(modfileData);
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
                    PresentData(m_data);
                }
            };
        }
#endif
    }
}
