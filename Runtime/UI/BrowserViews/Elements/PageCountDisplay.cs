using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the number of pages in the results for the view.</summary>
    public class PageCountDisplay : MonoBehaviour, ISubscriptionsViewElement, IExplorerViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent SubscriptionsView.</summary>
        private SubscriptionsView m_subsView = null;

        /// <summary>Parent ExplorerView.</summary>
        private ExplorerView m_explorerView = null;

        /// <summary>Result count used to generate the page count.</summary>
        private int m_resultCount = 0;

        /// <summary>Page size used to generate the page count.</summary>
        private int m_pageSize = 1;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Collect text component.</summary>
        protected virtual void Awake()
        {
            Component textDisplayComponent =
                GenericTextComponent.FindCompatibleTextComponent(this.gameObject);
            this.m_textComponent.SetTextDisplayComponent(textDisplayComponent);

#if DEBUG
            if(textDisplayComponent == null)
            {
                Debug.LogWarning("[mod.io] No compatible text components were found on this "
                                     + "GameObject to set text for."
                                     + "\nCompatible with any component that exposes a"
                                     + " publicly settable \'.text\' property.",
                                 this);
            }
#endif
        }

        /// <summary>Assert display is current.</summary>
        protected virtual void OnEnable()
        {
            this.Refresh();
        }

        /// <summary>ISubscriptionsViewElement interface.</summary>
        public void SetSubscriptionsView(SubscriptionsView view)
        {
            // early out
            if(this.m_subsView == view)
            {
                return;
            }

            // unhook
            if(this.m_subsView != null)
            {
                this.m_subsView.onModPageChanged.RemoveListener(DisplayPageCount);
            }
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onModPageChanged.RemoveListener(DisplayPageCount);
            }

            // assign
            this.m_explorerView = null;
            this.m_subsView = view;

            // hook
            if(this.m_subsView != null)
            {
                this.m_subsView.onModPageChanged.AddListener(DisplayPageCount);
                this.DisplayPageCount(this.m_subsView.modPage);
            }
            else
            {
                this.DisplayPageCount(null);
            }
        }

        /// <summary>IExplorerViewElement interface.</summary>
        public void SetExplorerView(ExplorerView view)
        {
            // early out
            if(this.m_explorerView == view)
            {
                return;
            }

            // unhook
            if(this.m_subsView != null)
            {
                this.m_subsView.onModPageChanged.RemoveListener(DisplayPageCount);
            }
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onModPageChanged.RemoveListener(DisplayPageCount);
            }

            // assign
            this.m_subsView = null;
            this.m_explorerView = view;

            // hook
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onModPageChanged.AddListener(DisplayPageCount);
                this.DisplayPageCount(this.m_explorerView.modPage);
            }
            else
            {
                this.DisplayPageCount(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Gets the value to display from a RequestPage.</summary>
        public void DisplayPageCount(RequestPage<ModProfile> page)
        {
            this.m_resultCount = 0;
            this.m_pageSize = 1;

            if(page != null && page.size > 0)
            {
                this.m_resultCount = page.resultTotal;
                this.m_pageSize = page.size;
            }

            this.Refresh();
        }

        /// <summary>Refreshes the display.</summary>
        public void Refresh()
        {
            if(this.isActiveAndEnabled)
            {
                int pageCount = (int)Mathf.Ceil((float)this.m_resultCount / (float)this.m_pageSize);
                this.m_textComponent.text = pageCount.ToString();
            }
        }
    }
}
