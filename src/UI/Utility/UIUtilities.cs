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

        /// <summary>Finds the first instance of a component in any loaded scenes.</summary>
        public static T FindComponentInAllScenes<T>(bool includeInactive)
        where T : Behaviour
        {
            foreach(T component in Resources.FindObjectsOfTypeAll<T>())
            {
                if(component.hideFlags == HideFlags.NotEditable
                    || component.hideFlags == HideFlags.HideAndDontSave)
                {
                    continue;
                }

                #if UNITY_EDITOR
                if(UnityEditor.EditorUtility.IsPersistent(component.transform.root.gameObject))
                {
                    continue;
                }
                #endif

                if(includeInactive
                   || component.isActiveAndEnabled)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>Finds the instances of a component in any loaded scenes.</summary>
        public static List<T> FindComponentsInAllScenes<T>(bool includeInactive)
        where T : Behaviour
        {

            List<T> sceneComponents = new List<T>();

            foreach(T component in Resources.FindObjectsOfTypeAll<T>())
            {
                if(component.hideFlags == HideFlags.NotEditable
                    || component.hideFlags == HideFlags.HideAndDontSave)
                {
                    continue;
                }

                #if UNITY_EDITOR
                if(UnityEditor.EditorUtility.IsPersistent(component.transform.root.gameObject))
                {
                    continue;
                }
                #endif

                if(includeInactive
                   || component.isActiveAndEnabled)
                {
                    sceneComponents.Add(component);
                }
            }

            return sceneComponents;
        }

        // NOTE(@jackson): Currently implemented with H-inc/decrement, V-align
        /// <summary>Explicitly links a collection of selectable components as a grid (or list).</summary>
        public static void SetExplicitGridNavigation(IList<Selectable> selectables, int columnCount,
                                                     bool wrapHorizontally, bool wrapVertically)
        {
            Debug.Assert(selectables != null);

            if(selectables == null || selectables.Count == 0) { return; }

            // check for single item
            if(selectables.Count == 1)
            {
                if(selectables[0] == null) { return; }

                Selectable s = selectables[0];
                Selectable hLink = (wrapHorizontally ? s : null);
                Selectable vLink = (wrapVertically ? s : null);

                Navigation nav = new Navigation()
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = hLink,
                    selectOnRight = hLink,
                    selectOnUp = vLink,
                    selectOnDown = vLink,
                };

                s.navigation = nav;

                return;
            }

            // assert valid columnCount
            if(columnCount < 1) { columnCount = 1; }
            if(columnCount > selectables.Count) { columnCount = selectables.Count; }

            // as int-division rounds toward zero, this ensures rounding-up.
            int rowCount = (selectables.Count + columnCount -1) / columnCount;

            // set grid index formula
            Func<int, int> getCol = (gridIndex) => gridIndex % columnCount;
            Func<int, int> getRow = (gridIndex) => gridIndex / columnCount;
            Func<int, int, int> getGridIndex = (col, row) => row * columnCount + col;

            // -- set the nav on the first and last items --
            Selectable firstItem = selectables[0];
            Selectable lastItem = selectables[selectables.Count-1];

            if(firstItem != null)
            {
                Navigation firstNav = new Navigation()
                {
                    mode = Navigation.Mode.Explicit,
                };

                // left
                if(wrapHorizontally)
                {
                    firstNav.selectOnLeft = lastItem;
                }
                else
                {
                    firstNav.selectOnLeft = null;
                }

                // right
                if(columnCount > 1 || wrapHorizontally)
                {
                    firstNav.selectOnRight = selectables[1];
                }
                else
                {
                    firstNav.selectOnRight = null;
                }

                // up
                if(wrapVertically)
                {
                    firstNav.selectOnUp = selectables[getGridIndex(0, rowCount-1)];
                }
                else
                {
                    firstNav.selectOnUp = null;
                }

                // down
                if(rowCount > 1)
                {
                    firstNav.selectOnDown = selectables[getGridIndex(0, 1)];
                }
                else if(wrapVertically)
                {
                    firstNav.selectOnDown = firstItem;
                }
                else
                {
                    firstNav.selectOnDown = null;
                }

                firstItem.navigation = firstNav;
            }

            if(lastItem != null)
            {
                int col = getCol(selectables.Count-1);
                int row = rowCount-1;

                Navigation lastNav = new Navigation()
                {
                    mode = Navigation.Mode.Explicit,
                };

                // left
                if(columnCount > 1 || wrapHorizontally)
                {
                    lastNav.selectOnLeft = selectables[selectables.Count-2];
                }
                else
                {
                    lastNav.selectOnLeft = null;
                }

                // right
                if(wrapHorizontally)
                {
                    lastNav.selectOnRight = firstItem;
                }
                else
                {
                    lastNav.selectOnRight = null;
                }

                // up
                if(rowCount > 0)
                {
                    lastNav.selectOnUp = selectables[getGridIndex(col, row-1)];
                }
                else if(wrapVertically)
                {
                    lastNav.selectOnUp = lastItem;
                }
                else
                {
                    lastNav.selectOnUp = null;
                }

                // down
                if(wrapVertically)
                {
                    lastNav.selectOnDown = selectables[getGridIndex(col, 0)];
                }
                else
                {
                    lastNav.selectOnDown = null;
                }

                lastItem.navigation = lastNav;
            }

            // do linkage
            for(int gridIndex = 1; gridIndex < selectables.Count-1; ++gridIndex)
            {
                Selectable currentItem = selectables[gridIndex];
                Navigation nav = new Navigation()
                {
                    mode = Navigation.Mode.Explicit,
                };

                int col = getCol(gridIndex);
                int row = getRow(gridIndex);

                // left
                if(col > 0 || wrapHorizontally)
                {
                    nav.selectOnLeft = selectables[gridIndex-1];
                }
                else
                {
                    nav.selectOnLeft = null;
                }

                // right
                if(col < columnCount-1 || wrapHorizontally)
                {
                    nav.selectOnRight = selectables[gridIndex+1];
                }
                else
                {
                    nav.selectOnRight = null;
                }

                // up
                if(row > 0)
                {
                    nav.selectOnUp = selectables[getGridIndex(col, row-1)];
                }
                else if(wrapVertically)
                {
                    int rowIndex = rowCount-1;
                    while(getGridIndex(col, rowIndex) >= selectables.Count)
                    {
                        --rowIndex;
                    }

                    nav.selectOnUp = selectables[getGridIndex(col, rowIndex)];
                }
                else
                {
                    nav.selectOnUp = null;
                }

                // down
                if(row < rowCount-1
                   && getGridIndex(col, row+1) < selectables.Count)
                {
                    nav.selectOnDown = selectables[getGridIndex(col, row+1)];
                }
                else if(wrapVertically)
                {
                    nav.selectOnDown = selectables[getGridIndex(col, 0)];
                }
                else
                {
                    nav.selectOnDown = null;
                }

                currentItem.navigation = nav;
            }
        }

        // ---------[ OBSOLETE ]---------
        /// <summary>[Obsolete] Finds the first instance of a component in the active scene.</summary>
        [Obsolete("Use UIUtilities.FindComponentInAllScenes() instead.")]
        public static T FindComponentInScene<T>(bool includeInactive)
        where T : class
        {
            /*
             * JC (2019-09-07): UIs are sometimes managed in their own scenes
             * (e.g. one scene per UI panel/screen), and those scenes will usually
             * not be the active scenes. For the purpose of resolving a singleton
             * instance, Resources.FindObjectsOfTypeAll<T>() is probably the safer
             * approach (Object.FindObjectOfType(type) would be more efficient but
             * cannot return inactive objects.
             */
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

        /// <summary>[Obsolete] Finds components within the active scene.</summary>
        [Obsolete("Use UIUtilities.FindComponentsInLoadedScenes() instead.")]
        public static List<T> FindComponentsInScene<T>(bool includeInactive)
        where T : class
        {
            // JC (2019-09-07): See comment above (FindComponentInScene).
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
