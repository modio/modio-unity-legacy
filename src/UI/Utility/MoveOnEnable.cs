using UnityEngine;

namespace ModIO.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class MoveOnEnable : MonoBehaviour
    {
        public Vector2 anchoredPosition = Vector2.zero;
        public bool lateMove = false;

        private void OnEnable()
        {
            StartCoroutine(this.DoMove());
        }

        private System.Collections.IEnumerator DoMove()
        {
            if(this.lateMove)
            {
                yield return null;
            }

            this.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }
}
