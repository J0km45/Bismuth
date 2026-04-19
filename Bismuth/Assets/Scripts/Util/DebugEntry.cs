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

    public string PlainText =>
        $"[{Time}] [{Type}] {Message}\n출처 : [{SourceName}.{MemberName} : {LineNumber}]";

    public string RichText =>
        $"<color={ColorHex}>[{Time}] [{Type}] {Message}</color>\n" +
        $"<color=#daa520>출처 : [{SourceName}.{MemberName} : {LineNumber}]</color>";
}
