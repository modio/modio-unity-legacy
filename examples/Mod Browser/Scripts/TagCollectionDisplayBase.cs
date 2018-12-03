using System.Collections.Generic;
using UnityEngine;
using ModIO;

public abstract class TagCollectionDisplayBase : MonoBehaviour
{
    public abstract void Initialize();

    public abstract void DisplayTags(IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories);
    public abstract void DisplayModTags(ModProfile profile, IEnumerable<ModTagCategory> tagCategories);
    public abstract void DisplayModTags(int modId, IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories);

    public abstract void DisplayLoading(int modId = -1);
}
