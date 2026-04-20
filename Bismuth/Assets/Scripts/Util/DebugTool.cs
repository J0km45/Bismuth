using System.IO;
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

    // 기존 코드 호환용
    // 커스텀 윈도우에서 전역 On/Off를 관리하므로 되도록 코드에서는 호출하지 않는 것을 권장한다.
    public static void DebugPrintAll(bool value)
    {
        if (DebugConsoleManager.Instance == null)
            return;

        DebugConsoleManager.Instance.GlobalEnabled = value;
    }

    // 기존 코드 호환용
    // 커스텀 윈도우에서 타입 On/Off를 관리하므로 되도록 코드에서는 호출하지 않는 것을 권장한다.
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
        GetTargetIds(context, out int gameObjectId, out int componentId);

        DebugConsoleManager manager = DebugConsoleManager.Instance;

        if (manager != null && !manager.IsAllowed(type, gameObjectId, componentId))
            return;

        string color = GetColor(type);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string sourceName = GetSourceName(context, fileName);

        if (memberName == ".ctor")
            memberName = "생성자";

        DebugEntry entry = new DebugEntry
        {
            Time = System.DateTime.Now.ToString("HH:mm:ss"),
            Message = text,
            SourceName = sourceName,
            MemberName = memberName,
            LineNumber = lineNumber,
            Type = type,
            Level = level,
            Context = context,
            GameObjectId = gameObjectId,
            ComponentId = componentId,
            ColorHex = color,
            CallerFilePath = filePath,
            CallerColumn = 1
        };

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

    private static void GetTargetIds(Object context, out int gameObjectId, out int componentId)
    {
        gameObjectId = 0;
        componentId = 0;

        if (context is GameObject go)
        {
            gameObjectId = go.GetInstanceID();
            return;
        }

        if (context is Component component)
        {
            gameObjectId = component.gameObject.GetInstanceID();
            componentId = component.GetInstanceID();
        }
    }

    private static string GetSourceName(Object context, string fallbackFileName)
    {
        if (context == null)
            return fallbackFileName;

        if (context is Component component)
            return $"{component.gameObject.name}/{component.GetType().Name}";

        if (context is GameObject go)
            return go.name;

        return context.name;
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

    private static string GetColor(DebugType type)
    {
        switch (type)
        {
            case DebugType.Game: return "#c6a1fa";
            case DebugType.Unit: return "#d9c61c";
            case DebugType.Synergy: return "#f0847f";
            case DebugType.Summon: return "#5eaad9";
            case DebugType.Combine: return "#F45911";
            case DebugType.Wave: return "#c53d34";
            case DebugType.Board: return "#bdd3b5";
            case DebugType.Enemy: return "#19cd48";
            case DebugType.UI: return "#b15b8b";
            case DebugType.Data: return "#e4ada4";
            case DebugType.Merge: return "#0eb6a6";
            case DebugType.Reforge: return "#A35ED3";
            case DebugType.Catalog: return "#D6EA15";
            case DebugType.Missing: return "#ffff00";
            case DebugType.Default: return "#251f59";
            default: return "#ffffff";
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
