#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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
                "DebugConsoleEditorWindow 타입을 찾지 못했습니다.\n클래스명, 네임스페이스, 컴파일 오류 여부를 확인해주세요.",
                "확인");
            return;
        }

        EditorWindow window = EditorWindow.GetWindow(windowType, false, WindowTitle, true);
        window.minSize = new Vector2(900f, 520f);
        window.Show();
        window.Focus();
    }

    private static Type FindDebugConsoleEditorWindowType()
    {
        const string targetTypeName = "DebugConsoleEditorWindow";

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }
            catch
            {
                continue;
            }

            Type match = types.FirstOrDefault(t =>
                t != null &&
                t.Name == targetTypeName &&
                typeof(EditorWindow).IsAssignableFrom(t));

            if (match != null)
                return match;
        }

        return null;
    }
}
#endif