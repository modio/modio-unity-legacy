using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class MessageDisplay : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public event System.Action<MessageDisplay> onClick;

        [Header("UI Components")]
        public Text content;

        // ---------[ EVENTS ]---------
        public void NotifyClicked()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }
    }
}
