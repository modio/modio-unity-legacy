using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class SelectChildOnEnable : MonoBehaviour
    {
        private void OnEnable()
        {
            Selectable childSelectable = this.gameObject.GetComponentInChildren<Selectable>();

            this.StartCoroutine(DelaySelect(childSelectable));
        }

        private System.Collections.IEnumerator DelaySelect(Selectable selectable)
        {
            yield return null;

            if(selectable != null)
            {
                selectable.Select();
            }
        }
    }
}
