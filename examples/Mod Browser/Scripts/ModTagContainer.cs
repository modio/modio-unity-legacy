using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModTagContainer : ModTagCollectionDisplay
    {
        // ---------[ FIELDS ]---------
        public event System.Action<ModTagDisplayComponent> tagClicked;

        [Header("Settings")]
        public GameObject tagDisplayPrefab;

        [Header("UI Components")]
        public RectTransform container;
        public GameObject loadingOverlay;

        [Header("Display Data")]
        [SerializeField] private ModTagDisplayData[] m_data = new ModTagDisplayData[0];

        // --- RUNTIME DATA ---
        private List<ModTagDisplayComponent> m_tagDisplays = new List<ModTagDisplayComponent>();

        // --- ACCESSORS ---
        public override IEnumerable<ModTagDisplayData> data
        {
            get { return m_data; }
            set
            {
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
        public IEnumerable<ModTagDisplayComponent> tagDisplays { get { return m_tagDisplays; } }

        private void PresentData(IEnumerable<ModTagDisplayData> displayData)
        {
            Debug.Assert(displayData != null);

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }

            // clear
            foreach(ModTagDisplayComponent display in m_tagDisplays)
            {
                GameObject.Destroy(display.gameObject);
            }
            m_tagDisplays.Clear();

            // create
            foreach(ModTagDisplayData tagData in displayData)
            {
                GameObject displayGO = GameObject.Instantiate(tagDisplayPrefab,
                                                              new Vector3(),
                                                              Quaternion.identity,
                                                              container);

                ModTagDisplayComponent display = displayGO.GetComponent<ModTagDisplayComponent>();
                display.Initialize();
                display.data = tagData;
                display.onClick += NotifyTagClicked;

                m_tagDisplays.Add(display);
            }

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
                    UnityEditor.EditorApplication.delayCall+= () =>
                    {
                        DestroyImmediate(displayGO);
                    };
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
                    GameObject displayGO = GameObject.Instantiate(tagDisplayPrefab,
                                                                  new Vector3(),
                                                                  Quaternion.identity,
                                                                  container);
                    displayGO.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;

                    ModTagDisplayComponent display = displayGO.GetComponent<ModTagDisplayComponent>();
                    display.data = tdata;
                    display.onClick += NotifyTagClicked;

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
            if(m_data != null)
            {
                PresentData(m_data);
            }
        }

        private void CollectChildTags()
        {
            m_tagDisplays = new List<ModTagDisplayComponent>();

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
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayTags(IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories)
        {
            DisplayTags(-1, tags, tagCategories);
        }
        public override void DisplayTags(ModProfile profile, IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(profile != null);
            DisplayTags(profile.id, profile.tagNames, tagCategories);
        }
        public override void DisplayTags(int modId, IEnumerable<string> tags,
                                            IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(tags != null);

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }

            // clear
            foreach(ModTagDisplayComponent display in m_tagDisplays)
            {
                GameObject.Destroy(display.gameObject);
            }
            m_tagDisplays.Clear();

            // create
            IDictionary<string, string> tagCategoryMap
                = ModTagCollectionDisplay.GenerateTagCategoryMap(tags, tagCategories);

            foreach(var tagCategory in tagCategoryMap)
            {
                GameObject displayGO = GameObject.Instantiate(tagDisplayPrefab,
                                                              new Vector3(),
                                                              Quaternion.identity,
                                                              container);

                ModTagDisplayComponent display = displayGO.GetComponent<ModTagDisplayComponent>();
                display.Initialize();
                display.DisplayModTag(tagCategory.Key, tagCategory.Value);
                display.onClick += NotifyTagClicked;

                m_tagDisplays.Add(display);
            }

            if(this.isActiveAndEnabled)
            {
                StartCoroutine(LateUpdateLayouting());
            }
        }

        public override void DisplayLoading(int modId = -1)
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
        #endif
    }
}
