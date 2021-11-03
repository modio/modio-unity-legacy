using UnityEngine;

namespace ModIO.UI
{
    [CreateAssetMenu(fileName = "Graphic Color Scheme.asset",
                     menuName = "mod.io/Theming/Graphic Color Scheme")]
    public class GraphicColorScheme : ScriptableObject
    {
        public Color baseColor = Color.white;
        public Color innerElementColor = Color.white;
    }
}
