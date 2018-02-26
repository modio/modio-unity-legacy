#if UNITY_EDITOR
using System.IO;

using UnityEngine;
using UnityEditor;


namespace ModIO
{
    // TODO(@jackson): Use Temp folder for storing donwloaded images
    [CustomPropertyDrawer(typeof(API.ImageObject))]
    public class ImageObjectPropertyDrawer : PropertyDrawer
    {
        private Texture2D lastTexture;
        private string lastSource;
        private string lastSourceDisplay;

        private void OnTextureDownloaded(Download download)
        {
            if(lastSource == download.sourceURL)
            {
                lastTexture = (download as TextureDownload).texture;
            }
        }

        // --- PROPERTY DRAWER OVERRIDES ---
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // - Display Image Preview -
            bool doBrowse = false;

            EditorGUI.BeginProperty(position, label, property);
            
            Rect sourcePosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            sourcePosition = EditorGUI.PrefixLabel(sourcePosition, GUIUtility.GetControlID(FocusType.Passive), label);

            if(Event.current.type == EventType.Layout)
            {
                EditorGUI.TextField(sourcePosition, lastSourceDisplay);
            }
            else
            {
                doBrowse = GUI.Button(sourcePosition,
                                      (lastSourceDisplay == ""
                                       ? "Browse..."
                                       : lastSourceDisplay),
                                      GUI.skin.textField);
            }

            if(lastTexture != null)
            {
                Rect texturePosition = new Rect(sourcePosition.x,
                                                position.y + sourcePosition.height,
                                                sourcePosition.width,
                                                sourcePosition.width * 180f/320f);
                EditorGUI.DrawPreviewTexture(texturePosition,
                                             lastTexture,
                                             null,
                                             ScaleMode.ScaleAndCrop,
                                             320f/180f);
            }

            EditorGUI.EndProperty();

            // --- Finalization ---
            if(doBrowse)
            {
                EditorApplication.delayCall += () =>
                {
                    // TODO(@jackson): Add other file-types
                    string path = EditorUtility.OpenFilePanel("Select Mod Image", "", "png");
                    if (path.Length != 0)
                    {
                        property.FindPropertyRelative("filename").stringValue = Path.GetFileName(path);
                        property.FindPropertyRelative("original").stringValue = path;
                        property.FindPropertyRelative("thumb_320x180").stringValue = "";
                        property.serializedObject.ApplyModifiedProperties();
                    }
                };
            }

            // - Load Texture -
            if(property.FindPropertyRelative("original").stringValue != lastSourceDisplay)
            {
                lastSourceDisplay = property.FindPropertyRelative("original").stringValue;
                lastTexture = null;

                if(lastSourceDisplay.Contains("http://") || lastSourceDisplay.Contains("https://"))
                {
                    lastSource = property.FindPropertyRelative("thumb_320x180").stringValue;

                    TextureDownload download = new TextureDownload();
                    download.sourceURL = lastSource;
                    download.OnCompleted += OnTextureDownloaded;
                }
                else
                {
                    if(File.Exists(lastSourceDisplay))
                    {
                        lastSource = lastSourceDisplay;
                        lastTexture = new Texture2D(0,0);
                        lastTexture.LoadImage(File.ReadAllBytes(lastSource));
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float previewHeight = 0f;
            if(property.FindPropertyRelative("original").stringValue == lastSourceDisplay
               && lastTexture != null)
            {
                previewHeight = (EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth)
                                * 180f / 320f;
            }

            return EditorGUIUtility.singleLineHeight + previewHeight;
        }
    }
}

#endif