using UnityEngine;

public static class DebugConsoleVirtualizationService
{
    public static float GetMaxScrollY(float contentHeight, float viewportHeight)
    {
        return Mathf.Max(0f, contentHeight - viewportHeight);
    }

    public static bool ShouldAutoScroll(bool autoScroll)
    {
        return autoScroll;
    }
}
