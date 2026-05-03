using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 디버그 타입에 따라 로그를 필터링하고 색상 및 출처 정보를 함께 출력하는 공용 유틸 클래스이다.
/// </summary>
public static class DebugTool
{
    private static readonly bool[] DebugTypeSelect = new bool[System.Enum.GetValues(typeof(DebugType)).Length];
    private static bool _debugAllOn;

    // ----------------------------
    // Public API
    // ----------------------------

    public static void Log(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!CanPrint(type))
            return;

        Print(LogType.Log, text, type, context, memberName, filePath, lineNumber);
    }

    public static void Warning(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!CanPrint(type))
            return;

        Print(LogType.Warning, text, type, context, memberName, filePath, lineNumber);
    }

    // 기존 코드 호환용
    public static void Warnning(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Warning(text, type, context, memberName, filePath, lineNumber);
    }

    public static void Error(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!CanPrint(type))
            return;

        Print(LogType.Error, text, type, context, memberName, filePath, lineNumber);
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

        Warning(message, DebugType.Missing, context, memberName, filePath, lineNumber);
    }

    public static void DebugPrintAll(bool value)
    {
        _debugAllOn = value;

        for (int i = 0; i < DebugTypeSelect.Length; i++)
            DebugTypeSelect[i] = value;

        if (value)
        {
            Debug.Log("<color=#ffffff>[DebugTool] 모든 디버그 출력 활성화</color>");
        }
        else
        {
            Debug.Log("<color=#ffffff>[DebugTool] 모든 디버그 출력 비활성화</color>");
        }
    }

    public static void DebugSelect(DebugType type, bool value)
    {
        DebugTypeSelect[(int)type] = value;
    }

    // ----------------------------
    // Internal
    // ----------------------------

    private static bool CanPrint(DebugType type)
    {
        if (!_debugAllOn)
            return false;

        return DebugTypeSelect[(int)type];
    }

    private static void Print(
        LogType logType,
        string text,
        DebugType type,
        Object context,
        string memberName,
        string filePath,
        int lineNumber)
    {
        string color = GetColor(type);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string sourceName = context != null ? context.name : fileName;

        if (memberName == ".ctor")
            memberName = "생성자";

        string message =
            $"<color={color}>[{type}] {text}</color>\n" +
            $"<color=#daa520>출처 : [{sourceName}.{memberName} : {lineNumber}]</color>";

        switch (logType)
        {
            case LogType.Warning:
                Debug.LogWarning(message, context);
                break;

            case LogType.Error:
                Debug.LogError(message, context);
                break;

            default:
                Debug.Log(message, context);
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

    private enum LogType
    {
        Log,
        Warning,
        Error
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