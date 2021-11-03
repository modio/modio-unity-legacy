using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the number of pages in the results for the view.</summary>
    public class PageNumberDisplay : MonoBehaviour, ISubscriptionsViewElement, IExplorerViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent SubscriptionsView.</summary>
        private SubscriptionsView m_subsView = null;

        /// <summary>Parent ExplorerView.</summary>
        private ExplorerView m_explorerView = null;

        /// <summary>Page size used to generate the page number.</summary>
        private int m_pageSize = 1;

        /// <summary>Result index used to generate the page number.</summary>
        private int m_resultIndex = 0;

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
                this.m_subsView.onModPageChanged.RemoveListener(DisplayPageNumber);
            }
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onModPageChanged.RemoveListener(DisplayPageNumber);
            }

            // assign
            this.m_explorerView = null;
            this.m_subsView = view;

            // hook
            if(this.m_subsView != null)
            {
                this.m_subsView.onModPageChanged.AddListener(DisplayPageNumber);
                this.DisplayPageNumber(this.m_subsView.modPage);
            }
            else
            {
                this.DisplayPageNumber(null);
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
                this.m_subsView.onModPageChanged.RemoveListener(DisplayPageNumber);
            }
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onModPageChanged.RemoveListener(DisplayPageNumber);
            }

            // assign
            this.m_subsView = null;
            this.m_explorerView = view;

            // hook
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onModPageChanged.AddListener(DisplayPageNumber);
                this.DisplayPageNumber(this.m_explorerView.modPage);
            }
            else
            {
                this.DisplayPageNumber(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Gets the value to display from a RequestPage.</summary>
        public void DisplayPageNumber(RequestPage<ModProfile> page)
        {
            this.m_pageSize = 0;
            this.m_resultIndex = 0;

            if(page != null)
            {
                this.m_pageSize = page.size;
                this.m_resultIndex = page.resultOffset;
            }

            this.Refresh();
        }

        /// <summary>Refreshes the display.</summary>
        public void Refresh()
        {
            if(this.isActiveAndEnabled)
            {
                int pageNumber = 0;
                if(this.m_pageSize > 0)
                {
                    pageNumber =
                        1 + (int)Mathf.Floor((float)this.m_resultIndex / (float)this.m_pageSize);
                }
                this.m_textComponent.text = pageNumber.ToString();
            }
        }
    }
}
