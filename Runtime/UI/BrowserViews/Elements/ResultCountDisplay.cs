using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the number of results for the view.</summary>
    public class ResultCountDisplay : MonoBehaviour, ISubscriptionsViewElement, IExplorerViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent SubscriptionsView.</summary>
        private SubscriptionsView m_subsView = null;

        /// <summary>Parent ExplorerView.</summary>
        private ExplorerView m_explorerView = null;

        /// <summary>Currently displayed count.</summary>
        private int m_resultCount = 0;

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
            this.m_textComponent.text = m_resultCount.ToString();
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
                this.m_subsView.onModPageChanged.RemoveListener(DisplayPageTotal);
            }
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onModPageChanged.RemoveListener(DisplayPageTotal);
            }

            // assign
            this.m_explorerView = null;
            this.m_subsView = view;

            // hook
            if(this.m_subsView != null)
            {
                this.m_subsView.onModPageChanged.AddListener(DisplayPageTotal);
                this.DisplayPageTotal(this.m_subsView.modPage);
            }
            else
            {
                this.DisplayPageTotal(null);
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
                this.m_subsView.onModPageChanged.RemoveListener(DisplayPageTotal);
            }
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onModPageChanged.RemoveListener(DisplayPageTotal);
            }

            // assign
            this.m_subsView = null;
            this.m_explorerView = view;

            // hook
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onModPageChanged.AddListener(DisplayPageTotal);
                this.DisplayPageTotal(this.m_explorerView.modPage);
            }
            else
            {
                this.DisplayPageTotal(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Gets the value to display from a RequestPage.</summary>
        public void DisplayPageTotal(RequestPage<ModProfile> page)
        {
            this.m_resultCount = 0;
            if(page != null)
            {
                this.m_resultCount = page.resultTotal;
            }

            if(this.isActiveAndEnabled)
            {
                this.m_textComponent.text = this.m_resultCount.ToString();
            }
        }
    }
}
