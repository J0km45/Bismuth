using System;
using UnityEngine;
using Object = UnityEngine.Object;

public enum DebugLogLevel
{
    Log,
    Warning,
    Error
}

public sealed class DebugEntry
{
    public string Time;
    public string Message;
    public string SourceName;
    public string MemberName;
    public int LineNumber;
    public DebugType Type;
    public DebugLogLevel Level;
    public Object Context;
    public int GameObjectId;
    public int ComponentId;
    public string ColorHex;
    public string CallerFilePath;
    public int CallerColumn = 1;
    public string StackTrace;

    public long SequenceId;
    public int FrameCount;
    public string CapturedAtIsoUtc;
    public string SceneKey;
    public string HierarchyPath;
    public string GameObjectKey;
    public string ComponentKey;
    public string GameObjectName;
    public string ComponentName;
    public string ComponentTypeName;

    public int RepeatCount = 1;
    public string CollapseKey;
    public string SearchableText;
    public string PlainTextCached;
    public string RichTextCached;
    public string SummaryTextCached;
    public string SummaryRichTextCached;

    public string PlainText => string.IsNullOrEmpty(PlainTextCached) ? BuildPlainText() : PlainTextCached;
    public string RichText => string.IsNullOrEmpty(RichTextCached) ? BuildRichText() : RichTextCached;
    public string SummaryText => string.IsNullOrEmpty(SummaryTextCached) ? BuildSummaryText() : SummaryTextCached;
    public string SummaryRichText => string.IsNullOrEmpty(SummaryRichTextCached) ? BuildSummaryRichText() : SummaryRichTextCached;

    public void RefreshDerivedFields()
    {
        RepeatCount = Mathf.Max(1, RepeatCount);
        CollapseKey = BuildCollapseKey();
        SearchableText = BuildSearchableText();
        PlainTextCached = BuildPlainText();
        RichTextCached = BuildRichText();
        SummaryTextCached = BuildSummaryText();
        SummaryRichTextCached = BuildSummaryRichText();
    }

    private string BuildPlainText()
    {
        return $"[{Time}] [{Type}] {Message}\n출처 : [{SourceName}.{MemberName} : {LineNumber}]";
    }

    private string BuildRichText()
    {
        return $"<color={ColorHex}>[{Time}] [{Type}] {Message}</color>\n" +
               $"<color=#daa520>출처 : [{SourceName}.{MemberName} : {LineNumber}]</color>";
    }

    private string BuildSummaryText()
    {
        string sourceText = $"[{SourceName}.{MemberName} : {LineNumber}]";
        return $"[{Time}] [{Type}] {Message} | {sourceText}";
    }

    private string BuildSummaryRichText()
    {
        string sourceText = $"[{SourceName}.{MemberName} : {LineNumber}]";
        return $"<color={ColorHex}>[{Time}] [{Type}] {Message}</color> " +
               $"<color=#daa520>| {sourceText}</color>";
    }

    private string BuildSearchableText()
    {
        return string.Join(" ",
            Time ?? string.Empty,
            Message ?? string.Empty,
            SourceName ?? string.Empty,
            MemberName ?? string.Empty,
            CallerFilePath ?? string.Empty,
            SceneKey ?? string.Empty,
            HierarchyPath ?? string.Empty,
            GameObjectName ?? string.Empty,
            ComponentName ?? string.Empty,
            ComponentTypeName ?? string.Empty,
            Type.ToString(),
            Level.ToString());
    }

    private string BuildCollapseKey()
    {
        return string.Join("|",
            Message ?? string.Empty,
            SourceName ?? string.Empty,
            MemberName ?? string.Empty,
            ColorHex ?? string.Empty,
            CallerFilePath ?? string.Empty,
            LineNumber.ToString(),
            CallerColumn.ToString(),
            Type.ToString(),
            Level.ToString(),
            GameObjectKey ?? GameObjectId.ToString(),
            ComponentKey ?? ComponentId.ToString());
    }
}
