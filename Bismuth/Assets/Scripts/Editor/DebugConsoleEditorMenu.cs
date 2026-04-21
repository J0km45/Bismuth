#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DebugConsoleEditorMenu
{
    private const string WindowTitle = "Debug Console";

    [MenuItem("Debug/Debug Console", priority = 1000)]
    [MenuItem("Tools/Debug/Debug Console", priority = 1000)]
    private static void OpenDebugConsole()
    {
        DebugConsoleEditorWindow window = EditorWindow.GetWindow<DebugConsoleEditorWindow>(false, WindowTitle, true);
        window.minSize = new Vector2(900f, 520f);
        window.Show();
        window.Focus();
    }
}
#endif