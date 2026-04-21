using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DebugEntryFactory
{
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
