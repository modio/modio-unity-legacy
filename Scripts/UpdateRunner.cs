using UnityEngine;

namespace ModIO
{
    public class UpdateRunner : MonoBehaviour
    {
        private static UpdateRunner _instance;
        public static UpdateRunner Instance
        {
            get
            {
                return _instance;
            }
        }
        
        public static event System.Action onUpdate;

        private void OnEnable()
        {
            if(_instance == null)
            {
                _instance = this;
            }
        }
        private void OnDisable()
        {
            if(_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if(onUpdate != null)
            {
                onUpdate();
            }
        }

    }
}