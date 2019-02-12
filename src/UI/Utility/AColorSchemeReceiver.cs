namespace ModIO.UI
{
    public abstract class AColorSchemeReceiver : UnityEngine.MonoBehaviour
    {
        public abstract void ApplyColorScheme(ColorScheme scheme);

        #if UNITY_EDITOR
        public abstract void ApplyColorScheme_withUndo(ColorScheme scheme);
        #endif
    }
}
