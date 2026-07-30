using UnityEditor;
using UnityEngine;

public static class HierarchyOrganizer
{
    [MenuItem("Tools/Organize Hierarchy")]
    static void OrganizeHierarchy()
    {
        GameObject rocksParent = GetOrCreateParent("Rocks");
        GameObject coinsParent = GetOrCreateParent("Coins");
        GameObject enemiesParent = GetOrCreateParent("Enemies");
        GameObject wormsParent = GetOrCreateParent("Worms");
        GameObject playerParent = GetOrCreateParent("Player");

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            // Ignore les objets cachés ou les parents eux-mêmes
            if (go.transform.parent != null)
                continue;

            if (go == rocksParent ||
                go == coinsParent ||
                go == enemiesParent ||
                go == wormsParent ||
                go == playerParent)
                continue;

            if (go.name.StartsWith("Rock"))
            {
                go.transform.SetParent(rocksParent.transform);
            }
            else if (go.name.StartsWith("Coin"))
            {
                go.transform.SetParent(coinsParent.transform);
            }
            else if (go.name.StartsWith("Butterfly") || go.name.StartsWith("Firefly"))
            {
                go.transform.SetParent(enemiesParent.transform);
            }
            else if (go.name.StartsWith("GoodWormSpawn") || go.name.StartsWith("EvilWormSpawn"))
            {
                go.transform.SetParent(wormsParent.transform);
            }
        }

        SortChildrenAlphabetically(rocksParent);
        SortChildrenAlphabetically(coinsParent);
        SortChildrenAlphabetically(enemiesParent);
        SortChildrenAlphabetically(wormsParent);
        SortChildrenAlphabetically(playerParent);

        Debug.Log("Hiérarchie réorganisée.");
    }

    static GameObject GetOrCreateParent(string parentName)
    {
        GameObject parent = GameObject.Find(parentName);

        if (parent == null)
            parent = new GameObject(parentName);

        return parent;
    }

    static void SortChildrenAlphabetically(GameObject parent)
    {
        Transform[] children = new Transform[parent.transform.childCount];

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            children[i] = parent.transform.GetChild(i);
        }

        System.Array.Sort(children, (a, b) =>
            string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

        for (int i = 0; i < children.Length; i++)
        {
            children[i].SetSiblingIndex(i);
        }
    }
}