using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 디버그 타입에 따라 로그를 필터링하고 별도 런타임 디버그 콘솔로 전달하는 공용 유틸 클래스이다.
/// </summary>
public static class DebugTool
{
    public static void Log(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(DebugLogLevel.Log, text, type, context, memberName, filePath, lineNumber);
    }

    public static void Warning(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(DebugLogLevel.Warning, text, type, context, memberName, filePath, lineNumber);
    }

    // 기존 오타 함수명 호환
    public static void Warnning(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(DebugLogLevel.Warning, text, type, context, memberName, filePath, lineNumber);
    }

    public static void Error(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(DebugLogLevel.Error, text, type, context, memberName, filePath, lineNumber);
    }

    public static void MissingComponent(
        string text = null,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string message = string.IsNullOrEmpty(text)
            ? "컴포넌트를 찾을 수 없습니다."
            : $"{text}을(를) 찾을 수 없습니다.";

        Write(DebugLogLevel.Warning, message, DebugType.Missing, context, memberName, filePath, lineNumber);
    }

    public static void DebugPrintAll(bool value)
    {
        if (DebugConsoleManager.Instance == null)
            return;

        DebugConsoleManager.Instance.GlobalEnabled = value;
    }

    public static void DebugSelect(DebugType type, bool value)
    {
        if (DebugConsoleManager.Instance == null)
            return;

        DebugConsoleManager.Instance.SetTypeEnabled(type, value);
    }

    private static void Write(
        DebugLogLevel level,
        string text,
        DebugType type,
        Object context,
        string memberName,
        string filePath,
        int lineNumber)
    {
        DebugConsoleManager manager = DebugConsoleManager.Instance;

        if (manager != null && !manager.IsAllowed(type, context))
            return;

        DebugEntry entry = DebugEntryFactory.Create(level, text, type, context, memberName, filePath, lineNumber);

        if (manager != null)
        {
            manager.AddEntry(entry);

            if (manager.MirrorToUnityConsole)
                PrintToUnityConsole(entry);
        }
        else
        {
            PrintToUnityConsole(entry);
        }
    }

    private static void PrintToUnityConsole(DebugEntry entry)
    {
        switch (entry.Level)
        {
            case DebugLogLevel.Warning:
                Debug.LogWarning(entry.RichText, entry.Context);
                break;

            case DebugLogLevel.Error:
                Debug.LogError(entry.RichText, entry.Context);
                break;

            default:
                Debug.Log(entry.RichText, entry.Context);
                break;
        }
    }
}

public enum DebugType
{
    Game = 0,
    Unit = 1,
    Synergy = 2,
    Summon = 3,
    Combine = 4,
    Wave = 5,
    Board = 6,
    Enemy = 7,
    UI = 8,
    Data = 9,
    Merge = 10,
    Reforge = 11,
    Catalog = 12,
    Missing = 13,
    Default = 14
}
