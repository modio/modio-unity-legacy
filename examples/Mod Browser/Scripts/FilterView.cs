using UnityEngine;
using ModIO;

public class FilterView : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public GameObject tagCategoryPrefab;

    [Header("UI Components")]
    public RectTransform categoryContainer;

    [Header("Display Data")]
    public ModTagCategory[] tagCategories;
    public string[] selectedTags;

    [Header("Runtime Data")]
    public ModTagCategoryDisplay[] categoryDisplayComponents;

    public void Initialize()
    {
        // asserts
        Debug.Assert(tagCategoryPrefab != null);
        Debug.Assert(tagCategoryPrefab.GetComponent<ModTagCategoryDisplay>() != null);
        Debug.Assert(categoryContainer != null);

        // clear existing
        if(categoryDisplayComponents != null
           && categoryDisplayComponents.Length > 0)
        {
            foreach(ModTagCategoryDisplay cat in categoryDisplayComponents)
            {
                // TODO(@jackson): Remove listener
                GameObject.Destroy(cat.gameObject);
            }
        }

        // setup categories
        categoryDisplayComponents = new ModTagCategoryDisplay[tagCategories.Length];
        for(int i = 0; i < tagCategories.Length; ++i)
        {
            ModTagCategory category = tagCategories[i];
            GameObject categoryGO = GameObject.Instantiate(tagCategoryPrefab,
                                                            new Vector3(),
                                                            Quaternion.identity,
                                                            categoryContainer);
            categoryGO.name = category.name;

            ModTagCategoryDisplay categoryDisp = categoryGO.GetComponent<ModTagCategoryDisplay>();
            categoryDisp.modTagCategory = category;
            categoryDisp.selectedTags = this.selectedTags;
            // TODO(@jackson): Add Listener
            categoryDisp.Initialize();
        }
    }

    public void UpdateTagSelection()
    {

    }
}
