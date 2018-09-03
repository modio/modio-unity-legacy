using System.Collections.Generic;

[System.Serializable]
public class SimpleModTag
{
    public string category;
    public string name;

    public static IEnumerable<string> EnumerateNames(IEnumerable<SimpleModTag> tags)
    {
        foreach(SimpleModTag tag in tags)
        {
            yield return tag.name;
        }
    }
}
