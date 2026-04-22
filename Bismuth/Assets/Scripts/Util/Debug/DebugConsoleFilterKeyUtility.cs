using UnityEngine;

public static class DebugConsoleFilterKeyUtility
{
    public static string GetSceneKey(GameObject gameObject)
    {
        return DebugTargetKeyBuilder.BuildSceneKey(gameObject);
    }

    public static string GetHierarchyPath(Transform target)
    {
        return DebugTargetKeyBuilder.BuildHierarchyPath(target);
    }

    public static string GetGameObjectKey(GameObject gameObject)
    {
        return DebugTargetKeyBuilder.BuildGameObjectKey(gameObject);
    }

    public static string GetComponentKey(Component component)
    {
        return DebugTargetKeyBuilder.BuildComponentKey(component);
    }
}