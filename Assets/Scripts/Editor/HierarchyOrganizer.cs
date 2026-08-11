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

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == rocksParent ||
                go == coinsParent ||
                go == enemiesParent ||
                go == wormsParent)
                continue;

            RenameObject(go);

            if (go.name.StartsWith("Rock"))
            {
                go.transform.SetParent(rocksParent.transform);
            }
            else if (go.name.StartsWith("Coin") || go.name.StartsWith("ShinyCoin"))
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

        UpdateFolderName(rocksParent, "Rocks");
        UpdateFolderName(coinsParent, "Coins");
        UpdateFolderName(enemiesParent, "Enemies");
        UpdateFolderName(wormsParent, "Worms");

        Debug.Log("Hiérarchie réorganisée.");
    }

    static GameObject GetOrCreateParent(string baseName)
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.transform.parent != null)
                continue;

            string cleanName = go.name;

            int index = cleanName.IndexOf(" (");
            if (index >= 0)
                cleanName = cleanName.Substring(0, index);

            if (cleanName == baseName)
                return go;
        }

        return new GameObject(baseName);
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

    static void RenameObject(GameObject go)
    {
        if (go.name.StartsWith("Rock"))
        {
            go.name = "Rock";
        }
        else if (go.name.StartsWith("Coin"))
        {
            go.name = "Coin";
        }
        else if (go.name.StartsWith("Butterfly"))
        {
            go.name = "Butterfly";
        }
        else if (go.name.StartsWith("Firefly"))
        {
            go.name = "Firefly";
        }
    }

    static void UpdateFolderName(GameObject folder, string baseName)
    {
        int count = folder.transform.childCount;
        folder.name = $"{baseName} ({count})";
    }
}