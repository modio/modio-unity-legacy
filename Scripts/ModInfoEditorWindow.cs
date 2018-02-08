#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ModIO
{
    public class ModInfoEditorWindow : EditorWindow
    {
        private const int GAME_ID = 0;
        private const string API_KEY = "";

        private EditableModInfo modInfo = new EditableModInfo();
        private List<ModTag> modTags = new List<ModTag>();

        [MenuItem("ModIO/Mod Inspector")]
        public static void ShowWindow()
        {
            GetWindow<ModInfoEditorWindow>("Mod Inspector");
        }

        public void LoadModInfo(ModInfo mod)
        {
            modInfo = EditableModInfo.FromModInfo(mod);
            // TODO(@jackson): Load ModTags
        }

        public void OnGUI()
        {
            ModManager.Initialize(GAME_ID, API_KEY);

            int modOptionIndex = 0;
            ModInfo[] modList = ModManager.GetMods(GetAllModsFilter.None);
            string[] modOptions = new string[modList.Length + 1];

            modOptions[0] = "[NEW MOD]";

            for(int i = 0; i < modList.Length; ++i)
            {
                ModInfo mod = modList[i];
                modOptions[i+1] = mod.name;

                if(modInfo.id == mod.id)
                {
                    modOptionIndex = i + 1;
                }
            }

            modOptionIndex = EditorGUILayout.Popup("Select Mod", modOptionIndex, modOptions, null);

            if(modOptionIndex > 0)
            {
                modInfo = EditableModInfo.FromModInfo(modList[modOptionIndex - 1]);
            }

            EditorGUILayout.TextField("Mod Name", modInfo.name, null);
        }
    }
}

#endif