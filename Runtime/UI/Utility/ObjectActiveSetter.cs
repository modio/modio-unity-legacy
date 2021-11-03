namespace ModIO.UI
{
    /// <summary>Component that can be used to activate/deactivate the GameObject attached to
    /// it.</summary>
    public class ObjectActiveSetter : UnityEngine.MonoBehaviour
    {
        /// <summary>Checks to see if an object is null.</summary>
        public void ActiveOnlyIfNull(object o)
        {
            this.gameObject.SetActive(o == null);
        }

        /// <summary>Checks to see if an object is null.</summary>
        public void ActiveOnlyIfNotNull(object o)
        {
            this.gameObject.SetActive(o != null);
        }
    }
}
