using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the YouTube thumbnails for a given mod.</summary>
    public class YouTubeThumbnailContainer : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying the YouTube thumbs.</summary>
        public RectTransform template = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = true;

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
        /// <summary>Initialize template.</summary>
        protected virtual void Awake()
        {
            // duplication protection
            if(this.m_itemTemplate != null) { return; }

            // initialize
            this.template.gameObject.SetActive(false);
            this.m_itemTemplate = this.template.GetComponentInChildren<YouTubeThumbnailDisplay>(true);

            if(this.m_itemTemplate != null
               && this.template.gameObject != this.m_itemTemplate.gameObject)
            {
                this.m_templateClone = GameObject.Instantiate(this.template.gameObject, this.template.parent);
                this.m_templateClone.SetActive(true);
                this.m_templateClone.transform.SetSiblingIndex(this.template.GetSiblingIndex() + 1);

                this.m_displays = new YouTubeThumbnailDisplay[1];
                this.m_displays[0] = this.m_templateClone.GetComponentInChildren<YouTubeThumbnailDisplay>(true);
                this.m_displays[0].gameObject.name = "YouTube Thumbnail [00]";

                this.m_container = (RectTransform)this.m_displays[0].transform.parent;
            }
            else
            {
                Debug.LogError("[mod.io] This YouTubeThumbnailContainer has an invalid template"
                               + " hierarchy. The Template must contain a child with a"
                               + " YouTubeThumbnailDisplay component to use as the item template.",
                               this);
            }
        }

        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public virtual void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

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

        /// <summary>Ensure the displays are accurate.</summary>
        protected virtual void OnEnable()
        {
            this.DisplayThumbnails(this.m_modId, this.m_youTubeIds);
        }


        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the YouTube thumbnails for a profile.</summary>
        public virtual void DisplayProfile(ModProfile profile)
        {
            int modId = ModProfile.NULL_ID;
            string[] youTubeIds = null;

            if(profile != null
               && profile.media != null
               && profile.media.youTubeURLs != null)
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
                for(int i = 0; i < thumbCount; ++i)
                {
                    this.m_youTubeIds[i] = youTubeIds[i];
                }
            }

            // display
            if(this.isActiveAndEnabled)
            {
                this.SetDisplayCount(this.m_youTubeIds.Length);

                for(int i = 0;
                    i < this.m_youTubeIds.Length;
                    ++i)
                {
                    this.m_displays[i].DisplayThumbnail(this.m_modId, this.m_youTubeIds[i]);
                }

                this.m_templateClone.SetActive(this.m_youTubeIds.Length > 0 || !this.hideIfEmpty);
            }
        }

        /// <summary>Creates/Destroys display objects to match the given value.</summary>
        protected virtual void SetDisplayCount(int newCount)
        {
            int difference = newCount - this.m_displays.Length;

            if(difference > 0)
            {
                YouTubeThumbnailDisplay[] newDisplayArray = new YouTubeThumbnailDisplay[newCount];

                for(int i = 0;
                    i < this.m_displays.Length;
                    ++i)
                {
                    newDisplayArray[i] = this.m_displays[i];
                }

                for(int i = this.m_displays.Length;
                    i < newDisplayArray.Length;
                    ++i)
                {
                    GameObject displayGO = GameObject.Instantiate(m_itemTemplate.gameObject);
                    displayGO.name = "YouTube Thumbnail [" + i.ToString("00") + "]";
                    displayGO.transform.SetParent(this.m_container, false);

                    newDisplayArray[i] = displayGO.GetComponent<YouTubeThumbnailDisplay>();
                }

                this.m_displays = newDisplayArray;
            }
            else if(difference < 0)
            {
                YouTubeThumbnailDisplay[] newDisplayArray = new YouTubeThumbnailDisplay[newCount];

                for(int i = 0;
                    i < newDisplayArray.Length;
                    ++i)
                {
                    newDisplayArray[i] = this.m_displays[i];
                }

                for(int i = newDisplayArray.Length;
                    i < this.m_displays.Length;
                    ++i)
                {
                    GameObject.Destroy(this.m_displays[i].gameObject);
                }

                this.m_displays = newDisplayArray;
            }
        }
    }
}
