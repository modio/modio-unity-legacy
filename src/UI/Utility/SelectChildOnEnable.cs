using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class SelectChildOnEnable : MonoBehaviour
    {
        private void OnEnable()
        {
            Selectable childSelectable = this.gameObject.GetComponentInChildren<Selectable>();
            if(childSelectable != null)
            {
                childSelectable.Select();
            }
        }
    }
}
