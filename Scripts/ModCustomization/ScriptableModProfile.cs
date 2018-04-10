using UnityEngine;

namespace ModIO
{
    [CreateAssetMenu(fileName = "New Mod Profile", menuName = "ModIO/Create Mod Profile")]
    public class ScriptableModProfile : ScriptableObject
    {
        public int modId = 0;
        public EditableModProfile modEdits = new EditableModProfile();
    }
}
