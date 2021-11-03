using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the YouTube thumbnails for a given mod.</summary>
    public class YouTubeThumbnailContainer : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying the YouTube
        /// thumbs.</summary>
        public RectTransform containerTemplate = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = false;

        // --- Run-Time Data ---
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Instance of the template clone.</summary>
        private GameObject m_templateClone = null;

        /// <summary>Container for the display objects.</summary>
        private RectTransform m_container = null;

        /// <summary>YouTube thumbnail display object prefab.</summary>
        private YouTubeThumbnailDisplay m_itemTemplate = null;

        /// <summary>ModId for the currently displayed images.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>YouTube ids to display.</summary>
        private string[] m_youTubeIds = new string[0];

        /// <summary>Display objects.</summary>
        private YouTubeThumbnailDisplay[] m_displays = new YouTubeThumbnailDisplay[0];

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            this.containerTemplate.gameObject.SetActive(false);
        }

        /// <summary>Initialize template.</summary>
        protected virtual void Start()
        {
// check template
#if DEBUG
            string message;
            if(!YouTubeThumbnailContainer.HasValidTemplate(this, out message))
            {
                Debug.LogError("[mod.io] " + message, this);
                return;
            }
#endif

            // get template vars
            Transform templateParent = this.containerTemplate.parent;
            string templateInstance_name = this.containerTemplate.gameObject.name + " (Instance)";
            int templateInstance_index = this.containerTemplate.GetSiblingIndex() + 1;
            this.m_itemTemplate =
                this.containerTemplate.GetComponentInChildren<YouTubeThumbnailDisplay>(true);

            // duplication protection
            bool isInstantiated =
                (templateParent.childCount > templateInstance_index
                 && templateParent.GetChild(templateInstance_index).gameObject.name
                        == templateInstance_name);
            if(isInstantiated)
            {
                this.m_templateClone = templateParent.GetChild(templateInstance_index).gameObject;
                YouTubeThumbnailDisplay[] itemInstances =
                    this.m_templateClone.GetComponentsInChildren<YouTubeThumbnailDisplay>(true);

                if(itemInstances == null || itemInstances.Length == 0)
                {
                    isInstantiated = false;
                    GameObject.Destroy(this.m_templateClone);
                }
                else
                {
                    this.m_container = (RectTransform)itemInstances[0].transform.parent;

                    foreach(YouTubeThumbnailDisplay item in itemInstances)
                    {
                        GameObject.Destroy(item.gameObject);
                    }
                }
            }

            if(!isInstantiated)
            {
                this.m_templateClone =
                    GameObject.Instantiate(this.containerTemplate.gameObject, templateParent);
                this.m_templateClone.transform.SetSiblingIndex(templateInstance_index);
                this.m_templateClone.name = templateInstance_name;

                YouTubeThumbnailDisplay itemInstance =
                    this.m_templateClone.GetComponentInChildren<YouTubeThumbnailDisplay>(true);
                this.m_container = (RectTransform)itemInstance.transform.parent;
                GameObject.Destroy(itemInstance.gameObject);

                this.m_templateClone.SetActive(true);
            }

            this.DisplayThumbnails(this.m_modId, this.m_youTubeIds);
        }

        /// <summary>Ensure the displays are accurate.</summary>
        protected virtual void OnEnable()
        {
            this.DisplayThumbnails(this.m_modId, this.m_youTubeIds);
        }

        /// <summary>IModViewElement interface.</summary>
        public virtual void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayProfile);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayProfile);
                this.DisplayProfile(this.m_view.profile);
            }
            else
            {
                this.DisplayProfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the YouTube thumbnails for a profile.</summary>
        public virtual void DisplayProfile(ModProfile profile)
        {
            int modId = ModProfile.NULL_ID;
            string[] youTubeIds = null;

            if(profile != null && profile.media != null && profile.media.youTubeURLs != null)
            {
                modId = profile.id;

                string[] URLs = profile.media.youTubeURLs;
                youTubeIds = new string[URLs.Length];

                for(int i = 0; i < URLs.Length; ++i)
                {
                    youTubeIds[i] = Utility.ExtractYouTubeIdFromURL(URLs[i]);
                }
            }

            this.DisplayThumbnails(modId, youTubeIds);
        }

        /// <summary>Displays a set of YouTube thumbnails.</summary>
        public virtual void DisplayThumbnails(int modId, IList<string> youTubeIds)
        {
            this.m_modId = modId;

            // copy ids
            if(this.m_youTubeIds != youTubeIds)
            {
                int thumbCount = 0;
                if(youTubeIds != null)
                {
                    thumbCount = youTubeIds.Count;
                }

                this.m_youTubeIds = new string[thumbCount];
                for(int i = 0; i < thumbCount; ++i) { this.m_youTubeIds[i] = youTubeIds[i]; }
            }

            // display
            if(this.m_itemTemplate != null)
            {
                // set view count
                UIUtilities.SetInstanceCount(this.m_container, this.m_itemTemplate,
                                             "YouTube Thumbnail", this.m_youTubeIds.Length,
                                             ref this.m_displays);

                // display data
                for(int i = 0; i < this.m_youTubeIds.Length; ++i)
                {
                    this.m_displays[i].DisplayThumbnail(this.m_modId, this.m_youTubeIds[i]);
                }

                // hide if necessary
                this.m_templateClone.SetActive(this.m_youTubeIds.Length > 0 || !this.hideIfEmpty);
            }
        }

        // ---------[ UTILITY ]---------
        /// <summary>Checks a YouTubeThumbnailContainer's template structure.</summary>
        public static bool HasValidTemplate(YouTubeThumbnailContainer container,
                                            out string helpMessage)
        {
            helpMessage = null;
            bool isValid = true;

            YouTubeThumbnailDisplay itemTemplate = null;

            // null check
            if(container.containerTemplate == null)
            {
                helpMessage = ("Invalid template:" + " The container template is unassigned.");
                isValid = false;
            }
            // containerTemplate is child of Component
            else if(!container.containerTemplate.IsChildOf(container.transform)
                    || container.containerTemplate == container.transform)
            {
                helpMessage = ("Invalid template:"
                               + " The container template must be a child of this object.");
                isValid = false;
            }
            // YouTubeThumbnailDisplay is found under containerTemplate
            else if((itemTemplate = container.containerTemplate.gameObject
                                        .GetComponentInChildren<YouTubeThumbnailDisplay>())
                    == null)
            {
                helpMessage =
                    ("Invalid template:"
                     + " No YouTubeThumbnailDisplay component found in the children of the container template.");
                isValid = false;
            }
            // YouTubeThumbnailDisplay is on same gameObject as containerTemplate
            else if(itemTemplate.transform == container.containerTemplate)
            {
                helpMessage =
                    ("Invalid template:"
                     + " The YouTubeThumbnailDisplay component cannot share a GameObject with the container template.");
                isValid = false;
            }

            return isValid;
        }
    }
}
