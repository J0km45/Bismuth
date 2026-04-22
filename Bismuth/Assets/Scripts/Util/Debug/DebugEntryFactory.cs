// ------------------------------------------------------------------------------
// 로그 문자열과 컨텍스트를 받아 DebugEntry를 만들고 파생 정보를 채우는 팩토리 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// DebugEntry 생성과 초기 파생값 계산을 담당하는 정적 팩토리 클래스이다.
/// </summary>
public static class DebugEntryFactory
{
    /// <summary>
    /// 관련 작업 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
    public static DebugEntry Create(
        DebugLogLevel level,
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        GetTargetIds(context, out int gameObjectId, out int componentId);

        string color = GetColor(type);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string sourceName = GetSourceName(context, fileName);

        if (memberName == ".ctor")
            memberName = "생성자";

        return new DebugEntry
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
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
    }

    /// <summary>
    /// 대상 ids 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
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

    /// <summary>
    /// 출처 이름 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
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

    /// <summary>
    /// 색상 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
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
