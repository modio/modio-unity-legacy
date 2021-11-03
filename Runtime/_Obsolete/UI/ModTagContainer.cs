using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO.UI
{
    [System.Obsolete("Use TagContainer instead.")]
    public class ModTagContainer : ModTagCollectionDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public event System.Action<ModTagDisplayComponent> tagClicked;

        [Header("Settings")]
        public GameObject tagDisplayPrefab;

        [Header("UI Components")]
        public RectTransform container;
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField]
        private ModTagDisplayData[] m_data = new ModTagDisplayData[0];
        private List<ModTagDisplayComponent> m_tagDisplays = new List<ModTagDisplayComponent>();

        // --- ACCESSORS ---
        public IEnumerable<ModTagDisplayComponent> tagDisplays
        {
            get {
                return m_tagDisplays;
            }
        }

        public override IEnumerable<ModTagDisplayData> data
        {
            get {
                return m_data;
            }
            set {
                if(value == null)
                {
                    m_data = new ModTagDisplayData[0];
                }
                else
                {
                    m_data = value.ToArray();
                }

#if UNITY_EDITOR
                if(!Application.isPlaying)
                {
                    PresentData_Editor(m_data);
                }
                else
#endif
                {
                    PresentData(m_data);
                }
            }
        }

        private void PresentData(IList<ModTagDisplayData> displayData)
        {
            Debug.Assert(displayData != null);

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }

            int newCount = displayData.Count;

            // remove unneeded displays
            while(newCount < m_tagDisplays.Count)
            {
                ModTagDisplayComponent display = m_tagDisplays[newCount];
                m_tagDisplays.RemoveAt(newCount);

                GameObject.Destroy(display.gameObject);
            }

            // create new displays
            while(m_tagDisplays.Count < newCount)
            {
                GameObject displayGO = GameObject.Instantiate(tagDisplayPrefab);
                displayGO.transform.SetParent(container, false);

                ModTagDisplayComponent display = displayGO.GetComponent<ModTagDisplayComponent>();
                display.Initialize();
                display.onClick += NotifyTagClicked;

                m_tagDisplays.Add(display);
            }

            // assign data
            for(int i = 0; i < newCount; ++i) { this.m_tagDisplays[i].data = displayData[i]; }

            // fix layouting
            if(this.isActiveAndEnabled)
            {
                StartCoroutine(LateUpdateLayouting());
            }
        }

#if UNITY_EDITOR
        private void PresentData_Editor(IEnumerable<ModTagDisplayData> displayData)
        {
            Debug.Assert(!Application.isPlaying);

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }

            // clear
            if(m_tagDisplays != null)
            {
                foreach(ModTagDisplayComponent display in m_tagDisplays)
                {
                    GameObject displayGO = display.gameObject;
                    UnityEditor.EditorApplication.delayCall += () =>
                    { DestroyImmediate(displayGO); };
                }
                m_tagDisplays.Clear();
            }

            if(tagDisplayPrefab == null || container == null)
            {
                return;
            }

            // create
            foreach(ModTagDisplayData tagData in displayData)
            {
                ModTagDisplayData tdata = tagData;

                UnityEditor.EditorApplication.delayCall += () =>
                {
                    GameObject displayGO = GameObject.Instantiate(tagDisplayPrefab, container);
                    displayGO.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;

                    ModTagDisplayComponent display =
                        displayGO.GetComponent<ModTagDisplayComponent>();
                    display.data = tdata;

                    m_tagDisplays.Add(display);
                };
            }

            // TODO: fix layouting?
        }
#endif

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            if(Application.isPlaying)
            {
                Debug.Assert(container != null);
                Debug.Assert(tagDisplayPrefab != null);
                Debug.Assert(tagDisplayPrefab.GetComponent<ModTagDisplayComponent>() != null);
            }

            CollectChildTags();
            PresentData(m_data);
        }

        private void CollectChildTags()
        {
            m_tagDisplays = new List<ModTagDisplayComponent>();

// TODO(@jackson): Why check isPlaying?
#if UNITY_EDITOR
            if(Application.isPlaying || container != null)
#endif
            {
                foreach(Transform t in container)
                {
                    ModTagDisplayComponent tagDisplay = t.GetComponent<ModTagDisplayComponent>();
                    if(tagDisplay != null)
                    {
                        m_tagDisplays.Add(tagDisplay);
                    }
                }
            }
        }

        public void OnEnable()
        {
            StartCoroutine(LateUpdateLayouting());
        }

        public System.Collections.IEnumerator LateUpdateLayouting()
        {
            yield return null;
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(container);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayTags(ModProfile profile,
                                         IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(profile != null);
            DisplayTags(profile.tagNames, tagCategories);
        }
        public override void DisplayTags(IEnumerable<string> tags,
                                         IEnumerable<ModTagCategory> tagCategories)
        {
            if(tags == null)
            {
                tags = new string[0];
            }

            m_data = ModTagDisplayData.GenerateArray(tags, tagCategories);
            PresentData(m_data);
        }

        public override void DisplayLoading()
        {
            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }

            // clear
            foreach(ModTagDisplayComponent display in m_tagDisplays)
            {
                GameObject.Destroy(display.gameObject);
            }
            m_tagDisplays.Clear();
        }

        // ---------[ EVENTS ]---------
        public void NotifyTagClicked(ModTagDisplayComponent display)
        {
            if(tagClicked != null)
            {
                tagClicked(display);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    CollectChildTags();
                    if(!Application.isPlaying)
                    {
                        PresentData_Editor(m_data);
                    }
                    else
                    {
                        PresentData(m_data);
                    }
                }
            };
        }
#endif
    }
}
