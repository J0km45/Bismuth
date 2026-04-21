using System.Text;
using UnityEngine;

public static class DebugConsoleFilterKeyUtility
{
    public static string GetGameObjectKey(GameObject go)
    {
        if (go == null)
            return string.Empty;

        return $"{GetSceneKey(go)}|{GetHierarchyPath(go.transform)}";
    }

    public static string GetComponentKey(Component component)
    {
        if (component == null)
            return string.Empty;

        int sameTypeIndex = GetSameTypeIndex(component);
        return $"{GetGameObjectKey(component.gameObject)}|{component.GetType().FullName}#{sameTypeIndex}";
    }

    public static string GetSceneKey(GameObject go)
    {
        if (go == null)
            return "[NullScene]";

        if (!go.scene.IsValid())
            return "[InvalidScene]";

        if (!string.IsNullOrWhiteSpace(go.scene.path))
            return go.scene.path;

        if (!string.IsNullOrWhiteSpace(go.scene.name))
            return go.scene.name;

        return "[UnnamedScene]";
    }

    public static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder(GetPathSegment(transform));
        Transform current = transform.parent;

        while (current != null)
        {
            builder.Insert(0, '/');
            builder.Insert(0, GetPathSegment(current));
            current = current.parent;
        }

        return builder.ToString();
    }

    private static string GetPathSegment(Transform transform)
    {
        return $"{transform.name}[{transform.GetSiblingIndex()}]";
    }

    private static int GetSameTypeIndex(Component component)
    {
        Component[] components = component.GetComponents<Component>();
        int sameTypeIndex = 0;

        for (int i = 0; i < components.Length; i++)
        {
            Component current = components[i];
            if (current == null)
                continue;

            if (current.GetType() != component.GetType())
                continue;

            if (current == component)
                return sameTypeIndex;

            sameTypeIndex++;
        }

        return sameTypeIndex;
    }
}
