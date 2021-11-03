using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModIO.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ToastAnimationSettings : MonoBehaviour
    {
        public Vector2 offset = new Vector2(0f, 100f);
        public float duration = 0.5f;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ToastAnimationSettings))]
    [CanEditMultipleObjects]
    public class ToastAnimationEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            ToastAnimationSettings t = target as ToastAnimationSettings;
            RectTransform rt = t.gameObject.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector2 pivot = new Vector2(corners[0].x + (rt.rect.width * rt.pivot.x),
                                        corners[0].y + (rt.rect.height * rt.pivot.y));

            for(int i = 0; i < corners.Length; ++i)
            {
                corners[i] += (Vector3)t.offset;
                // corners[i] = corners[i] + (Vector3)offsetProperty.vector2Value;
            }

            // Handles.BeginGUI();
            {
                Handles.color = Color.grey;

                Handles.DrawLine(corners[0], corners[1]);
                Handles.DrawLine(corners[1], corners[2]);
                Handles.DrawLine(corners[2], corners[3]);
                Handles.DrawLine(corners[3], corners[0]);
                Handles.DrawLine(pivot, pivot + t.offset);

                Handles.DrawWireDisc(pivot + t.offset, Vector3.forward, 10f);

                Handles.color = Color.white;
                Handles.DrawDottedLine(corners[0], corners[1], 10f);
                Handles.DrawDottedLine(corners[1], corners[2], 10f);
                Handles.DrawDottedLine(corners[2], corners[3], 10f);
                Handles.DrawDottedLine(corners[3], corners[0], 10f);
                Handles.DrawDottedLine(pivot, pivot + t.offset, 10f);

                Handles.DrawWireDisc(pivot + t.offset, Vector3.forward, 9f);
            }
            // Handles.EndGUI();
        }
    }
#endif
}
