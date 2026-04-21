using System;
using UnityEngine;

[Serializable]
public sealed class DebugConsoleLayoutState
{
    public Vector2 HierarchyScroll;
    public Vector2 LogScroll;
    public Vector2 TypeFilterScroll;
    public Rect WindowRect = DebugConsoleConstants.DefaultRuntimeWindowRect;
    public float HierarchyPanelWidth = DebugConsoleConstants.DefaultEditorHierarchyPanelWidth;
}
