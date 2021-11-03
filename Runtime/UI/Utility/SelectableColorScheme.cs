using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [CreateAssetMenu(fileName = "Selectable Color Scheme.asset",
                     menuName = "mod.io/Theming/Selectable Color Scheme")]
    public class SelectableColorScheme : ScriptableObject
    {
        public Color imageColor = Color.white;
        public ColorBlock functionalColors = ColorBlock.defaultColorBlock;
        public Color innerElementColor = Color.white;
        public Color toggleColor = Color.white;
    }
}
