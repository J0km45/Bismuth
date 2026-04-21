using UnityEngine;

public static class DebugConsoleFilterKeyUtility
{
    public static string GetGameObjectKey(GameObject go)
    {
        return DebugTargetKeyBuilder.BuildGameObjectKey(go);
    }

    public static string GetComponentKey(Component component)
    {
        return DebugTargetKeyBuilder.BuildComponentKey(component);
    }
}