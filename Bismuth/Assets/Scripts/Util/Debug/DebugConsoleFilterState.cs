using System;

[Serializable]
public sealed class DebugConsoleFilterState
{
    public bool AutoScroll = true;
    public bool HideTransform = true;
    public bool CollapsePreviousOnSelection = true;
    public bool ShowTypeFilterPanel;
    public bool GlobalEnabled = true;
    public bool MirrorToUnityConsole;
}
