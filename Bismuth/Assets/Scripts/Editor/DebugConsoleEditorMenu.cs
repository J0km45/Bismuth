#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

public static class DebugConsoleEditorMenu
{
    private const string WindowTitle = "Debug Console";

    [MenuItem("Debug/Debug Console", priority = 1000)]
    [MenuItem("Tools/Debug/Debug Console", priority = 1000)]
    private static void OpenDebugConsole()
    {
        Type windowType = FindDebugConsoleEditorWindowType();
        if (windowType == null)
        {
            EditorUtility.DisplayDialog(
                WindowTitle,
                "DebugConsoleEditorWindow 타입을 찾지 못했습니다. 클래스명 또는 네임스페이스가 바뀌었는지 확인해주세요.",
                "확인");
            return;
        }

        EditorWindow window = EditorWindow.GetWindow(windowType, false, WindowTitle, true);
        window.minSize = new UnityEngine.Vector2(900f, 520f);
        window.Show();
        window.Focus();
    }

    private static Type FindDebugConsoleEditorWindowType()
    {
        const string targetTypeName = "DebugConsoleEditorWindow";

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type matchedType = assembly
                .GetTypes()
                .FirstOrDefault(type =>
                    typeof(EditorWindow).IsAssignableFrom(type) &&
                    string.Equals(type.Name, targetTypeName, StringComparison.Ordinal));

            if (matchedType != null)
                return matchedType;
        }

        return null;
    }
}
#endif
