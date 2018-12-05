using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModfileDisplay : ModfileDataDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ModfileDataDisplayComponent> onClick;

        [Header("UI Components")]
        public Text dateAddedDisplay;
        public Text fileNameDisplay;
        public Text fileSizeDisplay;
        public Text fileHashDisplay;
        public Text versionDisplay;
        public Text changelogDisplay;

        [Header("Display Data")]
        [SerializeField] private ModfileDisplayData m_data = new ModfileDisplayData();

        // --- RUNTIME DATA ---
        private delegate string GetDisplayString(Modfile modfile);

        private Dictionary<Text, GetDisplayString> m_displayMapping = null;
        private List<TextLoadingOverlay> m_loadingOverlays = null;

        // --- ACCESSORS --
        public override ModfileDisplayData data
        {
            get { return data; }
            set
            {
                m_data = value;
                // PresentData();
            }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            m_displayMapping = new Dictionary<Text, GetDisplayString>();

            if(dateAddedDisplay != null)
            {
                m_displayMapping.Add(dateAddedDisplay, (m) => ServerTimeStamp.ToLocalDateTime(m.dateAdded).ToString());
            }
            if(fileNameDisplay != null)
            {
                m_displayMapping.Add(fileNameDisplay, (m) => m.fileName);
            }
            if(fileSizeDisplay != null)
            {
                m_displayMapping.Add(fileSizeDisplay, (m) => ModBrowser.ByteCountToDisplayString(m.fileSize));
            }
            if(fileHashDisplay != null)
            {
                m_displayMapping.Add(fileHashDisplay, (m) => m.fileHash.md5);
            }
            if(versionDisplay != null)
            {
                m_displayMapping.Add(versionDisplay, (m) => m.version);
            }
            if(changelogDisplay != null)
            {
                m_displayMapping.Add(changelogDisplay, (m) => m.changelog);
            }

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
        public override void DisplayModfile(Modfile modfile)
        {
            Debug.Assert(modfile != null);

            m_data.modfileId = modfile.id;

            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(false);
            }
            foreach(var kvp in m_displayMapping)
            {
                kvp.Key.text = kvp.Value(modfile);
                kvp.Key.enabled = true;
            }
        }

        public override void DisplayLoading()
        {
            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(true);
            }
            foreach(Text textComponent in m_displayMapping.Keys)
            {
                textComponent.enabled = false;
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
    }
}
