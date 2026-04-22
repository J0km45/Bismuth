using System.Text;
using UnityEngine;

public static class DebugTargetKeyBuilder
{
    public static string BuildSceneKey(GameObject go)
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

    public static string BuildHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder(BuildPathSegment(transform));
        Transform current = transform.parent;

        while (current != null)
        {
            builder.Insert(0, '/');
            builder.Insert(0, BuildPathSegment(current));
            current = current.parent;
        }

        return builder.ToString();
    }

    public static string BuildGameObjectKey(GameObject go)
    {
        if (go == null)
            return string.Empty;

        return $"{BuildSceneKey(go)}|{BuildHierarchyPath(go.transform)}";
    }

    public static string BuildComponentKey(Component component)
    {
        if (component == null)
            return string.Empty;

        int sameTypeIndex = GetSameTypeIndex(component);
        return $"{BuildGameObjectKey(component.gameObject)}|{component.GetType().FullName}#{sameTypeIndex}";
    }

    private static string BuildPathSegment(Transform transform)
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
