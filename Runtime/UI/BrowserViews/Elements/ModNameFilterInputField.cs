using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>A component that pairs an input field with the title filter of a ExplorerView or
    /// SubscriptionsView.</summary>
    [RequireComponent(typeof(InputField))]
    public class ModNameFilterInputField : MonoBehaviour,
                                           IExplorerViewElement,
                                           ISubscriptionsViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Parent ExplorerView.</summary>
        private ExplorerView m_explorerView = null;

        /// <summary>Parent SubscriptionsView.</summary>
        private SubscriptionsView m_subscriptionsView = null;

        // ---------[ INITIALIZATION ]---------
        /// <summary>IExplorerViewElement interface.</summary>
        public void SetExplorerView(ExplorerView view)
        {
            // early out
            if(this.m_explorerView == view)
            {
                return;
            }

            // unhook
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onRequestFilterChanged.RemoveListener(UpdateInputField);

                this.GetComponent<InputField>().onEndEdit.RemoveListener(SetExplorerViewFilter);
            }
            if(this.m_subscriptionsView != null)
            {
                this.m_subscriptionsView.onNameFieldFilterChanged.RemoveListener(UpdateInputField);

                this.GetComponent<InputField>().onValueChanged.RemoveListener(
                    SetSubscriptionsViewFilter);
            }

            // assign
            this.m_explorerView = view;
            this.m_subscriptionsView = null;

            // hook
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onRequestFilterChanged.AddListener(UpdateInputField);
                this.UpdateInputField(this.m_explorerView.requestFilter);

                this.GetComponent<InputField>().onEndEdit.AddListener(SetExplorerViewFilter);
            }
            else
            {
                this.UpdateInputField(string.Empty);
            }
        }

        /// <summary>ISubscriptionsViewElement interface.</summary>
        public void SetSubscriptionsView(SubscriptionsView view)
        {
            // early out
            if(this.m_subscriptionsView == view)
            {
                return;
            }

            // unhook
            if(this.m_explorerView != null)
            {
                this.m_explorerView.onRequestFilterChanged.RemoveListener(UpdateInputField);

                this.GetComponent<InputField>().onEndEdit.RemoveListener(SetExplorerViewFilter);
            }
            if(this.m_subscriptionsView != null)
            {
                this.m_subscriptionsView.onNameFieldFilterChanged.RemoveListener(UpdateInputField);

                this.GetComponent<InputField>().onValueChanged.RemoveListener(
                    SetSubscriptionsViewFilter);
            }

            // assign
            this.m_explorerView = null;
            this.m_subscriptionsView = view;

            // hook
            if(this.m_subscriptionsView != null)
            {
                this.m_subscriptionsView.onNameFieldFilterChanged.AddListener(UpdateInputField);
                this.UpdateInputField(this.m_subscriptionsView.nameFieldFilter);

                this.GetComponent<InputField>().onValueChanged.AddListener(
                    SetSubscriptionsViewFilter);
            }
            else
            {
                this.UpdateInputField(string.Empty);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Sets the value of the input field based on the value in the
        /// RequestFilter.</summary>
        public virtual void UpdateInputField(RequestFilter requestFilter)
        {
            string filterValue = string.Empty;
            List<IRequestFieldFilter> fieldFilterList;
            if(requestFilter != null
               && requestFilter.fieldFilterMap.TryGetValue(
                   ModIO.API.GetAllModsFilterFields.fullTextSearch, out fieldFilterList)
               && fieldFilterList != null && fieldFilterList.Count > 0)
            {
                IRequestFieldFilter fieldFilter = fieldFilterList[0];

                switch(fieldFilter.filterMethod)
                {
                    case FieldFilterMethod.Equal:
                    {
                        filterValue = ((EqualToFilter<string>)fieldFilter).filterValue;
                    }
                    break;
                    case FieldFilterMethod.LikeString:
                    {
                        filterValue = ((StringLikeFilter)fieldFilter).likeValue;
                    }
                    break;
                }
            }

            this.UpdateInputField(filterValue);
        }

        /// <summary>Sets the value of the input field.</summary>
        public virtual void UpdateInputField(string filterValue)
        {
            this.gameObject.GetComponent<InputField>().text = filterValue;
        }

        /// <summary>Sets the filter value in the ExplorerView.</summary>
        protected virtual void SetExplorerViewFilter(string newValue)
        {
            if(this.m_explorerView != null)
            {
                this.m_explorerView.SetNameFieldFilter(newValue);
            }
        }

        /// <summary>Sets the filter value in the SubscriptionsView.</summary>
        protected virtual void SetSubscriptionsViewFilter(string newValue)
        {
            if(this.m_subscriptionsView != null)
            {
                this.m_subscriptionsView.SetNameFieldFilter(newValue);
            }
        }
    }
}
