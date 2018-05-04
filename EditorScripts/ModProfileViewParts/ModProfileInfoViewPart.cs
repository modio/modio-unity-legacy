#if UNITY_EDITOR
// #define ENABLE_MOD_STOCK

using System.Collections.Generic;
using Path = System.IO.Path;
using System.Linq; // TODO(@jackson): Remove

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ModIO
{
    // TODO(@jackson): Force repaint on Callbacks
    // TODO(@jackson): Use cache originalModLogo and use Unity built in image inspector?
    public class ModProfileInfoViewPart : IModProfileViewPart
    {
        // ------[ CONSTANTS ]------
        private const ModLogoVersion LOGO_PREVIEW_VERSION = ModLogoVersion.Thumbnail_320x180;
        private const float LOGO_PREVIEW_WIDTH = 320;
        private const float LOGO_PREVIEW_HEIGHT = 180;

        // ------[ EDITOR CACHING ]------
        private SerializedProperty editableProfileProperty;
        private ModProfile profile;
        private bool isUndoEnabled = false;
        private bool isRepaintRequired = false;

        // - Logo -
        private SerializedProperty logoProperty;
        private Texture2D logoTexture;
        private string logoLocation;

        // - Tags -
        private bool isTagsExpanded;

        // ------[ INITIALIZATION ]------
        public void OnEnable(SerializedProperty serializedEditableModProfile, ModProfile baseProfile)
        {
            this.editableProfileProperty = serializedEditableModProfile;
            this.profile = baseProfile;
            this.isUndoEnabled = (baseProfile != null);

            isTagsExpanded = false;

            // - Configure Properties -
            logoProperty = editableProfileProperty.FindPropertyRelative("logoLocator");

            // - Load Textures -
            if(logoProperty.FindPropertyRelative("isDirty").boolValue == true)
            {
                logoLocation = logoProperty.FindPropertyRelative("value.url").stringValue;
                Utility.TryLoadTextureFromFile(logoLocation, out logoTexture);
            }
            else if(profile != null)
            {
                logoLocation = profile.logoLocator.GetVersionURL(LOGO_PREVIEW_VERSION);
                logoTexture = UISettings.Instance.DownloadingPlaceholderImages.modLogo;

                CacheManager.GetModLogo(profile, LOGO_PREVIEW_VERSION,
                                        (t) => { logoTexture = t; isRepaintRequired = true; },
                                        API.Client.LogError);
            }
            else
            {
                logoLocation = string.Empty;
                logoTexture = null;
            }

            // - Handle Updates -
            ModManager.OnModLogoUpdated += OnModLogoUpdated;
        }

        public void OnDisable()
        {
            ModManager.OnModLogoUpdated -= OnModLogoUpdated;
        }

        // ------[ UPDATE ]------
        public void OnUpdate() {}

        private void OnModLogoUpdated(int modId, ModLogoVersion version, Texture2D texture)
        {
            if(profile != null
               && profile.id == modId
               && version == LOGO_PREVIEW_VERSION
               && logoProperty.FindPropertyRelative("isDirty").boolValue == false)
            {
                logoTexture = texture;
                isRepaintRequired = true;
            }
        }

        // ------[ GUI ]------
        public virtual void OnGUI()
        {
            isRepaintRequired = false;

            LayoutNameField();
            LayoutNameIDField();
            LayoutLogoField();
            LayoutVisibilityField();
            LayoutTagsField();
            LayoutHomepageField();
            LayoutSummaryField();
            LayoutDescriptionField();
            LayoutStockField();
            LayoutMetadataField();

            // LayoutModDependenciesField();
            // LayoutMetadataKVPField();
        }

        public virtual bool IsRepaintRequired()
        {
            return this.isRepaintRequired;
        }

        // ---------[ SIMPLE LAYOUT FUNCTIONS ]---------
        protected void LayoutEditablePropertySimple(string fieldName,
                                                    SerializedProperty fieldProperty)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(fieldProperty.FindPropertyRelative("value"),
                                          new GUIContent(fieldName));
            if(EditorGUI.EndChangeCheck())
            {
                fieldProperty.FindPropertyRelative("isDirty").boolValue = true;
            }
        }

        protected virtual void LayoutNameField()
        {
            EditorGUILayout.BeginHorizontal();
                LayoutEditablePropertySimple("Name", editableProfileProperty.FindPropertyRelative("name"));
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                editableProfileProperty.FindPropertyRelative("name.value").stringValue = profile.name;
                editableProfileProperty.FindPropertyRelative("name.isDirty").boolValue = false;
            }
        }

        protected virtual void LayoutVisibilityField()
        {
            EditorGUILayout.BeginHorizontal();
                LayoutEditablePropertySimple("Visibility", editableProfileProperty.FindPropertyRelative("visibility"));
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                editableProfileProperty.FindPropertyRelative("visibility.value").intValue = (int)profile.visibility;
                editableProfileProperty.FindPropertyRelative("visibility.isDirty").boolValue = false;
            }
        }

        protected virtual void LayoutHomepageField()
        {
            EditorGUILayout.BeginHorizontal();
                LayoutEditablePropertySimple("Homepage", editableProfileProperty.FindPropertyRelative("homepageURL"));
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                editableProfileProperty.FindPropertyRelative("homepageURL.value").stringValue = profile.homepageURL;
                editableProfileProperty.FindPropertyRelative("homepageURL.isDirty").boolValue = false;
            }
        }

        // ---------[ TEXT AREAS ]---------
        protected void LayoutEditablePropertyTextArea(string fieldName,
                                                      SerializedProperty fieldProperty,
                                                      int characterLimit,
                                                      out bool isUndoRequested)
        {
            SerializedProperty fieldValueProperty = fieldProperty.FindPropertyRelative("value");
            int charCount = fieldValueProperty.stringValue.Length;

            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(fieldName);
                EditorGUILayout.LabelField("[" + (characterLimit - charCount).ToString()
                                           + " characters remaining]");
                isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
                fieldValueProperty.stringValue
                    = EditorGUILayoutExtensions.MultilineTextField(fieldValueProperty.stringValue);
            if(EditorGUI.EndChangeCheck())
            {
                if(fieldValueProperty.stringValue.Length > characterLimit)
                {
                    fieldValueProperty.stringValue
                        = fieldValueProperty.stringValue.Substring(0, characterLimit);
                }

                fieldProperty.FindPropertyRelative("isDirty").boolValue = true;
            }
            
        }

        protected virtual void LayoutSummaryField()
        {
            bool isUndoRequested;
            LayoutEditablePropertyTextArea("Summary",
                                           editableProfileProperty.FindPropertyRelative("summary"),
                                           API.EditModParameters.SUMMARY_CHAR_LIMIT,
                                           out isUndoRequested);
            if(isUndoRequested)
            {
                editableProfileProperty.FindPropertyRelative("summary.value").stringValue = profile.summary;
                editableProfileProperty.FindPropertyRelative("summary.isDirty").boolValue = false;
            }
        }

        protected virtual void LayoutDescriptionField()
        {
            bool isUndoRequested;
            LayoutEditablePropertyTextArea("Description",
                                           editableProfileProperty.FindPropertyRelative("description"),
                                           API.EditModParameters.DESCRIPTION_CHAR_LIMIT,
                                           out isUndoRequested);

            if(isUndoRequested)
            {
                editableProfileProperty.FindPropertyRelative("description.value").stringValue = profile.description;
                editableProfileProperty.FindPropertyRelative("description.isDirty").boolValue = false;
            }
        }

        protected virtual void LayoutMetadataField()
        {
            bool isUndoRequested;
            LayoutEditablePropertyTextArea("Metadata",
                                           editableProfileProperty.FindPropertyRelative("metadataBlob"),
                                           API.EditModParameters.DESCRIPTION_CHAR_LIMIT,
                                           out isUndoRequested);

            if(isUndoRequested)
            {
                editableProfileProperty.FindPropertyRelative("metadataBlob.value").stringValue = profile.metadataBlob;
                editableProfileProperty.FindPropertyRelative("metadataBlob.isDirty").boolValue = false;
            }
        }



        // ---------[ SUPER JANKY ]---------
        protected virtual void LayoutNameIDField()
        {
            bool isDirty = false;
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Profile URL");
                EditorGUILayout.LabelField("@", GUILayout.Width(13));
                
                EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(editableProfileProperty.FindPropertyRelative("nameId.value"),
                                                  GUIContent.none);
                isDirty = EditorGUI.EndChangeCheck();

                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
            EditorGUILayout.EndHorizontal();

            editableProfileProperty.FindPropertyRelative("nameId.isDirty").boolValue |= isDirty;

            if(isUndoRequested)
            {
                editableProfileProperty.FindPropertyRelative("nameId.value").stringValue = profile.nameId;
                editableProfileProperty.FindPropertyRelative("nameId.isDirty").boolValue = false;
            }
        }

        protected virtual void LayoutLogoField()
        {
            bool doBrowse = false;

            // - Browse Field -
            EditorGUILayout.BeginHorizontal();
                doBrowse |= EditorGUILayoutExtensions.BrowseButton(logoLocation,
                                                                   new GUIContent("Logo"));
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
            EditorGUILayout.EndHorizontal();

            // - Draw Texture -
            if(logoTexture != null)
            {
                //TODO(@jackson): Make full-width
                Rect logoRect = EditorGUILayout.GetControlRect(false,
                                                               LOGO_PREVIEW_HEIGHT,
                                                               null);
                EditorGUI.DrawPreviewTexture(new Rect((logoRect.width - LOGO_PREVIEW_WIDTH) * 0.5f,
                                                      logoRect.y,
                                                      LOGO_PREVIEW_WIDTH,
                                                      logoRect.height),
                                             logoTexture,
                                             null,
                                             ScaleMode.ScaleAndCrop);
                doBrowse |= GUI.Button(logoRect, "", GUI.skin.label);
            }

            if(doBrowse)
            {
                EditorApplication.delayCall += () =>
                {
                    Texture2D newLogoTexture;

                    // TODO(@jackson): Add other file-types
                    string path = EditorUtility.OpenFilePanel("Select Mod Logo", "", "png");
                    if (path.Length != 0
                        && Utility.TryLoadTextureFromFile(path, out newLogoTexture))
                    {
                        logoProperty.FindPropertyRelative("value.url").stringValue = path;
                        logoProperty.FindPropertyRelative("value.fileName").stringValue = Path.GetFileName(path);
                        logoProperty.FindPropertyRelative("isDirty").boolValue = true;
                        logoProperty.serializedObject.ApplyModifiedProperties();

                        logoTexture = newLogoTexture;
                        logoLocation = path;
                    }
                };
            }

            if(isUndoRequested)
            {
                logoProperty.FindPropertyRelative("value.url").stringValue = profile.logoLocator.GetURL();
                logoProperty.FindPropertyRelative("value.fileName").stringValue = profile.logoLocator.GetFileName();
                logoProperty.FindPropertyRelative("isDirty").boolValue = false;

                logoLocation = profile.logoLocator.GetVersionURL(LOGO_PREVIEW_VERSION);
                logoTexture = UISettings.Instance.DownloadingPlaceholderImages.modLogo;

                CacheManager.GetModLogo(profile, LOGO_PREVIEW_VERSION,
                                        (t) => { logoTexture = t; isRepaintRequired = true; },
                                        API.Client.LogError);
            }
        }

        protected virtual void LayoutTagsField()
        {
            using(new EditorGUI.DisabledScope(ModManager.GetGameProfile() == null))
            {
                EditorGUILayout.BeginHorizontal();
                    this.isTagsExpanded = EditorGUILayout.Foldout(this.isTagsExpanded, "Tags", true);
                    GUILayout.FlexibleSpace();
                    bool isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
                EditorGUILayout.EndHorizontal();

                if(this.isTagsExpanded)
                {
                    if(ModManager.GetGameProfile() == null)
                    {
                        EditorGUILayout.HelpBox("The Game's Profile is not yet loaded, and thus tags cannot be displayed. Please wait...", MessageType.Warning);
                    }
                    else if(ModManager.GetGameProfile().taggingOptions.Count == 0)
                    {
                        EditorGUILayout.HelpBox("The developers of "
                                                + ModManager.GetGameProfile().name
                                                + " have not designated any tagging options",
                                                MessageType.Info);
                    }
                    else
                    {
                        var tagsProperty = editableProfileProperty.FindPropertyRelative("tags.value");
                        var selectedTags = new List<string>(EditorUtilityExtensions.GetSerializedPropertyStringArray(tagsProperty));
                        bool isDirty = false;

                        ++EditorGUI.indentLevel;
                            foreach(ModTagCategory tagCategory in ModManager.GetGameProfile().taggingOptions)
                            {
                                if(!tagCategory.isHidden)
                                {
                                    bool wasSelectionModified;
                                    LayoutTagCategoryField(tagCategory, ref selectedTags, out wasSelectionModified);
                                    isDirty |= wasSelectionModified;
                                }
                            }
                        --EditorGUI.indentLevel;

                        if(isDirty)
                        {
                            EditorUtilityExtensions.SetSerializedPropertyStringArray(tagsProperty, selectedTags.ToArray());
                            editableProfileProperty.FindPropertyRelative("tags.isDirty").boolValue = true;
                        }
                    }

                    if(isUndoRequested)
                    {
                        var tagsProperty = editableProfileProperty.FindPropertyRelative("tags.value");
                        EditorUtilityExtensions.SetSerializedPropertyStringArray(tagsProperty,
                                                                                 profile.tagNames.ToArray());
                        editableProfileProperty.FindPropertyRelative("tags.isDirty").boolValue = false;
                    }
                }
            }
        }

        protected virtual void LayoutTagCategoryField(ModTagCategory tagCategory,
                                                      ref List<string> selectedTags,
                                                      out bool wasSelectionModified)
        {
            wasSelectionModified = false;

            // EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(tagCategory.name);

            EditorGUILayout.BeginVertical();
                if(!tagCategory.isFlag)
                {
                    string oldSelectedTag = string.Empty;
                    foreach(string tag in tagCategory.tags)
                    {
                        if(selectedTags.Contains(tag))
                        {
                            oldSelectedTag = tag;
                        }
                    }

                    string newSelectedTag = string.Empty;
                    foreach(string tag in tagCategory.tags)
                    {
                        bool isSelected = (tag == oldSelectedTag);
                        isSelected = EditorGUILayout.Toggle(tag, isSelected, EditorStyles.radioButton);

                        if(isSelected)
                        {
                            newSelectedTag = tag;
                        }
                    }

                    if(newSelectedTag != oldSelectedTag)
                    {
                        wasSelectionModified = true;

                        selectedTags.Remove(oldSelectedTag);

                        if(!System.String.IsNullOrEmpty(newSelectedTag))
                        {
                            selectedTags.Add(newSelectedTag);
                        }
                    }
                }
                else
                {
                    foreach(string tag in tagCategory.tags)
                    {
                        bool wasSelected = selectedTags.Contains(tag);
                        bool isSelected = EditorGUILayout.Toggle(tag, wasSelected);

                        if(wasSelected != isSelected)
                        {
                            wasSelectionModified = true;

                            if(isSelected)
                            {
                                selectedTags.Add(tag);
                            }
                            else
                            {
                                selectedTags.Remove(tag);
                            }
                        }
                    }
                }
            EditorGUILayout.EndVertical();
            // EditorGUILayout.EndHorizontal();
        }

        protected virtual void LayoutStockField()
        {
            #if ENABLE_MOD_STOCK
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Stock");

                EditorGUILayout.PropertyField(editableProfileProperty.FindPropertyRelative("stock.value"),
                                              GUIContent.none);//, GUILayout.Width(40));

                // TODO(@jackson): Change to checkbox
                EditorGUILayout.LabelField("0 = Unlimited", GUILayout.Width(80));
                bool isUndoRequested = EditorGUILayoutExtensions.UndoButton(isUndoEnabled);
            EditorGUILayout.EndHorizontal();

            if(isUndoRequested)
            {
                editableProfileProperty.FindPropertyRelative("stock.value").intValue = profile.stock;
                editableProfileProperty.FindPropertyRelative("stock.isDirty").boolValue = false;
            }
            #endif
        }
    }
}

#endif
