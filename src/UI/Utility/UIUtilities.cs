using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public static class UIUtilities
    {
        public static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            return Sprite.Create(texture,
                                 new Rect(0.0f, 0.0f, texture.width, texture.height),
                                 Vector2.zero);
        }

        public static void OpenYouTubeVideoURL(string youTubeVideoId)
        {
            if(!String.IsNullOrEmpty(youTubeVideoId))
            {
                Application.OpenURL(@"https://youtu.be/" + youTubeVideoId);
            }
        }

        /// <summary>Counts the cells that will fit in within the RectTransform of the given grid</summary>
        public static int CountVisibleGridCells(GridLayoutGroup gridLayout)
        {
            Debug.Assert(gridLayout != null);

            // calculate dimensions
            RectTransform transform = gridLayout.GetComponent<RectTransform>();
            Vector2 gridDisplayDimensions = new Vector2();
            gridDisplayDimensions.x = (transform.rect.width
                                       - gridLayout.padding.left
                                       - gridLayout.padding.right
                                       + gridLayout.spacing.x);
            gridDisplayDimensions.y = (transform.rect.height
                                       - gridLayout.padding.top
                                       - gridLayout.padding.bottom
                                       + gridLayout.spacing.y);

            // calculate cell count
            int columnCount = 0;
            if(gridLayout.cellSize.x + gridLayout.spacing.x > 0f)
            {
                columnCount = (int)Mathf.Floor(gridDisplayDimensions.x
                                               / (gridLayout.cellSize.x + gridLayout.spacing.x));

            }
            int rowCount = 0;
            if((gridLayout.cellSize.y + gridLayout.spacing.y) > 0f)
            {
                rowCount = (int)Mathf.Floor(gridDisplayDimensions.y
                                            / (gridLayout.cellSize.y + gridLayout.spacing.y));
            }

            // check constraints
            if(gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                if(gridLayout.constraintCount < columnCount)
                {
                    columnCount = gridLayout.constraintCount;
                }
            }
            else if(gridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount)
            {
                if(gridLayout.constraintCount < rowCount)
                {
                    rowCount = gridLayout.constraintCount;
                }
            }

            return rowCount * columnCount;
        }


        /// <summary>Finds the first instance of a component in the active scene.</summary>
        public static T FindComponentInScene<T>(bool includeInactive)
        where T : class
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            IEnumerable<GameObject> rootObjects = activeScene.GetRootGameObjects();
            T foundComponent = null;

            foreach(var root in rootObjects)
            {
                if(includeInactive
                   || root.activeInHierarchy)
                {
                    foundComponent = root.GetComponent<T>();
                    if(foundComponent != null)
                    {
                        return foundComponent;
                    }

                    foundComponent = root.GetComponentInChildren<T>(includeInactive);
                    if(foundComponent != null)
                    {
                        return foundComponent;
                    }
                }
            }

            return null;
        }

        /// <summary>Finds components within the active scene.</summary>
        public static List<T> FindComponentsInScene<T>(bool includeInactive)
        where T : class
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            IEnumerable<GameObject> rootObjects = activeScene.GetRootGameObjects();
            List<T> retVal = new List<T>();

            foreach(var root in rootObjects)
            {
                if(includeInactive
                   || root.activeInHierarchy)
                {
                    retVal.AddRange(root.GetComponents<T>());
                    retVal.AddRange(root.GetComponentsInChildren<T>(includeInactive));
                }
            }

            return retVal;
        }

        /// <summary>Creates/Destroys a number of GameObject instances as necessary.</summary>
        public static void SetInstanceCount<T>(Transform container, T template,
                                               string instanceName, int instanceCount,
                                               ref T[] instanceArray, bool reactivateAll = false)
        where T : MonoBehaviour
        {
            if(instanceArray == null)
            {
                instanceArray = new T[0];
            }

            int difference = instanceCount - instanceArray.Length;

            if(difference != 0)
            {
                T[] newInstanceArray = new T[instanceCount];

                // copy existing
                for(int i = 0;
                    i < instanceArray.Length && i < instanceCount;
                    ++i)
                {
                    newInstanceArray[i] = instanceArray[i];
                }

                // create new
                for(int i = instanceArray.Length;
                    i < instanceCount;
                    ++i)
                {
                    GameObject displayGO = GameObject.Instantiate(template.gameObject);
                    displayGO.name = instanceName + " [" + i.ToString("00") + "]";
                    displayGO.transform.SetParent(container, false);
                    displayGO.SetActive(true);

                    newInstanceArray[i] = displayGO.GetComponent<T>();
                }

                // destroy excess
                for(int i = instanceCount;
                    i < instanceArray.Length;
                    ++i)
                {
                    GameObject.Destroy(instanceArray[i].gameObject);
                }

                // assign
                instanceArray = newInstanceArray;
            }

            // reactivate
            if(reactivateAll)
            {
                foreach(T instance in instanceArray)
                {
                    instance.gameObject.SetActive(false);
                    instance.gameObject.SetActive(true);
                }
            }
        }
    }
}
