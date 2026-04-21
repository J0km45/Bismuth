using UnityEngine;

public static class DebugConsoleConstants
{
    public const int MaxDisplayNameLength = 15;
    public const int FooterFocusSegmentMaxLength = 16;

    public const float HierarchyRowHeight = 22f;
    public const float HierarchyToggleSize = 18f;
    public const float HierarchyFoldoutSize = 18f;
    public const float PanelSplitterWidth = 6f;
    public const float MaxHierarchyIndentPenalty = 24f;
    public const float MinHierarchyPanelWidth = 220f;
    public const float MinLogPanelWidth = 220f;
    public const float HierarchyRowContentRightReserve = 18f;

    public const float DefaultEditorHierarchyPanelWidth = 420f;
    public const float DefaultRuntimeHierarchyPanelWidth = 480f;
    public const float DefaultRuntimeWindowX = 20f;
    public const float DefaultRuntimeWindowY = 20f;
    public const float DefaultRuntimeWindowWidth = 1450f;
    public const float DefaultRuntimeWindowHeight = 850f;

    public static readonly Rect DefaultRuntimeWindowRect = new Rect(
        DefaultRuntimeWindowX,
        DefaultRuntimeWindowY,
        DefaultRuntimeWindowWidth,
        DefaultRuntimeWindowHeight);
}
